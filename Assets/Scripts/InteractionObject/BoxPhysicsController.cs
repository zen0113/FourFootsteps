using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxPhysicsController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PushableBox pushableBox;
    
    [Header("플레이어 충돌 방지")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerCheckRadius = 0.1f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pushableBox = GetComponent<PushableBox>();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어와 충돌했을 때
        if (collision.gameObject.CompareTag("Player"))
        {
            // 상호작용 중이 아니면 플레이어가 박스를 밀지 못하도록
            if (!IsPlayerInteracting())
            {
                // 충돌 방향을 확인
                Vector2 collisionDirection = collision.GetContact(0).normal;
                
                // 옆면 충돌인 경우
                if (Mathf.Abs(collisionDirection.x) > 0.5f)
                {
                    // 박스 위치를 약간 조정하여 플레이어가 박스를 통과하지 못하게 함
                    Vector2 pushDirection = -collisionDirection * 0.1f;
                    transform.position += (Vector3)pushDirection;
                    
                    // 속도를 0으로 설정
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }
            }
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 플레이어와 계속 충돌 중일 때
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!IsPlayerInteracting())
            {
                // 플레이어가 계속 밀고 있어도 박스가 움직이지 않도록
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    private bool IsPlayerInteracting()
    {
        // 플레이어가 현재 이 박스와 상호작용 중인지 확인
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerBoxInteraction interaction = player.GetComponent<PlayerBoxInteraction>();
            if (interaction != null)
            {
                // PlayerBoxInteraction의 isInteracting 상태를 확인
                System.Reflection.FieldInfo fieldInfo = interaction.GetType().GetField("isInteracting", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    return (bool)fieldInfo.GetValue(interaction);
                }
            }
        }
        return false;
    }
}