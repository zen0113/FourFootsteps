using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [Header("엘리베이터 위치 설정")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    
    [Header("엘리베이터 이동 설정")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float delayBeforeDown = 1f;
    
    [Header("트리거 감지")]
    [SerializeField] private string playerTag = "Player";
    
    // 엘리베이터 상태
    public enum ElevatorState
    {
        AtStart,      // 시작 지점에 대기
        MovingUp,     // 위로 이동 중
        AtEnd,        // 도착 지점에 대기
        MovingDown    // 아래로 이동 중
    }
    
    private ElevatorState currentState = ElevatorState.AtStart;
    private Rigidbody2D rb;
    private bool playerOnElevator = false;
    private Coroutine returnCoroutine;
    
    // 플레이어 추적을 위한 변수들
    private List<GameObject> playersOnElevator = new List<GameObject>();
    private Vector3 lastElevatorPosition;
    
    private void Awake()
    {
        // Rigidbody2D 설정
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        SetupElevatorStructure();
    }
    
    private void SetupElevatorStructure()
    {
        // 기존 콜라이더들 확인
        Collider2D[] colliders = GetComponents<Collider2D>();
        
        // 플랫폼 콜라이더 (플레이어가 서는 곳) - IsTrigger = false
        BoxCollider2D platformCollider = null;
        foreach (var col in colliders)
        {
            if (col is BoxCollider2D && !col.isTrigger)
            {
                platformCollider = col as BoxCollider2D;
                break;
            }
        }
        
        if (platformCollider == null)
        {
            platformCollider = gameObject.AddComponent<BoxCollider2D>();
            platformCollider.isTrigger = false;
            platformCollider.size = new Vector2(2f, 0.2f);
        }
        
        // 트리거 감지 콜라이더 확인 또는 생성
        BoxCollider2D triggerCollider = null;
        foreach (var col in colliders)
        {
            if (col is BoxCollider2D && col.isTrigger)
            {
                triggerCollider = col as BoxCollider2D;
                break;
            }
        }
        
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(2.2f, 1f); // 플랫폼보다 약간 크게
            triggerCollider.offset = new Vector2(0f, 0.5f); // 위쪽으로 올림
        }
        
        Debug.Log($"엘리베이터 구조 설정 완료: 플랫폼 콜라이더({platformCollider.size}), 트리거 콜라이더({triggerCollider.size})");
    }
    
    private void Start()
    {
        // 시작 위치로 이동
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            lastElevatorPosition = transform.position;
        }
        else
        {
            Debug.LogError("StartPoint가 설정되지 않았습니다.");
        }
    }
    
    private void Update()
    {
        // 엘리베이터 위의 플레이어들을 함께 이동시킴
        if (playersOnElevator.Count > 0)
        {
            MovePlayersWithElevator();
        }
        
        // 현재 프레임의 엘리베이터 위치 저장
        lastElevatorPosition = transform.position;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // 플레이어가 엘리베이터 위에 올라탔는지 확인
            if (IsPlayerAboveElevator(other.transform))
            {
                if (!playersOnElevator.Contains(other.gameObject))
                {
                    playersOnElevator.Add(other.gameObject);
                    UpdatePlayerOnElevatorStatus();
                    Debug.Log($"플레이어가 엘리베이터에 올라탔습니다. (총 {playersOnElevator.Count}명)");
                }
                
                // 시작 지점에서 대기 중일 때만 이동 시작
                if (currentState == ElevatorState.AtStart)
                {
                    StartCoroutine(MoveToEnd());
                }
                
                // 복귀 코루틴이 실행 중이면 취소
                if (returnCoroutine != null)
                {
                    StopCoroutine(returnCoroutine);
                    returnCoroutine = null;
                    Debug.Log("엘리베이터 복귀가 취소되었습니다.");
                }
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (playersOnElevator.Contains(other.gameObject))
            {
                playersOnElevator.Remove(other.gameObject);
                UpdatePlayerOnElevatorStatus();
                Debug.Log($"플레이어가 엘리베이터에서 내렸습니다. (남은 인원: {playersOnElevator.Count}명)");
                
                // 플레이어가 모두 내렸고 도착 지점에 있을 때 복귀 시작
                if (playersOnElevator.Count == 0 && currentState == ElevatorState.AtEnd)
                {
                    returnCoroutine = StartCoroutine(ReturnToStartAfterDelay());
                }
            }
        }
    }
    
    private bool IsPlayerAboveElevator(Transform playerTransform)
    {
        // 플레이어가 엘리베이터 위쪽에 있는지 확인 (플랫폼 콜라이더 기준)
        BoxCollider2D platformCollider = null;
        Collider2D[] colliders = GetComponents<Collider2D>();
        
        foreach (var col in colliders)
        {
            if (col is BoxCollider2D && !col.isTrigger)
            {
                platformCollider = col as BoxCollider2D;
                break;
            }
        }
        
        if (platformCollider != null)
        {
            float platformTop = transform.position.y + platformCollider.offset.y + platformCollider.size.y * 0.5f;
            return playerTransform.position.y > platformTop;
        }
        
        return playerTransform.position.y > transform.position.y + 0.1f;
    }
    
    private void UpdatePlayerOnElevatorStatus()
    {
        playerOnElevator = playersOnElevator.Count > 0;
    }
    
    private void MovePlayersWithElevator()
    {
        // 엘리베이터가 이동한 거리만큼 플레이어들도 함께 이동
        Vector3 elevatorMovement = transform.position - lastElevatorPosition;
        
        if (elevatorMovement.magnitude > 0.001f) // 미세한 움직임 무시
        {
            for (int i = playersOnElevator.Count - 1; i >= 0; i--)
            {
                if (playersOnElevator[i] != null)
                {
                    // 플레이어가 여전히 엘리베이터 위에 있는지 확인
                    if (IsPlayerAboveElevator(playersOnElevator[i].transform))
                    {
                        // 플레이어 위치를 엘리베이터와 함께 이동
                        playersOnElevator[i].transform.position += elevatorMovement;
                        
                        // 플레이어에 Rigidbody2D가 있다면 velocity도 동기화
                        Rigidbody2D playerRb = playersOnElevator[i].GetComponent<Rigidbody2D>();
                        if (playerRb != null && currentState != ElevatorState.AtStart && currentState != ElevatorState.AtEnd)
                        {
                            playerRb.velocity = new Vector2(playerRb.velocity.x, rb.velocity.y);
                        }
                    }
                }
                else
                {
                    // null 참조 제거
                    playersOnElevator.RemoveAt(i);
                }
            }
        }
    }
    
    private IEnumerator MoveToEnd()
    {
        if (endPoint == null)
        {
            Debug.LogError("EndPoint가 설정되지 않았습니다.");
            yield break;
        }
        
        currentState = ElevatorState.MovingUp;
        Vector3 targetPosition = endPoint.position;
        Debug.Log("엘리베이터가 올라가기 시작합니다.");
        
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // 엘리베이터 이동 (Rigidbody2D.MovePosition 사용)
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
            
            yield return new WaitForFixedUpdate();
        }
        
        // 정확한 위치로 설정
        rb.MovePosition(targetPosition);
        currentState = ElevatorState.AtEnd;
        Debug.Log("엘리베이터가 목적지에 도착했습니다.");
        
        // 플레이어가 없으면 바로 복귀 시작
        if (!playerOnElevator)
        {
            returnCoroutine = StartCoroutine(ReturnToStartAfterDelay());
        }
    }
    
    private IEnumerator ReturnToStartAfterDelay()
    {
        Debug.Log($"{delayBeforeDown}초 후 엘리베이터가 내려갑니다.");
        yield return new WaitForSeconds(delayBeforeDown);
        
        // 대기 시간 후에도 플레이어가 없으면 복귀
        if (!playerOnElevator && currentState == ElevatorState.AtEnd)
        {
            yield return StartCoroutine(MoveToStart());
        }
    }
    
    private IEnumerator MoveToStart()
    {
        if (startPoint == null)
        {
            Debug.LogError("StartPoint가 설정되지 않았습니다.");
            yield break;
        }
        
        currentState = ElevatorState.MovingDown;
        Vector3 targetPosition = startPoint.position;
        Debug.Log("엘리베이터가 아래로 내려갑니다.");
        
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // 엘리베이터 이동 (Rigidbody2D.MovePosition 사용)
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
            
            yield return new WaitForFixedUpdate();
        }
        
        // 정확한 위치로 설정
        rb.MovePosition(targetPosition);
        currentState = ElevatorState.AtStart;
        Debug.Log("엘리베이터가 시작 지점으로 돌아왔습니다.");
    }
    
    // 인스펙터에서 시작점과 끝점을 쉽게 설정할 수 있도록 도우미 메서드
    [ContextMenu("현재 위치를 시작점으로 설정")]
    private void SetCurrentPositionAsStart()
    {
        if (startPoint == null)
        {
            GameObject startObj = new GameObject("StartPoint");
            startPoint = startObj.transform;
            startPoint.SetParent(transform.parent);
        }
        startPoint.position = transform.position;
        Debug.Log("시작점이 설정되었습니다.");
    }
    
    [ContextMenu("현재 위치를 끝점으로 설정")]
    private void SetCurrentPositionAsEnd()
    {
        if (endPoint == null)
        {
            GameObject endObj = new GameObject("EndPoint");
            endPoint = endObj.transform;
            endPoint.SetParent(transform.parent);
        }
        endPoint.position = transform.position;
        Debug.Log("끝점이 설정되었습니다.");
    }
    
    [ContextMenu("엘리베이터 구조 재설정")]
    private void ResetElevatorStructure()
    {
        SetupElevatorStructure();
    }
    
    // 디버깅을 위한 기즈모
    private void OnDrawGizmos()
    {
        // 시작점 표시
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(startPoint.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, startPoint.position);
            
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(startPoint.position + Vector3.up * 0.7f, "Start");
#endif
        }
        
        // 끝점 표시
        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(endPoint.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, endPoint.position);
            
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(endPoint.position + Vector3.up * 0.7f, "End");
#endif
        }
        
        // 이동 경로 표시
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
        
        // 플랫폼 콜라이더 표시 (파란색)
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (col is BoxCollider2D && !col.isTrigger)
            {
                BoxCollider2D platformCollider = col as BoxCollider2D;
                Gizmos.color = new Color(0f, 0f, 1f, 0.7f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(platformCollider.offset, platformCollider.size);
                break;
            }
        }
        
        // 트리거 영역 표시 (청록색)
        foreach (var col in colliders)
        {
            if (col is BoxCollider2D && col.isTrigger)
            {
                BoxCollider2D triggerCollider = col as BoxCollider2D;
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(triggerCollider.offset, triggerCollider.size);
                break;
            }
        }
        
        // 현재 상태 표시
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // 플레이어 수 표시
#if UNITY_EDITOR
        if (Application.isPlaying && playersOnElevator.Count > 0)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, $"Players: {playersOnElevator.Count}");
        }
#endif
    }
    
    private Color GetStateColor()
    {
        switch (currentState)
        {
            case ElevatorState.AtStart: return Color.green;
            case ElevatorState.MovingUp: return Color.blue;
            case ElevatorState.AtEnd: return Color.red;
            case ElevatorState.MovingDown: return new Color(1f, 0.5f, 0f); // 오렌지 색상
            default: return Color.white;
        }
    }
    
    // 퍼블릭 프로퍼티들 (다른 스크립트에서 상태 확인용)
    public ElevatorState CurrentState => currentState;
    public bool PlayerOnElevator => playerOnElevator;
    public int PlayerCount => playersOnElevator.Count;
}