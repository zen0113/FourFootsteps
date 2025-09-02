using UnityEngine;

/// <summary>
/// 현실적인 도르레 시스템 - 분리된 플랫폼과 작동
/// </summary>
public class PulleySystem : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private PulleyPlatform platformA;
    [SerializeField] private PulleyPlatform platformB;
    
    [Header("도르레 물리 설정")]
    [SerializeField] private float maxHeight = 4f;
    [SerializeField] private float minHeight = -4f;
    [SerializeField] private float weightThreshold = 0.1f;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("상태 모니터링 (읽기 전용)")]
    [SerializeField] private ObjectType platformA_Priority = ObjectType.Empty;
    [SerializeField] private ObjectType platformB_Priority = ObjectType.Empty;
    [SerializeField] private float platformA_Weight = 0f;
    [SerializeField] private float platformB_Weight = 0f;
    [SerializeField] private string currentState = "Idle";
    [SerializeField] private string lastAction = "None";
    
    private void Start()
    {
        // 플랫폼 찾기 (분리된 플랫폼들을 찾아야 함)
        if (platformA == null || platformB == null)
        {
            AutoFindPlatforms();
        }
        
        if (platformA == null || platformB == null)
        {
            Debug.LogError("PulleySystem: 플랫폼 A 또는 B가 설정되지 않았습니다!");
            return;
        }
        
        // 초기 위치 설정
        SetupInitialPositions();
        
        // 이벤트 연결
        platformA.OnPriorityChanged += OnPlatformAChanged;
        platformB.OnPriorityChanged += OnPlatformBChanged;
        
        platformA.OnMoveComplete += OnPlatformMoveComplete;
        platformB.OnMoveComplete += OnPlatformMoveComplete;
        
        // 초기 상태 평가
        EvaluateSystemState();
        
        if (enableDebugLogs)
            Debug.Log("현실적인 PulleySystem 초기화 완료 (분리된 플랫폼)");
    }
    
    private void SetupInitialPositions()
    {
        platformA.SetInitialHeight(0f);
        platformB.SetInitialHeight(0f);
        
        if (enableDebugLogs)
            Debug.Log("초기 위치 설정: 두 플랫폼 모두 높이 0에서 시작");
    }
    
    private void AutoFindPlatforms()
    {
        // 씬 전체에서 PulleyPlatform 찾기 (이제 자식이 아니므로)
        PulleyPlatform[] allPlatforms = FindObjectsOfType<PulleyPlatform>();
        
        if (allPlatforms.Length >= 2)
        {
            // 가장 가까운 두 개의 플랫폼을 선택
            float minDistance = float.MaxValue;
            PulleyPlatform closestA = null, closestB = null;
            
            for (int i = 0; i < allPlatforms.Length; i++)
            {
                for (int j = i + 1; j < allPlatforms.Length; j++)
                {
                    float distanceA = Vector3.Distance(transform.position, allPlatforms[i].transform.position);
                    float distanceB = Vector3.Distance(transform.position, allPlatforms[j].transform.position);
                    float totalDistance = distanceA + distanceB;
                    
                    if (totalDistance < minDistance)
                    {
                        minDistance = totalDistance;
                        closestA = allPlatforms[i];
                        closestB = allPlatforms[j];
                    }
                }
            }
            
            platformA = closestA;
            platformB = closestB;
            
            if (enableDebugLogs)
                Debug.Log($"자동으로 가장 가까운 플랫폼 감지됨: A={platformA.name}, B={platformB.name}");
        }
        else
        {
            Debug.LogError($"PulleySystem: 2개의 PulleyPlatform이 필요합니다. (현재 {allPlatforms.Length}개 발견)");
        }
    }
    
    private void OnPlatformAChanged(PulleyPlatform platform, ObjectType priority, float weight)
    {
        platformA_Priority = priority;
        platformA_Weight = weight;
        EvaluateSystemState();
    }
    
    private void OnPlatformBChanged(PulleyPlatform platform, ObjectType priority, float weight)
    {
        platformB_Priority = priority;
        platformB_Weight = weight;
        EvaluateSystemState();
    }
    
    private void OnPlatformMoveComplete(PulleyPlatform platform)
    {
        if (enableDebugLogs)
            Debug.Log($"플랫폼 이동 완료: {platform.name} (높이: {platform.CurrentHeight:F1})");
    }
    
    private void EvaluateSystemState()
    {
        if (platformA.IsMoving || platformB.IsMoving)
        {
            currentState = "Moving";
            return;
        }
        
        ApplyRealisticPulleyPhysics();
    }
    
    private void ApplyRealisticPulleyPhysics()
    {
        bool hasWeightA = platformA_Priority != ObjectType.Empty;
        bool hasWeightB = platformB_Priority != ObjectType.Empty;
        
        if (!hasWeightA && !hasWeightB)
        {
            // 둘 다 비어있으면 평형 상태(중간 위치)로 복귀
            MovePlatformsToBalance("Both platforms empty - returning to balance");
            return;
        }
        
        if (hasWeightA && !hasWeightB)
        {
            MovePlatformsToExtremes(minHeight, maxHeight, "Only A has weight");
        }
        else if (!hasWeightA && hasWeightB)
        {
            MovePlatformsToExtremes(maxHeight, minHeight, "Only B has weight");
        }
        else
        {
            CompareWeightsAndMove();
        }
    }
    
    private void CompareWeightsAndMove()
    {
        if (platformA_Priority > platformB_Priority)
        {
            MovePlatformsToExtremes(minHeight, maxHeight, "A has higher priority");
        }
        else if (platformB_Priority > platformA_Priority)
        {
            MovePlatformsToExtremes(maxHeight, minHeight, "B has higher priority");
        }
        else
        {
            HandleSameTypeComparison();
        }
    }
    
    private void HandleSameTypeComparison()
    {
        float weightDifference = platformA_Weight - platformB_Weight;
        
        if (Mathf.Abs(weightDifference) <= weightThreshold)
        {
            // 무게 차이가 임계값 이하 → 평형 상태로 복귀
            MovePlatformsToBalance($"Weights are balanced (A:{platformA_Weight:F1} vs B:{platformB_Weight:F1})");
        }
        else if (weightDifference > 0)
        {
            // A가 더 무거움 → A는 바닥으로, B는 최대 높이로
            MovePlatformsToExtremes(minHeight, maxHeight, $"A is heavier ({platformA_Weight:F1} vs {platformB_Weight:F1})");
        }
        else
        {
            // B가 더 무거움 → B는 바닥으로, A는 최대 높이로
            MovePlatformsToExtremes(maxHeight, minHeight, $"B is heavier ({platformB_Weight:F1} vs {platformA_Weight:F1})");
        }
    }
    
    private void MovePlatformsToExtremes(float targetHeightA, float targetHeightB, string reason)
    {
        if (Mathf.Abs(platformA.CurrentHeight - targetHeightA) < 0.1f && 
            Mathf.Abs(platformB.CurrentHeight - targetHeightB) < 0.1f)
        {
            currentState = $"Already in correct position - {reason}";
            return;
        }
        
        // 도르레 제약 조건 확인
        float totalHeight = targetHeightA + targetHeightB;
        if (Mathf.Abs(totalHeight) > 0.1f)
        {
            float adjustment = -totalHeight / 2f;
            targetHeightA += adjustment;
            targetHeightB += adjustment;
        }
        
        targetHeightA = Mathf.Clamp(targetHeightA, minHeight, maxHeight);
        targetHeightB = Mathf.Clamp(targetHeightB, minHeight, maxHeight);
        
        platformA.MoveToHeight(targetHeightA);
        platformB.MoveToHeight(targetHeightB);
        
        currentState = $"Moving to extremes - {reason}";
        lastAction = $"A→{targetHeightA:F1}, B→{targetHeightB:F1}";
        
        if (enableDebugLogs)
        {
            Debug.Log($"도르레 이동: {reason}");
            Debug.Log($"  A: {platformA.CurrentHeight:F1} → {targetHeightA:F1}");
            Debug.Log($"  B: {platformB.CurrentHeight:F1} → {targetHeightB:F1}");
        }
    }
    
    /// <summary>
    /// 플랫폼들을 평형 상태(중간 위치)로 이동
    /// </summary>
    private void MovePlatformsToBalance(string reason)
    {
        float targetHeightA = 0f; // 평형 위치
        float targetHeightB = 0f; // 평형 위치
        
        // 이미 평형 상태에 있으면 이동하지 않음
        if (Mathf.Abs(platformA.CurrentHeight - targetHeightA) < 0.1f && 
            Mathf.Abs(platformB.CurrentHeight - targetHeightB) < 0.1f)
        {
            currentState = $"Already balanced - {reason}";
            return;
        }
        
        // 평형 위치로 이동
        platformA.MoveToHeight(targetHeightA);
        platformB.MoveToHeight(targetHeightB);
        
        currentState = $"Moving to balance - {reason}";
        lastAction = $"A→{targetHeightA:F1}, B→{targetHeightB:F1}";
        
        if (enableDebugLogs)
        {
            Debug.Log($"도르레 평형 복귀: {reason}");
            Debug.Log($"  A: {platformA.CurrentHeight:F1} → {targetHeightA:F1}");
            Debug.Log($"  B: {platformB.CurrentHeight:F1} → {targetHeightB:F1}");
        }
    }
    
    public bool IsSystemMoving()
    {
        return platformA.IsMoving || platformB.IsMoving;
    }
    
    public void ForceStopAllMovement()
    {
        platformA.StopMovement();
        platformB.StopMovement();
        currentState = "Force Stopped";
    }
    
    public void ResetToCenter()
    {
        platformA.MoveToHeight(0f);
        platformB.MoveToHeight(0f);
        currentState = "Resetting to Center";
    }
    
    [ContextMenu("Force Re-evaluate")]
    public void ForceReevaluate()
    {
        EvaluateSystemState();
    }
    
    [ContextMenu("Print System Status")]
    public void PrintSystemStatus()
    {
        Debug.Log($"=== Realistic Pulley System Status ===");
        Debug.Log($"Platform A: Priority={platformA_Priority}, Weight={platformA_Weight:F1}, Height={platformA.CurrentHeight:F1}");
        Debug.Log($"Platform B: Priority={platformB_Priority}, Weight={platformB_Weight:F1}, Height={platformB.CurrentHeight:F1}");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Last Action: {lastAction}");
        Debug.Log($"System Moving: {IsSystemMoving()}");
    }
    
    private void OnDrawGizmos()
    {
        // 도르레 중심점
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        if (platformA != null && platformB != null)
        {
            // 밧줄 시각화
            Gizmos.color = IsSystemMoving() ? Color.red : Color.cyan;
            Gizmos.DrawLine(platformA.transform.position, transform.position);
            Gizmos.DrawLine(transform.position, platformB.transform.position);
            
            // 도르레 휠
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        
        // 상태 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
            $"Pulley System\n{currentState}\n{lastAction}");
        #endif
    }
    
    private void OnDestroy()
    {
        if (platformA != null)
        {
            platformA.OnPriorityChanged -= OnPlatformAChanged;
            platformA.OnMoveComplete -= OnPlatformMoveComplete;
        }
        
        if (platformB != null)
        {
            platformB.OnPriorityChanged -= OnPlatformBChanged;
            platformB.OnMoveComplete -= OnPlatformMoveComplete;
        }
    }
}