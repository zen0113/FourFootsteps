using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도르레 시스템 전체를 관리하는 메인 컴포넌트
/// </summary>
public class PulleySystem : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private PulleyPlatform platformA;
    [SerializeField] private PulleyPlatform platformB;
    
    [Header("시스템 설정")]
    [SerializeField] private float weightThreshold = 0.1f;  // 무게 차이 임계값
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("상태 모니터링 (읽기 전용)")]
    [SerializeField] private ObjectType platformA_Priority = ObjectType.Empty;
    [SerializeField] private ObjectType platformB_Priority = ObjectType.Empty;
    [SerializeField] private float platformA_Weight = 0f;
    [SerializeField] private float platformB_Weight = 0f;
    [SerializeField] private string currentState = "Idle";
    
    private void Start()
    {
        // 플랫폼 컴포넌트 자동 찾기 (설정되지 않은 경우)
        if (platformA == null || platformB == null)
        {
            AutoFindPlatforms();
        }
        
        // 플랫폼 유효성 검증
        if (platformA == null || platformB == null)
        {
            Debug.LogError("PulleySystem: 플랫폼 A 또는 B가 설정되지 않았습니다!");
            return;
        }
        
        // 이벤트 연결
        platformA.OnPriorityChanged += OnPlatformAChanged;
        platformB.OnPriorityChanged += OnPlatformBChanged;
        
        platformA.OnMoveComplete += OnPlatformMoveComplete;
        platformB.OnMoveComplete += OnPlatformMoveComplete;
        
        // 초기 상태 설정
        EvaluateSystemState();
        
        if (enableDebugLogs)
            Debug.Log("PulleySystem 초기화 완료");
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
            Debug.Log($"플랫폼 이동 완료: {platform.name}");
    }
    
    private void EvaluateSystemState()
    {
        // 이동 중일 때는 새로운 평가를 하지 않음
        if (platformA.IsMoving || platformB.IsMoving)
        {
            currentState = "Moving";
            return;
        }
        
        // 우선순위 비교
        if (platformA_Priority > platformB_Priority)
        {
            // 플랫폼 A가 우선순위가 높음 -> A가 내려가고 B가 올라감
            MovePlatforms(platformA, platformB, "A has higher priority");
        }
        else if (platformB_Priority > platformA_Priority)
        {
            // 플랫폼 B가 우선순위가 높음 -> B가 내려가고 A가 올라감
            MovePlatforms(platformB, platformA, "B has higher priority");
        }
        else if (platformA_Priority == platformB_Priority)
        {
            // 같은 우선순위일 때 처리
            HandleSamePriority();
        }
    }
    
    private void HandleSamePriority()
    {
        switch (platformA_Priority)
        {
            case ObjectType.Empty:
                // 둘 다 비어있음 -> 현재 상태 유지
                currentState = "Both Empty - Maintain";
                break;
                
            case ObjectType.Player:
                // 둘 다 플레이어 -> 현재 상태 유지
                currentState = "Both Players - Maintain";
                break;
                
            case ObjectType.PhysicsObject:
                // 둘 다 물리 오브젝트 -> 무게 비교
                HandleWeightComparison();
                break;
        }
    }
    
    private void HandleWeightComparison()
    {
        float weightDifference = Mathf.Abs(platformA_Weight - platformB_Weight);
        
        if (weightDifference < weightThreshold)
        {
            // 무게 차이가 임계값보다 작음 -> 현재 상태 유지
            currentState = "Weight Balanced - Maintain";
        }
        else if (platformA_Weight > platformB_Weight)
        {
            // A가 더 무거움 -> A가 내려가고 B가 올라감
            MovePlatforms(platformA, platformB, "A is heavier");
        }
        else
        {
            // B가 더 무거움 -> B가 내려가고 A가 올라감
            MovePlatforms(platformB, platformA, "B is heavier");
        }
    }
    
    private void MovePlatforms(PulleyPlatform downPlatform, PulleyPlatform upPlatform, string reason)
    {
        // 이미 올바른 위치에 있는지 확인
        if (downPlatform.IsAtStartPoint && !upPlatform.IsAtStartPoint)
        {
            currentState = $"Correct Position - {reason}";
            return;
        }
        
        // 플랫폼 이동 실행
        downPlatform.MoveToStartPoint();  // 무거운/우선순위 높은 플랫폼이 아래로
        upPlatform.MoveToEndPoint();      // 가벼운/우선순위 낮은 플랫폼이 위로
        
        currentState = $"Moving - {reason}";
        
        if (enableDebugLogs)
        {
            Debug.Log($"도르레 이동: {downPlatform.name}(하강), {upPlatform.name}(상승) - {reason}");
            Debug.Log($"무게: A={platformA_Weight:F1}, B={platformB_Weight:F1}");
        }
    }
    
    // 외부에서 시스템 상태를 확인할 수 있는 메서드들
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
    
    public void ManualEvaluation()
    {
        EvaluateSystemState();
    }
    
    // 디버그 정보 출력
    [ContextMenu("Print System Status")]
    public void PrintSystemStatus()
    {
        Debug.Log($"=== Pulley System Status ===");
        Debug.Log($"Platform A: Priority={platformA_Priority}, Weight={platformA_Weight:F1}, AtStart={platformA.IsAtStartPoint}, Moving={platformA.IsMoving}");
        Debug.Log($"Platform B: Priority={platformB_Priority}, Weight={platformB_Weight:F1}, AtStart={platformB.IsAtStartPoint}, Moving={platformB.IsMoving}");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"System Moving: {IsSystemMoving()}");
    }
    
    private void OnDrawGizmos()
    {
        // 시스템 중앙점 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        // 플랫폼 간 연결선
        if (platformA != null && platformB != null)
        {
            Gizmos.color = IsSystemMoving() ? Color.yellow : Color.white;
            Gizmos.DrawLine(platformA.transform.position, platformB.transform.position);
            
            // 도르레 중심점 표시
            Vector3 center = (platformA.transform.position + platformB.transform.position) / 2f;
            Gizmos.DrawWireSphere(center, 0.1f);
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
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