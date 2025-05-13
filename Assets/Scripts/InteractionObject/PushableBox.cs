using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    private float originalDrag;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = 1f;
        rb.mass = 3f;
        rb.drag = 8f; // 높은 drag로 미끄러짐 방지
        originalDrag = rb.drag;
    }
    
    private void Update()
    {
        // 플레이어가 상호작용 중인지 확인
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerBoxInteraction interaction = player.GetComponent<PlayerBoxInteraction>();
            if (interaction != null && interaction.IsInteracting && interaction.CurrentBox == gameObject)
            {
                // 상호작용 중일 때는 drag 감소 (더 부드러운 움직임)
                rb.drag = 2f;
            }
            else
            {
                // 상호작용하지 않을 때는 원래 drag로 복원
                rb.drag = originalDrag;
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ground와 충돌 시 속도 감소
        if (collision.gameObject.CompareTag("Ground"))
        {
            rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);
        }
    }
}