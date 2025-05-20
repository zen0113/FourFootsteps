using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHumanMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    [Header("이동 관련 설정")]
    [SerializeField] private float movePower = 2f;       // 기본 이동 속도

    [Header("웅크리기 관련 변수")]
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private Sprite crouchSprite;    // 웅크린 상태의 스프라이트
    private Sprite originalSprite;                   // 기본 스프라이트

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;  // 기본 스프라이트 저장
    }

    private void Update()
    {
        Crouch();       // 웅크리기 처리
    }
    private void FixedUpdate()
    {
        Move();             // 이동
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

        if (!isCrouching)
        {
            Vector3 moveVelocity = new Vector3(horizontalInput, 0, 0);
            transform.position += moveVelocity * currentPower * Time.deltaTime;
        }
        
    }

    void Crouch()
    {
        // S키 입력으로 인한 웅크리기
        if (Input.GetKeyDown(KeyCode.S))
        {
            // 웅크리기 시작
            isCrouching = true;
            spriteRenderer.sprite = crouchSprite;  // 웅크린 스프라이트로 변경
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            // 웅크리기 종료
            isCrouching = false;
            spriteRenderer.sprite = originalSprite;  // 기본 스프라이트로 복귀
        }
    }
}
