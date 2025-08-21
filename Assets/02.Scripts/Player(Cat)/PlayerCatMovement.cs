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
    private Transform currentCart; // 현재 탑승 중인 카트 (Transform으로 변경)
    private float originalGravityScale;

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

        rb = GetComponent<Rigidbody2D>(); // rb를 Awake에서 찾아옵니다.
        originalGravityScale = rb.gravityScale; // 게임 시작 시 원래 중력 값을 저장
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

    // ######################################################################
    // ## 여기가 핵심 수정 부분입니다! ##
    // ######################################################################
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

            animator.SetBool("Climbing", true);

            // 애니메이션 속도로 움직임 표현
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

        // ✨ [수정] 속도(velocity) 대신 실제 키 입력(horizontalInput)을 기준으로 판단
        bool hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.01f;

        // 웅크리기 상태
        if (isCrouching || forceCrouch)
        {
            if (hasHorizontalInput)
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

        if (isDashing && hasHorizontalInput)
        {
            animator.SetBool("Dash", true);
        }
        else if (isJumping)
        {
            // 점프 애니메이션이 있다면 여기에 로직 추가
            // 예: animator.SetBool("Jumping", true);
        }
        else if (hasHorizontalInput)
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
            rb.velocity = new Vector2(0, rb.velocity.y); // x축 속도만 0으로 만들어 미끄러짐 방지
            if (dashParticle != null)
            {
                particleEmission.rateOverTime = 0f;
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        // 카트에 타지 않았을 때만 플레이어의 일반적인 움직임 처리
        if (!isOnCart)
        {
            if (!isClimbing)
            {
                Move();
                BetterJump();
                HandleSound();
                UpdateParticleState();
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
        // 카트에 탔을 때의 물리 로직 (필요 시 추가)
        else
        {
            // 예: 카트 위에서 좌우로 약간 움직이는 로직
            // MoveOnCart(); 
        }
    }

    // ✨ [추가] 카트 탑승 상태를 외부에서 설정하기 위한 함수
    public void SetOnCartState(bool onCart, Transform cartTransform = null)
    {
        isOnCart = onCart;

        if (onCart)
        {
            // 카트에 타면
            transform.SetParent(cartTransform);
            rb.gravityScale = 0; // 중력을 0으로 만들어 카트에 완전히 밀착
            rb.velocity = Vector2.zero; // 탑승 시 속도 초기화
        }
        else
        {
            // 카트에서 내리면
            transform.SetParent(null);
            rb.gravityScale = originalGravityScale; // 원래 중력 값으로 복원
        }
    }

    private void UpdateParticleState()
    {
        if (dashParticle == null) return;

        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            particleEmission.rateOverTime = 0f;
            if (dashParticle.isPlaying)
            {
                dashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        bool isAnyAnimationActive = animator.GetBool("Moving") ||
                                      animator.GetBool("Dash") ||
                                      animator.GetBool("Crouching") ||
                                      animator.GetBool("Climbing") ||
                                      animator.GetBool("Crouch");

        bool hasHorizontalInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f;
        bool isJumping = !isOnGround;

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
        else if (!isClimbing && canUseLadder && isOnGround && isNearLadder)
        {
            if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) &&
                Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartClimbingFromPlatform();
            }
        }
        else if (isClimbing)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExitLadder(true);
            }
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
        // 카트에 타고 있을 때는 이 함수가 호출되지 않으므로, 중복 체크 제거 가능
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

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
        // ✨ [수정] 속도(velocity) 대신 키 입력으로 소리 재생 여부 판단
        if (isClimbing || Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f || !isOnGround)
        {
            // 움직임이 멈추면 모든 발소리 관련 사운드 정지
            audioSource.Stop();
            return;
        }

        if (Time.time - lastLandingTime < landingSoundDelay)
        {
            return;
        }

        if (isCrouching)
        {
            if (Time.time - lastCrouchSoundTime >= crouchSoundInterval)
            {
                audioSource.PlayOneShot(crouchSound);
                lastCrouchSoundTime = Time.time;
            }
        }
        else if (isDashing)
        {
            if (Time.time - lastRunSoundTime >= runSoundInterval)
            {
                audioSource.PlayOneShot(runSound);
                lastRunSoundTime = Time.time;
            }
        }
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

    void Climb()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float moveY = verticalInput * climbSpeed;

        if (currentLadder != null)
        {
            float ladderTop = currentLadder.bounds.max.y;
            float ladderBottom = currentLadder.bounds.min.y;

            if (transform.position.y >= ladderTop - 0.1f && verticalInput > 0)
            {
                Vector3 exitPosition = new Vector3(transform.position.x, ladderTop + 0.5f, transform.position.z);
                transform.position = exitPosition;
                ExitLadder(false);
                return;
            }

            if (transform.position.y <= ladderBottom + 0.1f && verticalInput < 0)
            {
                ExitLadder(false);
                return;
            }

            float clampedY = Mathf.Clamp(transform.position.y + moveY * Time.fixedDeltaTime,
                                           ladderBottom + 0.2f, ladderTop - 0.2f);

            if ((transform.position.y >= ladderTop - 0.2f && verticalInput > 0) ||
                (transform.position.y <= ladderBottom + 0.2f && verticalInput < 0))
            {
                moveY = 0;
            }
        }

        rb.velocity = new Vector2(0, moveY);

        if (Mathf.Abs(verticalInput) > 0.01f && climbSound != null && Time.time - lastClimbSoundTime >= climbSoundInterval)
        {
            audioSource.PlayOneShot(climbSound);
            lastClimbSoundTime = Time.time;
        }
    }

    void StartClimbingFromPlatform()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        jumpCount = 0;

        Vector3 pos = transform.position;
        pos.x = currentLadder.bounds.center.x;
        pos.y -= 0.3f;
        transform.position = pos;

        animator.SetBool("Climbing", true);
        if (dashParticle != null) particleEmission.rateOverTime = 0f;
        audioSource.Stop();
    }

    void StartClimbing()
    {
        if (currentLadder == null) return;

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        jumpCount = 0;

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

    void ExitLadder(bool withJump)
    {
        isClimbing = false;
        rb.gravityScale = 1.5f;
        animator.SetBool("Climbing", false);

        if (withJump)
        {
            float hForce = spriteRenderer.flipX ? -1f : 1f;
            rb.velocity = new Vector2(hForce * movePower * 0.8f, jumpPower * 0.9f);
            jumpCount = 1;
        }
        else
        {
            rb.velocity = new Vector2(0, 0);
        }

        StartCoroutine(LadderCooldown());
    }

    private System.Collections.IEnumerator LadderCooldown()
    {
        canUseLadder = false;
        yield return new WaitForSeconds(0.2f);
        canUseLadder = true;
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadder = col;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ladder"))
        {
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