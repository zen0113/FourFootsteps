using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 고양이의 모든 이동, 애니메이션, 사운드를 관리하는 메인 클래스
/// 일반 이동, 점프, 대시, 웅크리기, 사다리 타기, 카트 탑승 등의 기능을 포함
/// </summary>
public class PlayerCatMovement : MonoBehaviour
{
    public static PlayerCatMovement Instance { get; private set; }

    // 핵심 컴포넌트들
    Rigidbody2D rb;                 // 물리 이동 처리
    BoxCollider2D boxCollider;      // 충돌 처리 및 크기 조절 (웅크리기용)
    SpriteRenderer spriteRenderer;  // 스프라이트 방향 전환
    Animator animator;              // 애니메이션 상태 관리
    AudioSource audioSource;        // 사운드 재생

    // 이동 관련 설정값들
    [Header("이동 및 점프")]
    [SerializeField] private float movePower = 2f;      // 기본 이동 속도
    [SerializeField] private float dashPower = 8f;      // 대시 이동 속도
    [SerializeField] private float jumpPower = 5f;      // 점프 힘
    [SerializeField] private float crouchPower = 1f;    // 웅크린 상태 이동 속도

    // 파티클 시스템 (발자국 효과)
    [Header("파티클 시스템")]
    [SerializeField] private ParticleSystem dashParticle;           // 이동 시 발생하는 파티클
    [SerializeField] private Vector3 dashParticleOffset = new Vector2(0.5f, 0f); // 파티클 위치 오프셋
    [SerializeField] private float walkEmissionRate = 3f;           // 걷기 시 파티클 발생률
    [SerializeField] private float runEmissionRate = 6f;            // 달리기 시 파티클 발생률
    private ParticleSystem.EmissionModule particleEmission;         // 파티클 방출 제어 모듈

    // 사운드 효과 관련
    [Header("오디오 클립")]
    [SerializeField] private AudioClip hurtSound;       // 피해를 받았을 때 소리
    [SerializeField] private AudioClip walkSound;       // 걷기 소리
    [SerializeField] private AudioClip runSound;        // 달리기 소리
    [SerializeField] private AudioClip jumpSound;       // 점프 소리
    [SerializeField] private AudioClip crouchSound;     // 웅크리기 이동 소리
    [SerializeField] private AudioClip climbSound;      // 사다리 오르기 소리

    // 사운드 재생 간격 제어 (너무 빠르게 재생되는 것을 방지)
    [SerializeField] private float walkSoundInterval = 0.3f;        // 걷기 소리 간격
    [SerializeField] private float runSoundInterval = 0.2f;         // 달리기 소리 간격
    [SerializeField] private float climbSoundInterval = 0.4f;       // 사다리 소리 간격
    [SerializeField] private float crouchSoundInterval = 0.5f;      // 웅크리기 소리 간격
    [SerializeField] private float landingSoundDelay = 0.3f;        // 착지 후 소리 재생 대기시간

    // 사운드 재생 시간 추적 변수들
    private float lastWalkSoundTime;    // 마지막 걷기 소리 재생 시간
    private float lastRunSoundTime;     // 마지막 달리기 소리 재생 시간
    private float lastClimbSoundTime;   // 마지막 사다리 소리 재생 시간
    private float lastCrouchSoundTime;  // 마지막 웅크리기 소리 재생 시간
    private float lastLandingTime;      // 마지막 착지 시간
    private int lastMoveDirection = 0;  // 이전 이동 방향 추적 (1: 오른쪽, -1: 왼쪽, 0: 정지)

    // 점프 물리 관련
    [Header("점프 중력 보정")]
    [SerializeField] private float fallMultiplier = 2.5f;   // 떨어질 때 중력 배수 (자연스러운 점프감을 위함)
    private int jumpCount = 0;                              // 현재 점프 횟수 (더블점프 구현용)

    // 지상 감지 시스템
    [Header("지상 체크")]
    [SerializeField] private Transform groundCheck;         // 지상 체크 포인트
    [SerializeField] private float groundCheckRadius = 0.2f; // 지상 체크 반경
    [SerializeField] private LayerMask groundMask;          // 지상으로 인식할 레이어
    private bool isOnGround;                                // 현재 지상에 있는지 여부

    [SerializeField] private float boxInteractingPower = 1.2f; // 박스와 상호작용 시 이동속도

    // 웅크리기 시스템
    [Header("웅크리기")]
    [SerializeField] private Transform headCheck;           // 머리 위 장애물 체크 포인트
    [SerializeField] private Transform tailCheck;           // 꼬리 위 장애물 체크 포인트
    [SerializeField] private float headCheckLength;         // 머리 체크 거리
    [SerializeField] private float tailCheckLength;         // 꼬리 체크 거리
    [SerializeField] private Sprite crouchSprite;           // 웅크린 상태 스프라이트
    private Sprite originalSprite;                          // 원본 스프라이트
    [SerializeField] private LayerMask obstacleMask;        // 장애물로 인식할 레이어
    private bool isCrouching = false;                       // 현재 웅크리고 있는지
    private bool isCrouchMoving = false;                    // 웅크린 상태에서 이동 중인지

    // 콜라이더 크기 관련 (웅크리기 시 콜라이더 크기 변경용)
    private Vector2 originalColliderSize, originalColliderOffset;   // 원본 콜라이더 크기와 오프셋
    private Vector2 crouchColliderSize, crouchColliderOffset;       // 웅크린 상태 콜라이더 크기와 오프셋
    private bool forceCrouch = false;                               // 외부에서 강제로 웅크리기 상태로 만들기

    // 사다리 시스템
    [Header("사다리")]
    [SerializeField] private float climbSpeed = 2f;             // 사다리 타는 속도
    [SerializeField] private float ladderSnapDistance = 0.3f;   // 사다리 중앙으로 자동 정렬되는 거리
    private Collider2D currentLadder;                           // 현재 근처에 있는 사다리
    private bool isClimbing = false;                            // 현재 사다리를 타고 있는지
    private bool isNearLadder = false;                          // 사다리 근처에 있는지
    private bool canUseLadder = true;                           // 사다리 사용 가능 여부 (쿨다운용)

    // 카트 탑승 시스템
    [Header("카트 상호작용")]
    private bool isOnCart = false;              // 카트에 탑승 중인지
    private Transform currentCart;              // 현재 탑승 중인 카트
    private float originalGravityScale;         // 원본 중력 값 (카트 탑승 시 복원용)

    // 박스 상호작용 관련
    private PlayerBoxInteraction boxInteraction;    // 박스 밀기/당기기 컴포넌트
    private bool isBoxInteractionEnabled = false;   // 박스 상호작용 활성화 여부
    private bool isDashing = false;                 // 현재 대시 중인지

    // 입력 차단 시스템 (미니게임, 대화 등에서 사용)
    private bool isMiniGameInputBlocked = false;

    /// <summary>
    /// 게임 시작 시 초기화 작업
    /// UI 설정, 컴포넌트 참조, 물리 설정 등을 수행
    /// </summary>
    private void Start()
    {
        // UI 상태 설정 (고양이 버전 UI 활성화, 사람 버전 UI 비활성화)
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);

        // 필요한 컴포넌트들 가져오기
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
        boxInteraction = GetComponent<PlayerBoxInteraction>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // AudioSource가 없으면 자동으로 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 콜라이더 크기 설정 (웅크리기용)
        originalSprite = spriteRenderer.sprite;
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;
        crouchColliderSize = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
        crouchColliderOffset = new Vector2(originalColliderOffset.x,
            originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f);

        // 물리 설정 최적화 (안정적인 움직임을 위함)
        rb.freezeRotation = true;                                           // 회전 고정
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;           // 부드러운 움직임
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;   // 정확한 충돌 감지

        // 파티클 시스템 초기화
        if (dashParticle != null)
        {
            particleEmission = dashParticle.emission;
            particleEmission.rateOverTime = 0f; // 초기에는 파티클 발생하지 않음
        }
    }

    /// <summary>
    /// 싱글톤 패턴 구현 및 기본 설정
    /// </summary>
    void Awake()
    {
        // 싱글톤 패턴: 하나의 인스턴스만 존재하도록 보장
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale; // 카트 탑승 시 복원할 원본 중력값 저장
    }

    /// <summary>
    /// 매 프레임 입력 처리 및 상태 업데이트
    /// 사용자 입력을 받아서 이동, 점프, 사다리, 웅크리기 등을 처리
    /// </summary>
    void Update()
    {
        // 입력이 차단되었거나 이동 불가능한 상태라면 애니메이션만 정지하고 리턴
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            UpdateAnimationState(0);
            return;
        }

        // 지상 감지 (이전 프레임과 비교하여 착지 판정)
        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        // 박스, 벽, 카트 위에 있는지 추가 체크
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        bool onBox = false;

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Box") || col.CompareTag("wall") || col.CompareTag("Cart"))
            {
                onBox = true;
                break;
            }
        }

        // 땅이나 박스 위에 있으면 지상으로 판정
        if (onBox)
        {
            isOnGround = true;
        }

        // 방금 착지했는지 확인 (착지 사운드 처리용)
        bool justLanded = isOnGround && !prevOnGround;
        if (justLanded)
        {
            lastLandingTime = Time.time;
        }

        // 지상에 있고 떨어지는 중이면 점프 카운트 리셋
        if (isOnGround && rb.velocity.y <= 0) jumpCount = 0;

        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 스프라이트 방향 설정 (왼쪽/오른쪽 보기)
        if (!isClimbing) // 사다리 타는 중이 아닐 때만
        {
            if (horizontalInput != 0)
            {
                // 박스를 당기는 중이 아니라면 이동 방향으로 스프라이트 방향 설정
                if (!(boxInteraction != null && boxInteraction.IsPulling))
                {
                    spriteRenderer.flipX = horizontalInput < 0;
                }
                else
                {
                    // 박스를 당기는 중이라면 박스 위치에 따라 방향 결정
                    bool isBoxOnRight = boxInteraction.CurrentBox != null &&
                                          boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                    spriteRenderer.flipX = !isBoxOnRight;
                }
            }
        }

        // 각종 입력 처리
        HandleLadderInput();    // 사다리 관련 입력
        if (!isClimbing) Jump(); // 사다리 타는 중이 아니면 점프 가능
        HandleCrouch(justLanded); // 웅크리기 처리
    }

    /// <summary>
    /// 애니메이션 상태를 업데이트하는 핵심 함수
    /// 현재 플레이어의 상태에 따라 적절한 애니메이션을 재생
    /// </summary>
    void UpdateAnimationState(float horizontalInput)
    {
        // 입력 차단 상태일 때는 모든 애니메이션을 정지상태로 설정
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Climbing", false);
            if (dashParticle != null)
            {
                particleEmission.rateOverTime = 0f;
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 사다리 타는 중인 경우
        if (isClimbing)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");
            bool isClimbingMoving = Mathf.Abs(verticalInput) > 0.01f;

            // 다른 모든 애니메이션 상태를 false로 설정
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Crouch", false);

            animator.SetBool("Climbing", true);

            // 움직이지 않을 때는 애니메이션 정지 (사다리에서 멈춰있기)
            animator.speed = isClimbingMoving ? 1f : 0f;
            return;
        }

        // 사다리를 타지 않을 때는 애니메이션 속도를 정상으로 복구
        if (animator.speed == 0f)
        {
            animator.speed = 1f;
        }

        // 기본 상태 초기화
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);
        animator.SetBool("Crouching", false);

        // 실제 키 입력이 있는지 확인 (물리적 속도가 아닌 입력 기준으로 판단)
        bool hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.01f;

        // 웅크리기 상태 처리
        if (isCrouching || forceCrouch)
        {
            if (hasHorizontalInput)
            {
                animator.SetBool("Crouching", true);    // 웅크린 채로 이동
            }
            else
            {
                animator.SetBool("Crouch", true);       // 웅크린 채로 정지
            }
            return;
        }

        // 일반 상태 처리 (이동, 점프, 대시)
        isDashing = Input.GetKey(KeyCode.LeftShift) && !(boxInteraction != null && boxInteraction.IsInteracting);
        bool isJumping = !isOnGround;

        if (isDashing && hasHorizontalInput)
        {
            animator.SetBool("Dash", true);         // 대시 애니메이션
        }
        else if (isJumping)
        {
            // 점프 애니메이션 (필요시 추가)
            // 예: animator.SetBool("Jumping", true);
        }
        else if (hasHorizontalInput)
        {
            animator.SetBool("Moving", true);       // 일반 이동 애니메이션
        }
    }

    /// <summary>
    /// Update 이후에 애니메이션 상태를 한 번 더 동기화
    /// 물리 업데이트와 애니메이션이 정확히 맞도록 보장
    /// </summary>
    void LateUpdate()
    {
        UpdateAnimationState(Input.GetAxisRaw("Horizontal"));
    }

    /// <summary>
    /// 물리 업데이트 - 실제 이동, 점프, 사운드 처리 등을 수행
    /// 고정된 시간 간격으로 호출되어 일정한 물리 처리를 보장
    /// </summary>
    void FixedUpdate()
    {
        // 입력이 차단된 상태라면 x축 속도만 0으로 만들어 미끄러짐 방지
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (dashParticle != null)
            {
                particleEmission.rateOverTime = 0f;
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 카트에 타지 않았을 때만 일반적인 움직임 처리
        if (!isOnCart)
        {
            if (!isClimbing)
            {
                Move();                 // 기본 이동 처리
                BetterJump();          // 점프 중력 보정
                HandleSound();         // 이동 사운드 처리
                UpdateParticleState(); // 파티클 시스템 업데이트
            }
            else
            {
                Climb(); // 사다리 타기 처리
                if (dashParticle != null)
                {
                    particleEmission.rateOverTime = 0f;
                    dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
        // 카트에 탔을 때의 물리 로직은 필요시 여기에 추가
    }

    /// <summary>
    /// 카트 탑승 상태를 외부에서 설정하기 위한 공개 함수
    /// </summary>
    /// <param name="onCart">카트에 탑승 여부</param>
    /// <param name="cartTransform">탑승할 카트의 Transform</param>
    public void SetOnCartState(bool onCart, Transform cartTransform = null)
    {
        isOnCart = onCart;

        if (onCart)
        {
            // 카트에 탑승: 부모-자식 관계로 설정하여 카트와 함께 움직임
            transform.SetParent(cartTransform);
            rb.gravityScale = 0;        // 중력 제거로 카트에 완전히 밀착
            rb.velocity = Vector2.zero; // 탑승 시 속도 초기화
        }
        else
        {
            // 카트에서 하차: 독립적인 움직임 복원
            transform.SetParent(null);
            rb.gravityScale = originalGravityScale; // 원래 중력값 복원
        }
    }

    /// <summary>
    /// 파티클 시스템 상태 업데이트
    /// 이동 상태에 따라 발자국 파티클의 발생량과 재생 상태를 조절
    /// </summary>
    private void UpdateParticleState()
    {
        if (dashParticle == null) return;

        // 입력이 차단된 상태라면 파티클 정지
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            particleEmission.rateOverTime = 0f;
            if (dashParticle.isPlaying)
            {
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 현재 활성화된 애니메이션이 있는지 확인
        bool isAnyAnimationActive = animator.GetBool("Moving") ||
                                      animator.GetBool("Dash") ||
                                      animator.GetBool("Crouching") ||
                                      animator.GetBool("Climbing") ||
                                      animator.GetBool("Crouch");

        bool hasHorizontalInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f;
        bool isJumping = !isOnGround;

        // 사다리 타는 중이거나 애니메이션이 없거나 입력이 없으면 파티클 정지
        if (isClimbing || !isAnyAnimationActive || (!hasHorizontalInput && !isJumping))
        {
            particleEmission.rateOverTime = 0f;
            if (dashParticle.isPlaying)
            {
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        else
        {
            // 대시 중이면 많은 파티클, 아니면 적은 파티클
            float currentRate = isDashing ? runEmissionRate : walkEmissionRate;
            particleEmission.rateOverTime = currentRate;

            if (!dashParticle.isPlaying)
            {
                dashParticle.Play();
            }

            UpdateParticlePosition(); // 파티클 위치 업데이트
        }
    }

    /// <summary>
    /// 미니게임 중 입력 차단 설정
    /// </summary>
    public void SetMiniGameInputBlocked(bool blocked)
    {
        isMiniGameInputBlocked = blocked;
        Debug.Log($"[PlayerCatMovement] 미니게임 입력 차단 설정: {blocked}");
    }

    /// <summary>
    /// 웅크리기 이동 상태 설정 (외부에서 호출용)
    /// </summary>
    public void SetCrouchMovingState(bool moving)
    {
        isCrouchMoving = moving;
        Debug.Log($"[PlayerCatMovement] 웅크리기 이동 상태 설정: {moving}");

        if (forceCrouch || isCrouching)
        {
            if (moving)
            {
                animator.SetBool("Crouching", true);    // 웅크린 채로 이동
                animator.SetBool("Crouch", false);
                Debug.Log("[PlayerCatMovement] 즉시 Crouching 애니메이션 활성화");
            }
            else
            {
                animator.SetBool("Crouch", true);       // 웅크린 채로 정지
                animator.SetBool("Crouching", false);
                Debug.Log("[PlayerCatMovement] 즉시 Crouch 애니메이션 활성화");
            }
        }
    }

    /// <summary>
    /// 강제 웅크리기 상태 프로퍼티 (외부에서 웅크리기 강제 설정/해제)
    /// </summary>
    public bool ForceCrouch
    {
        get { return forceCrouch; }
        set
        {
            forceCrouch = value;
            Debug.Log($"[PlayerCatMovement] 강제 웅크리기 설정: {value}");

            if (forceCrouch)
            {
                // 강제 웅크리기 활성화: 콜라이더 크기 줄이기
                isCrouching = true;
                isCrouchMoving = false;
                boxCollider.size = crouchColliderSize;
                boxCollider.offset = crouchColliderOffset;
            }
            else
            {
                // 강제 웅크리기 해제: 원래 콜라이더 크기로 복원
                isCrouching = false;
                isCrouchMoving = false;
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;
            }
        }
    }

    /// <summary>
    /// 현재 입력이 차단된 상태인지 확인
    /// 게임 일시정지, 대화 중, 씬 로딩 중, 미니게임 중 등의 상황을 체크
    /// </summary>
    bool IsInputBlocked()
    {
        return PauseManager.IsGamePaused
             || DialogueManager.Instance.isDialogueActive
             || (GameManager.Instance != null && GameManager.Instance.IsSceneLoading)
             || isMiniGameInputBlocked;
    }

    /// <summary>
    /// 사다리 관련 입력 처리
    /// 사다리 타기 시작, 사다리에서 내리기, 사다리 타는 중 이동 등을 처리
    /// </summary>
    void HandleLadderInput()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 사다리 근처에 있고 사다리를 타지 않는 상태에서 위/아래 키를 누르면 사다리 타기 시작
        if (isNearLadder && !isClimbing && canUseLadder)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                StartClimbing();
            }
            else if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isOnGround)
            {
                StartClimbing();
            }
        }
        // 지상에서 시프트+아래키로 플랫폼에서 사다리로 내려가기
        else if (!isClimbing && canUseLadder && isOnGround && isNearLadder)
        {
            if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) &&
                Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartClimbingFromPlatform();
            }
        }
        // 사다리 타는 중일 때의 입력 처리
        else if (isClimbing)
        {
            // 스페이스바로 점프하면서 사다리에서 내리기
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExitLadder(true);
            }
            // 좌우 키로 사다리에서 내리기
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                     Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                ExitLadder(false);
            }
        }
    }

    /// <summary>
    /// 머리와 꼬리 위에 직접적인 장애물이 있는지 확인
    /// 웅크리기 해제 가능 여부를 판단하기 위해 사용
    /// </summary>
    bool IsObstacleDirectlyAbove()
    {
        return Physics2D.Raycast(headCheck.position, Vector2.up, headCheckLength, obstacleMask) ||
               Physics2D.Raycast(tailCheck.position, Vector2.up, tailCheckLength, obstacleMask);
    }

    /// <summary>
    /// 웅크리기 입력 및 상태 처리
    /// 자동 웅크리기 (장애물 감지), 수동 웅크리기 (S키), 웅크리기 해제 등을 처리
    /// </summary>
    void HandleCrouch(bool justLanded)
    {
        bool obstacleAbove = IsObstacleDirectlyAbove();
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 장애물이 위에 있고 지상에 있으면 자동으로 웅크리기
        if (obstacleAbove && !isCrouching && isOnGround)
        {
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }

        // S키를 누르면 수동으로 웅크리기
        if (Input.GetKeyDown(KeyCode.S) && isOnGround)
        {
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }
        // S키를 떼고 위에 장애물이 없으면 웅크리기 해제
        else if (Input.GetKeyUp(KeyCode.S) && !obstacleAbove)
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        // 장애물이 없어지고 S키를 누르지 않으면 웅크리기 해제
        if (!obstacleAbove && isCrouching && !Input.GetKey(KeyCode.S))
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }
    }

    /// <summary>
    /// 기본 이동 처리 함수
    /// 일반 이동, 대시, 웅크리기 이동, 박스 상호작용 시 이동 등을 처리
    /// </summary>
    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower; // 기본 이동 속도

        // 박스와 상호작용 중인지 확인
        bool isInteractingWithBox = boxInteraction != null && boxInteraction.IsInteracting;
        bool isPullingBox = boxInteraction != null && boxInteraction.IsPulling;

        // 상황에 따른 이동 속도 결정
        if (isInteractingWithBox && isBoxInteractionEnabled)
        {
            // 박스와 상호작용 중일 때
            currentPower = boxInteractingPower;
            if (isPullingBox && boxInteraction.CurrentBox != null)
            {
                // 박스를 당기는 중이면 박스 위치에 따라 스프라이트 방향 결정
                bool isBoxOnRight = boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                spriteRenderer.flipX = !isBoxOnRight;
            }
        }
        else if (isCrouching)
        {
            // 웅크린 상태일 때
            currentPower = crouchPower;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isInteractingWithBox)
        {
            // 대시 상태일 때 (웅크리거나 박스 상호작용 중이 아닐 때만)
            currentPower = dashPower;
        }

        // 목표 속도 계산 및 부드러운 속도 변화 적용
        float targetVelocityX = horizontalInput * currentPower;
        float smoothedVelocityX = Mathf.Lerp(rb.velocity.x, targetVelocityX, Time.fixedDeltaTime * 10f);

        rb.velocity = new Vector2(smoothedVelocityX, rb.velocity.y);
    }

    /// <summary>
    /// 이동 관련 사운드 처리
    /// 걷기, 달리기, 웅크리기 이동에 따른 적절한 사운드를 재생
    /// </summary>
    void HandleSound()
    {
        // 사다리 타는 중이거나 이동 입력이 없거나 공중에 있으면 사운드 정지
        if (isClimbing || Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f || !isOnGround)
        {
            audioSource.Stop();
            return;
        }

        // 착지 직후 잠시 동안은 사운드 재생 안 함
        if (Time.time - lastLandingTime < landingSoundDelay)
        {
            return;
        }

        // 현재 상태에 따른 사운드 재생
        if (isCrouching)
        {
            // 웅크리기 이동 사운드
            if (Time.time - lastCrouchSoundTime >= crouchSoundInterval)
            {
                audioSource.PlayOneShot(crouchSound);
                lastCrouchSoundTime = Time.time;
            }
        }
        else if (isDashing)
        {
            // 달리기 사운드
            if (Time.time - lastRunSoundTime >= runSoundInterval)
            {
                audioSource.PlayOneShot(runSound);
                lastRunSoundTime = Time.time;
            }
        }
        else
        {
            // 일반 걷기 사운드
            if (Time.time - lastWalkSoundTime >= walkSoundInterval)
            {
                audioSource.PlayOneShot(walkSound);
                lastWalkSoundTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 점프 입력 처리
    /// 일반 점프와 더블 점프를 지원
    /// </summary>
    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isCrouching && !isClimbing)
        {
            // 지상에 있거나 더블 점프 가능한 상태에서만 점프
            if (isOnGround || jumpCount < 2)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);        // y축 속도 초기화
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse); // 점프 힘 적용
                jumpCount++;
                isOnGround = false;

                // 점프 시 파티클 효과
                if (dashParticle != null)
                {
                    UpdateParticlePosition();
                    if (!dashParticle.isPlaying)
                    {
                        dashParticle.Play();
                    }
                    particleEmission.rateOverTime = runEmissionRate;
                }

                // 점프 사운드 재생
                if (jumpSound != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(jumpSound);
                }
            }
        }
    }

    /// <summary>
    /// 점프 물리 개선 - 떨어질 때 중력을 증가시켜 자연스러운 점프감 구현
    /// </summary>
    void BetterJump()
    {
        if (isClimbing) return;

        // 떨어지는 중일 때 중력을 더 강하게 적용
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    /// <summary>
    /// 사다리 타기 처리
    /// 수직 이동과 사다리 경계 체크를 포함
    /// </summary>
    void Climb()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float moveY = verticalInput * climbSpeed;

        if (currentLadder != null)
        {
            // 사다리의 위아래 경계 확인
            float ladderTop = currentLadder.bounds.max.y;
            float ladderBottom = currentLadder.bounds.min.y;

            // 사다리 맨 위에서 위로 올라가려 하면 사다리에서 내리기
            if (transform.position.y >= ladderTop - 0.1f && verticalInput > 0)
            {
                Vector3 exitPosition = new Vector3(transform.position.x, ladderTop + 0.5f, transform.position.z);
                transform.position = exitPosition;
                ExitLadder(false);
                return;
            }

            // 사다리 맨 아래에서 아래로 내려가려 하면 사다리에서 내리기
            if (transform.position.y <= ladderBottom + 0.1f && verticalInput < 0)
            {
                ExitLadder(false);
                return;
            }

            // 사다리 범위 내에서만 이동 가능하도록 Y 위치 제한
            float clampedY = Mathf.Clamp(transform.position.y + moveY * Time.fixedDeltaTime,
                                           ladderBottom + 0.2f, ladderTop - 0.2f);

            // 경계에서는 이동 불가
            if ((transform.position.y >= ladderTop - 0.2f && verticalInput > 0) ||
                (transform.position.y <= ladderBottom + 0.2f && verticalInput < 0))
            {
                moveY = 0;
            }
        }

        // 사다리 타기 이동 적용 (x축은 0, y축만 이동)
        rb.velocity = new Vector2(0, moveY);

        // 사다리 타는 사운드 재생
        if (Mathf.Abs(verticalInput) > 0.01f && climbSound != null && Time.time - lastClimbSoundTime >= climbSoundInterval)
        {
            audioSource.PlayOneShot(climbSound);
            lastClimbSoundTime = Time.time;
        }
    }

    /// <summary>
    /// 플랫폼에서 사다리로 내려가기 시작
    /// </summary>
    void StartClimbingFromPlatform()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;       // 중력 제거
        rb.velocity = Vector2.zero; // 속도 초기화
        jumpCount = 0;

        // 사다리 중앙으로 위치 조정하고 약간 아래로
        Vector3 pos = transform.position;
        pos.x = currentLadder.bounds.center.x;
        pos.y -= 0.3f;
        transform.position = pos;

        animator.SetBool("Climbing", true);
        if (dashParticle != null) particleEmission.rateOverTime = 0f;
        audioSource.Stop();
    }

    /// <summary>
    /// 일반적인 사다리 타기 시작
    /// </summary>
    void StartClimbing()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;       // 중력 제거
        rb.velocity = Vector2.zero; // 속도 초기화
        jumpCount = 0;

        // 사다리 중앙으로 자동 정렬 (일정 거리 내에서만)
        Vector3 pos = transform.position;
        float targetX = currentLadder.bounds.center.x;

        if (Mathf.Abs(pos.x - targetX) <= ladderSnapDistance)
        {
            pos.x = targetX;
            transform.position = pos;
        }

        animator.SetBool("Climbing", true);
        if (dashParticle != null) particleEmission.rateOverTime = 0f;
        audioSource.Stop();
    }

    /// <summary>
    /// 사다리에서 내리기
    /// </summary>
    /// <param name="withJump">점프하면서 내릴지 여부</param>
    void ExitLadder(bool withJump)
    {
        isClimbing = false;
        rb.gravityScale = 1.5f;                     // 중력 복원
        animator.SetBool("Climbing", false);

        if (withJump)
        {
            // 점프하면서 사다리에서 내리기 (현재 보는 방향으로)
            float hForce = spriteRenderer.flipX ? -1f : 1f;
            rb.velocity = new Vector2(hForce * movePower * 0.8f, jumpPower * 0.9f);
            jumpCount = 1;
        }
        else
        {
            // 그냥 사다리에서 내리기
            rb.velocity = new Vector2(0, 0);
        }

        StartCoroutine(LadderCooldown()); // 사다리 사용 쿨다운 시작
    }

    /// <summary>
    /// 사다리 사용 쿨다운 (사다리에서 내린 직후 다시 타는 것을 방지)
    /// </summary>
    private System.Collections.IEnumerator LadderCooldown()
    {
        canUseLadder = false;
        yield return new WaitForSeconds(0.2f);
        canUseLadder = true;
    }

    /// <summary>
    /// 사다리 트리거 영역에 계속 머물러 있을 때
    /// </summary>
    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadder = col;
        }
    }

    /// <summary>
    /// 사다리 트리거 영역에서 나갔을 때
    /// </summary>
    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
            if (currentLadder == col)
            {
                float distance = Vector2.Distance(transform.position, col.bounds.center);
                // 일정 거리 이상 떨어지면 사다리에서 완전히 벗어난 것으로 판정
                if (distance > ladderSnapDistance * 2f)
                {
                    isNearLadder = false;
                    currentLadder = null;

                    // 사다리 타는 중이었다면 자동으로 내리기
                    if (isClimbing)
                    {
                        ExitLadder(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 충돌 시작 시 점프 카운트 리셋 처리
    /// 벽에 부딪히거나 지상에 닿으면 점프 카운트를 리셋
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Box") ||
            collision.gameObject.CompareTag("wall"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 위쪽에서 충돌하거나 옆에서 충돌하면 점프 카운트 리셋
                if (contact.normal.y > 0.5f)
                {
                    jumpCount = 0;
                    break;
                }
                else if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    jumpCount = 0;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Scene View에서 디버그 정보를 시각적으로 표시
    /// 지상 체크, 장애물 체크, 박스 상호작용 상태 등을 Gizmo로 표시
    /// </summary>
    void OnDrawGizmos()
    {
        // 머리 위 장애물 체크 레이
        if (headCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(headCheck.position, headCheck.position + Vector3.up * headCheckLength);
        }

        // 꼬리 위 장애물 체크 레이
        if (tailCheck)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tailCheck.position, tailCheck.position + Vector3.up * tailCheckLength);
        }

        // 지상 체크 원
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 박스 상호작용 상태 표시
        if (boxInteraction != null && boxInteraction.IsInteracting)
        {
            Gizmos.color = boxInteraction.IsPushing ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    /// <summary>
    /// 파티클 시스템의 위치와 방향을 플레이어에 맞게 업데이트
    /// </summary>
    private void UpdateParticlePosition()
    {
        if (dashParticle == null) return;

        dashParticle.transform.parent = transform;

        // 플레이어 방향에 따른 파티클 위치 조정
        Vector3 localPos = dashParticleOffset;
        localPos.x = spriteRenderer.flipX ? -dashParticleOffset.x : dashParticleOffset.x;
        dashParticle.transform.localPosition = localPos;

        // 플레이어 방향에 따른 파티클 회전
        dashParticle.transform.localRotation = spriteRenderer.flipX ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        // 파티클 스케일 조정
        Vector3 scale = dashParticle.transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        dashParticle.transform.localScale = scale;
    }

    /// <summary>
    /// 피해를 받았을 때 사운드 재생 (외부에서 호출)
    /// </summary>
    public void PlayHurtSound()
    {
        if (hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }
}