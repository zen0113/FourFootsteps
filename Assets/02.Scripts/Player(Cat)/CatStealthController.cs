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

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private HideObject currentHideObj;
    [SerializeField] private bool isHiding;
    [SerializeField] private float lastEnterTime;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (!movement) movement = PlayerCatMovement.Instance;
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!settings)
        {
            Debug.LogWarning("[CatStealthController] StealthSettings가 지정되지 않았습니다. 기본값으로 동작합니다.");
            settings = ScriptableObject.CreateInstance<StealthSettingsSO>();
        }
    }

    void Update()
    {
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
            OnEnterArea?.Invoke(ho);
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other || !other.CompareTag("HideObject")) return;
        if (other.TryGetComponent(out HideObject ho))
        {
            overlaps.Remove(ho);
            ho.SetEffect(false);
            OnExitArea?.Invoke(ho);
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

        // 바로 잠금 & 관성 제거 (버퍼보다 먼저)
        movement.SetMiniGameInputBlocked(true);
        movement.ForceCrouch = true;
        movement.SetCrouchMovingState(true);

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
            state = IsNearHide ? StealthState.Near : StealthState.Idle;
            yield break;
        }

        isHiding = true;
        // 컨트롤/애니 준비
        movement.UpdateAnimationCrouch();

        ho.SetEffect(false); // 하이라이트 제거
        OnHideStart?.Invoke(ho);

        // 은신 오브젝트 안쪽으로 이동
        yield return MoveToX(ho.AnchorX);

        // 최종 스냅 (물리 간섭 없이 정확히 고정)
        if (rb) rb.position = new Vector2(ho.AnchorX, rb.position.y);
        else transform.position = new Vector3(ho.AnchorX, transform.position.y, transform.position.z);

        // 물리 복원
        if (rb) rb.bodyType = prevBody;

        movement.SetCrouchMovingState(false);
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

        // 바깥 X 계산
        float targetX = ComputeOutsideX(ho);

        // 이동
        yield return MoveToX(targetX);

        // 원복
        movement.SetMiniGameInputBlocked(false);
        movement.ForceCrouch = false;

        isHiding = false;
        OnHideEnd?.Invoke(ho);

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
}
