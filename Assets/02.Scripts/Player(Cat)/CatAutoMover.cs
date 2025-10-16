using System.Collections;
using UnityEngine;
using System;

public class CatAutoMover : MonoBehaviour
{
    public Transform targetPoint;

    [Header("공통 이동")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;

    [Header("대쉬 옵션")]
    [SerializeField] private bool enableDash = false;          // 대쉬 기능 on/off
    [SerializeField] private bool dashAllTheWay = false;      // true면 끝까지 대쉬
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float switchToRunDistance = 1.5f;// 이 거리 미만부터는 걷기/뛰기 전환
    [SerializeField] private float accel = 40f;               // 가속도(속도 전환 부드럽게)
    [SerializeField] private float decel = 60f;               // 도착 직전 감속

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound; // 걷기 소리
    [SerializeField] private float walkSoundInterval = 0.3f; // 걷기 소리 재생 간격
    [SerializeField] private AudioClip runSound;              // 달리기 소리
    [SerializeField] private float runSoundInterval = 0.5f;
    private float lastWalkSoundTime; // 마지막 걷기 소리 재생 시간

    [Header("도착 처리(클린업)")]
    [SerializeField] private float arriveSnapDistance = 0.02f; // 최종 스냅 임계값
    [SerializeField] private bool useRigidbodyMotion = true;   // RB2D가 있으면 MovePosition 사용
    private Rigidbody2D rb;
    private bool arrivingLock = false; // 도착 분기 진입하면 이 안에서만 처리

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource; // AudioSource 추가
    [SerializeField] private bool isMoving = false;
    public bool IsMoving => isMoving;

    private float currentSpeed; // 가변 속도(보간 대상)
    private Vector2 _desiredVelocity; // 초당 속도 (Update에서 계산, FixedUpdate에서 적용)

    private enum MoveState { Idle, Walking, Dashing }
    private MoveState state = MoveState.Idle;

    int _hIsGrounded = Animator.StringToHash("IsGrounded");
    int _hSpeed = Animator.StringToHash("Speed");
    int _hShift = Animator.StringToHash("Shift");
    int _hJump = Animator.StringToHash("Jump");

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); // AudioSource 컴포넌트 가져오기
        if (audioSource == null) // AudioSource가 없으면 추가
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
        SetAnim(false, false);
        isMoving = false;
        state = MoveState.Idle;
        _desiredVelocity = Vector2.zero;
    }

    /// <summary> 걷기/기본 이동 시작(상황에 따라 대쉬 전환 가능) </summary>
    public void StartMoving(Transform destination)
    {
        targetPoint = destination;
        isMoving = true;
        state = MoveState.Walking;
        currentSpeed = 0f; // 가속 시작점
        SetAnim(moving: true, dashing: false);
        lastWalkSoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayFootstepIfDue(true);       // 이동 시작 시 걷는 소리 바로 재생
    }

    /// <summary> 시작부터 대쉬로 이동(필요 시 막판에 걷기/뛰기로 전환) </summary>
    public void StartDashMoving(Transform destination)
    {
        targetPoint = destination;
        isMoving = true;
        state = enableDash ? MoveState.Dashing : MoveState.Walking;
        currentSpeed = 0f;
        lastWalkSoundTime = Time.time;
        SetAnim(moving: !enableDash, dashing: enableDash);
        PlayFootstepIfDue(true);
    }

    /// <summary> 즉시 정지(연출 중단) </summary>
    public void Stop()
    {
        isMoving = false;
        state = MoveState.Idle;
        currentSpeed = 0f;
        _desiredVelocity = Vector2.zero;
        audioSource.Stop();
        SetAnim(false, false);
    }

    /// Dash 추가 전 Update()
    /*
    private void Update()
    {
        if (!isMoving || targetPoint == null)
        {
            if (!isMoving)
            {
                animator?.SetBool("Moving", false); // 정지 시 애니메이션 끄기
                if (audioSource.isPlaying && audioSource.clip == walkSound) // 걷는 소리 재생 중이면 멈춤
                {
                    audioSource.Stop();
                }
            }
            return;
        }

        Vector2 direction = targetPoint.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            isMoving = false;
            animator?.SetBool("Moving", false); // 도착 시 애니메이션 끄기
            if (audioSource.isPlaying && audioSource.clip == walkSound) // 걷는 소리 재생 중이면 멈춤
            {
                audioSource.Stop();
            }
            OnArrived?.Invoke(); // 도착 이벤트 호출
            return;
        }

        // 방향 반전
        if (direction.x > 0)
            spriteRenderer.flipX = false; // 오른쪽으로 이동 시 정상
        else if (direction.x < 0)
            spriteRenderer.flipX = true;  // 왼쪽으로 이동 시 반전

        Vector2 moveDir = direction.normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);
        animator?.SetBool("Moving", true); // 이동 중 애니메이션 설정

        // 걷는 소리 재생
        if (Time.time - lastWalkSoundTime >= walkSoundInterval)
        {
            PlayWalkSound();
            lastWalkSoundTime = Time.time;
        }
    }
    */

    private void Update()
    {
        if (!isMoving || targetPoint == null)
        {
            if (!isMoving) SetAnim(false, false);
            return;
        }

        Vector2 toTarget = (Vector2)(targetPoint.position - transform.position);
        float distance = toTarget.magnitude;

        // 도착 처리(ArriveStep 내부에서 스냅만 수행; 이동은 FixedUpdate에서)
        if (arrivingLock || distance <= stopDistance)
        {
            ArriveStep(toTarget, distance);
            _desiredVelocity = Vector2.zero;
            return;
        }

        // 방향 전환(좌/우 플립)
        if (toTarget.x > 0) spriteRenderer.flipX = false;
        else if (toTarget.x < 0) spriteRenderer.flipX = true;

        // 상태 결정(대쉬→걷기/뛰기 스위치)
        if (enableDash)
        {
            bool shouldDash = dashAllTheWay || distance > switchToRunDistance;
            if (shouldDash && state != MoveState.Dashing)
            {
                state = MoveState.Dashing;
                SetAnim(moving: false, dashing: true);
            }
            else if (!shouldDash && state != MoveState.Walking)
            {
                state = MoveState.Walking;
                SetAnim(moving: true, dashing: false);
            }
        }
        else
        {
            state = MoveState.Walking;
            SetAnim(moving: true, dashing: false);
        }

        // 애니메이터 파라미터 싱크
        SyncAnimatorParams();

        // 목표 속도 & 가감속 (초당 속도 계산)
        float targetSpeed = (state == MoveState.Dashing) ? dashSpeed : moveSpeed;
        float a = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, a * Time.deltaTime);

        // 초당 속도만 저장 (여기서 dt 곱하지 않음)
        _desiredVelocity = toTarget.normalized * currentSpeed;

        // Rigidbody를 쓰지 않는 경우에만 여기서 이동
        if (!(useRigidbodyMotion && rb != null))
        {
            transform.Translate(_desiredVelocity * Time.deltaTime, Space.World);
        }

        // 발소리
        PlayFootstepIfDue(false);
    }

    private void FixedUpdate()
    {
        if (useRigidbodyMotion && rb != null && isMoving && targetPoint != null && !arrivingLock)
        {
            rb.MovePosition(rb.position + _desiredVelocity * Time.fixedDeltaTime);
        }
    }

    void SyncAnimatorParams()
    {
        if (animator == null) return;

        bool grounded = true;
        float speedParam =
            (state == MoveState.Dashing) ? 1.0f :
            (state == MoveState.Walking) ? 0.5f : 0f;

        animator.SetBool(_hIsGrounded, grounded);
        animator.SetFloat(_hSpeed, speedParam);
        animator.SetBool(_hShift, state == MoveState.Dashing);
        animator.SetBool(_hJump, false); // 오토무브 구간에서는 점프 안씀

        // Dash일 때 Moving=false, Walk일 때만 Moving=true
        animator.SetBool("Dash", state == MoveState.Dashing);
        animator.SetBool("Moving", state == MoveState.Walking);
    }


    // 오버슈트 방지: 여기선 감속과 스냅/완료만 담당 (이동은 FixedUpdate가 처리)
    private void ArriveStep(Vector2 toTarget, float distance)
    {
        arrivingLock = true;

        // 감속
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);

        // 스냅 & 종료 조건
        if (currentSpeed <= 0.01f || distance <= arriveSnapDistance)
        {
            Vector3 snap = targetPoint.position;
            if (useRigidbodyMotion && rb != null) rb.MovePosition(snap);
            else transform.position = snap;

            isMoving = false;
            state = MoveState.Idle;
            arrivingLock = false;

            audioSource.Stop();
            SetAnim(false, false);
            OnArrived?.Invoke();
        }
        // 조건을 아직 못 채웠으면, 다음 프레임까지 arrivingLock을 유지하여 이동 계산을 멈춤
    }

    //// Transform 대신 RB2D.MovePosition 우선 사용(있으면)
    //private void MoveBy(Vector2 delta)
    //{
    //    if (useRigidbodyMotion && rb != null)
    //        rb.MovePosition(rb.position + delta);
    //    else
    //        transform.Translate(delta, Space.World);
    //}

    //private void Translate(float speed, Vector2 toTarget)
    //{
    //    Vector2 dir = toTarget.normalized;
    //    transform.Translate(dir * speed * Time.deltaTime, Space.World);
    //}

    private void SetAnim(bool moving, bool dashing)
    {
        if (animator == null) return;
        if (!moving && !dashing)
        {
            animator.SetBool("Crouch", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Climbing", false);
        }
    }

    private void PlayFootstepIfDue(bool force)
    {
        AudioClip clip;
        float interval;

        if (state == MoveState.Dashing)
        {
            clip = runSound != null ? runSound : walkSound;
            interval = runSound != null ? runSoundInterval : walkSoundInterval * 0.75f;
        }
        else if (state == MoveState.Walking)
        {
            clip = walkSound;
            interval = walkSoundInterval;
        }
        else
        {
            return;
        }

        if (clip == null) return;

        if (force || Time.time - lastWalkSoundTime >= interval)
        {
            audioSource.PlayOneShot(clip);
            lastWalkSoundTime = Time.time;
        }
    }

    private void PlayWalkSound()
    {
        if (walkSound != null)
        {
            // 현재 재생 중인 소리가 걷는 소리가 아니거나, 재생 중인 소리가 없으면 재생
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
            {
                audioSource.PlayOneShot(walkSound);
            }
        }
    }
}