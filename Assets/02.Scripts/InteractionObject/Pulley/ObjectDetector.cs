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
    [SerializeField] private float exitGraceTime = 0.15f; // 경계 떨림/플랫폼 이동으로 인한 Exit 스팸 방지
    
    private List<DetectedObject> detectedObjects = new List<DetectedObject>();
    private PulleyPlatform parentPlatform;

    // 여러 콜라이더/미세한 떨림을 안정화하기 위한 카운트/유예 타이머
    private readonly Dictionary<Transform, int> overlapCounts = new Dictionary<Transform, int>();
    private readonly Dictionary<Transform, float> pendingExitDeadline = new Dictionary<Transform, float>();
    
    // 이벤트
    public System.Action<ObjectType, float> OnPriorityChanged;
    
    private void Start()
    {
        // 같은 게임오브젝트 또는 부모에서 PulleyPlatform 찾기
        parentPlatform = GetComponent<PulleyPlatform>();
        
        if (parentPlatform == null)
        {
            parentPlatform = GetComponentInParent<PulleyPlatform>();
        }
        
        // 여전히 없으면 씬 전체에서 찾기 (분리된 구조)
        if (parentPlatform == null)
        {
            parentPlatform = FindObjectOfType<PulleyPlatform>();
        }
        
        if (parentPlatform == null)
        {
            Debug.LogError($"ObjectDetector({name})가 PulleyPlatform을 찾을 수 없습니다!");
            return;
        }
        
        if (showDebug)
            Debug.Log($"✓ ObjectDetector({name})가 PulleyPlatform({parentPlatform.name})을 찾았습니다.");
        
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

        Transform key = GetKeyTransform(other);

        // Exit 유예 중이었다면 취소
        if (pendingExitDeadline.ContainsKey(key))
            pendingExitDeadline.Remove(key);

        // 이미 내부로 카운트되어 있으면 카운트만 증가
        if (overlapCounts.ContainsKey(key))
        {
            overlapCounts[key] += 1;
            return;
        }

        overlapCounts[key] = 1;

        // 처음 들어온 경우에만 DetectedObject 추가/로그
        DetectedObject newObject = CreateDetectedObject(other, key);
        if (newObject.IsValid)
        {
            detectedObjects.Add(newObject);
            EvaluatePriority();

            if (showDebug)
                Debug.Log($"[{name}] 📦 오브젝트 올라옴: {newObject.objectName} (타입: {newObject.type}, 무게: {newObject.weight})");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInDetectionLayer(other.gameObject)) return;

        Transform key = GetKeyTransform(other);

        if (!overlapCounts.ContainsKey(key))
            return;

        overlapCounts[key] -= 1;
        if (overlapCounts[key] > 0)
            return;

        // 0이 된 경우 즉시 제거하지 않고 유예시간을 둔다(경계 떨림/MovePosition 영향)
        overlapCounts[key] = 0;
        pendingExitDeadline[key] = Time.time + exitGraceTime;
    }
    
    private bool IsInDetectionLayer(GameObject obj)
    {
        return ((detectionLayer.value & (1 << obj.layer)) > 0);
    }
    
    private DetectedObject CreateDetectedObject(Collider2D collider, Transform keyTransform)
    {
        GameObject obj = collider.gameObject;
        ObjectType type = DetermineObjectType(obj);
        float weight = GetObjectWeight(obj, type);

        // keyTransform(보통 attachedRigidbody의 Transform)을 기록해서
        // 여러 콜라이더/자식 콜라이더가 있어도 한 오브젝트로 취급
        return new DetectedObject(keyTransform, type, weight);
    }

    private Transform GetKeyTransform(Collider2D col)
    {
        return col.attachedRigidbody != null ? col.attachedRigidbody.transform : col.transform;
    }

    private void Update()
    {
        if (pendingExitDeadline.Count == 0) return;

        // 컬렉션 수정 안전하게 처리
        var keys = pendingExitDeadline.Keys.ToList();
        foreach (var key in keys)
        {
            if (key == null)
            {
                pendingExitDeadline.Remove(key);
                continue;
            }

            // 유예 시간 동안 재진입이 없고(Enter에서 pendingExitDeadline 제거됨)
            // 카운트가 0인 상태로 유지되면 제거 확정
            if (Time.time < pendingExitDeadline[key]) continue;
            if (!overlapCounts.ContainsKey(key) || overlapCounts[key] > 0)
            {
                pendingExitDeadline.Remove(key);
                continue;
            }

            pendingExitDeadline.Remove(key);
            overlapCounts.Remove(key);

            int removedCount = detectedObjects.RemoveAll(obj => obj.objectTransform == key);
            if (removedCount > 0)
            {
                if (showDebug)
                    Debug.Log($"[{name}] 📤 오브젝트 내려옴: {key.name}");

                EvaluatePriority();
            }
        }
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
            if (showDebug)
                Debug.Log($"[{name}] 🔄 상태 업데이트: Empty");
            
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
        
        if (showDebug)
            Debug.Log($"[{name}] 🔄 상태 업데이트: {highestPriority} (무게: {totalWeight:F1})");
        
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