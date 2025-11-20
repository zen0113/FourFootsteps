using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformActivationTrigger : MonoBehaviour
{
    [Header("트리거 설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool useOnlyOnce = true; // 한 번만 작동하도록 할지 여부
    
    [Header("활성화할 오브젝트 배열")]
    [SerializeField] private GameObject[] targetObjects;
    
    [Header("시각적 피드백")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Header("디버그")]
    [SerializeField] private bool debugMode = false;
    
    [Header("활성화 조건")]
    private bool isActivated = false; // 활성화 상태 (사슬이 끊어졌는지)
    
    /// <summary>
    /// 트리거 활성화 상태 확인용 프로퍼티
    /// </summary>
    public bool IsActivated => isActivated;
    
    private bool hasTriggered = false;
    private BoxCollider2D triggerCollider;
    
    private void Awake()
    {
   
        SetupTriggerCollider();
    }
    
    private void Start()
    {
        if (debugMode)
        {
            Debug.Log($"PlatformActivationTrigger 초기화됨. 대상 오브젝트 수: {targetObjects.Length}");
        }
    }
    
    /// <summary>
    /// 트리거 콜리더 설정
    /// </summary>
    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            if (debugMode)
            {
                Debug.Log("BoxCollider2D가 자동으로 추가되었습니다.");
            }
        }
        
        triggerCollider.isTrigger = true;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ★ 핵심: isActivated가 false면 (사슬이 안 끊어졌으면) 작동하지 않음
        if (!isActivated)
        {
            if (debugMode)
            {
                Debug.Log("PlatformActivationTrigger가 아직 활성화되지 않았습니다. (사슬을 먼저 끊으세요)");
            }
            return;
        }
        
        // 플레이어가 들어왔고, 아직 작동하지 않았거나 재사용 가능한 경우
        if (other.CompareTag(playerTag) && (!hasTriggered || !useOnlyOnce))
        {
            ActivatePlatforms();
            
            if (useOnlyOnce)
            {
                hasTriggered = true;
            }
        }
    }
    
    /// <summary>
    /// 배열에 있는 모든 오브젝트의 BoxCollider2D와 PlatformEffector2D 활성화
    /// </summary>
    private void ActivatePlatforms()
    {
        if (debugMode)
        {
            Debug.Log("플랫폼 활성화 시작!");
        }
        
        foreach (GameObject obj in targetObjects)
        {
            if (obj == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("배열에 null 오브젝트가 있습니다.");
                }
                continue;
            }
            
            // BoxCollider2D 활성화
            BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.enabled = true;
                if (debugMode)
                {
                    Debug.Log($"{obj.name}의 BoxCollider2D 활성화됨");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"{obj.name}에 BoxCollider2D가 없습니다.");
            }
            
            // PlatformEffector2D 활성화
            PlatformEffector2D platformEffector = obj.GetComponent<PlatformEffector2D>();
            if (platformEffector != null)
            {
                platformEffector.enabled = true;
                if (debugMode)
                {
                    Debug.Log($"{obj.name}의 PlatformEffector2D 활성화됨");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"{obj.name}에 PlatformEffector2D가 없습니다.");
            }
        }
        
        if (debugMode)
        {
            Debug.Log("모든 플랫폼 활성화 완료!");
        }
    }
    
    /// <summary>
    /// 에디터에서 트리거 영역 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 트리거 상태에 따른 색상 변경
        if (isActivated)
        {
            Gizmos.color = gizmoColor; // 활성화된 상태 - 초록색
        }
        else
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 비활성화 상태 - 빨간색
        }
        
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = col.offset;
            Vector3 size = col.size;
            Gizmos.DrawCube(center, size);
            
            // 테두리 표시
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        
        // 대상 오브젝트들과의 연결선 표시
        if (targetObjects != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] != null)
                {
                    Gizmos.DrawLine(transform.position, targetObjects[i].transform.position);
                    
                    #if UNITY_EDITOR
                    // 오브젝트 번호 표시 (에디터에서만)
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(targetObjects[i].transform.position + Vector3.up * 0.5f, (i + 1).ToString());
                    #endif
                }
            }
        }
    }
    
    /// <summary>
    /// 외부에서 트리거를 활성화 (ChainInteraction에서 호출)
    /// </summary>
    public void ActivateTrigger()
    {
        isActivated = true;
        if (debugMode)
        {
            Debug.Log("PlatformActivationTrigger가 활성화되었습니다!");
        }
    }
    
    /// <summary>
    /// 트리거 비활성화 (사슬이 다시 연결되었을 때 등)
    /// </summary>
    public void DeactivateTrigger()
    {
        isActivated = false;
        if (debugMode)
        {
            Debug.Log("PlatformActivationTrigger가 비활성화되었습니다.");
        }
    }
    
    /// <summary>
    /// 수동으로 플랫폼 활성화 (다른 스크립트에서 호출 가능)
    /// </summary>
    public void ManualActivate()
    {
        if (!isActivated)
        {
            if (debugMode)
            {
                Debug.LogWarning("트리거가 비활성화 상태에서는 수동 활성화할 수 없습니다.");
            }
            return;
        }
        
        if (!hasTriggered || !useOnlyOnce)
        {
            ActivatePlatforms();
            if (useOnlyOnce)
            {
                hasTriggered = true;
            }
        }
    }
    
    /// <summary>
    /// 트리거 상태 리셋 (재사용을 위해)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        if (debugMode)
        {
            Debug.Log("트리거 상태가 리셋되었습니다.");
        }
    }
}