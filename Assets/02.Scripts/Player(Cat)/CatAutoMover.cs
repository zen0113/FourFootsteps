using System.Collections;
using UnityEngine;
using System; // System 네임스페이스 추가

public class CatAutoMover : MonoBehaviour
{
    public Transform targetPoint;
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource; // AudioSource 추가
    private bool isMoving = false;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound; // 걷기 소리
    [SerializeField] private float walkSoundInterval = 0.3f; // 걷기 소리 재생 간격
    private float lastWalkSoundTime; // 마지막 걷기 소리 재생 시간

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
        animator?.SetBool("Moving", true); // 이동 시작 시 애니메이션 설정
        lastWalkSoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayWalkSound(); // 이동 시작 시 걷는 소리 바로 재생
    }

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
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;

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