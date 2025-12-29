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
    Rigidbody2D rb;             // 물리 이동 처리
    BoxCollider2D boxCollider;  // 충돌 처리 및 크기 조절 (웅크리기용)
    SpriteRenderer spriteRenderer;  // 스프라이트 방향 전환
    Animator animator;              // 애니메이션 상태 관리
    AudioSource audioSource;        // 사운드 재생

    // 이동 관련 설정값들
    [Header("이동 및 점프")]
    [SerializeField] private float movePower = 2f;      // 기본 이동 속도
    [SerializeField] private float dashPower = 8f;      // 대시 이동 속도
    [SerializeField] private float jumpPower = 7f;      // 점프 힘
    [SerializeField] private float crouchPower = 1f;    // 웅크린 상태 이동 속도
    
    // 특정 상황 시, 점프 불가능
    [SerializeField] private bool isJumpingBlocked = false;
    public bool IsJumpingBlocked
    {
        get => isJumpingBlocked;
        set => isJumpingBlocked = value;
    }
    // 달리기 불가능 변수 추가
    [SerializeField] private bool isRunningBlocked = false;
    public bool IsRunningBlocked
    {
        get => isRunningBlocked;
        set => isRunningBlocked = value;
    }

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
    [SerializeField] private float walkSoundInterval = 0.3f;    // 걷기 소리 간격
    [SerializeField] private float runSoundInterval = 0.2f;     // 달리기 소리 간격
    [SerializeField] private float climbSoundInterval = 0.4f;   // 사다리 소리 간격
    [SerializeField] private float crouchSoundInterval = 0.5f;  // 웅크리기 소리 간격
    [SerializeField] private float landingSoundDelay = 0.3f;    // 착지 후 소리 재생 대기시간

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
    [SerializeField] private float lowJumpMultiplier = 2f;  // 스페이스를 일찍 뗄 때 중력 배수
    private int jumpCount = 0;                              // 현재 점프 횟수 (더블점프 구현용)

    [Header("점프 개선 시스템")]
    [SerializeField] private float jumpBufferTime = 0.15f;  // 점프 입력 버퍼 시간
    [SerializeField] private float coyoteTime = 0.12f;      // 코요테 타임 (플랫폼에서 떨어진 후 점프 가능 시간)
    [SerializeField] private float landingJumpDelay = 0.05f; // 착지 후 점프 가능까지의 최소 딜레이 (기존 0.1초에서 단축)
    private float jumpBufferCounter = 0f;                   // 점프 버퍼 카운터
    private float coyoteTimeCounter = 0f;                   // 코요테 타임 카운터
    private bool isJumpButtonHeld = false;                  // 점프 버튼을 계속 누르고 있는지

    // 벽/박스 점프 개선
    [SerializeField] private float wallJumpBoost = 1.2f;     // 벽 근처 점프 배율
    [SerializeField] private float wallCheckDistance = 0.4f; // 벽 감지 거리
    [SerializeField] private float velocityProtectionTime = 0.15f; // Y축 속도 보호 시간
    private float velocityProtectionCounter = 0f;            // 속도 보호 타이머

    // 지상 감지 시스템
    [Header("지상 체크")]
    [SerializeField] private Transform groundCheck;         // 지상 체크 포인트
    [SerializeField] private float groundCheckRadius = 0.2f; // 지상 체크 반경
    [SerializeField] private LayerMask groundMask;          // 지상으로 인식할 레이어
    [SerializeField] private string floorLayerName = "Floor"; // Ground/Wall처럼 취급할 추가 바닥 레이어
    private int floorLayer = -1;
    private bool isOnGround;                                // 현재 지상에 있는지 여부
    private bool justLanded = false;
    private Vector3 originalGroundCheckLocalPosition;

    // 경사면 시스템
    [Header("경사면")]
    [SerializeField] private float slopeCheckRadius = 0.3f; // 경사면 체크를 위한 별도 반경
    private bool isOnSlope = false;                         // 현재 경사면에 있는지 여부
    [SerializeField] private float slopeExitDelay = 0.15f; // 경사면에서 벗어났다고 판정하기까지의 유예 시간
    private float timeSinceLeftSlope; // 경사면을 마지막으로 감지한 후 흐른 시간

    [Header("경사면 Multi-Raycast 설정")]
    [SerializeField] private float slopeRaycastDistance = 0.5f; // 레이캐스트의 감지 거리
    [SerializeField] private Vector2[] raycastOffsets; // 여러 개의 레이캐스트를 쏠 위치 오프셋 배열

    // 웅크리기 시스템
    [Header("웅크리기")]
    [SerializeField] private string passableTag = "Passable";
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
    [SerializeField] private bool forceCrouch = false;                                      // 외부에서 강제로 웅크리기 상태로 만들기

    // 사다리 시스템
    [Header("사다리")]
    [SerializeField] private float climbSpeed = 2f;         // 사다리 타는 속도
    [SerializeField] private float ladderSnapDistance = 0.3f;   // 사다리 중앙으로 자동 정렬되는 거리
    private Collider2D currentLadder;                       // 현재 근처에 있는 사다리
    private bool isClimbing = false;                        // 현재 사다리를 타고 있는지
    private bool isNearLadder = false;                      // 사다리 근처에 있는지
    private bool canUseLadder = true;                       // 사다리 사용 가능 여부 (쿨다운용)

    // 카트 탑승 시스템
    [Header("카트 상호작용")]
    private bool isOnCart = false;          // 카트에 탑승 중인지
    private Transform currentCart;          // 현재 탑승 중인 카트
    private float originalGravityScale;     // 원본 중력 값 (카트 탑승 시 복원용)

    // 엘리베이터 탑승 시스템
    [Header("엘리베이터 관련")]
    private bool isOnElevator = false;
    private bool elevatorMoving = false;

    // 박스 상호작용 관련
    [Header("박스 상호작용")]
    private PlayerBoxInteraction boxInteraction;    // 박스 밀기/당기기 컴포넌트
    private bool isBoxInteractionEnabled = false;   // 박스 상호작용 활성화 여부
    private bool isDashing = false;                 // 현재 대시 중인지
    [SerializeField] private float boxInteractingPower = 1.2f; // 박스와 상호작용 시 이동속도

    // 입력 차단 시스템 (미니게임, 대화 등에서 사용)
    private bool isMiniGameInputBlocked = false;

    // 웅크림 해제 그레이스(스티키) 설정
    [SerializeField] private float crouchReleaseGrace = 0.12f; // 2~3프레임 정도(60fps 기준)
    private float crouchStickyUntil = -1f;

    // 스티키 활성 여부 보조
    private bool IsCrouchStickyActive => Time.time < crouchStickyUntil;

    // 엔딩 조작 상태
    [Header("엔딩 조작 상태")]
    public bool processingBadEnding = false;

    // 애니메이션 파라미터 해쉬화 부분에 추가
    int _hashClimbDirection = Animator.StringToHash("ClimbDirection");

    /// <summary>
    /// 게임 시작 시 초기화 작업
    /// UI 설정, 컴포넌트 참조, 물리 설정 등을 수행
    /// </summary>
    private void Start()
    {
        // UI 상태 설정 (고양이 버전 UI 활성화, 사람 버전 UI 비활성화)
        UIManager.Instance.SetPlayerVersionUI(UIManager.PlayerType.Cat);

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
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;            // 부드러운 움직임
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;    // 정확한 충돌 감지

        // 파티클 시스템 초기화
        if (dashParticle != null)
        {
            particleEmission = dashParticle.emission;
            particleEmission.rateOverTime = 0f; // 초기에는 파티클 발생하지 않음
        }

        if (groundCheck != null)
        {
            originalGroundCheckLocalPosition = groundCheck.localPosition;
        }

        // Floor 레이어를 groundMask에 자동 포함 (인스펙터에서 누락돼도 동작하도록)
        floorLayer = LayerMask.NameToLayer(floorLayerName);
        if (floorLayer >= 0)
        {
            groundMask |= (1 << floorLayer);
        }
        else
        {
            Debug.LogWarning($"[PlayerCatMovement] 레이어 '{floorLayerName}'를 찾지 못했습니다. (Project Settings > Tags and Layers 확인)");
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

        if (groundCheck != null)
        {
            if (isCrouching || forceCrouch)
            {
                // 웅크렸을 때: 콜라이더가 줄어든 만큼 groundCheck 위치를 아래로 내립니다.
                // originalColliderSize.y - crouchColliderSize.y 는 콜라이더 높이 변화량입니다.
                float yOffset = (originalColliderSize.y - crouchColliderSize.y) * 0.5f;
                groundCheck.localPosition = new Vector3(
                    originalGroundCheckLocalPosition.x,
                    originalGroundCheckLocalPosition.y - yOffset,
                    originalGroundCheckLocalPosition.z
                );
            }
            else
            {
                // 서 있을 때: 원래 위치로 복원합니다.
                groundCheck.localPosition = originalGroundCheckLocalPosition;
            }
        }

        // 지상 감지 (이전 프레임과 비교하여 착지 판정)
        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        // 박스, 벽, 카트 위에 있는지 추가 체크
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        bool onBox = false;

        foreach (Collider2D col in colliders)
        {
            if (col == null || col.gameObject == gameObject) continue;

            // (중요) 태그 기반("wall") 외에도 레이어 기반으로 Ground/Wall/Floor를 지면으로 인정
            bool isGroundLikeLayer = ((groundMask.value & (1 << col.gameObject.layer)) != 0);

            if (isGroundLikeLayer || col.CompareTag("Box") || col.CompareTag("wall") || col.CompareTag("Cart"))
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
        justLanded = isOnGround && !prevOnGround;
        if (justLanded)
        {
            lastLandingTime = Time.time;
        }

        // 착지/지상 판정 시 점프 애니메이션(BOOL) 확실히 종료
        // - 벽/모서리에서 상태가 꼬여 Jump가 true로 남는 문제 방지
        if (isOnGround)
        {
            animator.SetBool(_hashJump, false);
        }

        // 지상에 있고 떨어지는 중이면 점프 카운트 리셋
        if (isOnGround && rb.velocity.y <= 0) 
        {
            jumpCount = 0;
            coyoteTimeCounter = coyoteTime; // 코요테 타임 리셋
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime; // 공중에 있을 때 코요테 타임 감소
        }

        // 점프 버퍼 카운터 감소
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 점프 버튼 입력 감지 (버퍼링용)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        // 점프 버튼을 누르고 있는지 체크 (가변 점프 높이용)
        isJumpButtonHeld = Input.GetKey(KeyCode.Space);

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

        // 👇 여기에 추가!
        // 속도 보호 타이머 감소
        if (velocityProtectionCounter > 0)
        {
            velocityProtectionCounter -= Time.deltaTime;
        }
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
            animator.SetBool("Climbing", false);

            // 입력 차단 중에도, 강제/수동 웅크림이면 상태를 유지
            if (isCrouching || forceCrouch)
            {
                animator.SetBool("Crouching", isCrouchMoving);
                animator.SetBool("Crouch", !isCrouchMoving);
            }
            else
            {
                animator.SetBool("Crouching", false);
                animator.SetBool("Crouch", false);
            }

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

        // 점프 중이면 다른 상태 끔
        if (animator.GetBool("Jump"))
            return;

        // 웅크리기 상태 처리
        if (isCrouching || forceCrouch || isOnSlope) 
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
        isDashing = Input.GetKey(KeyCode.LeftShift) && !(boxInteraction != null && boxInteraction.IsInteracting)
            && !processingBadEnding&& !isRunningBlocked;
        bool isJumping = !isOnGround;

        if (isDashing && hasHorizontalInput)
        {
            animator.SetBool("Dash", true);         // 대시 애니메이션
        }
        else if (isJumping)
        {

        }
        else if (hasHorizontalInput)
        {
            animator.SetBool("Moving", true);       // 일반 이동 애니메이션
        }
    }

    // 애니메이션 파라미터 해쉬화
    int _hashIsGrounded = Animator.StringToHash("IsGrounded");
    int _hashSpeed = Animator.StringToHash("Speed");
    int _hashShift = Animator.StringToHash("Shift");
    int _hashIsClimbing = Animator.StringToHash("Climbing");
    int _hashIsCrouch = Animator.StringToHash("Crouch");
    int _hashIsCrouching = Animator.StringToHash("Crouching");
    int _hashJump = Animator.StringToHash("Jump");

    // 애니메이션 파라미터 동기화
    void SyncAnimatorParams()
    {
        // 입력 차단/미니게임 때는 깔끔하게 0/false로
        bool blocked = IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving");

        // --- 입력 차단 중 처리: 웅크림만 유지하고 즉시 반환 ---
        if (blocked)
        {
            animator.SetBool(_hashIsGrounded, false);
            animator.SetFloat(_hashSpeed, 0f);
            animator.SetBool(_hashShift, false);
            animator.SetBool(_hashIsClimbing, false);
            animator.SetBool(_hashJump, false);

            // 강제/수동 웅크림 유지: isCrouchMoving에 따라 Crouching/Crouch 상호배타
            bool crouchAny = (isCrouching || forceCrouch);
            animator.SetBool(_hashIsCrouching, crouchAny && isCrouchMoving);
            animator.SetBool(_hashIsCrouch, crouchAny && !isCrouchMoving);
            return;
        }

        // 대시 키: Shift (박스 상호작용/웅크림/사다리 중엔 false)
        bool shiftDown = Input.GetKey(KeyCode.LeftShift)
                                 && !isCrouching
                                 && !isClimbing
                                 && !blocked
                                 && !isRunningBlocked;

        // 속도: 입력 기반이 전이 안정적 (물리 미끄러짐 영향 적음)
        float hInput = blocked ? 0f : Mathf.Abs(Input.GetAxisRaw("Horizontal"));
        float speedParam = hInput;

        // 공통 파라미터
        animator.SetBool(_hashIsGrounded, isOnGround && !blocked);
        animator.SetFloat(_hashSpeed, speedParam);
        animator.SetBool(_hashShift, shiftDown);
        animator.SetBool(_hashIsClimbing, isClimbing && !blocked);

        // ✨ 새로 추가: 사다리 타는 중일 때 방향 정보 업데이트
        if (isClimbing && !blocked)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");
            animator.SetFloat(_hashClimbDirection, verticalInput);
        }
        else
        {
            animator.SetFloat(_hashClimbDirection, 0f);
        }

        // 웅크림 상태 계산
        bool jumpAnim = animator.GetBool(_hashJump);

        bool inCrouchMode =
           (
               // 일반적인 웅크리기(수동, 장애물)는 땅 위에 있어야 함
               (isCrouching || forceCrouch) && isOnGround ||
               // 하지만 경사면 위에서는 땅 체크와 상관없이 항상 웅크림 모드
               isOnSlope
           ) &&
           !isClimbing &&
           !blocked &&
           !jumpAnim;

        bool crouchMoving = inCrouchMode && (speedParam > 0.01f);
        bool crouchIdle = inCrouchMode && !crouchMoving;

        animator.SetBool(_hashIsCrouching, crouchMoving);
        animator.SetBool(_hashIsCrouch, crouchIdle);

        isCrouchMoving = crouchMoving;
    }

    /// <summary>
    /// Update 이후에 애니메이션 상태를 한 번 더 동기화
    /// 물리 업데이트와 애니메이션이 정확히 맞도록 보장
    /// </summary>
    void LateUpdate()
    {
        UpdateAnimationState(Input.GetAxisRaw("Horizontal"));

        // 애니메이션 파라미터 동기화
        SyncAnimatorParams();
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

        CheckSlope();

        //if (!isOnSlope)
        //{
        //    transform.rotation = Quaternion.identity;
        //}

        // 카트에 타지 않았을 때만 일반적인 움직임 처리
        if (!isOnCart)
        {
            if (!isClimbing)
            {
                Move();             // 기본 이동 처리

                // 👇 여기에 추가! (Move() 바로 다음)
                // 벽 점프 직후 Y축 속도 보호 (벽 충돌로 인한 속도 감소 방지)
                if (velocityProtectionCounter > 0 && rb.velocity.y > 0)
                {
                    // 점프 직후 최소 속도 보장 (점프력의 80% 이상 유지)
                    float minVelocityY = jumpPower * wallJumpBoost * 0.8f;
                    
                    if (rb.velocity.y < minVelocityY)
                    {
                        // 속도가 너무 감소했으면 복원
                        rb.velocity = new Vector2(rb.velocity.x, minVelocityY);
                    }
                }

                BetterJump();       // 점프 중력 보정
                HandleSound();      // 이동 사운드 처리
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
    /// 엘리베이터 탑승 상태를 설정합니다.
    /// </summary>
    public void SetOnElevator(bool onElevator, bool isMoving)
    {
        isOnElevator = onElevator;
        elevatorMoving = isMoving;
        
        if (onElevator && isMoving)
        {
            // 엘리베이터가 이동 중이면 자동으로 앉기
            ForceCrouch = true;
            Debug.Log("엘리베이터 이동 중 - 자동 앉기 활성화");
        }
        else if (onElevator && !isMoving)
        {
            // 엘리베이터가 멈추면 다시 움직일 수 있음
            ForceCrouch = false;
            Debug.Log("엘리베이터 정지 - 이동 가능");
        }
        else
        {
            // 엘리베이터에서 내렸을 때
            ForceCrouch = false;
            Debug.Log("엘리베이터 하차");
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
                                      animator.GetBool("Crouch") ||
                                      animator.GetBool("Jump");

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
                // 해제 직후 기본은 스티키 걸어서 튐 방지
                crouchStickyUntil = Time.time + crouchReleaseGrace;

                // 첫 프레임엔 웅크림 유지(한 프레임 튐 방지용)
                isCrouching = true;
                isCrouchMoving = false;
                boxCollider.size = crouchColliderSize;
                boxCollider.offset = crouchColliderOffset;

                // 해제 프레임 즉시 머리 위 장애물 체크
                bool obstacleAboveNow = IsObstacleDirectlyAbove();

                if (!obstacleAboveNow)
                {
                    // 장애물 없음: 스티키 건너뛰고 즉시 해제(자연스럽게 일어서기)
                    crouchStickyUntil = -1f; // 스티키 무효화
                    isCrouching = false;
                    boxCollider.size = originalColliderSize;
                    boxCollider.offset = originalColliderOffset;

                    // 즉시 애니메이터도 맞춰주고 싶다면(선택):
                    animator.SetBool("Crouching", false);
                    animator.SetBool("Crouch", false);
                }
                else
                {
                    // 장애물 있음: 스티키 유지(계속 엎드림)
                    // 필요하면 스티키를 약간 더 길게
                    crouchStickyUntil = Time.time + Mathf.Max(crouchReleaseGrace, 0.15f);
                    animator.SetBool("Crouching", false);
                    animator.SetBool("Crouch", true);
                }
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
        // Raycast로 머리와 꼬리 위를 각각 체크
        RaycastHit2D headHit = Physics2D.Raycast(headCheck.position, Vector2.up, headCheckLength, obstacleMask);
        RaycastHit2D tailHit = Physics2D.Raycast(tailCheck.position, Vector2.up, tailCheckLength, obstacleMask);

        // 머리 위에 무언가 감지되었고, 그것이 '통과 가능' 태그가 아닐 경우 => 장애물임
        if (headHit.collider != null && !headHit.collider.CompareTag(passableTag))
        {
            return true;
        }

        // 꼬리 위에 무언가 감지되었고, 그것이 '통과 가능' 태그가 아닐 경우 => 장애물임
        if (tailHit.collider != null && !tailHit.collider.CompareTag(passableTag))
        {
            return true;
        }

        // 위 두 경우에 해당하지 않으면 (아무것도 없거나, '통과 가능' 태그만 있을 경우) => 장애물이 아님
        return false;
    }

    /// <summary>
    /// 웅크리기 입력 및 상태 처리
    /// 자동 웅크리기 (장애물 감지), 수동 웅크리기 (S키), 웅크리기 해제 등을 처리
    /// </summary>
    void HandleCrouch(bool justLanded)
    {
        bool obstacleAbove = IsObstacleDirectlyAbove();
        bool playerHoldsCrouchKey = Input.GetKey(KeyCode.S);

        // 스티키가 활성화되어 있으면 머리 장애물 여부와 무관하게 '계속 웅크림'
        if (IsCrouchStickyActive)
            obstacleAbove = true;

        // 웅크려야 하는 모든 조건
        bool shouldBeCrouching = isOnSlope || obstacleAbove || (playerHoldsCrouchKey && isOnGround);

        // 위 조건에 따라 현재 캐릭터의 웅크리기 상태를 동기화합니다.
        if (shouldBeCrouching)
        {
            // 웅크려야 하는데, 웅크리고 있지 않다면 -> 웅크리기 시작
            if (!isCrouching)
            {
                isCrouching = true;
                boxCollider.size = crouchColliderSize;
                boxCollider.offset = crouchColliderOffset;
            }
        }
        else
        {
            // 웅크릴 필요가 없는데, 웅크리고 있다면 -> 웅크리기 해제
            if (isCrouching)
            {
                isCrouching = false;
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;
            }
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

        if (isMiniGameInputBlocked || forceCrouch || (isOnElevator && elevatorMoving))
        {
            horizontalInput = 0;
        }

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
        else if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isInteractingWithBox&& !processingBadEnding && !isRunningBlocked)
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
/// 점프 입력 처리 - 개선 버전
/// - 점프 버퍼링: 착지 전 입력을 기억
/// - 코요테 타임: 플랫폼에서 떨어진 직후에도 점프 가능
/// - 착지 딜레이 단축: 0.1초 → 0.05초
/// </summary>
    void Jump()
    {
            // 착지 직후 짧은 딜레이 (0.05초로 단축)
        if (Time.time - lastLandingTime < landingJumpDelay)
        {
            return;
        }

        if (IsInputBlocked()) return;
        if (isJumpingBlocked) return; // 경사면 조건은 아래에서 따로 처리
        if (isCrouching || isClimbing) return;

        // 점프 가능 조건 판정 (코요테 타임 포함)
        bool canJumpFromGround = (isOnGround || coyoteTimeCounter > 0) && !isOnSlope;
        bool canDoubleJump = jumpCount < 2;

        // 점프 버퍼가 활성화되어 있고, 점프 가능한 상태라면 점프 실행
        if (jumpBufferCounter > 0 && (canJumpFromGround || canDoubleJump))
        {
            PerformJump();
            jumpBufferCounter = 0; // 버퍼 소모
            coyoteTimeCounter = 0; // 코요테 타임 소모
        }
    }

    /// <summary>
    /// 실제 점프 실행 (중복 코드 제거용 헬퍼 함수)
    /// </summary>
    void PerformJump()
    {
        // 벽/박스 근처에 있는지 체크
        bool isNearWall = CheckNearWall();
        
        // y축 속도를 완전히 0으로 리셋 (벽 충돌 간섭 제거)
        rb.velocity = new Vector2(rb.velocity.x, 0);
        
        // 벽 근처에서는 점프력 강화
        float actualJumpPower = isNearWall ? jumpPower * wallJumpBoost : jumpPower;
        
        // 점프 힘 적용
        rb.AddForce(Vector2.up * actualJumpPower, ForceMode2D.Impulse);
        
        jumpCount++;
        isOnGround = false;
        
        // 👇 벽 근처에서 점프하면 속도 보호 시작!
        if (isNearWall)
        {
            velocityProtectionCounter = velocityProtectionTime;
        }

        animator.SetBool(_hashJump, true);

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

    /// <summary>
    /// 플레이어가 벽이나 박스 근처에 있는지 체크
    /// </summary>
    bool CheckNearWall()
    {
        // 플레이어 중심 위치
        Vector2 playerCenter = transform.position;
        
        // 좌우 양쪽 체크 (BoxCast 사용 - 더 정확한 감지)
        Vector2 checkSize = new Vector2(0.1f, boxCollider.size.y * 0.8f);
        
        // 왼쪽 체크
        RaycastHit2D leftCheck = Physics2D.BoxCast(
            playerCenter,
            checkSize,
            0f,
            Vector2.left,
            wallCheckDistance,
            groundMask
        );
        
        // 오른쪽 체크
        RaycastHit2D rightCheck = Physics2D.BoxCast(
            playerCenter,
            checkSize,
            0f,
            Vector2.right,
            wallCheckDistance,
            groundMask
        );
        
        // Box 태그 오브젝트도 추가로 체크
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(
            playerCenter, 
            wallCheckDistance
        );
        
        foreach (Collider2D col in nearbyColliders)
        {
            // 자기 자신은 제외
            if (col.gameObject == gameObject) continue;
            
            if (col.CompareTag("Box") || col.CompareTag("wall"))
            {
                return true;
            }
        }
        
        return leftCheck.collider != null || rightCheck.collider != null;
    }

    /// <summary>
    /// 점프 물리 개선 - 가변 점프 높이 및 자연스러운 낙하 구현
    /// - 스페이스를 일찍 떼면 점프가 낮아짐
    /// - 떨어질 때 중력 증가로 자연스러운 점프감 구현
    /// </summary>
    void BetterJump()
    {
        if (isClimbing) return;

        // 떨어지는 중일 때 중력을 더 강하게 적용
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // 올라가는 중인데 스페이스를 떼면 점프를 낮게 (가변 점프 높이)
        else if (rb.velocity.y > 0 && !isJumpButtonHeld)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
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

        // ✨ 새로 추가: 애니메이션 방향 설정
        animator.SetFloat(_hashClimbDirection, verticalInput);

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
        rb.gravityScale = 1.5f;                 // 중력 복원
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
        // Ground/Wall/Floor 레이어도 지면으로 인정 (애니메이션/점프 카운트 리셋 안정화)
        bool isGroundLikeLayer = ((groundMask.value & (1 << collision.gameObject.layer)) != 0);

        if (isGroundLikeLayer ||
            collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Box") ||
            collision.gameObject.CompareTag("wall"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // contact.normal.y > 0.5f는 캐릭터 아래에 바닥이 있다는 뜻
                if (contact.normal.y > 0.5f)
                {
                    jumpCount = 0;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 충돌이 지속되는 동안 - 점프 중 벽과의 충돌 처리
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 속도 보호 중이고 점프 중일 때만 작동
        if (velocityProtectionCounter > 0 && rb.velocity.y > 0)
        {
            // 벽이나 박스와 충돌 중인지 확인
            if (collision.gameObject.CompareTag("wall") || 
                collision.gameObject.CompareTag("Box"))
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    // 옆면 충돌인지 확인 (수평 방향 충돌)
                    if (Mathf.Abs(contact.normal.x) > 0.7f)
                    {
                        // Y축 속도만 유지하고 X축 속도는 약간 감소
                        float protectedYVelocity = rb.velocity.y;
                        float reducedXVelocity = rb.velocity.x * 0.5f;
                        
                        rb.velocity = new Vector2(reducedXVelocity, protectedYVelocity);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 여러 개의 Raycast를 사용해 발밑의 경사면을 종합적으로 판단하고 부드럽게 회전합니다.
    /// 경사면 끝에서 발생하는 상태 깜빡임(jittering) 현상을 개선하고, 경사면에서 웅크리기 애니메이션이 정상적으로 재생되도록 수정합니다.
    /// </summary>
    void CheckSlope()
    {
        int hitsFound = 0;
        Vector2 combinedNormal = Vector2.zero;
        bool foundActualSlope = false; // 실제 기울어진 경사면을 찾았는지

        // 모든 오프셋에서 Ground 레이어(경사면 포함)를 체크
        foreach (var offset in raycastOffsets)
        {
            Vector2 castOrigin = (Vector2)transform.position + offset;
            RaycastHit2D hit = Physics2D.Raycast(castOrigin, Vector2.down, slopeRaycastDistance, groundMask); // slopeLayer 대신 groundMask 사용

            if (hit.collider != null)
            {
                combinedNormal += hit.normal;
                hitsFound++;

                // 실제로 기울어진 면인지 체크 (5.7도 이상)
                if (Mathf.Abs(hit.normal.x) > 0.1f)
                {
                    foundActualSlope = true;
                }
            }
        }

        if (hitsFound > 0 && foundActualSlope)
        {
            Vector2 averageNormal = combinedNormal / hitsFound;

            // 경사면 상태로 확정
            isOnSlope = true;
            timeSinceLeftSlope = 0;

            // 부드러운 회전 적용 (튀는 현상 방지)
            float slopeAngle = Vector2.SignedAngle(Vector2.up, averageNormal);
            Quaternion targetRotation = Quaternion.Euler(0, 0, slopeAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 8f);
            return;
        }

        // 경사면을 벗어날 때 부드러운 전환
        if (isOnSlope)
        {
            timeSinceLeftSlope += Time.fixedDeltaTime;
            if (timeSinceLeftSlope > slopeExitDelay)
            {
                isOnSlope = false;
                // 부드럽게 수직으로 복귀
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * 10f);
            }
        }
        else
        {
            // 경사면이 아닐 때는 즉시 수직으로
            if (transform.rotation != Quaternion.identity)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * 10f);
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

        // 지상 체크 원 및 경사면 체크 원
        if (groundCheck)
        {
            // 지상 체크 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            // 경사면 체크 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, slopeCheckRadius);
        }

        if (Application.isPlaying && raycastOffsets != null)
        {
            Gizmos.color = Color.green; // 잘 보이는 초록색으로 변경
            foreach (var offset in raycastOffsets)
            {
                Vector2 castOrigin = (Vector2)transform.position + offset;
                Gizmos.DrawLine(castOrigin, castOrigin + Vector2.down * slopeRaycastDistance);
            }
        }

        // 박스 상호작용 상태 표시
        if (boxInteraction != null && boxInteraction.IsInteracting)
        {
            Gizmos.color = boxInteraction.IsPushing ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // 👇 여기에 새로 추가!
        // 벽/박스 감지 범위 표시 (게임 실행 중에만)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            
            Vector2 playerCenter = transform.position;
            Vector2 checkSize = new Vector2(0.1f, boxCollider != null ? boxCollider.size.y * 0.8f : 1f);
            
            // 좌우 감지 범위 표시
            Gizmos.DrawWireCube(playerCenter + Vector2.left * wallCheckDistance, checkSize);
            Gizmos.DrawWireCube(playerCenter + Vector2.right * wallCheckDistance, checkSize);
            
            // 감지 원 표시
            Gizmos.DrawWireSphere(playerCenter, wallCheckDistance);
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

    public void StopDashParticle()
    {
        particleEmission.rateOverTime = 0f;
        if (dashParticle.particleCount == 0)
        {
            dashParticle.Stop();
        }
    }

    public void UpdateAnimationCrouch()
    {
        // 숨는 중/락 중에는 이동/대시 꺼두고, 쭈그림/기어감만 유지
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        if (isCrouchMoving)
        {
            animator.SetBool("Crouching", true);
            animator.SetBool("Crouch", false);
        }
        else
        {
            animator.SetBool("Crouch", ForceCrouch);
            animator.SetBool("Crouching", false);
        }
        StopDashParticle();
    }

    /// <summary>
    /// 달리기 활성화/비활성화 설정
    /// </summary>
    public void SetRunEnabled(bool enabled)
    {
        isRunningBlocked = !enabled;
        Debug.Log($"[PlayerCatMovement] 달리기 {(enabled ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 점프 활성화/비활성화 설정
    /// </summary>
    public void SetJumpEnabled(bool enabled)
    {
        isJumpingBlocked = !enabled;
        Debug.Log($"[PlayerCatMovement] 점프 {(enabled ? "활성화" : "비활성화")}");
    }
}