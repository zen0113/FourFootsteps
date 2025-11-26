using UnityEngine;

/// <summary>
/// 플레이어가 트리거에 닿으면 지정된 위치에 까마귀 공격 생성
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CrowAttackTrigger : MonoBehaviour
{
    [Header("Crow Settings")]
    [Tooltip("까마귀 프리팹")]
    public GameObject crowPrefab;
    
    [Tooltip("까마귀 시작 위치 (여기서 날아옴)")]
    public Transform crowStartPosition;
    
    [Tooltip("경고 및 공격 목표 위치 (까마귀가 여기로 낙하)")]
    public Transform attackPosition;

    [Header("Trigger Settings")]
    [Tooltip("플레이어 태그")]
    public string playerTag = "Player";
    
    [Header("Visual Gizmo")]
    [Tooltip("씬 뷰에서 트리거 영역 표시")]
    public bool showGizmo = true;

    private BoxCollider2D triggerCollider;
    private bool hasTriggered = false;

    void Start()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 실행되었으면 무시
        if (hasTriggered) return;

        // 플레이어와 충돌 확인
        if (collision.CompareTag(playerTag))
        {
            SpawnCrow();
            hasTriggered = true;
            triggerCollider.enabled = false;
            
            Debug.Log($"[CrowTrigger] {gameObject.name} 트리거 실행 완료");
        }
    }

    void SpawnCrow()
    {
        if (crowPrefab == null)
        {
            Debug.LogError($"[CrowTrigger] {gameObject.name}: 까마귀 프리팹이 없습니다!");
            return;
        }

        if (crowStartPosition == null)
        {
            Debug.LogError($"[CrowTrigger] {gameObject.name}: 까마귀 시작 위치가 없습니다!");
            return;
        }

        if (attackPosition == null)
        {
            Debug.LogError($"[CrowTrigger] {gameObject.name}: 공격 위치가 없습니다!");
            return;
        }

        // 까마귀 생성 (시작 위치에)
        GameObject crow = Instantiate(crowPrefab, crowStartPosition.position, Quaternion.identity);
        
        // RoadCrow 초기화
        RoadCrow crowScript = crow.GetComponent<RoadCrow>();
        if (crowScript != null)
        {
            crowScript.Initialize(crowStartPosition.position, attackPosition.position);
        }
        
        Debug.Log($"[CrowTrigger] 까마귀 생성 - 시작: {crowStartPosition.position}, 목표: {attackPosition.position}");
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        
        // 트리거 영역 (초록색)
        if (showGizmo && col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.offset, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // 까마귀 시작 위치 (파란색)
        if (crowStartPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(crowStartPosition.position, 0.5f);
            Gizmos.DrawLine(crowStartPosition.position, crowStartPosition.position + Vector3.down * 1f);
        }

        // 공격 위치 (빨간색) - 경고가 뜨고 까마귀가 낙하할 위치
        if (attackPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition.position, 0.5f);
            Gizmos.DrawLine(attackPosition.position, attackPosition.position + Vector3.up * 2f);
        }

        // 까마귀 경로 (시안색)
        if (crowStartPosition != null && attackPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(crowStartPosition.position, attackPosition.position);
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        triggerCollider.enabled = true;
    }
}