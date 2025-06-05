using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    private float originalDrag;
    private float originalMass;
    
    [Header("박스 물리 설정")]
    [SerializeField] private float normalDrag = 10f;      // 일반 상태 drag
    [SerializeField] private float interactingDrag = 1f;  // 상호작용 중 drag
    [SerializeField] private float boxMass = 2f;          // 박스 질량
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = 1f;
        rb.mass = boxMass;
        rb.drag = normalDrag;
        originalDrag = rb.drag;
        originalMass = rb.mass;
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
                rb.drag = interactingDrag;
            }
            else
            {
                // 상호작용하지 않을 때는 원래 drag로 복원
                rb.drag = originalDrag;
                
                // 상호작용 중이 아닐 때는 속도를 빠르게 감소
                if (rb.velocity.magnitude > 0.1f)
                {
                    rb.velocity *= 0.95f;
                }
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ground와 충돌 시 수평 속도 감소
        if (collision.gameObject.CompareTag("Ground"))
        {
            rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);
        }
        
        // 플레이어와 충돌 시 처리는 PlayerBoxInteraction에서 처리
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 플레이어와 계속 충돌 중일 때
        if (collision.gameObject.CompareTag("Player"))
        {
            GameObject player = collision.gameObject;
            PlayerBoxInteraction interaction = player.GetComponent<PlayerBoxInteraction>();
            
            // 상호작용 중이 아니면 박스가 움직이지 않도록
            if (interaction == null || !interaction.IsInteracting || interaction.CurrentBox != gameObject)
            {
                // 플레이어가 박스를 밀지 못하도록 속도 제한
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
}