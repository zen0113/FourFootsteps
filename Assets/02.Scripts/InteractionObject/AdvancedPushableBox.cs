using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedPushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    
    [Header("박스 물리 설정")]
    [SerializeField] private float normalDrag = 0.5f;       
    [SerializeField] private float interactingDrag = 0.2f;  
    [SerializeField] private float boxMass = 2f;            
    [SerializeField] private float normalGravityScale = 1f; 
    
    [Header("이동 최적화")]
    [SerializeField] private float maxPushSpeed = 8f;       
    [SerializeField] private float groundFriction = 0.1f;   
    
    [Header("낙하 속도 설정")]
    [SerializeField] private float normalFallSpeed = 50f;   // 일반 낙하 속도
    [SerializeField] private float holeFallSpeed = 80f;     // 구멍 낙하 속도
    [SerializeField] private bool useCustomFallSpeed = true; // 커스텀 낙하 속도 사용
    
    [Header("원래 위치 복귀 시스템")]
    [SerializeField] private bool enableReturnToOrigin = true;      
    [SerializeField] private float returnDelay = 10f;               
    [SerializeField] private float minDistanceFromOrigin = 3f;     
    [SerializeField] private Vector3 customReturnPosition = Vector3.zero; 
    [SerializeField] private bool useCustomReturnPosition = false; 
    
    [Header("구멍 감지")]
    [SerializeField] private float holeDetectionDistance = 0.2f;   
    [SerializeField] private float normalFallCheckTime = 0.3f;     
    [SerializeField] private LayerMask groundLayerMask = -1;
    
    [Header("도르래 시스템")]
    [SerializeField] private string pulleyTag = "wall";
    [SerializeField] private float pulleyGravityScale = 0.3f;
    
    // 상태 변수들
    private Vector3 originalPosition;
    private Vector3 targetReturnPosition;
    private bool hasLeftOriginalArea = false;
    private bool isBeingPushed = false;
    private bool isFalling = false;
    private bool isInHole = false;
    private float timeNotGrounded = 0f;
    private bool isOnPulley = false;
    private bool wasOnPulley = false;
    
    // 복귀 시스템 변수들
    private Vector3 lastIntegerPosition;
    private bool isObjectMoving = false;
    private float timeStoppedMoving = 0f;
    private float timeWaitingForReturn = 0f;
    private bool isWaitingForReturn = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        originalPosition = transform.position;
        targetReturnPosition = useCustomReturnPosition ? customReturnPosition : originalPosition;
        
        lastIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        // Rigidbody2D 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = normalGravityScale;
        rb.mass = boxMass;
        rb.drag = normalDrag;
        
        // 물리 머테리얼 설정
        PhysicsMaterial2D lowFrictionMaterial = new PhysicsMaterial2D("LowFriction");
        lowFrictionMaterial.friction = groundFriction;
        lowFrictionMaterial.bounciness = 0f;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.sharedMaterial = lowFrictionMaterial;
        }
        
        Debug.Log("[AdvancedPushableBox] 상자 초기화 완료");
    }
    
    private void FixedUpdate()
    {
        CheckGroundStatus();
        HandlePlayerInteraction();
        OptimizePhysics();
        HandleMovementBasedReturn();
        CheckForHoleFall();
        StabilizeOnPulley();
        HandleFallSpeed();
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
                isInHole = false;
                Debug.Log("[AdvancedPushableBox] 착지 완료");
            }
            timeNotGrounded = 0f;
        }
        else
        {
            timeNotGrounded += Time.fixedDeltaTime;
            
            if (timeNotGrounded > normalFallCheckTime && !isFalling)
            {
                isFalling = true;
                Debug.Log("[AdvancedPushableBox] 낙하 시작");
            }
        }
    }
    
    private void HandleFallSpeed()
    {
        // 커스텀 낙하 속도 사용하고 떨어지는 중일 때만
        if (useCustomFallSpeed && isFalling)
        {
            Vector2 velocity = rb.velocity;
            
            // 구멍에 떨어지는지에 따라 속도 결정
            float targetSpeed = isInHole ? holeFallSpeed : normalFallSpeed;
            
            // Y 속도를 강제로 설정
            velocity.y = -targetSpeed;
            rb.velocity = velocity;
        }
    }
    
    private void CheckForHoleFall()
    {
        if (!isBeingPushed || isFalling) return;
        
        Vector2 boxSize = GetComponent<BoxCollider2D>().size;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
        
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, holeDetectionDistance, groundLayerMask);
        
        if (hit.collider == null && IsGrounded())
        {
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
        
        // 미세한 진동 방지
        if (!isBeingPushed && !isFalling && rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private void StabilizeOnPulley()
    {
        if (isOnPulley)
        {
            // 도르래 위에서는 Y축 속도를 크게 제한
            Vector2 velocity = rb.velocity;
            velocity.y = Mathf.Clamp(velocity.y, -2f, 2f);
            rb.velocity = velocity;
            
            // 중력 영향 감소
            if (rb.gravityScale != pulleyGravityScale)
            {
                rb.gravityScale = pulleyGravityScale;
                Debug.Log("[AdvancedPushableBox] 도르래 위 - 중력 감소");
            }
        }
        else if (wasOnPulley)
        {
            // 도르래에서 내려왔을 때 원래 중력으로 복구
            rb.gravityScale = normalGravityScale;
            Debug.Log("[AdvancedPushableBox] 도르래 벗어남 - 중력 복구");
        }
        
        wasOnPulley = isOnPulley;
    }
    
    private void HandleMovementBasedReturn()
    {
        if (!enableReturnToOrigin) return;
        
        Vector3 currentIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        float distanceFromOrigin = Vector3.Distance(transform.position, targetReturnPosition);
        bool isOutsideOriginArea = (distanceFromOrigin > minDistanceFromOrigin);
        
        if (!isOutsideOriginArea)
        {
            if (hasLeftOriginalArea)
            {
                hasLeftOriginalArea = false;
                isObjectMoving = false;
                timeStoppedMoving = 0f;
                timeWaitingForReturn = 0f;
                isWaitingForReturn = false;
                Debug.Log("[AdvancedPushableBox] 원래 영역으로 돌아왔습니다");
            }
            lastIntegerPosition = currentIntegerPosition;
            return;
        }
        
        if (!hasLeftOriginalArea)
        {
            hasLeftOriginalArea = true;
            lastIntegerPosition = currentIntegerPosition;
            isObjectMoving = false;
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;
            isWaitingForReturn = false;
            Debug.Log("[AdvancedPushableBox] 원래 영역을 벗어났습니다");
            return;
        }
        
        bool positionChanged = (currentIntegerPosition != lastIntegerPosition);
        
        if (positionChanged)
        {
            lastIntegerPosition = currentIntegerPosition;
            
            if (!isObjectMoving)
            {
                Debug.Log("[AdvancedPushableBox] 첫 움직임 감지!");
                isObjectMoving = true;
            }
            
            timeStoppedMoving = 0f;
            
            if (isWaitingForReturn)
            {
                isWaitingForReturn = false;
                timeWaitingForReturn = 0f;
                Debug.Log("[AdvancedPushableBox] 복귀 취소됨");
            }
        }
        else if (isObjectMoving)
        {
            timeStoppedMoving += Time.fixedDeltaTime;
            
            if (timeStoppedMoving >= returnDelay && !isWaitingForReturn)
            {
                isWaitingForReturn = true;
                timeWaitingForReturn = 0f;
                Debug.Log($"[AdvancedPushableBox] {returnDelay}초 이상 멈춤! 복귀 대기 시작");
            }
            
            if (isWaitingForReturn)
            {
                timeWaitingForReturn += Time.fixedDeltaTime;
                
                if (timeWaitingForReturn >= 10f)
                {
                    InstantReturnToOrigin();
                }
            }
        }
    }
    
    private void InstantReturnToOrigin()
    {
        Debug.Log("[AdvancedPushableBox] 즉시 원래 위치로 복귀!");
        
        transform.position = targetReturnPosition;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        hasLeftOriginalArea = false;
        isObjectMoving = false;
        timeStoppedMoving = 0f;
        timeWaitingForReturn = 0f;
        isWaitingForReturn = false;
        isFalling = false;
        isInHole = false;
        isOnPulley = false;
        
        lastIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        Debug.Log("[AdvancedPushableBox] 복귀 완료");
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 도르래 감지
        if (collision.gameObject.CompareTag(pulleyTag))
        {
            isOnPulley = true;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            Debug.Log("[AdvancedPushableBox] 도르래 위에 올라감");
        }
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (isInHole)
            {
                rb.velocity = new Vector2(rb.velocity.x * 0.3f, rb.velocity.y);
                Debug.Log("[AdvancedPushableBox] 구멍에서 바닥으로 착지!");
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x * 0.8f, rb.velocity.y);
            }
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed && !isOnPulley)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 도르래 위에 있을 때 계속 안정화
        if (collision.gameObject.CompareTag(pulleyTag))
        {
            isOnPulley = true;
            
            // Y축 속도가 너무 크면 강제로 제한
            if (Mathf.Abs(rb.velocity.y) > 1f)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
            }
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isBeingPushed && !isOnPulley)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        // 도르래에서 벗어남
        if (collision.gameObject.CompareTag(pulleyTag))
        {
            isOnPulley = false;
            Debug.Log("[AdvancedPushableBox] 도르래에서 벗어남");
        }
    }
    
    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        if (isOnPulley)
            Gizmos.color = Color.cyan;
        else if (isInHole)
            Gizmos.color = Color.red;
        else if (isBeingPushed)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.yellow;
        
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        
        if (enableReturnToOrigin)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetReturnPosition, 0.2f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetReturnPosition, minDistanceFromOrigin);
            
            if (hasLeftOriginalArea)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, targetReturnPosition);
            }
        }
        
        if (GetComponent<BoxCollider2D>() != null)
        {
            Vector2 boxSize = GetComponent<BoxCollider2D>().size;
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y/2);
            
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * 0.1f);
            
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * holeDetectionDistance);
        }
    }
    
    // 외부에서 사용할 수 있는 메서드들
    public bool IsBeingPushed => isBeingPushed;
    public bool IsFalling => isFalling;
    public bool IsInHole => isInHole;
    public bool IsOnPulley => isOnPulley;
    public Vector3 OriginalPosition => originalPosition;
    public Vector3 ReturnPosition => targetReturnPosition;
    
    public void ForceReturnToOrigin()
    {
        InstantReturnToOrigin();
    }
    
    public void SetReturnPosition(Vector3 newReturnPosition)
    {
        targetReturnPosition = newReturnPosition;
        useCustomReturnPosition = true;
        hasLeftOriginalArea = false;
    }
    
    public void SetNormalFallSpeed(float newSpeed)
    {
        normalFallSpeed = newSpeed;
    }
    
    public void SetHoleFallSpeed(float newSpeed)
    {
        holeFallSpeed = newSpeed;
    }
    
    public void SetUseCustomFallSpeed(bool useCustom)
    {
        useCustomFallSpeed = useCustom;
    }
}