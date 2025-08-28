using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도르레 시스템의 개별 플랫폼을 관리하는 컴포넌트
/// </summary>
public class PulleyPlatform : MonoBehaviour
{
    [Header("이동 지점")]
    [SerializeField] private Transform startPoint;  // 아래 지점 (내려갔을 때)
    [SerializeField] private Transform endPoint;    // 위 지점 (올라갔을 때)
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("현재 상태")]
    [SerializeField] private bool isAtStartPoint = true;  // 시작 지점에 있는지 여부
    [SerializeField] private bool isMoving = false;
    
    private ObjectDetector detector;
    private PulleySystem parentSystem;
    private Coroutine moveCoroutine;
    
    // 상태 정보
    public ObjectType CurrentPriority { get; private set; } = ObjectType.Empty;
    public float CurrentWeight { get; private set; } = 0f;
    public bool IsAtStartPoint => isAtStartPoint;
    public bool IsMoving => isMoving;
    
    // 이벤트
    public System.Action<PulleyPlatform, ObjectType, float> OnPriorityChanged;
    public System.Action<PulleyPlatform> OnMoveComplete;
    
    private void Start()
    {
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
        
        // 초기 위치 설정
        SetInitialPosition();
        
        // 지점 설정 검증
        ValidatePoints();
    }
    
    private void SetInitialPosition()
    {
        if (isAtStartPoint && startPoint != null)
        {
            transform.position = startPoint.position;
        }
        else if (!isAtStartPoint && endPoint != null)
        {
            transform.position = endPoint.position;
        }
    }
    
    private void ValidatePoints()
    {
        if (startPoint == null)
            Debug.LogError($"PulleyPlatform({name})의 StartPoint가 설정되지 않았습니다!");
            
        if (endPoint == null)
            Debug.LogError($"PulleyPlatform({name})의 EndPoint가 설정되지 않았습니다!");
    }
    
    private void OnDetectorPriorityChanged(ObjectType priority, float weight)
    {
        CurrentPriority = priority;
        CurrentWeight = weight;
        
        // 부모 시스템에 변경 사항 알림
        OnPriorityChanged?.Invoke(this, priority, weight);
    }
    
    public void MoveToStartPoint()
    {
        MoveToPosition(startPoint, true);
    }
    
    public void MoveToEndPoint()
    {
        MoveToPosition(endPoint, false);
    }
    
    private void MoveToPosition(Transform target, bool movingToStart)
    {
        if (target == null)
        {
            Debug.LogError($"이동할 지점이 null입니다! (movingToStart: {movingToStart})");
            return;
        }
        
        // 이미 해당 위치에 있으면 이동하지 않음
        if (isAtStartPoint == movingToStart)
        {
            return;
        }
        
        // 기존 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MovePlatformCoroutine(target.position, movingToStart));
    }
    
    private IEnumerator MovePlatformCoroutine(Vector3 targetPosition, bool movingToStart)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        
        // 이동 시간 계산 (속도 기반)
        float duration = distance / moveSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 애니메이션 커브 적용
            float curveValue = moveCurve.Evaluate(progress);
            
            // 위치 보간
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            transform.position = currentPosition;
            
            yield return null;
        }
        
        // 최종 위치 설정
        transform.position = targetPosition;
        isAtStartPoint = movingToStart;
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
    
    // 수동으로 지점 설정
    public void SetPoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
        ValidatePoints();
    }
    
    private void OnDrawGizmos()
    {
        // 이동 지점들 시각화
        if (startPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, startPoint.position);
        }
        
        if (endPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, endPoint.position);
        }
        
        // 현재 플랫폼 상태 표시
        Gizmos.color = isMoving ? Color.yellow : (isAtStartPoint ? Color.red : Color.blue);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
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