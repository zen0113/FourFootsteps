using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatMovement : MonoBehaviour
{
    // 컴포넌트
    Rigidbody2D rb;
    BoxCollider2D boxCollider;
    SpriteRenderer spriteRenderer;
    Animator animator;

    // 이동/점프
    [Header("이동 및 점프")]
    [SerializeField] private float movePower = 2f;
    [SerializeField] private float dashPower = 8f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float crouchPower = 1f;

    // 점프 중력 보정
    [Header("점프 중력 보정")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    private int jumpCount = 0;

    // 지상 체크
    [Header("지상 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    private bool isOnGround;

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
    private Vector2 originalColliderSize, originalColliderOffset, crouchColliderSize, crouchColliderOffset;

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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
        boxInteraction = GetComponent<PlayerBoxInteraction>();
        animator = GetComponent<Animator>();
        originalSprite = spriteRenderer.sprite;  // 기본 스프라이트 저장
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;
        crouchColliderSize = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
        crouchColliderOffset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f);
    }

    void Update()
    {
        bool prevOnGround = isOnGround;
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        bool justLanded = isOnGround && !prevOnGround;
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

        HandleLadderInput();
        if (!isClimbing) Jump();
        HandleCrouch(justLanded);
    }

    void UpdateAnimationState(float horizontalInput)
    {
        // 기본 상태 초기화
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouching", false);
        animator.SetBool("Climbing", false);

        // 대시 상태 체크
        isDashing = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !(boxInteraction != null && boxInteraction.IsInteracting);
        
        // 상태 우선순위에 따라 애니메이션 설정
        if (isCrouching)
        {
            animator.SetBool("Crouching", true);
        }
        else if (isDashing)
        {
            animator.SetBool("Dash", true);
        }
        else if (horizontalInput != 0)
        {
            animator.SetBool("Moving", true);
        }
        else if (isClimbing)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");

            // 오르고 있을 때만 Climbing 애니메이션 재생
            bool isClimbingMoving = Mathf.Abs(verticalInput) > 0.01f;
            animator.SetBool("Climbing", isClimbingMoving);
        }

        // 점프 상태는 별도로 처리 (BetterJump에서 처리)
        //animator.SetBool("IsJumping", !isOnGround);
    }

    void FixedUpdate()
    {
        if (!isClimbing)
        {
            Move();
            BetterJump();
        }
        else
        {
            Climb();
        }
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

        // 몸체 충돌 체크
        if (obstacleAbove && !isCrouching && isOnGround)
        {
            // 강제 웅크리기
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
            spriteRenderer.sprite = crouchSprite;  // 웅크린 스프라이트로 변경
        }

        // S키 입력으로 인한 웅크리기
        if (Input.GetKeyDown(KeyCode.S) && isOnGround)
        {
            // 웅크리기 시작
            isCrouching = true;
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
            spriteRenderer.sprite = crouchSprite;  // 웅크린 스프라이트로 변경
        }
        else if (Input.GetKeyUp(KeyCode.S) && !obstacleAbove)
        {
            // 웅크리기 해제 (몸 전체가 장애물에서 벗어났을 때만)
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
            spriteRenderer.sprite = originalSprite;  // 기본 스프라이트로 복귀
        }

        // 몸 전체가 장애물에서 벗어나고 S키도 누르고 있지 않으면 자동으로 일어나기
        if (!obstacleAbove && isCrouching && !Input.GetKey(KeyCode.S))
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
            spriteRenderer.sprite = originalSprite;  // 기본 스프라이트로 복귀
        }
    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

        // 박스 상호작용 상태 확인
        bool isInteractingWithBox = boxInteraction != null && boxInteraction.IsInteracting;
        bool isPullingBox = boxInteraction != null && boxInteraction.IsPulling;

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
        else if (isDashing)
        {
            currentPower = dashPower;
        }

        float targetVelocityX = horizontalInput * currentPower;
        float smoothSpeed = 0.05f;
        float newVelocityX = Mathf.Lerp(rb.velocity.x, targetVelocityX, smoothSpeed / Time.deltaTime);
        rb.velocity = new Vector2(newVelocityX, rb.velocity.y);
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
            }
        }
    }

    void BetterJump()
    {
        if (isClimbing) return;

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            //animator.SetBool("IsFalling", true);
            //animator.SetBool("IsJumping", false);
        }
        else if (rb.velocity.y > 0)
        {
            //animator.SetBool("IsFalling", false);
            //animator.SetBool("IsJumping", true);
        }
        else
        {
            //animator.SetBool("IsFalling", false);
            //animator.SetBool("IsJumping", false);
        }
    }

    void Climb()
    {
        float v = Input.GetAxisRaw("Vertical");
       // float h = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(0, v * climbSpeed);
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
    }
}
