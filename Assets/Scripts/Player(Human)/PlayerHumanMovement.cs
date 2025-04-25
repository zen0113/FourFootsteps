using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHumanMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("이동 관련 설정")]
    public float movePower = 2f;       // 기본 이동 속도
    public float dashPower = 8f;       // 대쉬 시 속도
    public float croushPower = 1f;     // 웅크릴 때 속도 감소

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Move();             // 이동
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

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

        // 이동 처리
        transform.position += moveVelocity * currentPower * Time.deltaTime;
    }
}
