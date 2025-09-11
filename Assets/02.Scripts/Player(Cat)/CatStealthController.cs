using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class CatStealthController : MonoBehaviour
{
    public static CatStealthController Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;
    [SerializeField] private PlayerCatMovement movement;       // 외부 컨트롤러
    [SerializeField] private SpriteRenderer spriteRenderer;    // 방향 판단용
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float toggleCooldown = 1.5f;
    [SerializeField] private Animator animator;

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private HideObject currentHideObj;
    [SerializeField] private bool isHiding;
    [SerializeField] private float lastEnterTime;

    static readonly int H_IsGrounded = Animator.StringToHash("IsGrounded");
    static readonly int H_Speed = Animator.StringToHash("Speed");
    static readonly int H_Shift = Animator.StringToHash("Shift");
    static readonly int H_Climbing = Animator.StringToHash("Climbing");
    static readonly int H_Crouch = Animator.StringToHash("Crouch");
    static readonly int H_Crouching = Animator.StringToHash("Crouching");
    static readonly int H_Jump = Animator.StringToHash("Jump");
    static readonly int H_Moving = Animator.StringToHash("Moving");
    static readonly int H_Dash = Animator.StringToHash("Dash");

    // 근접 판정: 트리거 안의 HideObject 집합
    private readonly HashSet<HideObject> overlaps = new();

    // 상태
    private enum StealthState { Idle, Near, Entering, Hiding, Exiting }
    [SerializeField] private StealthState state = StealthState.Idle;
    private float nextToggleAllowedTime;

    // 전이 코루틴 핸들
    private Coroutine enterCo, exitCo;
    // 연출(이펙트) 코루틴 핸들(필요 시 여러 개 분리)
    private Coroutine enterFxCo, exitFxCo;

    // 이벤트 훅
    public event Action<HideObject> OnEnterArea;
    public event Action<HideObject> OnExitArea;
    public event Action<HideObject> OnHideStart;
    public event Action<HideObject> OnHideEnd;

    public bool IsHiding => isHiding;
    public bool IsInGrace => Time.time - lastEnterTime < (settings ? settings.graceSeconds : 0f);
    public bool IsNearHide => currentHideObj != null;
    // 최종 목적지인 벤치 밑에 숨었을 경우, ForceNotHiding true면 E키가 안 눌리게 함.
    private bool ForceNotHiding = false;
    public bool isPlaying = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (!movement) movement = PlayerCatMovement.Instance;
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();

        if (!settings)
        {
            Debug.LogWarning("[CatStealthController] StealthSettings가 지정되지 않았습니다. 기본값으로 동작합니다.");
            settings = ScriptableObject.CreateInstance<StealthSettingsSO>();
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        // E키 입력 시, Hide Object에 은신
        if (Input.GetKeyDown(settings.toggleKey) && !ForceNotHiding && Time.time >= nextToggleAllowedTime)
        {
            // 전이 중엔 무시 (중복 호출 방지)
            if (state == StealthState.Entering || state == StealthState.Exiting)
                return;

            if (state == StealthState.Hiding)
            {
                StartExit();
            }
            else if ((state == StealthState.Near || state == StealthState.Idle) && currentHideObj != null && !isHiding)
            {
                StartEnter(currentHideObj);
            }
        }

        // 상태 표시(디버그/에디터 확인용)
        if (state == StealthState.Idle && IsNearHide) state = StealthState.Near;
        else if (state == StealthState.Near && !IsNearHide) state = StealthState.Idle;
    }

    // 트리거 관리: HashSet으로 안정화
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag("HideObject")) return;
        if (other.TryGetComponent(out HideObject ho))
        {
            overlaps.Add(ho);
            UpdateCurrentHideObj();
            ho.SetEffect(true);
            if (isPlaying) OnEnterArea?.Invoke(ho);
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other || !other.CompareTag("HideObject")) return;
        if (other.TryGetComponent(out HideObject ho))
        {
            overlaps.Remove(ho);
            ho.SetEffect(false);
            if (isPlaying) OnExitArea?.Invoke(ho);
            UpdateCurrentHideObj();
        }
    }

    private void UpdateCurrentHideObj()
    {
        // 가장 가까운 HideObject 선택 (여러 개 겹칠 때 대비)
        HideObject nearest = null;
        float bestDist = float.MaxValue;
        foreach (var ho in overlaps)
        {
            float d = Mathf.Abs(transform.position.x - ho.AnchorX);
            if (d < bestDist) { bestDist = d; nearest = ho; }
        }
        currentHideObj = nearest;
    }

    // ===== Enter / Exit 진입점 =====
    public void StartEnter(HideObject ho)
    {
        if (ho == null || isHiding || state is StealthState.Entering or StealthState.Exiting) return;

        if (exitCo != null) { StopCoroutine(exitCo); exitCo = null; }
        if (exitFxCo != null) { StopCoroutine(exitFxCo); exitFxCo = null; }

        enterCo = StartCoroutine(EnterFlow(ho));
    }

    public void StartExit()
    {
        if (state != StealthState.Hiding) return;

        if (enterCo != null) { StopCoroutine(enterCo); enterCo = null; }
        if (enterFxCo != null) { StopCoroutine(enterFxCo); enterFxCo = null; }

        exitCo = StartCoroutine(ExitFlow(currentHideObj));
    }

    // ===== 코루틴: 단일 플로우 =====
    private IEnumerator EnterFlow(HideObject ho)
    {
        state = StealthState.Entering;

        // 바로 잠금 & 관성 제거
        movement.SetMiniGameInputBlocked(true);
        movement.ForceCrouch = true;

        movement.SetCrouchMovingState(true); // 내부 플래그
        ApplyCrouchAnim(true);               // 애니 파라미터 즉시 반영

        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 잠깐 키네마틱으로 전환해 물리 간섭 차단
        RigidbodyType2D prevBody = rb ? rb.bodyType : RigidbodyType2D.Dynamic;
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;

        // 버퍼(경계 떨림 방지)
        yield return new WaitForSeconds(settings.enterExitBuffer);

        if (ho == null || !overlaps.Contains(ho))
        {
            // 롤백
            if (rb) rb.bodyType = prevBody;
            movement.SetCrouchMovingState(false);
            movement.ForceCrouch = false;
            movement.SetMiniGameInputBlocked(false);
            ClearCrouchAnim();
            state = IsNearHide ? StealthState.Near : StealthState.Idle;
            yield break;
        }

        isHiding = true;

        ho.SetEffect(false);// 하이라이트 및 아이콘 끄기
        if (isPlaying&& !currentHideObj.isGoalObject) 
        {
            ho.SetHidingAlpha(true);    // 오브젝트 알파값 조절
            SetHidingAlpha(true);
        } 

        if (isPlaying) OnHideStart?.Invoke(ho);

        // 은신 오브젝트 안쪽으로 이동
        yield return MoveToX(ho.AnchorX);

        // 최종 스냅 (물리 간섭 없이 정확히 고정)
        if (rb) rb.position = new Vector2(ho.AnchorX, rb.position.y);
        else transform.position = new Vector3(ho.AnchorX, transform.position.y, transform.position.z);

        // 물리 복원
        if (rb) rb.bodyType = prevBody;

        // 도착 후 정지 crouch 포즈로 전환
        movement.SetCrouchMovingState(false);
        ApplyCrouchAnim(false);

        state = StealthState.Hiding;
        nextToggleAllowedTime = Time.time + toggleCooldown;
        enterCo = null;

        // 숨은 곳이 최종인 벤치 의자면 Hiding 처리 X
        if (currentHideObj.isGoalObject)
        {
            ForceNotHiding = true;
            isHiding = false;
            state = StealthState.Near;
            KidWatcher.Instance.FinalWatchLoop();
        }
    }

    private IEnumerator ExitFlow(HideObject ho)
    {
        state = StealthState.Exiting;

        // 버퍼
        yield return new WaitForSeconds(settings.enterExitBuffer);

        movement.SetCrouchMovingState(true);
        ApplyCrouchAnim(true);

        if (isPlaying && !currentHideObj.isGoalObject)
        {
            ho.SetHidingAlpha(false);    // 오브젝트 알파값 조절
            SetHidingAlpha(false);
        }
        
        // 바깥 X 계산
        float targetX = ComputeOutsideX(ho);
        yield return MoveToX(targetX);

        // 원복
        movement.SetMiniGameInputBlocked(false);
        movement.ForceCrouch = false;

        isHiding = false;
        OnHideEnd?.Invoke(ho);

        // 웅크림 애니메이션 해제
        ClearCrouchAnim();

        // 현재 근처 여부 갱신
        UpdateCurrentHideObj();
        state = IsNearHide ? StealthState.Near : StealthState.Idle;
        nextToggleAllowedTime = Time.time + toggleCooldown;
        exitCo = null;
    }

    // ===== 이동 유틸 =====
    private IEnumerator MoveToX(float targetX)
    {
        float tol = Mathf.Max(0.0001f, settings.snapTolerance);
        float speed = Mathf.Max(0.0001f, settings.pushSpeed);

        while (Mathf.Abs(transform.position.x - targetX) > tol)
        {
            Vector3 pos = transform.position;
            float dir = Mathf.Sign(targetX - pos.x);
            float step = speed * Time.deltaTime * dir;

            float nextX = pos.x + step;
            // 목표를 넘어가면 스냅
            if (Mathf.Sign(targetX - nextX) != dir)
                nextX = targetX;

            Vector3 next = new Vector3(nextX, pos.y, pos.z);

            if (rb) rb.MovePosition(next);
            else transform.position = next;

            yield return null;
        }

        // 최종 스냅
        Vector3 snap = transform.position;
        snap.x = targetX;
        if (rb) rb.MovePosition(snap);
        else transform.position = snap;
    }

    /// <summary>
    /// 플레이어 알파값 조절 메소드. 코루틴 호출.
    /// <param name="isActive"> true일 시, 0.8f로 변경.</param>
    /// <param name="isActive"> false일 시, 원래 1로 돌아옴</param>
    /// </summary>
    /// 
    public void SetHidingAlpha(bool isActive)
    {
        if (isActive)
            StartCoroutine(ChangeAlphaValue(settings.HidingAlphaValue));
        else
            StartCoroutine(ChangeAlphaValue(1f));
    }

    private IEnumerator ChangeAlphaValue(float finalValue)
    {
        float elapsedTime = 0f;
        float duration = 1f;

        Color currentColor = spriteRenderer.color;
        float startValue = spriteRenderer.color.a;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentColor.a = Mathf.Lerp(startValue, finalValue, (elapsedTime / duration));
            spriteRenderer.color = currentColor;
            yield return null;
        }

        currentColor.a = finalValue;
        spriteRenderer.color = currentColor;
    }


    private float ComputeOutsideX(HideObject ho)
    {
        if (ho == null) return transform.position.x;

        var col = ho.AreaCollider;
        if (!col) return transform.position.x;

        Bounds b = col.bounds;

        // 오른쪽 바라보는 경우: sr.flipX == false
        bool facingRight = spriteRenderer ? (spriteRenderer.flipX == false) : true;

        float pad = settings.pushOutsidePadding + Mathf.Abs(transform.localScale.x) * 1.0f;

        return facingRight ? (b.max.x + pad) : (b.min.x - pad);
    }

    // 웅크림 애니메이션 즉시 적용
    void ApplyCrouchAnim(bool moving)
    {
        if (!animator) return;

        // 다른 상태는 끄고
        animator.SetBool(H_Moving, false);
        animator.SetBool(H_Dash, false);
        animator.SetBool(H_Climbing, false);
        animator.SetBool(H_Jump, false);

        // 웅크림만 켠다 (이동/정지 상호배타)
        animator.SetBool(H_Crouching, moving);
        animator.SetBool(H_Crouch, !moving);
    }

    // 웅크림 종료 시 깔끔히 정리
    void ClearCrouchAnim()
    {
        if (!animator) return;
        animator.SetBool(H_Crouching, false);
        animator.SetBool(H_Crouch, false);
    }

    public void Chase_StartEnter(HideObject ho)
    {
        PlayerCatMovement.Instance.enabled = true;

        if (!overlaps.Contains(ho)) overlaps.Add(ho);
        currentHideObj = ho;

        if (exitCo != null) { StopCoroutine(exitCo); exitCo = null; }
        if (exitFxCo != null) { StopCoroutine(exitFxCo); exitFxCo = null; }

        enterCo = StartCoroutine(EnterFlow(ho));
    }

    public void Chase_GameOver()
    {
        StopAllCoroutines();
        movement.SetCrouchMovingState(false);
        this.enabled = false;
    }

    public void Chase_CourchingDisabled()
    {
        // 원복
        movement.SetMiniGameInputBlocked(false);
        isHiding = false;
        this.enabled = false;
    }
}
