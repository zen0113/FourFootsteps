using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    [Header("지상 체크")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    private bool isOnGround;

    [Header("이동 관련 변수")]
    public float movePower = 2f;       // 기본 이동 속도
    public float dashPower = 8f;        // 대쉬 속도
    public float jumpPower = 5f;        // 점프 힘
    public float crouchPower = 1f;      // 웅크린 상태 이동 속도

    [Header("점프 중력 보정")]
    public float fallMultiplier = 2.5f; // 낙하할 때 중력 가중치
    public float lowJumpMultiplier = 2f;// 짧은 점프 시 중력 가중치

    private int jumpCount = 0;          // 점프 횟수(2단 점프까지)

    [Header("사다리 관련")]
    public float climbSpeed = 2f;       // 사다리 타기 속도
    public float ladderCheckRadius = 0.2f; // 사다리 체크 범위
    private Collider2D currentLadder;  // 현재 접촉 중인 사다리

    private bool isClimbing = false;    // 사다리 타고 있는지
    private bool isNearLadder = false;  // 사다리 근처에 있는지

    [Header("웅크리기 관련 변수")]
    public bool isCrouching = false;
    public Transform headCheck;    // 머리 체크 위치
    public Transform tailCheck;    // 꼬리 체크 위치
    public float headCheckLength;  // 머리 체크 길이
    public float tailCheckLength;  // 꼬리 체크 길이

    // 콜라이더 크기 저장용
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 crouchColliderSize;
    private Vector2 crouchColliderOffset;
    

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        // 기본 콜라이더 크기와 오프셋 저장
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;
        
        // 웅크렸을 때의 크기와 오프셋 계산
        crouchColliderSize = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
        // 위쪽을 기준으로 줄어들도록 오프셋 조정
        crouchColliderOffset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f);
    }

    private void Update()
    {
        // 지상 체크 업데이트
        isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        HandleLadderInput();

        if (!isClimbing)
        {
            Move();        // 일반 이동
            Jump();        // 점프 처리
            BetterJump();  // 점프 물리 보정
        }
        else
        {
            Climb();       // 사다리 타기 처리
        }

        // 웅크리기 처리
        HandleCrouch();
    }

    bool HeadDetect()
    {
        bool hit = Physics2D.Raycast(headCheck.position, Vector2.up, headCheckLength, groundMask);
        return hit;
    }

    bool TailDetect()
    {
        bool hit = Physics2D.Raycast(tailCheck.position, Vector2.up, tailCheckLength, groundMask);
        return hit;
    }

    private void HandleCrouch()
    {
        bool isHeadHitting = HeadDetect();
        bool isTailHitting = TailDetect();
        bool isBodyHitting = isHeadHitting || isTailHitting;  // 머리나 꼬리 중 하나라도 충돌 중이면 true
        
        // 몸체 충돌 체크
        if (isBodyHitting && !isCrouching && isOnGround)
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
        else if (Input.GetKeyUp(KeyCode.S) && !isBodyHitting)
        {
            // 웅크리기 해제 (몸 전체가 장애물에서 벗어났을 때만)
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        // 몸 전체가 장애물에서 벗어나고 S키도 누르고 있지 않으면 자동으로 일어나기
        if (!isBodyHitting && isCrouching && !Input.GetKey(KeyCode.S))
        {
            isCrouching = false;
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }
    }

    private void HandleLadderInput()
    {
        if (isNearLadder)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");
            
            // 사다리 근처에서 위/아래 키를 누르면 사다리 타기 시작
            if (!isClimbing && verticalInput != 0)
            {
                StartClimbing();
            }
            // 사다리 타는 중에 Shift를 누르거나 점프하면 사다리에서 내림
            else if (isClimbing && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)))
            {
                ExitLadder(true);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadder = collision;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isNearLadder = false;
            if (isClimbing && currentLadder == collision)
            {
                ExitLadder(false);
            }
            currentLadder = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpCount = 0; // 바닥에 닿으면 점프 리셋
        }
    }

    void Move()
    {
        Vector3 moveVelocity = Vector3.zero;
        float currentPower = movePower;

        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 좌우 방향 설정
        if (horizontalInput < 0)
        {
            moveVelocity = Vector3.left;
            GetComponent<SpriteRenderer>().flipX = true;
        }
        else if (horizontalInput > 0)
        {
            moveVelocity = Vector3.right;
            GetComponent<SpriteRenderer>().flipX = false;
        }

        // 웅크린 상태에서는 이동 속도 감소
        if (isCrouching)
        {
            currentPower = crouchPower;
        }
        // 웅크리지 않은 상태에서만 대시 가능
        else if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
        {
            currentPower = dashPower;
        }

        // 이동 처리
        transform.position += moveVelocity * currentPower * Time.deltaTime;
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isCrouching && !isClimbing)
        {
            if ((isOnGround && jumpCount == 0) || (!isOnGround && jumpCount < 2))
            {
                rb.velocity = new Vector2(rb.velocity.x, 0); // 점프 시 y속도 초기화
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpCount++;
            }
        }
    }

    void BetterJump()
    {
        // 낙하 중일 때
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // 짧은 점프일 때
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    void Climb()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 사다리 위/아래 이동
        Vector2 climbVelocity = new Vector2(horizontalInput * movePower * 0.5f, verticalInput * climbSpeed);
        rb.velocity = climbVelocity;
    }

    void StartClimbing()
    {
        if (currentLadder != null)
        {
            isClimbing = true;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;

            // 사다리 중앙에 위치 고정
            Vector3 newPosition = transform.position;
            newPosition.x = currentLadder.bounds.center.x;
            transform.position = newPosition;

            Debug.Log("사다리 타기 시작");
        }
    }

    void ExitLadder(bool withJump)
    {
        isClimbing = false;
        rb.gravityScale = 1f;
        
        if (withJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        Debug.Log("사다리에서 내림");
    }

    private void OnDrawGizmos()
    {
        // 머리 체크 기즈모
        if (headCheck != null)
        {
            Gizmos.color = Color.red;  // 머리 체크는 빨간색
            Vector2 from = headCheck.position;
            Vector2 to = new Vector2(headCheck.position.x, headCheck.position.y + headCheckLength);
            Gizmos.DrawLine(from, to);
        }

        // 꼬리 체크 기즈모
        if (tailCheck != null)
        {
            Gizmos.color = Color.blue;  // 꼬리 체크는 파란색
            Vector2 from = tailCheck.position;
            Vector2 to = new Vector2(tailCheck.position.x, tailCheck.position.y + tailCheckLength);
            Gizmos.DrawLine(from, to);
        }

        // 지상 체크 기즈모
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

}
