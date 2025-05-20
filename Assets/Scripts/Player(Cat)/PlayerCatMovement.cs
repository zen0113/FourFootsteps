using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatMovement : MonoBehaviour
{
    // 컴포넌트
    Rigidbody2D rb;
    BoxCollider2D boxCollider;
    SpriteRenderer spriteRenderer;

    // 이동/점프
    [Header("이동 및 점프")]
    [SerializeField] private float movePower = 2f;
    [SerializeField] private float dashPower = 8f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float crouchPower = 1f;
    [SerializeField] private float accelerationSmoothing = 0.1f;
    private float velocityXSmoothing;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
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
        if (horizontalInput != 0 && !isClimbing)
            spriteRenderer.flipX = horizontalInput < 0;

        HandleLadderInput();
        if (!isClimbing) Jump();
        HandleCrouch(justLanded);
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
        float speed = isCrouching ? crouchPower : (Input.GetKey(KeyCode.LeftShift) ? dashPower : movePower);
        float targetX = horizontalInput * speed;
        float smoothX = Mathf.SmoothDamp(rb.velocity.x, targetX, ref velocityXSmoothing, accelerationSmoothing);
        rb.velocity = new Vector2(smoothX, rb.velocity.y);
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
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    void Climb()
    {
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(h * movePower * 0.25f, v * climbSpeed);
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
        rb.gravityScale = 1f;
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
