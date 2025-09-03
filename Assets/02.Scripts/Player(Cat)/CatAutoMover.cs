using System.Collections;
using UnityEngine;
using System; // System 네임스페이스 추가

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

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource; // AudioSource 추가
    private bool isMoving = false;
    public bool IsMoving => isMoving;

    private float currentSpeed; // 가변 속도(보간 대상)

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
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
        SetAnim(false, false);
        isMoving = false;
        state = MoveState.Idle;
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
        PlayFootstepIfDue(true); // 이동 시작 시 걷는 소리 바로 재생
        //animator?.SetBool("Moving", true); // 이동 시작 시 애니메이션 설정
        //PlayWalkSound(); // 이동 시작 시 걷는 소리 바로 재생
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

        Vector2 toTarget = targetPoint.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
        {
            // 부드러운 감속
            if (Mathf.Abs(currentSpeed) > 0.01f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);
                Translate(currentSpeed, toTarget);
                return;
            }

            // 완전 정지
            isMoving = false;
            state = MoveState.Idle;
            audioSource.Stop();
            SetAnim(false, false);
            OnArrived?.Invoke();
            return;
        }

        // 방향 전환
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

        // 상태 결정 이후(Translate 전에) 파라미터 싱크
        SyncAnimatorParams();

        // 목표 속도 & 가감속
        float targetSpeed = (state == MoveState.Dashing) ? dashSpeed : moveSpeed;
        float a = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, a * Time.deltaTime);

        // 실제 이동
        Translate(currentSpeed, toTarget);

        // 발소리
        PlayFootstepIfDue(false);
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

        // 아래 두 줄은 “보조용” (그래프가 Dash/Moving 불리언을 보지 않는다면 없어도 됨)
        animator.SetBool("Dash", state == MoveState.Dashing);
        animator.SetBool("Moving", state != MoveState.Idle);
    }

    private void Translate(float speed, Vector2 toTarget)
    {
        Vector2 dir = toTarget.normalized;
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }

    private void SetAnim(bool moving, bool dashing)
    {
        if (animator == null) return;
        animator.SetBool("Moving", moving);
        animator.SetBool("Dash", dashing);
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