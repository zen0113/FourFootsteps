using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedPushableBox : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private int floorLayer = -1;
    private readonly HashSet<Collider2D> ignoredFloorColliders = new HashSet<Collider2D>();
    private readonly HashSet<PlatformEffector2D> disabledFloorEffectors = new HashSet<PlatformEffector2D>();
    private bool floorObjectsDisabled = false; // 바닥 오브젝트들이 현재 비활성화되어 있는지
    
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
    [SerializeField] private float returnDelay = 2f;               
    [SerializeField] private float minDistanceFromOrigin = 3f;     
    [SerializeField] private Vector3 customReturnPosition = Vector3.zero; 
    [SerializeField] private bool useCustomReturnPosition = false; 
    [SerializeField] private float returnSpeedThreshold = 0.05f;   // 거의 멈춤 판정 속도
    [SerializeField] private float returnSettleTime = 0.2f;        // 멈춤이 안정적으로 유지되어야 하는 시간
    [SerializeField] private float returnInteractionCooldown = 0.8f; // 플레이어가 최근에 밀었으면 복귀 금지(초)
    [SerializeField] private LayerMask returnBlockerMask = -1;     // 복귀 위치 겹침 검사 마스크(기본: 전부)
    [SerializeField] private float returnSearchStep = 0.15f;       // 안전 위치 탐색 간격
    [SerializeField] private int returnSearchSteps = 12;           // 안전 위치 탐색 횟수
    [SerializeField] private float returnOverlapPadding = 0.02f;   // 콜라이더 크기 여유분
    
    [Header("구멍 감지")]
    [SerializeField] private float holeDetectionDistance = 0.2f;   
    [SerializeField] private float normalFallCheckTime = 0.3f;     
    [SerializeField] private LayerMask groundLayerMask = -1;
    
    [Header("도르래 시스템")]
    [SerializeField] private float pulleyGravityScale = 0.3f;
    [SerializeField] private GameObject[] floorObjectsToDisable = new GameObject[0]; // 도르레 탑승 시 비활성화할 바닥 오브젝트들

    [Header("안정화/안전장치")]
    [SerializeField] private bool forceNonTriggerCollider = true;
    [SerializeField] private bool forceContinuousCollision = true;
    [SerializeField] private bool debugCollisionLayerLog = false; // 특정 구간에서 왜 막히는지(레이어/오브젝트) 추적용
    [SerializeField] private bool debugFloorPhaseLog = false; // [FloorPhase] 로그만 별도 토글(스팸 방지)
    
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

    // 안전장치 로그 스팸 방지
    private bool warnedTriggerOnce = false;

    // 복귀 금지 타이밍(최근 상호작용/운반 중 판정용)
    private float lastInteractionTime = -999f;

    // 도르레 플랫폼 위(운반 중) 판정
    private PulleyPlatform currentPulleyPlatform = null;
    private bool isOnPulleyPlatform = false;
    
    [Header("도르레 하드 고정(미끄러짐 방지)")]
    [SerializeField] private bool enablePulleyHardLock = true;
    [SerializeField] private float pulleyHardLockContactEpsilon = 0.002f; // 콜라이더 거리(표면 간격)가 이 값 이하면 '닿음'으로 판정
    [SerializeField] private float pulleyHardLockSnapDistance = 0.12f; // (호환/백업) 콜라이더가 없을 때만 거리 기반으로 사용
    
    [Header("도르레 하드락 해제/원상복귀")]
    [Tooltip("이 콜라이더에 닿으면(겹치거나 접촉) 도르레 하드락을 즉시 해제하고, delay 후 원상복귀(InstantReturnToOrigin)합니다.")]
    [SerializeField] private Collider2D pulleyHardLockReleaseCollider;
    [SerializeField] private float pulleyHardLockReleaseReturnDelay = 2f;

    // 도르레 위에 올라갔을 때 박스를 완전 고정(좌우로 밀리지 않게)하기 위한 상태/백업
    private bool isPulleyHardLocked = false;
    private PulleyPlatform lockedPulleyPlatform = null;
    private Transform lockedPulleySnapPoint = null;
    private Collider2D lockedPulleySnapCollider = null;
    private bool warnedMissingSnapColliderOnce = false;
    private Rigidbody2D lockedPulleyPlatformRb = null;
    private Vector2 pulleyLockOffsetFromPlatform = Vector2.zero;
    private Coroutine pulleyHardLockReturnCoroutine = null;
    private float suppressPulleyHardLockUntilTime = -1f;

    // 하드락 해제 시 원래 값 복원용
    private RigidbodyType2D prevBodyType;
    private float prevGravityScale;
    private float prevDrag;
    private RigidbodyConstraints2D prevConstraints;
    private CollisionDetectionMode2D prevCollisionDetectionMode;
    private RigidbodyInterpolation2D prevInterpolation;
    private bool prevSimulated;

    // 플레이어가 박스에 "닿아있는 동안"은 복귀하면 안됨(밀기 판정이 간헐적으로 끊기는 케이스 방어)
    private bool isPlayerTouching = false;
    
    // 복귀 시스템 변수들
    private Vector3 lastIntegerPosition;
    private bool isObjectMoving = false;
    private float timeStoppedMoving = 0f;
    private float timeWaitingForReturn = 0f;
    private bool isWaitingForReturn = false;
    private float settleTimer = 0f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (rb == null)
        {
            Debug.LogError("[AdvancedPushableBox] Rigidbody2D가 없습니다. 동작 불가");
            enabled = false;
            return;
        }

        if (boxCollider == null)
        {
            Debug.LogError("[AdvancedPushableBox] BoxCollider2D가 없습니다. 동작 불가");
            enabled = false;
            return;
        }
        
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

        if (forceContinuousCollision)
        {
            // 빠른 낙하(50~80)에서 Discrete면 터널링(바닥 뚫음) 확률이 커집니다.
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        // 물리 머테리얼 설정
        PhysicsMaterial2D lowFrictionMaterial = new PhysicsMaterial2D("LowFriction");
        lowFrictionMaterial.friction = groundFriction;
        lowFrictionMaterial.bounciness = 0f;
        
        Collider2D collider = boxCollider;
        if (collider != null)
        {
            collider.sharedMaterial = lowFrictionMaterial;
        }

        EnsureColliderState();
        floorLayer = LayerMask.NameToLayer("Floor");

        if (debugCollisionLayerLog)
        {
            Debug.Log($"[AdvancedPushableBox][Debug] enabled on '{name}' layer='{LayerMask.LayerToName(gameObject.layer)}' rb='{(rb != null ? rb.bodyType.ToString() : "null")}'");
        }
        
        Debug.Log("[AdvancedPushableBox] 상자 초기화 완료");
    }
    
    private void FixedUpdate()
    {
        // 리스폰/원상복구 과정에서 콜라이더가 꺼지거나 Trigger로 바뀌는 케이스 방어
        EnsureColliderState();

        // (중요) 특정 콜라이더에 닿으면 하드락 해제 + 지연 복귀
        CheckPulleyHardLockRelease();

        // 도르레 위 하드락(완전 고정): 플랫폼 위치에 강제 동기화
        SyncPulleyHardLock();

        // 도르레로 운반(특히 내려갈 때) 중에는 Floor를 통과시키기 위해 IgnoreCollision을 토글
        MaintainFloorPhasing();
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
            
            // (중요) Y 속도를 "항상 -targetSpeed"로 덮어쓰면
            // 착지/충돌 시 Solver가 만든 반발/정지 값을 무력화해서 바닥을 뚫을 수 있습니다.
            // 따라서 "최대 낙하 속도 제한"으로만 사용합니다.
            if (velocity.y < -targetSpeed)
                velocity.y = -targetSpeed;
            rb.velocity = velocity;
        }
    }
    
    private void CheckForHoleFall()
    {
        if (!isBeingPushed || isFalling) return;

        // 현재는 땅 위에 있으면서, 진행 방향 앞쪽 바닥이 비어있으면 "구멍 낙하"로 전환
        if (!IsGrounded()) return;

        Vector2 boxSize = boxCollider.size;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - boxSize.y / 2);

        Vector2 moveDirection = rb.velocity.sqrMagnitude > 0.0001f ? rb.velocity.normalized : Vector2.zero;
        if (moveDirection == Vector2.zero) return;

        Vector2 futurePosition = rayOrigin + moveDirection * (boxSize.x / 2 + 0.1f);
        RaycastHit2D futureHit = Physics2D.Raycast(futurePosition, Vector2.down, holeDetectionDistance, groundLayerMask);

        if (futureHit.collider == null)
            StartHoleFall();
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
        // Gizmo/에디터 상태에서 Start()가 안 돌아 boxCollider가 null일 수 있습니다.
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return false;
        }

        float checkDistance = 0.1f;
        Vector2 boxSize = boxCollider.size;
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
                lastInteractionTime = Time.time;
                rb.drag = interactingDrag;
                Debug.Log("[AdvancedPushableBox] 상호작용 시작");
            }
            else
            {
                rb.drag = normalDrag;
                Debug.Log("[AdvancedPushableBox] 상호작용 종료");
            }
        }
        else if (isBeingPushed)
        {
            // 상호작용 유지 중이면 계속 갱신(밀다가 플랫폼 이동 등으로 순간 끊겨도 복귀 방지)
            lastInteractionTime = Time.time;
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
        // (중요) 운반 중(도르레 플랫폼 이동 등)에는 0으로 스냅하면 "멈춤"으로 오판되어 복귀가 켜질 수 있어 제외
        if (!isBeingPushed && !isFalling && !IsBeingCarriedByPulley() && rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private void StabilizeOnPulley()
    {
        // (중요) 태그("wall") 기반 판정은 바닥/벽과 동일 태그/레이어를 쓰는 순간 오작동합니다.
        // 도르레 위 판정은 PulleyPlatform 컴포넌트 기준(isOnPulleyPlatform)으로만 합니다.
        isOnPulley = isOnPulleyPlatform;

        if (isOnPulley)
        {
            // 하드락 모드에서는 물리 안정화(속도/중력 조정)보다 "완전 고정"이 우선입니다.
            if (enablePulleyHardLock && isPulleyHardLocked)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                wasOnPulley = isOnPulley;
                return;
            }

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

    private void EngagePulleyHardLock(PulleyPlatform pp, Transform snapPoint)
    {
        if (!enablePulleyHardLock) return;
        if (rb == null) return;
        if (pp == null) return;
        if (snapPoint == null) return;

        // 같은 플랫폼에 이미 락이 걸려있으면 무시
        if (isPulleyHardLocked && lockedPulleyPlatform == pp && lockedPulleySnapPoint == snapPoint)
            return;

        // 첫 진입 시 원래 상태 백업
        if (!isPulleyHardLocked)
        {
            prevBodyType = rb.bodyType;
            prevGravityScale = rb.gravityScale;
            prevDrag = rb.drag;
            prevConstraints = rb.constraints;
            prevCollisionDetectionMode = rb.collisionDetectionMode;
            prevInterpolation = rb.interpolation;
            prevSimulated = rb.simulated;
        }

        lockedPulleyPlatform = pp;
        lockedPulleySnapPoint = snapPoint;
        lockedPulleySnapCollider = pp.HardLockSnapCollider;
        lockedPulleyPlatformRb = pp.GetComponent<Rigidbody2D>();

        // (중요) 순간이동(스냅 포인트로 텔레포트) 느낌을 줄이기 위해,
        // '접촉 순간의 현재 위치'를 그대로 고정하고 플랫폼 움직임만 따라가도록 상대 오프셋을 저장합니다.
        Vector2 platformPos = lockedPulleyPlatformRb != null ? lockedPulleyPlatformRb.position : (Vector2)pp.transform.position;
        pulleyLockOffsetFromPlatform = rb.position - platformPos;

        // 완전 고정 세팅
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        isPulleyHardLocked = true;
    }

    private void ReleasePulleyHardLock()
    {
        if (!isPulleyHardLocked) return;
        if (rb == null) return;

        // 원래 상태 복원
        rb.bodyType = prevBodyType;
        rb.gravityScale = prevGravityScale;
        rb.drag = prevDrag;
        rb.constraints = prevConstraints;
        rb.collisionDetectionMode = prevCollisionDetectionMode;
        rb.interpolation = prevInterpolation;
        rb.simulated = prevSimulated;

        isPulleyHardLocked = false;
        lockedPulleyPlatform = null;
        lockedPulleySnapPoint = null;
        lockedPulleySnapCollider = null;
        lockedPulleyPlatformRb = null;
        pulleyLockOffsetFromPlatform = Vector2.zero;
    }

    private void CheckPulleyHardLockRelease()
    {
        if (pulleyHardLockReleaseCollider == null) return;
        if (boxCollider == null) return;

        // 하드락이 걸렸거나, 도르레 판정 중일 때만 해제 조건을 체크(불필요한 비용 감소)
        if (!isPulleyHardLocked && !isOnPulleyPlatform) return;

        // 콜라이더 기준 접촉/겹침 판정
        ColliderDistance2D cd = pulleyHardLockReleaseCollider.Distance(boxCollider);
        if (!(cd.isOverlapped || cd.distance <= pulleyHardLockContactEpsilon)) return;

        TriggerPulleyHardLockReleaseAndScheduleReturn();
    }

    private void TriggerPulleyHardLockReleaseAndScheduleReturn()
    {
        // 일정 시간 동안 도르레 판정/하드락 재진입을 막아(트리거 겹침으로 계속 붙는 현상 방지)
        suppressPulleyHardLockUntilTime = Time.time + pulleyHardLockReleaseReturnDelay + 0.1f;

        // 즉시 해제(물리값 복원 + 도르레 판정 해제)
        if (enablePulleyHardLock && isPulleyHardLocked)
            ReleasePulleyHardLock();

        // 도르레 판정도 강제로 끊는다(Trigger 겹침으로 onPlatform이 계속 true로 남는 문제 방지)
        currentPulleyPlatform = null;
        isOnPulleyPlatform = false;
        isOnPulley = false;
        wasOnPulley = false;

        // Floor 관련 상태도 안전하게 복구
        ManageFloorObjects(false);

        // 기존 예약이 있으면 교체
        if (pulleyHardLockReturnCoroutine != null)
            StopCoroutine(pulleyHardLockReturnCoroutine);
        pulleyHardLockReturnCoroutine = StartCoroutine(PulleyHardLockReturnRoutine());
    }

    private IEnumerator PulleyHardLockReturnRoutine()
    {
        yield return new WaitForSeconds(pulleyHardLockReleaseReturnDelay);

        // 혹시 다시 잠겨있으면 풀고 복귀
        if (enablePulleyHardLock && isPulleyHardLocked)
            ReleasePulleyHardLock();

        currentPulleyPlatform = null;
        isOnPulleyPlatform = false;
        isOnPulley = false;
        wasOnPulley = false;

        InstantReturnToOrigin();

        pulleyHardLockReturnCoroutine = null;
    }

    private void SyncPulleyHardLock()
    {
        if (!enablePulleyHardLock) return;
        if (rb == null) return;

        // 도르레 판정이 풀렸으면 즉시 해제
        if (!isOnPulleyPlatform || currentPulleyPlatform == null)
        {
            if (isPulleyHardLocked)
                ReleasePulleyHardLock();
            return;
        }

        // 해제 후 유예 시간 동안은 도르레 하드락 로직을 실행하지 않는다
        if (suppressPulleyHardLockUntilTime > 0f && Time.time < suppressPulleyHardLockUntilTime)
            return;

        // 아직 락이 안 걸렸다면: 스냅 포인트에 "닿았을 때만" 락을 건다
        if (!isPulleyHardLocked)
        {
            Transform snapPoint = currentPulleyPlatform.HardLockSnapPoint;
            if (snapPoint == null) return;

            // 1) 콜라이더가 있으면 콜라이더 기준(접촉/겹침)으로 판정
            Collider2D snapCol = currentPulleyPlatform.HardLockSnapCollider;
            if (snapCol != null && boxCollider != null)
            {
                // ColliderDistance2D.distance: 표면 간 거리(겹치면 음수)
                ColliderDistance2D cd = snapCol.Distance(boxCollider);
                if (cd.isOverlapped || cd.distance <= pulleyHardLockContactEpsilon)
                {
                    EngagePulleyHardLock(currentPulleyPlatform, snapPoint);
                }
                return;
            }

            // 2) 콜라이더가 없으면(세팅 누락) 거리 기반 백업
            if (!warnedMissingSnapColliderOnce)
            {
                warnedMissingSnapColliderOnce = true;
                Debug.LogWarning("[AdvancedPushableBox] PulleyPlatform.HardLockSnapCollider가 비어있어 거리 기반 스냅으로 대체합니다. SnapPoint에 Collider2D(Trigger 권장)를 추가하세요.");
            }

            float dist = Vector2.Distance(rb.position, (Vector2)snapPoint.position);
            if (dist <= pulleyHardLockSnapDistance)
            {
                EngagePulleyHardLock(currentPulleyPlatform, snapPoint);
            }
            return;
        }

        // 락이 걸린 상태인데 플랫폼이 바뀌었으면 해제(안전)
        if (lockedPulleyPlatform != currentPulleyPlatform)
        {
            ReleasePulleyHardLock();
            return;
        }

        if (rb.bodyType != RigidbodyType2D.Kinematic)
            rb.bodyType = RigidbodyType2D.Kinematic;

        if (lockedPulleySnapPoint == null)
        {
            ReleasePulleyHardLock();
            return;
        }

        // 락 유지 중에도 스냅 콜라이더가 사라졌다면 해제(안전)
        if (lockedPulleyPlatform != null && lockedPulleyPlatform.HardLockSnapCollider != null)
            lockedPulleySnapCollider = lockedPulleyPlatform.HardLockSnapCollider;

        Vector2 platformPos = lockedPulleyPlatformRb != null ? lockedPulleyPlatformRb.position : (Vector2)lockedPulleyPlatform.transform.position;
        Vector2 target = platformPos + pulleyLockOffsetFromPlatform;

        // 하드락 유지: 플랫폼에 강제 동기화
        rb.position = target;
        transform.position = target;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    private void HandleMovementBasedReturn()
    {
        if (!enableReturnToOrigin) return;

        // 1) 원래 영역 밖인지 확인
        // Rigidbody를 기준으로 판정해야 텔레포트 직후(Transform 갱신 전)에도 안정적입니다.
        float distanceFromOrigin = Vector2.Distance(rb.position, (Vector2)targetReturnPosition);
        bool isOutsideOriginArea = (distanceFromOrigin > minDistanceFromOrigin);
        if (!isOutsideOriginArea)
        {
            // 영역 안으로 돌아오면 상태 초기화
            hasLeftOriginalArea = false;
            isWaitingForReturn = false;
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;
            settleTimer = 0f;
            return;
        }

        if (!hasLeftOriginalArea)
        {
            hasLeftOriginalArea = true;
            isWaitingForReturn = false;
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;
            settleTimer = 0f;
            Debug.Log("[AdvancedPushableBox] 원래 영역을 벗어났습니다");
        }

        // 2) “정말 멈춰있다” 판정: 바닥 위 + 속도 거의 0 + 플레이어가 밀지 않음 + 도르레 위 아님 + 낙하 아님
        bool isStable =
            !isBeingPushed &&
            !isOnPulley &&
            !isFalling &&
            !IsBeingCarriedByPulley() &&
            IsGrounded() &&
            rb.velocity.sqrMagnitude <= (returnSpeedThreshold * returnSpeedThreshold);

        if (!isStable)
        {
            settleTimer = 0f;
            timeStoppedMoving = 0f;
            timeWaitingForReturn = 0f;

            if (isWaitingForReturn)
            {
                isWaitingForReturn = false;
                Debug.Log("[AdvancedPushableBox] 복귀 취소됨(다시 움직임/상태 변화)");
            }
            return;
        }

        // 3) 멈춤이 잠깐이 아니라 일정 시간 유지되어야 카운트 시작(미세 떨림 방지)
        settleTimer += Time.fixedDeltaTime;
        if (settleTimer < returnSettleTime) return;

        // 4) 복귀 카운트다운
        timeStoppedMoving += Time.fixedDeltaTime;

        if (!isWaitingForReturn)
        {
            isWaitingForReturn = true;
            Debug.Log($"[AdvancedPushableBox] 안정적으로 멈춤 감지 → {returnDelay:F1}초 후 복귀");
        }

        if (timeStoppedMoving >= returnDelay)
        {
            InstantReturnToOrigin();
        }
    }
    
    private void InstantReturnToOrigin()
    {
        Debug.Log("[AdvancedPushableBox] 즉시 원래 위치로 복귀!");

        Vector2 desired = targetReturnPosition;
        Vector2 safe = FindSafeReturnPosition(desired);
        if ((safe - desired).sqrMagnitude > 0.0001f)
            Debug.LogWarning($"[AdvancedPushableBox] 복귀 위치가 겹쳐서 안전 위치로 보정: {desired} -> {safe}");

        // Transform 텔레포트 시 물리/충돌 동기 문제가 생길 수 있어 Rigidbody 기준으로 이동
        rb.position = safe;
        // (중요) rb.position만 바꾸면 transform이 즉시 갱신되지 않는 경우가 있어
        // 로직/기즈모/거리판정이 1프레임 틀어질 수 있습니다. 둘을 동기화합니다.
        transform.position = safe;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = normalGravityScale;

        if (forceContinuousCollision)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // 리스폰/복귀 후 콜라이더가 꺼지거나 Trigger로 남아 바닥을 뚫는 케이스 방어
        EnsureColliderState(forceLog: true);

        // 이동 직후 잔진동/관성으로 다시 밀려나는 것을 줄이기 위해 sleep
        rb.Sleep();
        
        hasLeftOriginalArea = false;
        isObjectMoving = false;
        timeStoppedMoving = 0f;
        timeWaitingForReturn = 0f;
        isWaitingForReturn = false;
        isFalling = false;
        isInHole = false;
        isOnPulley = false;
        isOnPulleyPlatform = false;
        currentPulleyPlatform = null;
        wasOnPulley = false;
        
        lastIntegerPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        Debug.Log("[AdvancedPushableBox] 복귀 완료");
    }

    private Vector2 FindSafeReturnPosition(Vector2 desired)
    {
        if (boxCollider == null) return desired;

        // 월드 기준 OverlapBox 크기 계산(스케일 반영, 약간의 여유 추가)
        Vector3 ls = transform.lossyScale;
        Vector2 size = new Vector2(Mathf.Abs(boxCollider.size.x * ls.x), Mathf.Abs(boxCollider.size.y * ls.y));
        size += Vector2.one * returnOverlapPadding;

        // BoxCollider2D는 offset이 있을 수 있어서 "콜라이더 중심" 기준으로 검사해야 정확합니다.
        // (offset을 무시하면 겹치는데도 겹치지 않는 것처럼 오판할 수 있음)
        float angle = rb != null ? rb.rotation : transform.eulerAngles.z;
        Vector2 offsetWorld = Quaternion.Euler(0f, 0f, angle) * new Vector2(boxCollider.offset.x * ls.x, boxCollider.offset.y * ls.y);

        bool IsClear(Vector2 p)
        {
            Vector2 center = p + offsetWorld;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, returnBlockerMask);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h == boxCollider) continue;
                if (h.transform == transform) continue;
                if (h.isTrigger) continue; // 트리거는 무시(감지 영역 등)
                return false;
            }
            return true;
        }

        if (IsClear(desired)) return desired;

        // 위로 올리면서 찾기 + 좌우 약간 시도(스폰 위치가 벽/플레이어와 겹치는 경우)
        for (int i = 1; i <= returnSearchSteps; i++)
        {
            float dy = returnSearchStep * i;
            Vector2 up = desired + Vector2.up * dy;
            if (IsClear(up)) return up;

            Vector2 left = up + Vector2.left * returnSearchStep;
            if (IsClear(left)) return left;

            Vector2 right = up + Vector2.right * returnSearchStep;
            if (IsClear(right)) return right;
        }

        // 어쩔 수 없으면 원래 위치 그대로(이 경우 로그로 확인 가능)
        Debug.LogWarning("[AdvancedPushableBox] 안전한 복귀 위치를 찾지 못했습니다. returnBlockerMask/탐색 파라미터를 확인하세요.");
        return desired;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdatePlayerContact(collision, isEnteringOrStaying: true);
        UpdatePulleyPlatformContact(collision, isEnteringOrStaying: true);
        // 도르레 위 판정은 UpdatePulleyPlatformContact에서 처리

        if (debugCollisionLayerLog && collision != null && collision.collider != null)
        {
            string otherLayer = LayerMask.LayerToName(collision.gameObject.layer);
            string myLayer = LayerMask.LayerToName(gameObject.layer);
            bool ignored = Physics2D.GetIgnoreLayerCollision(gameObject.layer, collision.gameObject.layer);
            Debug.Log($"[AdvancedPushableBox][CollisionEnter] me='{name}'({myLayer}) hit='{collision.gameObject.name}'({otherLayer}) tag='{collision.gameObject.tag}' ignoreLayerPair='{ignored}'");
        }

        TryIgnoreFloorCollisionIfNeeded(collision);
        
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
        UpdatePlayerContact(collision, isEnteringOrStaying: true);
        UpdatePulleyPlatformContact(collision, isEnteringOrStaying: true);
        // 도르레 위 판정은 UpdatePulleyPlatformContact에서 처리

        if (debugCollisionLayerLog && collision != null && collision.collider != null)
        {
            string otherLayer = LayerMask.LayerToName(collision.gameObject.layer);
            string myLayer = LayerMask.LayerToName(gameObject.layer);
            bool ignored = Physics2D.GetIgnoreLayerCollision(gameObject.layer, collision.gameObject.layer);
            Debug.Log($"[AdvancedPushableBox][CollisionStay] me='{name}'({myLayer}) hit='{collision.gameObject.name}'({otherLayer}) tag='{collision.gameObject.tag}' ignoreLayerPair='{ignored}'");
        }

        TryIgnoreFloorCollisionIfNeeded(collision);
        
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
        UpdatePlayerContact(collision, isEnteringOrStaying: false);
        UpdatePulleyPlatformContact(collision, isEnteringOrStaying: false);
        // 도르레 위 판정은 UpdatePulleyPlatformContact에서 처리
    }

    // 일부 플레이어/상호작용 구성에서는 Trigger로 밀기 판정이 이뤄질 수 있어 추가 방어
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugCollisionLayerLog && other != null)
        {
            string otherLayer = LayerMask.LayerToName(other.gameObject.layer);
            string myLayer = LayerMask.LayerToName(gameObject.layer);
            Debug.Log($"[AdvancedPushableBox][TriggerEnter] me='{name}'({myLayer}) hit='{other.gameObject.name}'({otherLayer}) tag='{other.gameObject.tag}' isTrigger='{other.isTrigger}'");
        }

        UpdatePulleyPlatformFromTrigger(other, isEnteringOrStaying: true);

        if (other != null && other.CompareTag("Player"))
        {
            isPlayerTouching = true;
            lastInteractionTime = Time.time;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (debugCollisionLayerLog && other != null)
        {
            string otherLayer = LayerMask.LayerToName(other.gameObject.layer);
            string myLayer = LayerMask.LayerToName(gameObject.layer);
            Debug.Log($"[AdvancedPushableBox][TriggerStay] me='{name}'({myLayer}) hit='{other.gameObject.name}'({otherLayer}) tag='{other.gameObject.tag}' isTrigger='{other.isTrigger}'");
        }

        UpdatePulleyPlatformFromTrigger(other, isEnteringOrStaying: true);

        if (other != null && other.CompareTag("Player"))
        {
            isPlayerTouching = true;
            lastInteractionTime = Time.time;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (debugCollisionLayerLog && other != null)
        {
            string otherLayer = LayerMask.LayerToName(other.gameObject.layer);
            string myLayer = LayerMask.LayerToName(gameObject.layer);
            Debug.Log($"[AdvancedPushableBox][TriggerExit] me='{name}'({myLayer}) hit='{other.gameObject.name}'({otherLayer}) tag='{other.gameObject.tag}' isTrigger='{other.isTrigger}'");
        }

        UpdatePulleyPlatformFromTrigger(other, isEnteringOrStaying: false);

        if (other != null && other.CompareTag("Player"))
        {
            isPlayerTouching = false;
            lastInteractionTime = Time.time;
        }
    }

    private void UpdatePlayerContact(Collision2D collision, bool isEnteringOrStaying)
    {
        if (collision == null) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (isEnteringOrStaying)
        {
            isPlayerTouching = true;
            lastInteractionTime = Time.time;
        }
        else
        {
            isPlayerTouching = false;
            lastInteractionTime = Time.time;
        }
    }

    private void UpdatePulleyPlatformContact(Collision2D collision, bool isEnteringOrStaying)
    {
        // PulleyPlatform(도르레 발판) 위에서 운반되는 동안은 복귀하면 안됨
        PulleyPlatform pp = collision.collider != null ? collision.collider.GetComponentInParent<PulleyPlatform>() : null;
        if (pp == null) return;

        if (isEnteringOrStaying)
        {
            if (IsStandingOn(collision))
            {
                SetOnPulleyPlatform(pp, true);
            }
        }
        else
        {
            if (currentPulleyPlatform == pp)
            {
                SetOnPulleyPlatform(null, false);
            }
        }
    }

    private void UpdatePulleyPlatformFromTrigger(Collider2D trigger, bool isEnteringOrStaying)
    {
        // ObjectDetector 트리거에 들어갔을 때도 도르레 플랫폼 위로 판정
        if (trigger == null) return;

        ObjectDetector detector = trigger.GetComponent<ObjectDetector>();
        if (detector == null) return;

        // ObjectDetector의 부모에서 PulleyPlatform 찾기
        PulleyPlatform pp = trigger.GetComponentInParent<PulleyPlatform>();
        if (pp == null) return;

        if (isEnteringOrStaying)
        {
            SetOnPulleyPlatform(pp, true);
        }
        else
        {
            if (currentPulleyPlatform == pp)
            {
                SetOnPulleyPlatform(null, false);
            }
        }
    }

    private void SetOnPulleyPlatform(PulleyPlatform pp, bool onPlatform)
    {
        // 하드락 해제 직후에는 트리거 겹침으로 다시 도르레 판정이 켜지는 것을 막는다
        if (onPlatform && suppressPulleyHardLockUntilTime > 0f && Time.time < suppressPulleyHardLockUntilTime)
            return;

        currentPulleyPlatform = pp;
        isOnPulleyPlatform = onPlatform;
        lastInteractionTime = Time.time;

        if (debugCollisionLayerLog)
        {
            Debug.Log($"[AdvancedPushableBox][PulleyPlatform] SetOnPulleyPlatform: {onPlatform} platform='{(pp != null ? pp.name : "null")}'");
        }

        if (!onPlatform)
        {
            // 도르레에서 벗어났으므로 바닥 오브젝트 다시 활성화
            ManageFloorObjects(false);

            // 도르레 하드락 해제(원래 물리값으로 복원)
            if (enablePulleyHardLock)
                ReleasePulleyHardLock();
        }
    }

    private bool IsBeingCarriedByPulley()
    {
        // 0) 플레이어가 닿아있으면(=밀고 있거나 막 놓은 직후) 복귀 금지
        if (isPlayerTouching)
            return true;

        // 1) 도르레 플랫폼 위에 있고, 플랫폼이 움직이면 복귀 금지
        if (isOnPulleyPlatform && currentPulleyPlatform != null && currentPulleyPlatform.IsMoving)
            return true;

        // 2) 최근 상호작용/접촉이 있었다면 복귀 금지
        if (Time.time - lastInteractionTime < returnInteractionCooldown)
            return true;

        return false;
    }

    private bool ShouldPhaseThroughFloor()
    {
        // “도르레를 타고 내려갈 때 Floor를 뚫고 내려가야 함” 요구사항을 그대로 반영
        if (!isOnPulleyPlatform || currentPulleyPlatform == null)
        {
            if (debugCollisionLayerLog && debugFloorPhaseLog)
                Debug.Log($"[AdvancedPushableBox][FloorPhase] ShouldPhase=false: isOnPulley={isOnPulleyPlatform}, platform={currentPulleyPlatform != null}");
            return false;
        }
        if (!currentPulleyPlatform.IsMoving)
        {
            if (debugCollisionLayerLog && debugFloorPhaseLog)
                Debug.Log($"[AdvancedPushableBox][FloorPhase] ShouldPhase=false: platform not moving");
            return false;
        }

        // TargetHeight가 현재보다 낮으면 내려가는 중
        bool isDescending = currentPulleyPlatform.TargetHeight < currentPulleyPlatform.CurrentHeight;
        if (debugCollisionLayerLog && debugFloorPhaseLog && isDescending)
        {
            Debug.Log($"[AdvancedPushableBox][FloorPhase] ShouldPhase=true: descending (T={currentPulleyPlatform.TargetHeight:F1} < H={currentPulleyPlatform.CurrentHeight:F1})");
        }
        return isDescending;
    }

    private void TryIgnoreFloorCollisionIfNeeded(Collision2D collision)
    {
        if (collision == null || collision.collider == null || boxCollider == null) return;
        if (!ShouldPhaseThroughFloor()) return;

        // 충돌 상대가 Floor 레이어일 때만 무시
        if (floorLayer >= 0 && collision.gameObject.layer != floorLayer) return;
        if (collision.collider.isTrigger) return;

        if (!ignoredFloorColliders.Contains(collision.collider))
        {
            Physics2D.IgnoreCollision(boxCollider, collision.collider, true);
            ignoredFloorColliders.Add(collision.collider);

            if (debugCollisionLayerLog && debugFloorPhaseLog)
            {
                Debug.Log($"[AdvancedPushableBox][FloorPhase] IgnoreCollision ON -> '{collision.collider.name}' layer='{LayerMask.LayerToName(collision.gameObject.layer)}'");
            }
        }
    }

    private void MaintainFloorPhasing()
    {
        // 도르레로 내려가는 동안엔 "충돌 콜백"에 의존하지 않고,
        // 현재/근처 Overlap으로 Floor 콜라이더를 찾아 강제로 IgnoreCollision을 켭니다.
        // (PlatformEffector/키네마틱 이동/타임스텝 타이밍 때문에 OnCollision이 안 타는 케이스 방어)

        bool phasing = ShouldPhaseThroughFloor();
        
        // 인스펙터에서 지정한 바닥 오브젝트들 비활성화/활성화
        ManageFloorObjects(phasing);

        // 유효하지 않은 항목 정리
        ignoredFloorColliders.RemoveWhere(c => c == null);

        if (phasing && boxCollider != null && floorLayer >= 0)
        {
            // 박스 콜라이더의 월드 AABB 기준으로 겹치는 Floor 후보를 찾는다(오프셋/스케일 반영).
            Bounds b = boxCollider.bounds;
            Vector2 center = b.center;
            Vector2 size = b.size + Vector3.one * returnOverlapPadding;

            // Floor 레이어만 대상으로
            int floorMask = 1 << floorLayer;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, rb != null ? rb.rotation : 0f, floorMask);

            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.isTrigger) continue;
                if (ignoredFloorColliders.Contains(h)) continue;

                Physics2D.IgnoreCollision(boxCollider, h, true);
                ignoredFloorColliders.Add(h);

                // Platform Effector 2D가 있으면 일시적으로 비활성화 (One Way가 위→아래를 막는 문제 해결)
                PlatformEffector2D effector = h.GetComponent<PlatformEffector2D>();
                if (effector != null && effector.enabled && !disabledFloorEffectors.Contains(effector))
                {
                    effector.enabled = false;
                    disabledFloorEffectors.Add(effector);
                    if (debugCollisionLayerLog && debugFloorPhaseLog)
                    {
                        Debug.Log($"[AdvancedPushableBox][FloorPhase] PlatformEffector2D DISABLED -> '{h.name}'");
                    }
                }

                if (debugCollisionLayerLog && debugFloorPhaseLog)
                {
                    Debug.Log($"[AdvancedPushableBox][FloorPhase] IgnoreCollision ON(overlap) -> '{h.name}' layer='{LayerMask.LayerToName(h.gameObject.layer)}'");
                }
            }

            return; // phasing 중엔 복구하지 않음
        }

        // 조건이 풀리면 무시했던 Floor 콜라이더 충돌을 모두 복구
        if (ignoredFloorColliders.Count == 0 && disabledFloorEffectors.Count == 0) return;

        foreach (var col in new List<Collider2D>(ignoredFloorColliders))
        {
            if (col == null) continue;
            Physics2D.IgnoreCollision(boxCollider, col, false);
        }
        ignoredFloorColliders.Clear();

        // Platform Effector 2D도 모두 복구
        foreach (var effector in new List<PlatformEffector2D>(disabledFloorEffectors))
        {
            if (effector != null)
            {
                effector.enabled = true;
                if (debugCollisionLayerLog && debugFloorPhaseLog)
                {
                    Debug.Log($"[AdvancedPushableBox][FloorPhase] PlatformEffector2D ENABLED -> '{effector.name}'");
                }
            }
        }
        disabledFloorEffectors.Clear();

        if (debugCollisionLayerLog && debugFloorPhaseLog)
            Debug.Log("[AdvancedPushableBox][FloorPhase] IgnoreCollision OFF (restore)");
    }

    private void ManageFloorObjects(bool shouldDisable)
    {
        // 인스펙터에서 지정한 바닥 오브젝트가 없으면 무시
        if (floorObjectsToDisable == null || floorObjectsToDisable.Length == 0)
        {
            if (debugCollisionLayerLog && debugFloorPhaseLog && shouldDisable)
                Debug.LogWarning("[AdvancedPushableBox][FloorPhase] floorObjectsToDisable 배열이 비어있습니다!");
            return;
        }

        // 이미 원하는 상태면 무시
        if (shouldDisable == floorObjectsDisabled) return;

        floorObjectsDisabled = shouldDisable;

        int disabledCount = 0;
        foreach (var floorObj in floorObjectsToDisable)
        {
            if (floorObj == null)
            {
                if (debugCollisionLayerLog && debugFloorPhaseLog)
                    Debug.LogWarning("[AdvancedPushableBox][FloorPhase] floorObjectsToDisable 배열에 null이 있습니다!");
                continue;
            }

            bool wasActive = floorObj.activeSelf;
            floorObj.SetActive(!shouldDisable);
            disabledCount++;

            if (debugCollisionLayerLog && debugFloorPhaseLog)
            {
                Debug.Log($"[AdvancedPushableBox][FloorPhase] Floor object '{floorObj.name}' {(shouldDisable ? "DISABLED" : "ENABLED")} (was: {wasActive})");
            }
        }

        if (debugCollisionLayerLog && debugFloorPhaseLog && disabledCount == 0)
        {
            Debug.LogWarning("[AdvancedPushableBox][FloorPhase] 비활성화할 바닥 오브젝트가 없습니다!");
        }
    }

    private bool IsStandingOn(Collision2D collision)
    {
        // Contact normal이 위를 향하면(다른 콜라이더가 아래쪽에 있으면) "위에 올라탐"으로 판단
        foreach (var c in collision.contacts)
        {
            if (c.normal.y > 0.5f)
                return true;
        }
        return false;
    }

    private void EnsureColliderState(bool forceLog = false)
    {
        if (boxCollider == null) return;

        if (!boxCollider.enabled)
        {
            boxCollider.enabled = true;
            if (forceLog) Debug.LogWarning("[AdvancedPushableBox] BoxCollider2D가 비활성화되어 있어 강제로 활성화했습니다.");
        }

        if (forceNonTriggerCollider && boxCollider.isTrigger)
        {
            boxCollider.isTrigger = false;
            if (!warnedTriggerOnce || forceLog)
            {
                Debug.LogWarning("[AdvancedPushableBox] BoxCollider2D가 Trigger로 되어 있어 강제로 해제했습니다. (바닥 통과 방지)");
                warnedTriggerOnce = true;
            }
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