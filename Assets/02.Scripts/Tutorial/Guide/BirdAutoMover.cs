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
        animator?.SetBool("Moving", true); // 날기 시작 시 애니메이션 설정
        lastFlySoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayFlySound(); // 이동 시작 시 날기 소리 바로 재생

        OnMovementStarted?.Invoke(); // 이동 시작 이벤트 호출
    }

    private void Update()
    {
        if (!isMoving || targetPoint == null)
        {
            if (!isMoving)
            {
                animator?.SetBool("Moving", false); // 정지 시 애니메이션 끄기
                if (audioSource.isPlaying && audioSource.clip == flySound) // 날기 소리 재생 중이면 멈춤
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
            if (audioSource.isPlaying && audioSource.clip == flySound) // 날기 소리 재생 중이면 멈춤
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
            // 현재 재생 중인 소리가 날기 소리가 아니거나, 재생 중인 소리가 없으면 재생
            if (!audioSource.isPlaying || audioSource.clip != flySound)
            {
                audioSource.PlayOneShot(flySound);
            }
        }
    }
}