using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 도르레 플랫폼 위의 오브젝트를 감지하고 우선순위를 판별하는 컴포넌트
/// </summary>
public class ObjectDetector : MonoBehaviour
{
    [Header("감지 설정")]
    [SerializeField] private LayerMask detectionLayer = -1;
    [SerializeField] private bool showDebug = true;
    
    private List<DetectedObject> detectedObjects = new List<DetectedObject>();
    private PulleyPlatform parentPlatform;
    
    // 이벤트
    public System.Action<ObjectType, float> OnPriorityChanged;
    
    private void Start()
    {
        // 부모의 PulleyPlatform 컴포넌트 찾기
        parentPlatform = GetComponentInParent<PulleyPlatform>();
        
        if (parentPlatform == null)
        {
            Debug.LogError($"ObjectDetector({name})가 PulleyPlatform을 찾을 수 없습니다!");
        }
        
        // Trigger 설정 확인
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning($"ObjectDetector({name})의 Collider가 Trigger로 설정되지 않았습니다!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInDetectionLayer(other.gameObject)) return;
        
        DetectedObject newObject = CreateDetectedObject(other);
        if (newObject.IsValid)
        {
            detectedObjects.Add(newObject);
            EvaluatePriority();
            
            if (showDebug)
                Debug.Log($"[{name}] 오브젝트 감지됨: {newObject.objectName} (타입: {newObject.type}, 무게: {newObject.weight})");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInDetectionLayer(other.gameObject)) return;
        
        // 해당 오브젝트 제거
        int removedCount = detectedObjects.RemoveAll(obj => obj.objectTransform == other.transform);
        
        if (removedCount > 0)
        {
            EvaluatePriority();
            
            if (showDebug)
                Debug.Log($"[{name}] 오브젝트 제거됨: {other.name}");
        }
    }
    
    private bool IsInDetectionLayer(GameObject obj)
    {
        return ((detectionLayer.value & (1 << obj.layer)) > 0);
    }
    
    private DetectedObject CreateDetectedObject(Collider2D collider)
    {
        GameObject obj = collider.gameObject;
        ObjectType type = DetermineObjectType(obj);
        float weight = GetObjectWeight(obj, type);
        
        return new DetectedObject(obj.transform, type, weight);
    }
    
    private ObjectType DetermineObjectType(GameObject obj)
    {
        if (obj.CompareTag("Player"))
            return ObjectType.Player;
            
        if (obj.CompareTag("PhysicsObject") || obj.GetComponent<Rigidbody2D>() != null)
            return ObjectType.PhysicsObject;
            
        return ObjectType.Empty;
    }
    
    private float GetObjectWeight(GameObject obj, ObjectType type)
    {
        switch (type)
        {
            case ObjectType.Player:
                return 1.0f; // 플레이어 기본 무게
                
            case ObjectType.PhysicsObject:
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                return rb != null ? rb.mass : 2.0f; // 기본 물리 오브젝트 무게
                
            default:
                return 0f;
        }
    }
    
    private void EvaluatePriority()
    {
        // 유효하지 않은 오브젝트들 제거
        detectedObjects.RemoveAll(obj => !obj.IsValid);
        
        if (detectedObjects.Count == 0)
        {
            OnPriorityChanged?.Invoke(ObjectType.Empty, 0f);
            return;
        }
        
        // 우선순위 계산
        ObjectType highestPriority = detectedObjects.Max(obj => obj.type);
        
        float totalWeight = 0f;
        
        if (highestPriority == ObjectType.PhysicsObject)
        {
            // 물리 오브젝트들의 총 무게 계산
            totalWeight = detectedObjects
                .Where(obj => obj.type == ObjectType.PhysicsObject)
                .Sum(obj => obj.weight);
        }
        else if (highestPriority == ObjectType.Player)
        {
            // 플레이어만 있을 때
            totalWeight = 1.0f;
        }
        
        OnPriorityChanged?.Invoke(highestPriority, totalWeight);
    }
    
    public ObjectType GetCurrentPriority()
    {
        if (detectedObjects.Count == 0)
            return ObjectType.Empty;
            
        return detectedObjects.Max(obj => obj.type);
    }
    
    public float GetCurrentWeight()
    {
        ObjectType priority = GetCurrentPriority();
        
        if (priority == ObjectType.PhysicsObject)
        {
            return detectedObjects
                .Where(obj => obj.type == ObjectType.PhysicsObject)
                .Sum(obj => obj.weight);
        }
        else if (priority == ObjectType.Player)
        {
            return 1.0f;
        }
        
        return 0f;
    }
    
    // 디버그용
    public List<DetectedObject> GetDetectedObjects()
    {
        return new List<DetectedObject>(detectedObjects);
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        
        // Trigger 영역 시각화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = detectedObjects.Count > 0 ? Color.red : Color.green;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            
            if (col is BoxCollider2D)
            {
                BoxCollider2D boxCollider = col as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
        
        // 감지된 오브젝트들과의 연결선 그리기
        Gizmos.color = Color.cyan;
        foreach (var obj in detectedObjects)
        {
            if (obj.IsValid)
            {
                Gizmos.DrawLine(transform.position, obj.objectTransform.position);
            }
        }
    }
}