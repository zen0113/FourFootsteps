using System.Collections;
using UnityEngine;
using System; // System 네임스페이스 추가

public class BirdAutoMover : MonoBehaviour
{
    public Transform targetPoint;
    public float moveSpeed = 5f;
    public float stopDistance = 0.05f;
    public Action OnArrived; // 도착 콜백
    public Action OnMovementStarted; // 이동 시작 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource; // AudioSource 추가
    private bool isMoving = false;
    public bool isInfiniteFlying = false;
    public event Action OnMovementFinished;
    // 목표지점 도착했어도 계속 나는 애니메이션 재생(추격 게임 중간에 필요)

    // 도착 처리 플래그 & 마지막 이동 방향(1=오른쪽, -1=왼쪽)
    private bool hasArrivedOnce = false;
    private int lastMoveDirX = 1;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip flySound; // 날기 소리
    [SerializeField] private float flySoundInterval = 0.3f; // 날기 소리 재생 간격
    private float lastFlySoundTime; // 마지막 날기 소리 재생 시간

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

    public void StartMoving(Transform destination)
    {
        targetPoint = destination;
        isMoving = true;
        hasArrivedOnce = false;      // 새 이동 시작 시 초기화
        animator?.SetBool("Moving", true); // 날기 시작 시 애니메이션 설정
        lastFlySoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayFlySound(); // 이동 시작 시 날기 소리 바로 재생

        OnMovementStarted?.Invoke(); // 이동 시작 이벤트 호출
    }

    private void Update()
    {
        if (targetPoint == null) return;

        // 무한 비행 연출 모드
        if (hasArrivedOnce && isInfiniteFlying)
        {
            // 방향 고정(도착 직전의 마지막 방향)
            spriteRenderer.flipX = (lastMoveDirX < 0);
            // 계속 날기 애니메이션
            animator?.SetBool("Moving", true);
            // 계속 효과음 재생
            PlayFlyLoop();
            return; // 이동 없음
        }

        if (!isMoving)
        {
            // 완전 정지 상태: 애니/사운드 종료
            animator?.SetBool("Moving", false);
            if (audioSource.isPlaying && audioSource.clip == flySound) audioSource.Stop();
            return;
        }

        Vector2 direction = targetPoint.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            // 도착 이벤트 실행
            if (!hasArrivedOnce)
            {
                hasArrivedOnce = true;
                OnArrived?.Invoke();

                OnMovementFinished?.Invoke();
            }

            if (isInfiniteFlying)
            {
                // 이동은 멈추되 날기 연출은 유지
                isMoving = false; // 위치 이동 중단
                animator?.SetBool("Moving", true);

                spriteRenderer.flipX = (lastMoveDirX < 0);
                PlayFlyLoop();
            }
            else
            {
                // 완전 정지
                isMoving = false;
                animator?.SetBool("Moving", false);
                if (audioSource.isPlaying && audioSource.clip == flySound) audioSource.Stop();
            }
            return;
        }

        // 방향 반전
        if (direction.x > 0)
        {
            lastMoveDirX = 1; 
            spriteRenderer.flipX = false; // 오른쪽으로 이동 시 정상
        }
        else if (direction.x < 0)
        {
            lastMoveDirX = -1; 
            spriteRenderer.flipX = true;  // 왼쪽으로 이동 시 반전
        }

        Vector2 moveDir = direction.normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);

        animator?.SetBool("Moving", true); // 이동 중 애니메이션 설정

        // 날기 소리 재생
        if (Time.time - lastFlySoundTime >= flySoundInterval)
        {
            PlayFlySound();
            lastFlySoundTime = Time.time;
        }
    }

    private void PlayFlySound()
    {
        if (flySound != null)
        {
            if (!audioSource.isPlaying || audioSource.clip != flySound)
            {
                audioSource.PlayOneShot(flySound);
            }
        }
    }

    private void PlayFlyLoop()
    {
        if (flySound == null) return;
        if (Time.time - lastFlySoundTime >= flySoundInterval)
        {
            audioSource.PlayOneShot(flySound);
            lastFlySoundTime = Time.time;
        }
    }
}