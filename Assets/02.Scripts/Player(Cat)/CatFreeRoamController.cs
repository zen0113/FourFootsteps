using System.Collections;
using UnityEngine;
using System;

public class CatFreeRoam : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;
    public float idleTime = 2f; // 정지 시간
    public float minIdleTime = 1f; // 최소 정지 시간
    public float maxIdleTime = 5f; // 최대 정지 시간

    [Header("시작 설정")]
    public float startDelay = 0f; // 시작 딜레이 시간 (초)
    public bool autoStartRoaming = true; // 자동 배회 시작 여부

    [Header("이동 범위 설정")]
    public Vector2 minBounds = new Vector2(-5f, -3f); // 이동 가능한 최소 위치
    public Vector2 maxBounds = new Vector2(5f, 3f);   // 이동 가능한 최대 위치
    public bool useColliderBounds = false; // Collider2D 경계를 사용할지 여부

    [Header("이동 패턴")]
    public float wanderRadius = 3f; // 배회 반경
    public float newTargetDistance = 1f; // 새로운 목표까지의 최소 거리

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private float walkSoundInterval = 0.3f;

    // 이벤트
    public Action OnStartMoving;
    public Action OnStopMoving;
    public Action OnNewDestination;
    public Action OnRoamingStarted; // 배회 시작 이벤트
    public Action OnStartDelayBegin; // 시작 딜레이 시작 이벤트

    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;
    private Collider2D areaBounds; // 이동 가능 영역의 콜라이더

    // 상태 변수
    private Vector2 targetPoint;
    private bool isMoving = false;
    private bool isIdling = false;
    private bool isWaitingToStart = false; // 시작 대기 상태
    private float lastWalkSoundTime;
    private Coroutine roamingCoroutine;
    private Coroutine startDelayCoroutine;

    // 이동 상태 열거형
    public enum RoamState
    {
        WaitingToStart, // 시작 대기 상태 추가
        Idle,
        MovingToTarget,
        Wandering
    }

    private RoamState currentState = RoamState.WaitingToStart;

    private void Awake()
    {
        InitializeComponents();

        // Collider 경계 사용 시 찾기
        if (useColliderBounds)
        {
            areaBounds = GameObject.FindGameObjectWithTag("CatArea")?.GetComponent<Collider2D>();
            if (areaBounds == null)
            {
                Debug.LogWarning("CatArea 태그를 가진 Collider2D를 찾을 수 없습니다. 기본 경계를 사용합니다.");
                useColliderBounds = false;
            }
        }
    }

    private void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (autoStartRoaming)
        {
            StartRoamingWithDelay();
        }
    }

    public void StartRoamingWithDelay()
    {
        if (startDelayCoroutine != null)
        {
            StopCoroutine(startDelayCoroutine);
        }

        startDelayCoroutine = StartCoroutine(StartDelayCoroutine());
    }

    private IEnumerator StartDelayCoroutine()
    {
        if (startDelay > 0f)
        {
            isWaitingToStart = true;
            currentState = RoamState.WaitingToStart;
            OnStartDelayBegin?.Invoke();

            yield return new WaitForSeconds(startDelay);
        }

        isWaitingToStart = false;
        StartRoaming();
        OnRoamingStarted?.Invoke();
    }

    public void StartRoaming()
    {
        if (roamingCoroutine != null)
        {
            StopCoroutine(roamingCoroutine);
        }

        roamingCoroutine = StartCoroutine(RoamingBehavior());
    }

    public void StopRoaming()
    {
        if (roamingCoroutine != null)
        {
            StopCoroutine(roamingCoroutine);
            roamingCoroutine = null;
        }

        if (startDelayCoroutine != null)
        {
            StopCoroutine(startDelayCoroutine);
            startDelayCoroutine = null;
        }

        StopMoving();
        isWaitingToStart = false;
        currentState = RoamState.Idle;
    }

    private IEnumerator RoamingBehavior()
    {
        while (true)
        {
            // 새로운 목적지 설정
            Vector2 newTarget = GetRandomTargetPosition();
            StartMovingToTarget(newTarget);

            currentState = RoamState.MovingToTarget;
            OnNewDestination?.Invoke();

            // 목적지에 도달할 때까지 대기
            yield return new WaitUntil(() => !isMoving);

            // 랜덤한 시간 동안 정지
            float randomIdleTime = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
            currentState = RoamState.Idle;

            yield return new WaitForSeconds(randomIdleTime);
        }
    }

    private Vector2 GetRandomTargetPosition()
    {
        Vector2 randomTarget;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            if (useColliderBounds && areaBounds != null)
            {
                // Collider 경계 내에서 랜덤 위치 생성
                Bounds bounds = areaBounds.bounds;
                randomTarget = new Vector2(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
                );
            }
            else
            {
                // 설정된 경계 내에서 랜덤 위치 생성
                randomTarget = new Vector2(
                    UnityEngine.Random.Range(minBounds.x, maxBounds.x),
                    UnityEngine.Random.Range(minBounds.y, maxBounds.y)
                );
            }

            attempts++;
        }
        while (Vector2.Distance(transform.position, randomTarget) < newTargetDistance && attempts < maxAttempts);

        return randomTarget;
    }

    private void StartMovingToTarget(Vector2 target)
    {
        targetPoint = target;
        isMoving = true;
        isIdling = false;

        animator?.SetBool("Moving", true);
        lastWalkSoundTime = Time.time;
        PlayWalkSound();

        OnStartMoving?.Invoke();
    }

    private void StopMoving()
    {
        isMoving = false;
        animator?.SetBool("Moving", false);

        if (audioSource.isPlaying && audioSource.clip == walkSound)
        {
            audioSource.Stop();
        }

        OnStopMoving?.Invoke();
    }

    private void Update()
    {
        // 시작 대기 중이거나 움직이지 않는 경우 리턴
        if (isWaitingToStart || !isMoving) return;

        Vector2 direction = targetPoint - (Vector2)transform.position;
        float distance = direction.magnitude;

        // 목적지 도달 확인
        if (distance <= stopDistance)
        {
            StopMoving();
            return;
        }

        // 스프라이트 방향 설정
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;

        // 이동
        Vector2 moveDir = direction.normalized;
        Vector2 newPosition = (Vector2)transform.position + (moveDir * moveSpeed * Time.deltaTime);

        // 경계 확인
        if (IsPositionValid(newPosition))
        {
            transform.position = newPosition;
        }
        else
        {
            // 경계에 닿으면 새로운 목적지 찾기
            StopMoving();
            return;
        }

        // 걷기 소리 재생
        if (Time.time - lastWalkSoundTime >= walkSoundInterval)
        {
            PlayWalkSound();
            lastWalkSoundTime = Time.time;
        }
    }

    private bool IsPositionValid(Vector2 position)
    {
        if (useColliderBounds && areaBounds != null)
        {
            return areaBounds.bounds.Contains(position);
        }
        else
        {
            return position.x >= minBounds.x && position.x <= maxBounds.x &&
                   position.y >= minBounds.y && position.y <= maxBounds.y;
        }
    }

    private void PlayWalkSound()
    {
        if (walkSound != null)
        {
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
            {
                audioSource.PlayOneShot(walkSound);
            }
        }
    }

    // 수동으로 특정 위치로 이동 (기존 기능 유지)
    public void MoveToPosition(Vector2 destination)
    {
        StopRoaming();
        StartMovingToTarget(destination);
    }

    // 현재 상태 반환
    public RoamState GetCurrentState()
    {
        return currentState;
    }

    // 시작 대기 상태 확인
    public bool IsWaitingToStart()
    {
        return isWaitingToStart;
    }

    // 수동으로 배회 시작 (딜레이 무시)
    public void ForceStartRoaming()
    {
        if (startDelayCoroutine != null)
        {
            StopCoroutine(startDelayCoroutine);
            startDelayCoroutine = null;
        }

        isWaitingToStart = false;
        StartRoaming();
        OnRoamingStarted?.Invoke();
    }

    // 경계 시각화 (에디터에서)
    private void OnDrawGizmosSelected()
    {
        if (useColliderBounds && areaBounds != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(areaBounds.bounds.center, areaBounds.bounds.size);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Vector2 center = (minBounds + maxBounds) / 2f;
            Vector2 size = maxBounds - minBounds;
            Gizmos.DrawWireCube(center, size);
        }

        // 현재 목표 지점 표시
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint, 0.2f);
            Gizmos.DrawLine(transform.position, targetPoint);
        }

        // 시작 대기 상태 표시
        if (isWaitingToStart)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}