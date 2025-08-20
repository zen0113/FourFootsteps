using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatMovement : MonoBehaviour
{
    public static PlayerCatMovement Instance { get; private set; }

    // 컴포넌트
    Rigidbody2D rb;
    BoxCollider2D boxCollider;
    SpriteRenderer spriteRenderer;
    Animator animator;
    AudioSource audioSource;

    // 이동/점프
    [Header("이동 및 점프")]
    [SerializeField] private float movePower = 2f;
    [SerializeField] private float dashPower = 8f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float crouchPower = 1f;

    [Header("파티클 시스템")]
    [SerializeField] private ParticleSystem dashParticle; // 대시 파티클 시스템
    [SerializeField] private Vector3 dashParticleOffset = new Vector2(0.5f, 0f); // 파티클 오프셋
    [SerializeField] private float walkEmissionRate = 3f; // 걷기 시 파티클 발생률
    [SerializeField] private float runEmissionRate = 6f;  // 달리기 시 파티클 발생률
    private ParticleSystem.EmissionModule particleEmission;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip hurtSound; // 다칠 때 소리
    [SerializeField] private AudioClip walkSound; // 걷기 소리
    [SerializeField] private AudioClip runSound;  // 달리기 소리
    [SerializeField] private AudioClip jumpSound; // 점프 소리
    [SerializeField] private AudioClip crouchSound; // 웅크리기 소리
    [SerializeField] private AudioClip climbSound; // 사다리 오르기 소리
    [SerializeField] private float walkSoundInterval = 0.3f; // 걷기 소리 재생 간격
    [SerializeField] private float runSoundInterval = 0.2f;  // 달리기 소리 재생 간격
    [SerializeField] private float climbSoundInterval = 0.4f; // 사다리 오르기 소리 재생 간격
    [SerializeField] private float crouchSoundInterval = 0.5f; // 웅크리기 소리 재생 간격
    [SerializeField] private float landingSoundDelay = 0.3f; // 착지 후 소리 재생까지 대기 시간
    private float lastWalkSoundTime; // 마지막 걷기 소리 재생 시간
    private float lastRunSoundTime;  // 마지막 달리기 소리 재생 시간
    private float lastClimbSoundTime; // 마지막 사다리 오르기 소리 재생 시간
    private float lastCrouchSoundTime; // 마지막 웅크리기 소리 재생 시간
    private float lastLandingTime; // 마지막 착지 시간
    private int lastMoveDirection = 0; // 이전 이동 방향 (1: 오른쪽, -1: 왼쪽, 0: 정지)

    // 점프 중력 보정
    [Header("점프 중력 보정")]
    [SerializeField] private float fallMultiplier = 2.5f;
    private int jumpCount = 0;

    // 지상 체크
    [Header("지상 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    private bool isOnGround;

    [SerializeField] private float boxInteractingPower = 1.2f; // 박스 상호작용 시 이동 속도

    // 웅크리기
    [Header("웅크리기")]
    [SerializeField] private Transform headCheck;
    [SerializeField] private Transform tailCheck;
    [SerializeField] private float headCheckLength;
    [SerializeField] private float tailCheckLength;
    [SerializeField] private Sprite crouchSprite;
    private Sprite originalSprite;
    [SerializeField] private LayerMask obstacleMask;
    private bool isCrouching = false;
    private bool isCrouchMoving = false;  // 웅크린 상태에서 이동 중인지 여부
    private Vector2 originalColliderSize, originalColliderOffset, crouchColliderSize, crouchColliderOffset;
    private bool forceCrouch = false; // 외부에서 강제 웅크리기

    // 사다리
    [Header("사다리")]
    [SerializeField] private float climbSpeed = 2f;
    [SerializeField] private float ladderSnapDistance = 0.3f; // 사다리 중앙으로 스냅되는 거리
    private Collider2D currentLadder;
    private bool isClimbing = false;
    private bool isNearLadder = false;
    private bool canUseLadder = true; // 사다리 사용 가능 여부

    [Header("카트 상호작용")]
    private bool isOnCart = false; // 카트에 탑승 중인지 여부
    private Cart currentCart; // 현재 탑승 중인 카트

    // 박스 상호작용 관련
    private PlayerBoxInteraction boxInteraction;
    private bool isBoxInteractionEnabled = false;
    private bool isDashing = false;

    // 미니게임 입력 차단용 플래그
    private bool isMiniGameInputBlocked = false;

    private void Start()
    {
        // 사람 버전 UI 그룹 비활성화
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);

        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
        boxInteraction = GetComponent<PlayerBoxInteraction>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        originalSprite = spriteRenderer.sprite;  // 기본 스프라이트 저장
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;
        crouchColliderSize = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
        crouchColliderOffset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f);
        // 물리 설정 최적화
        rb.freezeRotation = true; // 회전 고정으로 안정성 확보
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 움직임
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 정확한 충돌 감지

        if (dashParticle != null)
        {
            particleEmission = dashParticle.emission;
            particleEmission.rateOverTime = 0f; // 초기에는 파티클 발생 안 함
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // 입력이 차단되거나 이동 불가능 상태일 때
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            UpdateAnimationState(0);
            return;
        }

        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        // 박스 위에 있는지 추가 체크 (태그 기반)
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

        bool justLanded = isOnGround && !prevOnGround;
        if (justLanded)
        {
            lastLandingTime = Time.time; // 착지 시간 기록
        }

        if (isOnGround && rb.velocity.y <= 0) jumpCount = 0;

        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 이동 방향에 따른 스프라이트 방향 설정
        if (!isClimbing) // 카트 탑승 중에는 방향 전환하지 않도록 추가
        {
            if (horizontalInput != 0)
            {
                if (!(boxInteraction != null && boxInteraction.IsPulling))
                {
                    spriteRenderer.flipX = horizontalInput < 0;
                }
                else
                {
                    bool isBoxOnRight = boxInteraction.CurrentBox != null &&
                                             boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                    spriteRenderer.flipX = !isBoxOnRight;
                }
            }
        }

        HandleLadderInput();
        if (!isClimbing) Jump();
        HandleCrouch(justLanded);
    }

    void UpdateAnimationState(float horizontalInput)
    {
        // 입력 차단 상태 (대화, 씬 로딩, 미니게임)
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

            // 모든 다른 애니메이션 상태를 false로 설정
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Crouch", false);

            // 사다리와 충돌 중일 때는 항상 Climbing을 true로 유지
            // 움직이는지 여부에 따라 애니메이션 속도만 조절
            animator.SetBool("Climbing", true);

            // 애니메이션 속도로 움직임 표현 (선택사항)
            if (isClimbingMoving)
            {
                animator.speed = 1f; // 정상 속도
            }
            else
            {
                animator.speed = 0f; // 애니메이션 일시정지로 정지 상태 표현
            }

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

        bool isActuallyMoving = Mathf.Abs(rb.velocity.x) > 0.01f;

        // 웅크리기 상태
        if (isCrouching || forceCrouch)
        {
            if (isActuallyMoving)
            {
                animator.SetBool("Crouching", true);
            }
            else
            {
                animator.SetBool("Crouch", true);
            }
            return;
        }

        // 그 외 상태 (이동, 점프, 대시)
        isDashing = Input.GetKey(KeyCode.LeftShift) && !(boxInteraction != null && boxInteraction.IsInteracting);
        bool isJumping = !isOnGround;

        if (isDashing)
        {
            animator.SetBool("Dash", true);
        }
        else if (isJumping)
        {
            // 점프 중에는 Dash 애니메이션이 아닌 Jump 애니메이션을 사용한다고 가정
            // animator.SetBool("Jump", true); // 필요하다면 이 부분을 추가하세요.
        }
        else if (isActuallyMoving)
        {
            animator.SetBool("Moving", true);
        }
    }


    void LateUpdate()
    {
        // 물리 업데이트 후 애니메이션 상태 동기화
        UpdateAnimationState(Input.GetAxisRaw("Horizontal"));
    }

    void FixedUpdate()
    {
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            rb.velocity = Vector2.zero;
            if (dashParticle != null)
            {
                particleEmission.rateOverTime = 0f;
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 사다리 타는 중에는 Move, BetterJump, HandleSound 호출하지 않음
        if (!isClimbing)
        {
            Move();
            BetterJump();
            HandleSound();
            UpdateParticleState(); // 사다리 타지 않을 때만 파티클 업데이트
        }
        else
        {
            Climb();
            if (dashParticle != null)
            {
                particleEmission.rateOverTime = 0f;
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void UpdateParticleState()
    {
        if (dashParticle == null) return;

        // 입력이 차단된 상태면 파티클 끔
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            particleEmission.rateOverTime = 0f;
            if (dashParticle.isPlaying)
            {
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 모든 애니메이션이 꺼져있는 상태 체크
        bool isAnyAnimationActive = animator.GetBool("Moving") ||
                                     animator.GetBool("Dash") ||
                                     animator.GetBool("Crouching") ||
                                     animator.GetBool("Climbing") ||
                                     animator.GetBool("Crouch");

        bool isActuallyMoving = Mathf.Abs(rb.velocity.x) > 0.01f;
        bool isJumping = !isOnGround;

        // 사다리 타는 중이거나 모든 애니메이션이 꺼져있거나 멈춰있거나 웅크리는 중이면 파티클 끔
        if (isClimbing || !isAnyAnimationActive || (!isActuallyMoving && !isJumping))
        {
            particleEmission.rateOverTime = 0f;
            if (dashParticle.isPlaying)
            {
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        else
        {
            float currentRate = isDashing ? runEmissionRate : walkEmissionRate;
            particleEmission.rateOverTime = currentRate;

            if (!dashParticle.isPlaying)
            {
                dashParticle.Play();
            }

            UpdateParticlePosition();
        }
    }

    public void SetMiniGameInputBlocked(bool blocked)
    {
        isMiniGameInputBlocked = blocked;
        Debug.Log($"[PlayerCatMovement] 미니게임 입력 차단 설정: {blocked}");
    }

    public void SetCrouchMovingState(bool moving)
    {
        isCrouchMoving = moving;
        Debug.Log($"[PlayerCatMovement] 웅크리기 이동 상태 설정: {moving}");
        //}
        // 상태가 변경되면 즉시 애니메이션 업데이트
        if (forceCrouch || isCrouching)
        {
            if (moving)
            {
                animator.SetBool("Crouching", true);
                animator.SetBool("Crouch", false);
                Debug.Log("[PlayerCatMovement] 즉시 Crouching 애니메이션 활성화");
            }
            else
            {
                animator.SetBool("Crouch", true);
                animator.SetBool("Crouching", false);
                Debug.Log("[PlayerCatMovement] 즉시 Crouch 애니메이션 활성화");
            }
        }
    }

    // 강제 상태 프로퍼티
    public bool ForceCrouch
    {
        get { return forceCrouch; }
        set
        {
            forceCrouch = value;
            Debug.Log($"[PlayerCatMovement] 강제 웅크리기 설정: {value}");

            if (forceCrouch)
            {
                isCrouching = true;
                isCrouchMoving = false;
                boxCollider.size = crouchColliderSize;
                boxCollider.offset = crouchColliderOffset;
            }
            else
            {
                isCrouching = false;
                isCrouchMoving = false;
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;
            }
        }
    }

    // 다이얼로그 출력 중, 씬 로딩 중이면 입력을 받지 않음.
    bool IsInputBlocked()
    {
        return PauseManager.IsGamePaused
             || DialogueManager.Instance.isDialogueActive
             || (GameManager.Instance != null && GameManager.Instance.IsSceneLoading)
             || isMiniGameInputBlocked;
    }

    void HandleLadderInput()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 사다리 근처에서 위/아래 키를 누르면 사다리 타기 시작
        if (isNearLadder && !isClimbing && canUseLadder)
        {
            // 위 키를 눌렀을 때: 사다리 아래쪽이나 중간에서 올라가기
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                StartClimbing();
            }
            // 아래 키를 눌렀을 때: 사다리 위쪽에서 내려가기 (지상에서만)
            else if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isOnGround)
            {
                StartClimbing();
            }
        }
        // 플랫폼 위에서 아래 키 + Shift 키로 사다리 타기 (메이플 방식)
        else if (!isClimbing && canUseLadder && isOnGround && isNearLadder)
        {
            if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) &&
                Input.GetKeyDown(KeyCode.LeftShift))
            {
                // 플랫폼을 무시하고 사다리로 진입
                StartClimbingFromPlatform();
            }
        }
        // 사다리 타는 중
        else if (isClimbing)
        {
            // 점프 키로 사다리에서 뛰어내리기
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExitLadder(true);
            }
            // 좌우 이동키로 사다리에서 내려오기
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                     Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                ExitLadder(false);
            }
        }
    }

    bool IsObstacleDirectlyAbove()
    {
        return Physics2D.Raycast(headCheck.position, Vector2.up, headCheckLength, obstacleMask) ||
               Physics2D.Raycast(tailCheck.position, Vector2.up, tailCheckLength, obstacleMask);
    }

    void HandleCrouch(bool justLanded)
    {
        bool obstacleAbove = IsObstacleDirectlyAbove();
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        if (obstacleAbove && !isCrouching && isOnGround)
        {
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }

        if (Input.GetKeyDown(KeyCode.S) && isOnGround)
        {
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }
        else if (Input.GetKeyUp(KeyCode.S) && !obstacleAbove)
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        if (!obstacleAbove && isCrouching && !Input.GetKey(KeyCode.S))
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }
    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

        if (isMiniGameInputBlocked || forceCrouch || isOnCart)
        {
            horizontalInput = 0;
        }

        bool isInteractingWithBox = boxInteraction != null && boxInteraction.IsInteracting;
        bool isPullingBox = boxInteraction != null && boxInteraction.IsPulling;

        if (isInteractingWithBox && isBoxInteractionEnabled)
        {
            currentPower = boxInteractingPower;
            if (isPullingBox && boxInteraction.CurrentBox != null)
            {
                bool isBoxOnRight = boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                spriteRenderer.flipX = !isBoxOnRight;
            }
        }
        else if (isCrouching)
        {
            currentPower = crouchPower;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isInteractingWithBox)
        {
            currentPower = dashPower;
        }

        float targetVelocityX = horizontalInput * currentPower;
        float smoothedVelocityX = Mathf.Lerp(rb.velocity.x, targetVelocityX, Time.fixedDeltaTime * 10f);

        rb.velocity = new Vector2(smoothedVelocityX, rb.velocity.y);
    }

    void HandleSound()
    {
        if (isClimbing || Mathf.Abs(rb.velocity.x) < 0.1f || !isOnGround)
        {
            // 움직임이 멈추면 모든 발소리 관련 사운드 정지
            audioSource.Stop();
            return;
        }

        if (Time.time - lastLandingTime < landingSoundDelay)
        {
            return;
        }

        // 웅크리기 상태일 때
        if (isCrouching)
        {
            if (Time.time - lastCrouchSoundTime >= crouchSoundInterval)
            {
                audioSource.PlayOneShot(crouchSound);
                lastCrouchSoundTime = Time.time;
            }
        }
        // 대시 상태일 때
        else if (isDashing)
        {
            if (Time.time - lastRunSoundTime >= runSoundInterval)
            {
                audioSource.PlayOneShot(runSound);
                lastRunSoundTime = Time.time;
            }
        }
        // 일반 이동 상태일 때
        else
        {
            if (Time.time - lastWalkSoundTime >= walkSoundInterval)
            {
                audioSource.PlayOneShot(walkSound);
                lastWalkSoundTime = Time.time;
            }
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isCrouching && !isClimbing)
        {
            if (isOnGround || jumpCount < 2)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpCount++;
                isOnGround = false;

                if (dashParticle != null)
                {
                    UpdateParticlePosition();
                    if (!dashParticle.isPlaying)
                    {
                        dashParticle.Play();
                    }
                    particleEmission.rateOverTime = runEmissionRate;
                }

                if (jumpSound != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(jumpSound);
                }
            }
        }
    }

    void BetterJump()
    {
        if (isClimbing) return;

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    // Climb 메서드 수정
    void Climb()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float moveY = verticalInput * climbSpeed;

        // 사다리 경계 체크
        if (currentLadder != null)
        {
            float ladderTop = currentLadder.bounds.max.y;
            float ladderBottom = currentLadder.bounds.min.y;

            // 사다리 위쪽에서 더 올라가려고 할 때만 사다리에서 나가기
            if (transform.position.y >= ladderTop - 0.1f && verticalInput > 0)
            {
                // 사다리 위로 올라가기 (지면으로 이동)
                Vector3 exitPosition = new Vector3(transform.position.x, ladderTop + 0.5f, transform.position.z);
                transform.position = exitPosition;
                ExitLadder(false);
                return;
            }

            // 사다리 아래쪽에서 더 내려가려고 할 때만 사다리에서 나가기
            if (transform.position.y <= ladderBottom + 0.1f && verticalInput < 0)
            {
                ExitLadder(false);
                return;
            }

            // 사다리 경계 내에서는 자유롭게 위아래 이동 가능
            // Y 위치를 사다리 경계 내로 제한
            float clampedY = Mathf.Clamp(transform.position.y + moveY * Time.fixedDeltaTime,
                                         ladderBottom + 0.2f, ladderTop - 0.2f);

            // 경계에 도달했을 때는 움직임 제한
            if ((transform.position.y >= ladderTop - 0.2f && verticalInput > 0) ||
                (transform.position.y <= ladderBottom + 0.2f && verticalInput < 0))
            {
                moveY = 0;
            }
        }

        rb.velocity = new Vector2(0, moveY);

        // 사다리 오르기 효과음 재생
        if (Mathf.Abs(verticalInput) > 0.01f && climbSound != null && Time.time - lastClimbSoundTime >= climbSoundInterval)
        {
            audioSource.PlayOneShot(climbSound);
            lastClimbSoundTime = Time.time;
        }
    }

    // StartClimbingFromPlatform 메서드 추가 (플랫폼에서 사다리로 진입)
    void StartClimbingFromPlatform()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        jumpCount = 0;

        // 사다리 중앙으로 스냅하되, Y축은 약간 아래로 이동
        Vector3 pos = transform.position;
        pos.x = currentLadder.bounds.center.x;
        pos.y -= 0.3f; // 플랫폼을 관통하여 사다리 안으로 들어가기
        transform.position = pos;

        animator.SetBool("Climbing", true);
        if (dashParticle != null) particleEmission.rateOverTime = 0f;
        audioSource.Stop();
    }

    // StartClimbing 메서드는 그대로 유지
    void StartClimbing()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        jumpCount = 0;

        // 사다리 중앙으로 스냅 (X축 위치 조정)
        Vector3 pos = transform.position;
        float targetX = currentLadder.bounds.center.x;

        // 현재 위치에서 사다리 중앙까지의 거리가 스냅 거리 내에 있으면 스냅
        if (Mathf.Abs(pos.x - targetX) <= ladderSnapDistance)
        {
            pos.x = targetX;
            transform.position = pos;
        }

        animator.SetBool("Climbing", true);
        if (dashParticle != null) particleEmission.rateOverTime = 0f;
        audioSource.Stop();
    }

    // ExitLadder 메서드 수정
    void ExitLadder(bool withJump)
    {
        isClimbing = false;
        rb.gravityScale = 1.5f;
        animator.SetBool("Climbing", false);

        // 사다리에서 점프로 벗어날 때
        if (withJump)
        {
            // 현재 보고 있는 방향으로 점프
            float hForce = spriteRenderer.flipX ? -1f : 1f;
            rb.velocity = new Vector2(hForce * movePower * 0.8f, jumpPower * 0.9f);
            jumpCount = 1;
        }
        else
        {
            rb.velocity = new Vector2(0, 0);
        }

        // 잠시 사다리 사용 불가능하게 만들기 (연속 입력 방지)
        StartCoroutine(LadderCooldown());
    }

    // 사다리 쿨다운 코루틴 추가
    private System.Collections.IEnumerator LadderCooldown()
    {
        canUseLadder = false;
        yield return new WaitForSeconds(0.2f);
        canUseLadder = true;
    }

    // OnTriggerStay2D 수정
    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadder = col;
        }
    }

    // OnTriggerExit2D 수정
    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
            // 사다리에서 일정 거리 이상 벗어났을 때만 상태 해제
            if (currentLadder == col)
            {
                float distance = Vector2.Distance(transform.position, col.bounds.center);
                if (distance > ladderSnapDistance * 2f)
                {
                    isNearLadder = false;
                    currentLadder = null;

                    if (isClimbing)
                    {
                        ExitLadder(false);
                    }
                }
            }
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Box") ||
            collision.gameObject.CompareTag("wall"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
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

    void OnDrawGizmos()
    {
        if (headCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(headCheck.position, headCheck.position + Vector3.up * headCheckLength);
        }
        if (tailCheck)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tailCheck.position, tailCheck.position + Vector3.up * tailCheckLength);
        }
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (boxInteraction != null && boxInteraction.IsInteracting)
        {
            Gizmos.color = boxInteraction.IsPushing ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    private void UpdateParticlePosition()
    {
        if (dashParticle == null) return;

        dashParticle.transform.parent = transform;

        Vector3 localPos = dashParticleOffset;
        localPos.x = spriteRenderer.flipX ? -dashParticleOffset.x : dashParticleOffset.x;
        dashParticle.transform.localPosition = localPos;

        dashParticle.transform.localRotation = spriteRenderer.flipX ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        Vector3 scale = dashParticle.transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        dashParticle.transform.localScale = scale;
    }

    public void PlayHurtSound()
    {
        if (hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }
}