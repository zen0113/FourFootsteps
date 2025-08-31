using System.Collections;
using UnityEngine;
using System; // System 네임스페이스 추가
using UnityEngine.Events;

public class PlayerAutoRunner : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private StealthSFX SFX;
    [SerializeField] private CatStealthController catStealth;

    [Header("센터 기준")]
    [Tooltip("플레이어가 '정위치'로 간주할 앵커(대개 메인카메라 중앙의 월드좌표 또는 빈 오브젝트). 비우면 시작 시 자신의 위치를 앵커로 사용.")]
    public Transform centerAnchor;
    [Tooltip("A/D로 이동 가능한 최대 좌/우 오프셋(World Space)")]
    [SerializeField] private float maxOffsetX = 1.2f;
    [Tooltip("A/D 입력 시 목표오프셋으로 붙는 스피드")]
    [SerializeField] private float nudgeSpeed = 6f;
    [Tooltip("키에서 손 떼면 0(센터)로 돌아오는 스피드")]
    [SerializeField] private float returnSpeed = 4f;
    [Tooltip("오프셋 이동을 더 부드럽게(가속/감속)")]
    [SerializeField] private float offsetSmoothTime = 0.08f;

    [Header("점프(더블점프)")]
    [SerializeField] private float jumpPower = 5.0f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private int maxJumpCount = 2;
    private int jumpCount = 0;

    [Header("지면 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    private bool isOnGround;

    [Header("애니/사운드")]
    [SerializeField] private AudioClip hurtSound;       // 피해를 받았을 때 소리
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip runLoopOrStep;
    [SerializeField] private float footstepInterval = 0.25f;

    [Header("월드 스크롤 보정")]
    [SerializeField] private WorldScroller scroller;
    [SerializeField] private bool compensateScrollOnOffset = true;    // 보정 on/off
    [SerializeField, Range(0f, 1.5f)] private float compensationFactor = 1.0f; // 1=완전보정
    [SerializeField] private bool compensateBackwardOnly = true;      // 뒤로 갈때만 보정

    // === HideObject 도착/정지 상태 ===
    [Header("HideObject Stop")]
    [SerializeField] private string hideTag = "HideObject";
    [SerializeField] private float xDampOnStop = 40f; // 정지 시 X속도 빠르게 0으로
    private bool stopAnimPlayed = false;

    private bool sTriggeredOnce = false;
    private bool atHide = false;        // HideObject에 도착
    public bool chaseFinished = false; // S로 마무리했는지
    private HideObject lastHide;

    // 내부
    Rigidbody2D rb;
    BoxCollider2D col;
    Animator animator;
    SpriteRenderer sr;
    AudioSource audioSrc;

    float targetOffset;         // 입력으로 가고 싶은 목표 오프셋
    float currentOffset;        // 현재 오프셋
    float offsetVelocity;       // SmoothDamp 속도 버퍼
    float lastFootstepTime;
    Vector2 startAnchor;        // centerAnchor 없을 때 시작 위치

    private string FinishChaseEventID = "EventChaseGameEnd";

    void Awake()
    {
        // UI 상태 설정 (고양이 버전 UI 활성화, 사람 버전 UI 비활성화)
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);
        // 필요한 컴포넌트들 가져오기
        UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, true);
        //================================

        mainCamera = Camera.main;

        SFX = GameObject.FindObjectOfType<StealthSFX>().GetComponent<StealthSFX>();
        catStealth = GetComponent<CatStealthController>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        audioSrc = GetComponent<AudioSource>();

        // Rigidbody2D 권장 설정
        rb.gravityScale = 1.5f; // 프로젝트 감에 맞게
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (centerAnchor == null)
            startAnchor = rb.position;
    }

    private void Start()
    {
        StartCoroutine(ChangeCameraSize(7f));
        mainCamera.GetComponent<FollowCamera>().smoothSpeedX = 0.05f;
    }

    void Update()
    {
        // 지상 체크
        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        if (isOnGround && rb.velocity.y <= 0.01f)
            jumpCount = 0;

        if (atHide)
        {
            // S키로 마무리
            if (!chaseFinished && !sTriggeredOnce && Input.GetKeyDown(KeyCode.S))
            {
                //FinishChase();
                StartCoroutine(FinishChase());
            }
            return;
        }

        // 입력 → 목표 오프셋
        float h = Input.GetAxisRaw("Horizontal"); // A/D or ←/→
        if (Mathf.Abs(h) > 0.01f)
        {
            targetOffset = Mathf.Sign(h) * maxOffsetX; // 끝까지(‘살짝’의 최대치) 밀고
        }
        else
        {
            targetOffset = 0f; // 손 떼면 센터 복귀
        }

        // 부드러운 오프셋 업데이트 (가속/감속)
        float approachSpeed = (Mathf.Abs(targetOffset - currentOffset) > 0.01f)
            ? (Mathf.Abs(targetOffset) > Mathf.Abs(currentOffset) ? nudgeSpeed : returnSpeed)
            : returnSpeed;

        currentOffset = Mathf.SmoothDamp(currentOffset, targetOffset, ref offsetVelocity, offsetSmoothTime, Mathf.Infinity, Time.deltaTime);

        // 애니메이션: 항상 '달리기' 연출(대시는 애니로만)
        animator.SetBool("Dash", true);
        animator.SetBool("Moving", Mathf.Abs(currentOffset) > 0.01f || !isOnGround); // 살짝 움직일 땐 Moving도 true 가능
        animator.SetBool("Crouch", false);
        animator.SetBool("Crouching", false);
        animator.SetBool("Climbing", false);

        // 점프 입력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }
    }

    void FixedUpdate()
    {
        if (atHide && !chaseFinished)
        {
            rb.velocity = new Vector2(
                Mathf.MoveTowards(rb.velocity.x, 0f, xDampOnStop * Time.fixedDeltaTime),
                rb.velocity.y
            );
            return;
        }

        // 1) 앵커 + 오프셋으로 가야 할 X
        float anchorX = (centerAnchor != null ? centerAnchor.position.x : startAnchor.x);
        float desiredX = anchorX + currentOffset;

        // 2) X 위치 오차를 "속도"로 보정 (Y는 기존 물리 속도 유지)
        //    - 오차/Δt = 그 프레임에 도달하기 위한 이론상 속도
        //    - 너무 튀지 않게 클램프(최대 보정 속도) 권장
        const float maxXCorrectionSpeed = 40f; // 필요에 따라 조절
        float xError = desiredX - rb.position.x;
        float xVel = Mathf.Clamp(xError / Time.fixedDeltaTime, -maxXCorrectionSpeed, maxXCorrectionSpeed);
        rb.velocity = new Vector2(xVel, rb.velocity.y);

        // 3) 점프 중력 보정(그대로 유지)
        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;

        // 4) 발소리 등 부가 로직
        if (isOnGround && Mathf.Abs(currentOffset) > 0.02f && runLoopOrStep != null&& !sTriggeredOnce)
        {
            if (Time.time - lastFootstepTime >= footstepInterval)
            {
                audioSrc.PlayOneShot(runLoopOrStep);
                lastFootstepTime = Time.time;
            }
        }
    }

    // Chase_Goal에 도착 후 S키 눌렀을 때 실행될 연출
    IEnumerator FinishChase()
    {
        sTriggeredOnce = true;

        if (SFX) SFX.PlayEnterSFX(5f);
        if (catStealth) catStealth.Chase_StartEnter(lastHide);

        chaseFinished = true;

        yield return new WaitForSeconds(3f);
        //EventManager.Instance.CallEvent(FinishChaseEventID);

        // Tutorial Manager 진행 가능하게


        this.enabled = false;
    }


    void TryJump()
    {
        if (jumpCount >= maxJumpCount) return;

        // 현재 y속도 리셋 후 임펄스
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpCount++;

        if (jumpSound != null) audioSrc.PlayOneShot(jumpSound);
    }

    public void PlayHurtSound()
    {
        if (hurtSound != null)
        {
            audioSrc.PlayOneShot(hurtSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 씬뷰에서 좌/우 클램프 시각화 (centerAnchor 기준)
        Vector3 anchor = (centerAnchor != null) ? centerAnchor.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchor + Vector3.left * maxOffsetX + Vector3.down * 0.5f,
                        anchor + Vector3.left * maxOffsetX + Vector3.up * 0.5f);
        Gizmos.DrawLine(anchor + Vector3.right * maxOffsetX + Vector3.down * 0.5f,
                        anchor + Vector3.right * maxOffsetX + Vector3.up * 0.5f);
    }

    IEnumerator ChangeCameraSize(float finalValue)
    {
        float elapsedTime = 0f;
        float duration = 2.5f;
        float startValue = mainCamera.orthographicSize;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startValue, finalValue, (elapsedTime / duration));
            mainCamera.GetComponent<FollowCamera>().UpdateCameraHalfSize();
            yield return null;
        }

        mainCamera.orthographicSize = finalValue;
        mainCamera.GetComponent<FollowCamera>().UpdateCameraHalfSize();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(hideTag)) return;

        lastHide = other.GetComponent<HideObject>();

        // 1) 월드 스크롤 정지
        if (scroller != null)
        {
            scroller.SetPaused(true);            // 완전 일시정지
        }

        // 2) 오토런/오프셋 보정 중지 상태 진입
        atHide = true;
        chaseFinished = false;
        sTriggeredOnce = false;

        // 지금 위치를 유지하도록(센터로 끌려가지 않게) 현재 오프셋을 목표로 고정
        targetOffset = currentOffset;
        offsetVelocity = 0f;

        // 3) 애니메이션: 대시/이동 끄고 '정지' 상태
        if (!stopAnimPlayed)
        {
            animator.SetBool("Dash", false);
            animator.SetBool("Moving", false);
            animator.SetBool("Crouch", false);
            animator.SetBool("Crouching", false);
            stopAnimPlayed = true;
        }

        // 4) X 속도 즉시 0에 가깝게
        rb.velocity = new Vector2(0f, rb.velocity.y);

        catStealth.enabled = true;
        catStealth.isPlaying = false;
        PlayerCatMovement.Instance.enabled = true;

        mainCamera.GetComponent<FollowCamera>().target = this.transform;
        mainCamera.GetComponent<CameraController>().playerTransform = this.transform;
        mainCamera.GetComponent<CameraShake>().target = this.transform;
    }

   
}
