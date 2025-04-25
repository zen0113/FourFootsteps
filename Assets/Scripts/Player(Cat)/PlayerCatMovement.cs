using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("이동 관련 설정")]
    public float movePower = 2f;       // 기본 이동 속도
    public float dashPower = 8f;       // 대쉬 시 속도
    public float jumpPower = 5f;       // 점프 힘
    public float croushPower = 1f;     // 웅크릴 때 속도 감소

    [Header("점프 중력 설정")]
    public float fallMultiplier = 2.5f;       // 낙하 시 중력 가중치
    public float lowJumpMultiplier = 2f;      // 짧은 점프 시 중력 가중치

    private int jumpCount = 0;                // 점프 횟수 (2단 점프 제한)
    private bool isCrouching = false;         // S 키 눌림 여부 (웅크림 상태)

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Move();             // 이동
        Jump();             // 점프 입력 감지
        BetterJump();       // 점프 물리 보정
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 바닥에 닿으면 점프 횟수 초기화
        jumpCount = 0;
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
            this.GetComponent<SpriteRenderer>().flipX = true;
        }
        else if (horizontalInput > 0)
        {
            moveVelocity = Vector3.right;
            this.GetComponent<SpriteRenderer>().flipX = false;

        }

        // 웅크리기 (S 키)
        if (Input.GetKey(KeyCode.S))
        {
            isCrouching = true;
            currentPower = croushPower;
        }
        else
        {
            isCrouching = false;

            // 대쉬 (Shift 키) - 웅크리지 않았을 때만 적용
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentPower = dashPower;
            }
        }

        // 이동 처리
        transform.position += moveVelocity * currentPower * Time.deltaTime;
    }

    void Jump()
    {
        // Space 키 입력 & 웅크리지 않았을 경우
        if (Input.GetKeyDown(KeyCode.Space) && !isCrouching)
        {
            if (jumpCount < 2)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0); // y 속도 초기화
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpCount++;
            }
        }
    }

    void BetterJump()
    {
        // 낙하 중일 때 더 빠르게 떨어지게 함
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // 짧게 점프(키 떼었을 경우) 시 낮게 점프
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }
}
