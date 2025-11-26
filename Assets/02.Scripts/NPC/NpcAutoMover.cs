using UnityEngine;
using System; // System 네임스페이스 추가

public class NpcAutoMover : MonoBehaviour
{
    public Transform targetPoint;

    [Header("이동")]
    public float moveSpeed = 3f;
    public float stopDistance = 0.05f;
    [SerializeField] private float switchToRunDistance = 1.5f;// 이 거리 미만부터는 걷기/뛰기 전환
    [SerializeField] private float accel = 40f;               // 가속도(속도 전환 부드럽게)
    [SerializeField] private float decel = 60f;               // 도착 직전 감속

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound; // 걷기 소리
    [SerializeField] private float walkSoundInterval = 0.3f; // 걷기 소리 재생 간격
    //[SerializeField] private AudioClip runSound;              // 달리기 소리
    //[SerializeField] private float runSoundInterval = 0.5f;
    private float lastWalkSoundTime; // 마지막 걷기 소리 재생 시간

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    [SerializeField] private AudioSource sfxSource; // 발소리용
    private bool isMoving = false;

    private float currentSpeed; // 가변 속도(보간 대상)
    private float NpcYValue; // NPC의 고정될 Y축 값

    private enum MoveState { Idle, Moving }
    private MoveState state = MoveState.Idle;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        sfxSource = GetComponent<AudioSource>(); // AudioSource 컴포넌트 가져오기
        if (sfxSource == null) // AudioSource가 없으면 추가
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // 시작할 때 Y축 값을 저장
        NpcYValue = transform.position.y;
    }

    private void OnDisable()
    {
        if (sfxSource != null) sfxSource.Stop();
        //SetAnim(false, false);
        isMoving = false;
        animator?.SetBool("Moving", false);
        state = MoveState.Idle;
    }

    /// <summary> 걷기/기본 이동 시작(상황에 따라 대쉬 전환 가능) </summary>
    public void StartMoving(Transform destination)
    {
        // 이동 시작 시에도 현재 Y축 값을 다시 저장 (혹시 모를 변경사항 반영)
        NpcYValue = transform.position.y;
        targetPoint = destination;
        isMoving = true;
        state = MoveState.Moving;
        currentSpeed = 0f; // 가속 시작점
        //SetAnim(moving: true, dashing: false);
        lastWalkSoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayFootstepIfDue(true); // 이동 시작 시 걷는 소리 바로 재생
        animator?.SetBool("Moving", true); // 이동 시작 시 애니메이션 설정
        //PlayWalkSound(); // 이동 시작 시 걷는 소리 바로 재생
    }

    /// <summary> 즉시 정지(연출 중단) </summary>
    public void Stop()
    {
        isMoving = false;
        state = MoveState.Idle;
        currentSpeed = 0f;
        sfxSource.Stop();
        animator?.SetBool("Moving", false);
        //SetAnim(false, false);
    }

    private void Update()
    {
        if (!isMoving || targetPoint == null)
        {
            //if (!isMoving) SetAnim(false, false);
            return;
        }

        // Y축은 고정하고 X축만 계산
        Vector2 currentPos = new Vector2(transform.position.x, NpcYValue);
        Vector2 targetPos = new Vector2(targetPoint.position.x, NpcYValue);
        Vector2 toTarget = targetPos - currentPos;

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
            sfxSource.Stop();
            //SetAnim(false, false);
            OnArrived?.Invoke();
            return;
        }

        // 방향 전환
        if (toTarget.x > 0) spriteRenderer.flipX = false;
        else if (toTarget.x < 0) spriteRenderer.flipX = true;

        state = MoveState.Moving;
        //SetAnim(moving: true, dashing: false);

        // 목표 속도 & 가감속
        float targetSpeed = moveSpeed;
        float a = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, a * Time.deltaTime);

        // 실제 이동
        Translate(currentSpeed, toTarget);

        // 발소리
        PlayFootstepIfDue(false);
    }

    private void Translate(float speed, Vector2 toTarget)
    {
        Vector2 dir = toTarget.normalized;
        // Y축은 항상 고정값으로 설정
        Vector3 movement = new Vector3(dir.x * speed * Time.deltaTime, 0f, 0f);
        transform.Translate(movement, Space.World);

        // 혹시 모를 Y축 변동을 방지하기 위해 강제로 Y축 위치 고정
        Vector3 currentPosition = transform.position;
        transform.position = new Vector3(currentPosition.x, NpcYValue, currentPosition.z);
    }

    private void SetAnim(bool moving, bool dashing)
    {
        if (animator == null) return;
        //animator.SetBool("Moving", moving);
        //animator.SetBool("Dash", dashing);
        //if (!moving && !dashing)
        //{
        //    animator.SetBool("Crouch", false);
        //    animator.SetBool("Crouching", false);
        //    animator.SetBool("Climbing", false);
        //}
    }

    private void PlayFootstepIfDue(bool force)
    {
        AudioClip clip;
        float interval;

        if (state == MoveState.Moving)
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
            sfxSource.PlayOneShot(clip);
            lastWalkSoundTime = Time.time;
        }
    }
}