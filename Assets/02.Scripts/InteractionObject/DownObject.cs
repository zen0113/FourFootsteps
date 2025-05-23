using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownObject : MonoBehaviour
{
    [Header("드롭 설정")]
    [SerializeField] private GameObject[] objectsToDrop; // Inspector에서 드롭할 오브젝트들 지정
    [SerializeField] private float dropDelay = 0.3f;     // 각 오브젝트 사이의 드롭 딜레이
    [SerializeField] private float dropForce = 5f;       // 오브젝트가 떨어지는 힘
    
    [Header("드롭 방향")]
    [SerializeField] private Vector2 dropDirection = Vector2.down; // 기본 방향은 아래쪽
    
    private bool hasTriggered = false;
    private Coroutine dropRoutine;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 시에만 작동
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            
            // 배열에 오브젝트가 있는지 확인
            if (objectsToDrop.Length > 0)
            {
                // 드롭 코루틴 시작
                dropRoutine = StartCoroutine(DropObjectsSequentially());
            }
            else
            {
                Debug.LogWarning("드롭할 오브젝트가 없습니다. Inspector에서 오브젝트를 추가해주세요.");
            }
        }
    }
    
    private IEnumerator DropObjectsSequentially()
    {
        // 배열의 각 오브젝트를 순차적으로 처리
        for (int i = 0; i < objectsToDrop.Length; i++)
        {
            GameObject obj = objectsToDrop[i];
            
            if (obj != null)
            {
                // 오브젝트에 Rigidbody2D가 있는지 확인
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                
                if (rb == null)
                {
                    // Rigidbody2D가 없으면 추가
                    rb = obj.AddComponent<Rigidbody2D>();
                }
                
                // 이전에 물리 이동이 제한되어 있었을 경우를 대비해 설정
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                rb.freezeRotation = true; // 회전 방지
                rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전만 고정
                
                // BoxCollider2D가 없으면 추가
                BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = obj.AddComponent<BoxCollider2D>();
                }
                
                // Ground 감지 컴포넌트 추가
                GroundLandingHandler landingHandler = obj.GetComponent<GroundLandingHandler>();
                if (landingHandler == null)
                {
                    landingHandler = obj.AddComponent<GroundLandingHandler>();
                }
                
                // 오브젝트를 떨어뜨리기 위한 힘 적용
                rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);
                
                // 다음 오브젝트가 떨어지기 전에 지정된 시간만큼 대기
                yield return new WaitForSeconds(dropDelay);
            }
            else
            {
                Debug.LogWarning($"배열의 {i}번 인덱스에 있는 오브젝트가 null입니다.");
            }
        }
    }
    
    // Unity 에디터에서 시각화
    private void OnDrawGizmos()
    {
        // 트리거 영역 시각화
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // 콜라이더 종류에 따라 다르게 그리기
            if (col is BoxCollider2D)
            {
                BoxCollider2D boxCollider = col as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                // 다른 형태의 콜라이더일 경우 간단하게 표시
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
        
        // 드롭 방향 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, dropDirection.normalized * 1.5f);
        
        // 연결된 오브젝트 표시
        Gizmos.color = Color.cyan;
        if (objectsToDrop != null)
        {
            for (int i = 0; i < objectsToDrop.Length; i++)
            {
                if (objectsToDrop[i] != null)
                {
                    Gizmos.DrawLine(transform.position, objectsToDrop[i].transform.position);
                    
                    // 드롭 순서 표시
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(objectsToDrop[i].transform.position + Vector3.up * 0.5f, $"{i+1}");
                    #endif
                }
            }
        }
    }
}

// Ground 감지 및 처리를 위한 컴포넌트
public class GroundLandingHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool hasLanded = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 레이어로 Ground 감지 (레이어가 "Ground"인 경우)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && !hasLanded)
        {
            hasLanded = true;
            
            // 충돌 지점 확인
            ContactPoint2D contact = collision.GetContact(0);
            
            // Ground의 위쪽 표면에 충돌했는지 확인 (y 법선이 위쪽을 향함)
            if (contact.normal.y > 0.5f)
            {
                // 충돌 위치 계산 - Ground 표면 위에 정확하게 위치시키기
                float halfHeight = boxCollider.bounds.extents.y;
                Vector3 newPosition = new Vector3(
                    transform.position.x, 
                    contact.point.y + halfHeight, 
                    transform.position.z);
                
                // 오브젝트 위치 조정
                transform.position = newPosition;
                
                // 물리 이동 중지
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f; // 중력 비활성화
                
                // x축 이동만 제한하고 싶다면:
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                
                // 또는 모든 이동 제한:
                // rb.constraints = RigidbodyConstraints2D.FreezeAll;
                
                Debug.Log($"{gameObject.name}이(가) Ground 위에 착지했습니다.");
            }
        }
    }
    
    // 추가적인 안정성을 위한 지속적인 체크 (선택 사항)
    private void FixedUpdate()
    {
        if (hasLanded)
        {
            // 이미 착지했으면 추가 처리가 필요 없음
            return;
        }
        
        // 아래쪽 레이캐스트로 Ground 체크 (선택적)
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            boxCollider.bounds.extents.y + 0.1f,
            LayerMask.GetMask("Ground"));
            
        if (hit.collider != null)
        {
            hasLanded = true;
            
            // Ground 위에 위치 조정
            float halfHeight = boxCollider.bounds.extents.y;
            Vector3 newPosition = new Vector3(
                transform.position.x,
                hit.point.y + halfHeight,
                transform.position.z);
                
            transform.position = newPosition;
            
            // 물리 이동 중지
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            
            Debug.Log($"{gameObject.name}이(가) Ground 위에 레이캐스트로 착지했습니다.");
        }
    }
}