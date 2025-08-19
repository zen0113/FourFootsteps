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
    private Collider2D currentLadder;
    private bool isClimbing = false;
    private bool isNearLadder = false;

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
        boxInteraction = GetComponent<PlayerBoxInteraction>();
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
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
            return;

        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        // 박스 위에 있는지 추가 체크 (태그 기반)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        bool onBox = false;

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Box"))
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
        if (horizontalInput != 0)
        {
            // 당기기 상태가 아닐 때만 스프라이트 방향 변경
            if (!(boxInteraction != null && boxInteraction.IsPulling))
            {
                spriteRenderer.flipX = horizontalInput < 0;
            }
            // 당기기 중일 때는 시선 방향 반대로 설정 (박스를 바라보게)
            else
            {
                bool isBoxOnRight = boxInteraction.CurrentBox != null &&
                                     boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                spriteRenderer.flipX = !isBoxOnRight; // 당기기 중일 때는 항상 박스를 바라보도록
            }
        }
        if (isClimbing)
        {
            horizontalInput = 0;
        }

        // 애니메이션 상태 업데이트
        UpdateAnimationState(horizontalInput);

        // 박스 상호작용 상태 확인
        isBoxInteractionEnabled = Input.GetKey(KeyCode.E);

        // 박스 상호작용 상태 확인
        isBoxInteractionEnabled = Input.GetKey(KeyCode.E);

        HandleLadderInput();
        if (!isClimbing) Jump();
        HandleCrouch(justLanded);
    }

    void UpdateAnimationState(float horizontalInput)
    {
        // 기본 상태 초기화
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);
        animator.SetBool("Crouching", false);
        animator.SetBool("Climbing", false);

        // 실제 캐릭터의 속도를 사용하여 이동 여부를 판단
        bool isActuallyMoving = Mathf.Abs(rb.velocity.x) > 0.01f;

        // 대시 상태 체크
        bool wasDashing = isDashing;
        isDashing = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !(boxInteraction != null && boxInteraction.IsInteracting) && isActuallyMoving;

        // 이동 방향 변경 감지
        int currentDirection = Mathf.RoundToInt(Mathf.Sign(horizontalInput));
        bool directionChanged = currentDirection != 0 && currentDirection != lastMoveDirection;
        lastMoveDirection = currentDirection;

        // 점프 중인지 확인
        bool isJumping = !isOnGround;

        // 웅크리기 상태 (강제 또는 일반)를 가장 먼저 확인
        if (isCrouching || forceCrouch)
        {
            // 물리적인 이동 속도를 사용하여 애니메이션 결정
            if (isActuallyMoving)
            {
                // 웅크린 상태에서 이동
                animator.SetBool("Crouching", true);
            }
            else
            {
                // 웅크린 상태에서 정지
                animator.SetBool("Crouch", true);
            }
        }
        else if (isClimbing)
        {
            // 사다리 오르기 애니메이션
            float verticalInput = Input.GetAxisRaw("Vertical");
            bool isClimbingMoving = Mathf.Abs(verticalInput) > 0.01f;
            animator.SetBool("Climbing", isClimbingMoving);
        }
        // 대시 또는 점프 중일 때 Dash 애니메이션을 활성화
        else if (isDashing || isJumping)
        {
            animator.SetBool("Dash", true);
        }
        else if (isActuallyMoving) // 키 입력이 아닌 실제 이동 속도로 판단
        {
            // 일반 이동 애니메이션
            animator.SetBool("Moving", true);
        }

        if (dashParticle != null)
        {
            UpdateParticlePosition();

            // 파티클을 켜야 하는 조건 (이동 중이거나, 대시 중이거나, 점프 중이거나)
            bool shouldEmit = isActuallyMoving || isDashing || isJumping;

            // 이동 중이거나 점프 중일 때만 파티클을 켬
            if (shouldEmit)
            {
                float currentRate = isDashing ? runEmissionRate : walkEmissionRate;
                particleEmission.rateOverTime = currentRate;

                if (!dashParticle.isPlaying)
                {
                    dashParticle.Play();
                }
            }
            else
            {
                // 모든 조건에 해당하지 않으면 파티클을 끔
                particleEmission.rateOverTime = 0f;
                if (dashParticle.particleCount == 0)
                {
                    dashParticle.Stop();
                }
            }
        }
    }

    void LateUpdate()
    {
        // 물리 업데이트 후 애니메이션 상태 동기화
        if (!IsInputBlocked() && (bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            UpdateAnimationState(Input.GetAxisRaw("Horizontal"));
        }
    }

    void FixedUpdate()
    {
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
            return;

        if (!isClimbing)
        {
            Move();
            BetterJump();
            HandleSound();
        }
        else
        {
            Climb();
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
                // 웅크리기 적용
                isCrouching = true;
                isCrouchMoving = false;
                boxCollider.size = crouchColliderSize;
                boxCollider.offset = crouchColliderOffset;

                // 즉시 애니메이션 적용
                animator.SetBool("Crouch", true);
                animator.SetBool("Crouching", false);
                Debug.Log("[PlayerCatMovement] 강제 웅크리기 - Crouch 애니메이션 즉시 적용");
            }
            else
            {
                // 웅크리기 해제
                isCrouching = false;
                isCrouchMoving = false;
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;

                // 애니메이션 해제
                animator.SetBool("Crouch", false);
                animator.SetBool("Crouching", false);
                Debug.Log("[PlayerCatMovement] 강제 웅크리기 해제 - 모든 웅크리기 애니메이션 해제");
            }
        }
    }

    // 다이얼로그 출력 중, 씬 로딩 중이면 입력을 받지 않음.
    bool IsInputBlocked()
    {
        return PauseManager.IsGamePaused
             || DialogueManager.Instance.isDialogueActive
             || (GameManager.Instance != null && GameManager.Instance.IsSceneLoading)
             || isMiniGameInputBlocked;  // ← 여기!
    }

    void HandleLadderInput()
    {
        if (!isNearLadder) return;
        float verticalInput = Input.GetAxisRaw("Vertical");
        if (!isClimbing && verticalInput != 0) StartClimbing();
        else if (isClimbing && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)))
            ExitLadder(Input.GetKeyDown(KeyCode.Space));
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

        // 몸체 충돌 체크
        if (obstacleAbove && !isCrouching && isOnGround)
        {
            // 강제 웅크리기
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }

        // S키 입력으로 인한 웅크리기
        if (Input.GetKeyDown(KeyCode.S) && isOnGround)
        {
            // 웅크리기 시작
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }
        else if (Input.GetKeyUp(KeyCode.S) && !obstacleAbove)
        {
            // 웅크리기 해제 (몸 전체가 장애물에서 벗어났을 때만)
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        // 몸 전체가 장애물에서 벗어나고 S키도 누르고 있지 않으면 자동으로 일어나기
        if (!obstacleAbove && isCrouching && !Input.GetKey(KeyCode.S))
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        // 웅크린 상태에서 이동 중인지 여부 업데이트
        isCrouchMoving = isCrouching && Mathf.Abs(horizontalInput) > 0.01f;

        // ❌ 사운드 재생 로직은 FixedUpdate()의 HandleSound() 함수로 옮겼습니다.
        // 이 곳에서는 상태 변경만 담당합니다.
    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

        // 미니게임 중이거나 강제 웅크리기 상태일 때
        // horizontalInput을 강제로 0으로 만들어 이동을 차단합니다.
        if (isMiniGameInputBlocked || forceCrouch)
        {
            horizontalInput = 0;
        }

        // 박스 상호작용 상태 확인
        bool isInteractingWithBox = boxInteraction != null && boxInteraction.IsInteracting;
        bool isPullingBox = boxInteraction != null && boxInteraction.IsPulling;

        // 이동 방향이 변경될 때 파티클 위치와 방향 업데이트
        if (isDashing && dashParticle != null)
        {
            UpdateParticlePosition();
        }

        // 박스 상호작용 중이고 E키를 누르고 있을 때
        if (isInteractingWithBox && isBoxInteractionEnabled)
        {
            // E키를 누른 상태에서 움직임 제한 (당기기/밀기 속도 조정)
            currentPower = boxInteractingPower;

            // 당기기 중일 때 플레이어 시선 조정 (박스를 항상 바라보게)
            if (isPullingBox && boxInteraction.CurrentBox != null)
            {
                bool isBoxOnRight = boxInteraction.CurrentBox.transform.position.x > transform.position.x;
                spriteRenderer.flipX = !isBoxOnRight; // 박스를 바라보는 방향으로 설정
            }
        }
        // 웅크린 상태에서는 이동 속도 감소
        else if (isCrouching)
        {
            currentPower = crouchPower;
        }
        // 웅크리지 않고 박스와 상호작용 중이 아닐 때만 대시 가능
        else if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isInteractingWithBox)
        {
            currentPower = dashPower;
        }

        // 부드러운 속도 변화를 위한 보간 적용
        float targetVelocityX = horizontalInput * currentPower;
        float smoothedVelocityX = Mathf.Lerp(rb.velocity.x, targetVelocityX, Time.fixedDeltaTime * 10f);

        rb.velocity = new Vector2(smoothedVelocityX, rb.velocity.y);
    }

    void HandleSound()
    {
        // 멈춰있거나 공중에 떠있으면 소리 재생하지 않음
        if (Mathf.Abs(rb.velocity.x) < 0.1f || !isOnGround)
        {
            return;
        }

        // 착지 후 딜레이
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

                // 점프 시 파티클 처리
                if (dashParticle != null)
                {
                    UpdateParticlePosition();
                    if (!dashParticle.isPlaying)
                    {
                        dashParticle.Play();
                    }
                    particleEmission.rateOverTime = runEmissionRate;
                }

                // 점프 소리 재생
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
        float v = Input.GetAxisRaw("Vertical");
        // float h = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(0, v * climbSpeed);

        // 사다리 오르기 효과음 재생
        if (v != 0 && climbSound != null && Time.time - lastClimbSoundTime >= climbSoundInterval)
        {
            audioSource.PlayOneShot(climbSound);
            lastClimbSoundTime = Time.time;
        }
    }

    void StartClimbing()
    {
        if (currentLadder == null) return;
        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        jumpCount = 0;
        var pos = transform.position;
        pos.x = currentLadder.bounds.center.x;
        transform.position = pos;

    }

    void ExitLadder(bool withJump)
    {
        isClimbing = false;
        rb.gravityScale = 1.5f;
        if (withJump)
        {
            float hForce = spriteRenderer.flipX ? -1f : 1f;
            rb.velocity = new Vector2(hForce * movePower * 0.5f, jumpPower);
            jumpCount = 1;
        }
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
            isNearLadder = false;
            if (isClimbing && currentLadder == col) ExitLadder(false);
            if (currentLadder == col) currentLadder = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 바닥, 박스, 또는 벽에 닿으면 점프 리셋
        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Box") ||
            collision.gameObject.CompareTag("wall"))
        {
            // 위에서 아래로 충돌했는지 확인 (발이 닿았는지)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // 아래쪽에서 충돌 (바닥/박스)
                {
                    jumpCount = 0; // 점프 카운트 리셋
                    break;
                }
                else if (Mathf.Abs(contact.normal.x) > 0.5f) // 좌우에서 충돌 (벽)
                {
                    jumpCount = 0; // 점프 카운트 리셋
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

        // 박스 상호작용 상태 표시
        if (boxInteraction != null && boxInteraction.IsInteracting)
        {
            Gizmos.color = boxInteraction.IsPushing ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

    }

    // 파티클 위치와 방향 업데이트 함수
    private void UpdateParticlePosition()
    {
        if (dashParticle == null) return;

        // 파티클을 플레이어의 자식으로 설정하고 로컬 위치 사용
        dashParticle.transform.parent = transform;

        // 로컬 위치 설정 (부모 기준 상대 위치)
        Vector3 localPos = dashParticleOffset;
        localPos.x = spriteRenderer.flipX ? -dashParticleOffset.x : dashParticleOffset.x;
        dashParticle.transform.localPosition = localPos;

        // 로컬 회전만 적용 (부모의 스케일/회전에 영향받지 않음)
        dashParticle.transform.localRotation = spriteRenderer.flipX ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        // localScale은 항상 양수로 유지
        Vector3 scale = dashParticle.transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        dashParticle.transform.localScale = scale;
    }

    // 다칠 때 호출될 함수
    public void PlayHurtSound()
    {
        if (hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }
}