using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedPushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    
    [Header("박스 물리 설정 - 고급 버전")]
    [SerializeField] private float normalDrag = 0.5f;       
    [SerializeField] private float interactingDrag = 0.2f;  
    [SerializeField] private float boxMass = 2f;            
    [SerializeField] private float fallGravityScale = 3f;   
    [SerializeField] private float normalGravityScale = 1f; 
    
    [Header("이동 최적화")]
    [SerializeField] private float maxPushSpeed = 8f;       
    [SerializeField] private float groundFriction = 0.1f;   
    
    [Header("원래 위치 복귀 시스템")]
    [SerializeField] private bool enableReturnToOrigin = true;      // 복귀 기능 활성화
    [SerializeField] private float returnDelay = 5f;               // 멈춤 감지 시간 (이 시간만큼 멈춰있어야 복귀 대상)
    [SerializeField] private float minDistanceFromOrigin = 3f;     // 복귀 조건 최소 거리
    [SerializeField] private Vector3 customReturnPosition = Vector3.zero; // 인스펙터에서 설정할 복귀 위치
    [SerializeField] private bool useCustomReturnPosition = false; // 커스텀 위치 사용 여부
    
    [Header("빠른 낙하 시스템")]
    [SerializeField] private float holeDetectionDistance = 0.2f;   // 구멍 감지 거리
    [SerializeField] private float fastFallGravity = 8f;           // 구멍으로 떨어질 때 중력
    [SerializeField] private float normalFallCheckTime = 0.3f;     // 일반 낙하 판정 시간
    [SerializeField] private LayerMask groundLayerMask = -1;       // 바닥 레이어 마스크
    
    // 원래 위치 관련 변수들
    private Vector3 originalPosition;
    private Vector3 targetReturnPosition; // 실제 복귀할 위치
    private bool hasLeftOriginalArea = false;
    
    // 물리 상태 변수들
    private bool isBeingPushed = false;
    private bool isFalling = false;
    private bool isInHole = false; // 구멍에 떨어지는 중인지 여부
    private float timeNotGrounded = 0f;
    
    // 움직임 감지 기반 복귀 시스템 변수들
    private Vector3 lastIntegerPosition; // 마지막으로 기록된 정수 위치
    private bool isObjectMoving = false; // 오브젝트가 움직이는 중인지
    private float timeStoppedMoving = 0f; // 멈춘 시간 누적
    private float timeWaitingForReturn = 0f; // 복귀 대기 시간 누적
    private bool isWaitingForReturn = false; // 복귀 대기 중인지 (5초 멈춤 후 10초 카운트다운)
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 원래 위치 저장
        originalPosition = transform.position;
        
        // 복귀 위치 설정 (커스텀 위치 사용 여부에 따라)
        targetReturnPosition = useCustomReturnPosition ? customReturnPosition : originalPosition;
        
        // 움직임 감지 초기화
        lastIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        // Rigidbody2D 최적화 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = normalGravityScale;
        rb.mass = boxMass;
        rb.drag = normalDrag;
        
        // 물리 머테리얼 설정 (낮은 마찰)
        PhysicsMaterial2D lowFrictionMaterial = new PhysicsMaterial2D("LowFriction");
        lowFrictionMaterial.friction = groundFriction;
        lowFrictionMaterial.bounciness = 0f;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.sharedMaterial = lowFrictionMaterial;
        }
        
        Debug.Log("[AdvancedPushableBox] 고급 상자 초기화 완료");
        Debug.Log("원래 위치: " + originalPosition + ", 복귀 위치: " + targetReturnPosition);
        Debug.Log("시작 정수 위치: " + lastIntegerPosition);
    }
    
    private void Update()
    {
        CheckGroundStatus();
        HandlePlayerInteraction();
        OptimizePhysics();
        HandleMovementBasedReturn(); // 움직임 기반 복귀 시스템
        CheckForHoleFall();
    }
    
    private void CheckGroundStatus()
    {
        bool wasOnGround = !isFalling;
        bool isOnGround = IsGrounded();
        
        if (isOnGround)
        {
            if (isFalling)
            {
                isFalling = false;
                isInHole = false; // 착지하면 구멍 상태 해제
                rb.gravityScale = normalGravityScale;
                Debug.Log("[AdvancedPushableBox] 착지 완료");
            }
            timeNotGrounded = 0f;
        }
        else
        {
            timeNotGrounded += Time.deltaTime;
            
            if (timeNotGrounded > normalFallCheckTime && !isFalling)
            {
                isFalling = true;
                // 구멍에 떨어지는 중이 아니면 일반 낙하
                if (!isInHole)
                {
                    rb.gravityScale = fallGravityScale;
                    Debug.Log("[AdvancedPushableBox] 일반 낙하 모드 활성화");
                }
            }
        }
    }
    
    private void CheckForHoleFall()
    {
        // 상자가 바닥에 있고 밀리고 있을 때만 구멍 체크
        if (!isBeingPushed || isFalling) return;
        
        // 상자 바로 아래 구멍 감지
        Vector2 boxSize = GetComponent<BoxCollider2D>().size;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
        
        // 아래쪽으로 약간 더 긴 거리로 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, holeDetectionDistance, groundLayerMask);
        
        // 바닥이 감지되지 않으면 구멍으로 판정
        if (hit.collider == null && IsGrounded())
        {
            // 현재 바닥에 있지만 앞으로 한 발짝 더 가면 구멍이 있는 상황
            Vector2 moveDirection = rb.velocity.normalized;
            Vector2 futurePosition = rayOrigin + moveDirection * (boxSize.x/2 + 0.1f);
            
            RaycastHit2D futureHit = Physics2D.Raycast(futurePosition, Vector2.down, holeDetectionDistance, groundLayerMask);
            
            if (futureHit.collider == null)
            {
                StartHoleFall();
            }
        }
    }
    
    private void StartHoleFall()
    {
        if (!isInHole)
        {
            isInHole = true;
            isFalling = true;
            rb.gravityScale = fastFallGravity;
            Debug.Log("[AdvancedPushableBox] 구멍으로 빠른 낙하 시작!");
        }
    }
    
    private bool IsGrounded()
    {
        float checkDistance = 0.1f;
        Vector2 boxSize = GetComponent<BoxCollider2D>().size;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
        
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkDistance, groundLayerMask);
        return hit.collider != null;
    }
    
    private void HandlePlayerInteraction()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool currentlyBeingPushed = false;
        
        if (player != null)
        {
            PlayerBoxInteraction interaction = player.GetComponent<PlayerBoxInteraction>();
            if (interaction != null && interaction.IsInteracting && interaction.CurrentBox == gameObject)
            {
                currentlyBeingPushed = true;
            }
        }
        
        if (currentlyBeingPushed != isBeingPushed)
        {
            isBeingPushed = currentlyBeingPushed;
            
            if (isBeingPushed)
            {
                rb.drag = interactingDrag;
                Debug.Log("[AdvancedPushableBox] 상호작용 시작");
            }
            else
            {
                rb.drag = normalDrag;
                Debug.Log("[AdvancedPushableBox] 상호작용 종료");
            }
        }
    }
    
    private void OptimizePhysics()
    {
        if (isBeingPushed)
        {
            Vector2 velocity = rb.velocity;
            
            if (Mathf.Abs(velocity.x) > maxPushSpeed)
            {
                velocity.x = Mathf.Sign(velocity.x) * maxPushSpeed;
                rb.velocity = velocity;
            }
        }
        
        if (!isBeingPushed && rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private void HandleMovementBasedReturn()
    {
        if (!enableReturnToOrigin) return;
        
        // 현재 정수 위치 계산
        Vector3 currentIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        // 원래 영역에서 벗어났는지 확인
        float distanceFromOrigin = Vector3.Distance(transform.position, targetReturnPosition);
        bool isOutsideOriginArea = (distanceFromOrigin > minDistanceFromOrigin);
        
        if (!isOutsideOriginArea)
        {
            // 원래 영역 내에 있으면 모든 상태 초기화
            if (hasLeftOriginalArea)
            {
                hasLeftOriginalArea = false;
                isObjectMoving = false;
                timeStoppedMoving = 0f;
                timeWaitingForReturn = 0f;
                isWaitingForReturn = false;
                Debug.Log("[AdvancedPushableBox] 원래 영역으로 돌아왔습니다 - 복귀 시스템 리셋");
            }
            lastIntegerPosition = currentIntegerPosition;
            return;
        }
        
        // 영역을 벗어났음을 기록
        if (!hasLeftOriginalArea)
        {
            hasLeftOriginalArea = true;
            lastIntegerPosition = currentIntegerPosition;
            isObjectMoving = false; // 아직 움직임이 감지되지 않은 상태
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;
            isWaitingForReturn = false;
            Debug.Log("[AdvancedPushableBox] 원래 영역을 벗어났습니다. 움직임을 기다리는 중...");
            return;
        }
        
        // 정수 위치가 변경되었는지 확인 (움직임 감지)
        bool positionChanged = (currentIntegerPosition != lastIntegerPosition);
        
        if (positionChanged)
        {
            // 위치가 변경됨 = 움직이고 있음
            lastIntegerPosition = currentIntegerPosition;
            
            // 처음 움직임이 감지된 경우
            if (!isObjectMoving)
            {
                Debug.Log("[AdvancedPushableBox] 첫 움직임 감지! 이제부터 멈춤을 기다립니다");
                isObjectMoving = true;
            }
            
            timeStoppedMoving = 0f;
            
            // 복귀 대기 중이었다면 취소
            if (isWaitingForReturn)
            {
                isWaitingForReturn = false;
                timeWaitingForReturn = 0f;
                Debug.Log("[AdvancedPushableBox] 움직임 감지! 복귀 취소됨");
            }
        }
        else if (isObjectMoving)
        {
            // 움직이던 중에 멈춤 - 이때부터 멈춤 시간 측정 시작
            timeStoppedMoving += Time.deltaTime;
            
            // 5초 이상 멈춰있고, 아직 복귀 대기 상태가 아니면 복귀 대기 시작
            if (timeStoppedMoving >= returnDelay && !isWaitingForReturn)
            {
                isWaitingForReturn = true;
                timeWaitingForReturn = 0f;
                Debug.Log($"[AdvancedPushableBox] {returnDelay}초 이상 멈춤! 복귀 대기 시작 (10초 후 복귀)");
            }
            
            // 복귀 대기 중이면 카운트다운
            if (isWaitingForReturn)
            {
                timeWaitingForReturn += Time.deltaTime;
                
                // 1초마다 남은 시간 출력
                if (Mathf.FloorToInt(timeWaitingForReturn) != Mathf.FloorToInt(timeWaitingForReturn - Time.deltaTime))
                {
                    float remainingTime = 10f - timeWaitingForReturn;
                    Debug.Log($"[AdvancedPushableBox] 복귀까지 남은 시간: {remainingTime:F0}초");
                }
                
                // 10초가 지나면 복귀 실행
                if (timeWaitingForReturn >= 10f)
                {
                    InstantReturnToOrigin();
                }
            }
        }
        // else: 아직 한 번도 움직인 적이 없으면 아무것도 하지 않음
    }
    
    private void InstantReturnToOrigin()
    {
        Debug.Log("[AdvancedPushableBox] 즉시 원래 위치로 복귀!");
        
        // 즉시 위치 이동
        transform.position = targetReturnPosition;
        
        // 물리 상태 초기화
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // 모든 상태 초기화
        hasLeftOriginalArea = false;
        isObjectMoving = false;
        timeStoppedMoving = 0f;
        timeWaitingForReturn = 0f;
        isWaitingForReturn = false;
        isFalling = false;
        isInHole = false;
        rb.gravityScale = normalGravityScale;
        
        // 정수 위치도 업데이트
        lastIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        Debug.Log("[AdvancedPushableBox] 순간이동 복귀 완료 - 모든 상태 리셋");
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 구멍에서 떨어져 바닥에 착지했을 때
            if (isInHole)
            {
                rb.velocity = new Vector2(rb.velocity.x * 0.3f, rb.velocity.y); // 더 많이 감속
                Debug.Log("[AdvancedPushableBox] 구멍에서 바닥으로 착지!");
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x * 0.8f, rb.velocity.y);
            }
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        // 상자 상태 시각화
        if (isInHole)
            Gizmos.color = Color.red;
        else if (isBeingPushed)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.yellow;
        
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        
        // 원래 위치 표시
        if (enableReturnToOrigin)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetReturnPosition, 0.2f);
            
            // 복귀 범위 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetReturnPosition, minDistanceFromOrigin);
            
            // 현재 위치에서 복귀 위치로의 선
            if (hasLeftOriginalArea)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, targetReturnPosition);
            }
        }
        
        // 바닥 체크 레이 시각화
        if (GetComponent<BoxCollider2D>() != null)
        {
            Vector2 boxSize = GetComponent<BoxCollider2D>().size;
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
            
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * 0.1f);
            
            // 구멍 감지 레이 시각화
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * holeDetectionDistance);
        }
    }
    
    // 외부에서 사용할 수 있는 프로퍼티와 메서드
    public bool IsBeingPushed => isBeingPushed;
    public bool IsFalling => isFalling;
    public bool IsInHole => isInHole;
    public Vector3 OriginalPosition => originalPosition;
    public Vector3 ReturnPosition => targetReturnPosition;
    
    // 수동으로 즉시 복귀
    public void ForceReturnToOrigin()
    {
        InstantReturnToOrigin();
    }
    
    // 복귀 위치 재설정
    public void SetReturnPosition(Vector3 newReturnPosition)
    {
        targetReturnPosition = newReturnPosition;
        useCustomReturnPosition = true;
        hasLeftOriginalArea = false;
        Debug.Log("[AdvancedPushableBox] 새로운 복귀 위치 설정: " + newReturnPosition);
    }
    
    // 복귀 위치를 현재 위치로 설정
    public void SetReturnPositionToCurrent()
    {
        SetReturnPosition(transform.position);
    }
    
    // 복귀 위치를 원래 시작 위치로 리셋
    public void ResetToOriginalPosition()
    {
        targetReturnPosition = originalPosition;
        useCustomReturnPosition = false;
        hasLeftOriginalArea = false;
        Debug.Log("[AdvancedPushableBox] 복귀 위치를 원래 위치로 리셋");
    }
    
    // 빠른 낙하 강제 시작
    public void ForceHoleFall()
    {
        StartHoleFall();
    }
    
    // 설정 동적 변경
    public void SetReturnDelay(float newDelay)
    {
        returnDelay = newDelay;
    }
    
    public void SetFastFallGravity(float newGravity)
    {
        fastFallGravity = newGravity;
    }
    
    // 복귀 기능 활성화/비활성화
    public void SetReturnToOriginEnabled(bool enabled)
    {
        enableReturnToOrigin = enabled;
        
        if (!enabled)
        {
            // 복귀 기능 비활성화 시 상태 초기화
            hasLeftOriginalArea = false;
            isObjectMoving = false;
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;
            isWaitingForReturn = false;
        }
    }
}