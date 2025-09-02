using UnityEngine;

/// <summary>
/// 현실적인 도르레 시스템 - 실제 물리 법칙을 따름
/// 핵심 원리: 무게 차이에 따라 한쪽은 최대한 내려가고, 다른쪽은 최대한 올라감
/// </summary>
public class PulleySystem : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private PulleyPlatform platformA;
    [SerializeField] private PulleyPlatform platformB;
    
    [Header("도르레 물리 설정")]
    [SerializeField] private float maxHeight = 4f;      // 최대 올라갈 수 있는 높이
    [SerializeField] private float minHeight = -4f;     // 최대 내려갈 수 있는 높이
    [SerializeField] private float weightThreshold = 0.1f; // 무게 차이 임계값
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
        // 플랫폼 자동 찾기
        if (platformA == null || platformB == null)
        {
            AutoFindPlatforms();
        }
        
        if (platformA == null || platformB == null)
        {
            Debug.LogError("PulleySystem: 플랫폼 A 또는 B가 설정되지 않았습니다!");
            return;
        }
        
        // 초기 위치 설정 (둘 다 중간 높이에서 시작)
        SetupInitialPositions();
        
        // 이벤트 연결
        platformA.OnPriorityChanged += OnPlatformAChanged;
        platformB.OnPriorityChanged += OnPlatformBChanged;
        
        platformA.OnMoveComplete += OnPlatformMoveComplete;
        platformB.OnMoveComplete += OnPlatformMoveComplete;
        
        // 초기 상태 평가
        EvaluateSystemState();
        
        if (enableDebugLogs)
            Debug.Log("현실적인 PulleySystem 초기화 완료");
    }
    
    private void SetupInitialPositions()
    {
        // 두 플랫폼을 중간 높이(0)에서 시작
        platformA.SetInitialHeight(0f);
        platformB.SetInitialHeight(0f);
        
        if (enableDebugLogs)
            Debug.Log("초기 위치 설정: 두 플랫폼 모두 높이 0에서 시작");
    }
    
    private void AutoFindPlatforms()
    {
        PulleyPlatform[] platforms = GetComponentsInChildren<PulleyPlatform>();
        
        if (platforms.Length >= 2)
        {
            platformA = platforms[0];
            platformB = platforms[1];
            
            if (enableDebugLogs)
                Debug.Log($"자동으로 플랫폼 감지됨: A={platformA.name}, B={platformB.name}");
        }
        else
        {
            Debug.LogError($"PulleySystem: 2개의 PulleyPlatform이 필요합니다. (현재 {platforms.Length}개 발견)");
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
        // 이동 중일 때는 새로운 평가를 하지 않음
        if (platformA.IsMoving || platformB.IsMoving)
        {
            currentState = "Moving";
            return;
        }
        
        // 현실적인 도르레 물리 법칙 적용
        ApplyRealisticPulleyPhysics();
    }
    
    private void ApplyRealisticPulleyPhysics()
    {
        bool hasWeightA = platformA_Priority != ObjectType.Empty;
        bool hasWeightB = platformB_Priority != ObjectType.Empty;
        
        if (!hasWeightA && !hasWeightB)
        {
            // 둘 다 비어있음 → 현재 위치 유지 (관성의 법칙)
            currentState = "Both Empty - Maintaining Current Position";
            lastAction = "No movement (inertia)";
            return;
        }
        
        if (hasWeightA && !hasWeightB)
        {
            // A에만 무게 → A는 바닥으로, B는 최대 높이로
            MovePlatformsToExtremes(minHeight, maxHeight, "Only A has weight");
        }
        else if (!hasWeightA && hasWeightB)
        {
            // B에만 무게 → B는 바닥으로, A는 최대 높이로  
            MovePlatformsToExtremes(maxHeight, minHeight, "Only B has weight");
        }
        else
        {
            // 둘 다 무게가 있음 → 무게 비교
            CompareWeightsAndMove();
        }
    }
    
    private void CompareWeightsAndMove()
    {
        // 우선순위 비교 (PhysicsObject > Player)
        if (platformA_Priority > platformB_Priority)
        {
            // A의 우선순위가 높음 (예: A에 박스, B에 플레이어)
            MovePlatformsToExtremes(minHeight, maxHeight, "A has higher priority");
        }
        else if (platformB_Priority > platformA_Priority)
        {
            // B의 우선순위가 높음
            MovePlatformsToExtremes(maxHeight, minHeight, "B has higher priority");
        }
        else
        {
            // 같은 우선순위 → 무게 비교
            HandleSameTypeComparison();
        }
    }
    
    private void HandleSameTypeComparison()
    {
        float weightDifference = platformA_Weight - platformB_Weight;
        
        if (Mathf.Abs(weightDifference) <= weightThreshold)
        {
            // 무게 차이가 임계값 이하 → 현재 위치 유지
            currentState = "Weights are balanced - Maintaining position";
            lastAction = "No movement (balanced)";
            
            if (enableDebugLogs)
                Debug.Log($"균형 상태: A무게={platformA_Weight:F1}, B무게={platformB_Weight:F1}, 차이={Mathf.Abs(weightDifference):F1}");
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
        // 현재 위치와 목표 위치가 거의 같으면 이동하지 않음
        if (Mathf.Abs(platformA.CurrentHeight - targetHeightA) < 0.1f && 
            Mathf.Abs(platformB.CurrentHeight - targetHeightB) < 0.1f)
        {
            currentState = $"Already in correct position - {reason}";
            return;
        }
        
        // 도르레 제약 조건 확인: A + B = 일정값 (0으로 설정)
        float totalHeight = targetHeightA + targetHeightB;
        if (Mathf.Abs(totalHeight) > 0.1f)
        {
            // 높이 합이 0이 되도록 조정
            float adjustment = -totalHeight / 2f;
            targetHeightA += adjustment;
            targetHeightB += adjustment;
        }
        
        // 범위 제한
        targetHeightA = Mathf.Clamp(targetHeightA, minHeight, maxHeight);
        targetHeightB = Mathf.Clamp(targetHeightB, minHeight, maxHeight);
        
        // 플랫폼 이동 실행
        platformA.MoveToHeight(targetHeightA);
        platformB.MoveToHeight(targetHeightB);
        
        currentState = $"Moving to extremes - {reason}";
        lastAction = $"A→{targetHeightA:F1}, B→{targetHeightB:F1}";
        
        if (enableDebugLogs)
        {
            Debug.Log($"도르레 극한 이동: {reason}");
            Debug.Log($"  A: {platformA.CurrentHeight:F1} → {targetHeightA:F1}");
            Debug.Log($"  B: {platformB.CurrentHeight:F1} → {targetHeightB:F1}");
            Debug.Log($"  우선순위: A={platformA_Priority}({platformA_Weight:F1}), B={platformB_Priority}({platformB_Weight:F1})");
        }
    }
    
    // 외부 제어 메서드들
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
        Debug.Log($"Height Range: {minHeight} to {maxHeight}");
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
            
            // 이동 범위 표시
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            
            // A 플랫폼 범위
            Vector3 aBase = platformA.transform.position - Vector3.up * platformA.CurrentHeight;
            Gizmos.DrawLine(aBase + Vector3.up * maxHeight, aBase + Vector3.up * minHeight);
            Gizmos.DrawWireCube(aBase + Vector3.up * maxHeight, new Vector3(1, 0.1f, 1));
            Gizmos.DrawWireCube(aBase + Vector3.up * minHeight, new Vector3(1, 0.1f, 1));
            
            // B 플랫폼 범위
            Vector3 bBase = platformB.transform.position - Vector3.up * platformB.CurrentHeight;
            Gizmos.DrawLine(bBase + Vector3.up * maxHeight, bBase + Vector3.up * minHeight);
            Gizmos.DrawWireCube(bBase + Vector3.up * maxHeight, new Vector3(1, 0.1f, 1));
            Gizmos.DrawWireCube(bBase + Vector3.up * minHeight, new Vector3(1, 0.1f, 1));
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