using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastPushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    
    [Header("박스 물리 설정 - 빠른 버전")]
    [SerializeField] private float normalDrag = 0.5f;       // 매우 낮은 저항 (빠른 이동)
    [SerializeField] private float interactingDrag = 0.2f;  // 상호작용 중에도 빠르게
    [SerializeField] private float boxMass = 2f;            // 적당한 질량
    [SerializeField] private float fallGravityScale = 3f;   // 떨어질 때 중력 증가
    [SerializeField] private float normalGravityScale = 1f; // 일반 중력
    
    [Header("이동 최적화")]
    [SerializeField] private float maxPushSpeed = 8f;       // 최대 밀기 속도
    [SerializeField] private float groundFriction = 0.1f;   // 바닥 마찰 (낮을수록 빠름)
    
    private bool isBeingPushed = false;
    private bool isFalling = false;
    private float timeNotGrounded = 0f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D 최적화 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = normalGravityScale;
        rb.mass = boxMass;
        rb.drag = normalDrag;
        
        // 물리 머테리얼 설정 (낮은 마찰)
        PhysicsMaterial2D lowFrictionMaterial = new PhysicsMaterial2D("LowFriction");
        lowFrictionMaterial.friction = groundFriction;
        lowFrictionMaterial.bounciness = 0f;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.sharedMaterial = lowFrictionMaterial;
        }
        
        Debug.Log("[FastPushableBox] 빠른 상자 초기화 완료");
    }
    
    private void Update()
    {
        CheckGroundStatus();
        HandlePlayerInteraction();
        OptimizePhysics();
    }
    
    private void CheckGroundStatus()
    {
        // 땅에 닿아있는지 체크
        bool wasOnGround = !isFalling;
        bool isOnGround = IsGrounded();
        
        if (isOnGround)
        {
            // 땅에 착지
            if (isFalling)
            {
                isFalling = false;
                rb.gravityScale = normalGravityScale;
                Debug.Log("[FastPushableBox] 착지 완료");
            }
            timeNotGrounded = 0f;
        }
        else
        {
            // 공중에 있음
            timeNotGrounded += Time.deltaTime;
            
            // 0.1초 이상 공중에 있으면 떨어지는 중으로 판정
            if (timeNotGrounded > 0.1f && !isFalling)
            {
                isFalling = true;
                rb.gravityScale = fallGravityScale; // 빠른 낙하
                Debug.Log("[FastPushableBox] 빠른 낙하 모드 활성화");
            }
        }
    }
    
    private bool IsGrounded()
    {
        // 상자 아래쪽에 레이캐스트로 바닥 체크
        float checkDistance = 0.1f;
        Vector2 boxSize = GetComponent<BoxCollider2D>().size;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
        
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkDistance, LayerMask.GetMask("Ground", "Default"));
        return hit.collider != null;
    }
    
    private void HandlePlayerInteraction()
    {
        // 플레이어가 상호작용 중인지 확인
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool currentlyBeingPushed = false;
        
        if (player != null)
        {
            PlayerBoxInteraction interaction = player.GetComponent<PlayerBoxInteraction>();
            if (interaction != null && interaction.IsInteracting && interaction.CurrentBox == gameObject)
            {
                currentlyBeingPushed = true;
            }
        }
        
        // 상호작용 상태 변경 처리
        if (currentlyBeingPushed != isBeingPushed)
        {
            isBeingPushed = currentlyBeingPushed;
            
            if (isBeingPushed)
            {
                // 상호작용 시작 - 더 빠르게
                rb.drag = interactingDrag;
                Debug.Log("[FastPushableBox] 빠른 밀기 모드 활성화");
            }
            else
            {
                // 상호작용 종료 - 원래 속도로
                rb.drag = normalDrag;
            }
        }
    }
    
    private void OptimizePhysics()
    {
        // 속도 제한 (너무 빨라지지 않게)
        if (isBeingPushed)
        {
            Vector2 velocity = rb.velocity;
            
            // 수평 속도 제한
            if (Mathf.Abs(velocity.x) > maxPushSpeed)
            {
                velocity.x = Mathf.Sign(velocity.x) * maxPushSpeed;
                rb.velocity = velocity;
            }
        }
        
        // 미세한 움직임 정리 (성능 최적화)
        if (!isBeingPushed && rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 땅과 충돌 시 수평 속도 약간만 감소 (기존보다 덜 감소)
        if (collision.gameObject.CompareTag("Ground"))
        {
            rb.velocity = new Vector2(rb.velocity.x * 0.8f, rb.velocity.y); // 0.5f → 0.8f로 변경
        }
        
        // 플레이어와의 충돌 처리
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed)
            {
                // 상호작용 중이 아니면 밀리지 않도록
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        
        // 나무판자와 충돌 시 로그
        if (collision.gameObject.GetComponent<SimpleWoodenPlank>() != null)
        {
            Debug.Log("[FastPushableBox] 나무판자와 충돌!");
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 플레이어와 계속 충돌 중일 때
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed)
            {
                // 상호작용 중이 아니면 움직이지 않도록
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        // 상자 상태 시각화
        Gizmos.color = isBeingPushed ? Color.green : Color.yellow;
        if (isFalling) Gizmos.color = Color.red;
        
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        
        // 바닥 체크 레이 시각화
        if (GetComponent<BoxCollider2D>() != null)
        {
            Vector2 boxSize = GetComponent<BoxCollider2D>().size;
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
            
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * 0.1f);
        }
        
        #if UNITY_EDITOR
        // 상태 텍스트 표시
        UnityEditor.Handles.color = Color.white;
        string status = isBeingPushed ? "밀기중" : (isFalling ? "낙하중" : "대기중");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, status);
        #endif
    }
    
    // 외부에서 사용할 수 있는 프로퍼티
    public bool IsBeingPushed => isBeingPushed;
    public bool IsFalling => isFalling;
    
    // 즉시 빠른 낙하 모드 활성화 (테스트용)
    public void EnableFastFall()
    {
        isFalling = true;
        rb.gravityScale = fallGravityScale;
        Debug.Log("[FastPushableBox] 강제 빠른 낙하 활성화");
    }
    
    // 물리 설정 동적 변경
    public void SetFallSpeed(float newGravityScale)
    {
        fallGravityScale = newGravityScale;
        if (isFalling)
        {
            rb.gravityScale = fallGravityScale;
        }
    }
    
    public void SetPushSpeed(float newMaxSpeed)
    {
        maxPushSpeed = newMaxSpeed;
    }
}