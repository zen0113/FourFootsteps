using UnityEngine;
using System.Collections;

/// <summary>
/// 도르레 시스템의 개별 플랫폼을 관리하는 컴포넌트 (개선된 버전)
/// </summary>
public class PulleyPlatform : MonoBehaviour
{
    [Header("플랫폼 설정")]
    [SerializeField] private string platformName = "Platform";
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("현재 상태 (읽기 전용)")]
    [SerializeField] private float currentHeight = 0f;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private bool isMoving = false;
    
    private ObjectDetector detector;
    private PulleySystem parentSystem;
    private Coroutine moveCoroutine;
    private Vector3 initialPosition;
    
    // 상태 정보
    public ObjectType CurrentPriority { get; private set; } = ObjectType.Empty;
    public float CurrentWeight { get; private set; } = 0f;
    public bool IsMoving => isMoving;
    public float CurrentHeight => currentHeight;
    public float TargetHeight => targetHeight;
    
    // 이벤트
    public System.Action<PulleyPlatform, ObjectType, float> OnPriorityChanged;
    public System.Action<PulleyPlatform> OnMoveComplete;
    
    private void Start()
    {
        // 초기 위치 저장
        initialPosition = transform.position;
        currentHeight = 0f; // 초기 높이를 0으로 설정
        targetHeight = 0f;
        
        // 컴포넌트 찾기
        detector = GetComponentInChildren<ObjectDetector>();
        parentSystem = GetComponentInParent<PulleySystem>();
        
        if (detector == null)
        {
            Debug.LogError($"PulleyPlatform({name})에서 ObjectDetector를 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 연결
        detector.OnPriorityChanged += OnDetectorPriorityChanged;
    }
    
    private void OnDetectorPriorityChanged(ObjectType priority, float weight)
    {
        CurrentPriority = priority;
        CurrentWeight = weight;
        
        // 부모 시스템에 변경 사항 알림
        OnPriorityChanged?.Invoke(this, priority, weight);
    }
    
    /// <summary>
    /// 특정 높이로 이동 (도르레 시스템에서 호출)
    /// </summary>
    public void MoveToHeight(float height)
    {
        if (Mathf.Approximately(currentHeight, height))
        {
            return; // 이미 목표 높이에 있으면 이동하지 않음
        }
        
        targetHeight = height;
        
        // 기존 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MovePlatformCoroutine());
    }
    
    private IEnumerator MovePlatformCoroutine()
    {
        isMoving = true;
        
        float startHeight = currentHeight;
        float distance = Mathf.Abs(targetHeight - startHeight);
        
        // 이동 시간 계산 (속도 기반)
        float duration = distance / moveSpeed;
        float elapsed = 0f;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = initialPosition + Vector3.up * targetHeight;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 애니메이션 커브 적용
            float curveValue = moveCurve.Evaluate(progress);
            
            // 높이 보간
            currentHeight = Mathf.Lerp(startHeight, targetHeight, curveValue);
            
            // 위치 업데이트
            Vector3 newPosition = initialPosition + Vector3.up * currentHeight;
            transform.position = newPosition;
            
            yield return null;
        }
        
        // 최종 위치 설정
        currentHeight = targetHeight;
        transform.position = initialPosition + Vector3.up * currentHeight;
        isMoving = false;
        
        // 이동 완료 이벤트
        OnMoveComplete?.Invoke(this);
        
        moveCoroutine = null;
    }
    
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// 초기 높이 설정 (도르레 시스템 초기화시 사용)
    /// </summary>
    public void SetInitialHeight(float height)
    {
        currentHeight = height;
        targetHeight = height;
        transform.position = initialPosition + Vector3.up * height;
    }
    
    /// <summary>
    /// 초기 위치 재설정
    /// </summary>
    public void SetInitialPosition(Vector3 position)
    {
        initialPosition = position;
        transform.position = initialPosition + Vector3.up * currentHeight;
    }
    
    private void OnDrawGizmos()
    {
        // 초기 위치 표시
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(initialPosition, Vector3.one * 0.3f);
        
        // 현재 위치에서 초기 위치로의 높이 차이 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(initialPosition, transform.position);
        
        // 현재 플랫폼 상태 표시
        Gizmos.color = isMoving ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // 높이 정보 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
            $"{platformName}\nH: {currentHeight:F1}\nT: {targetHeight:F1}");
        #endif
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
        if (detector != null)
        {
            detector.OnPriorityChanged -= OnDetectorPriorityChanged;
        }
    }
}