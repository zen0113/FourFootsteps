using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHumanMovement : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;

    [Header("이동")]
    [SerializeField] private float movePower = 2f;       // 기본 이동 속도
    [SerializeField] private float dashPower = 8f;
    private bool isDashing = false;

    [Header("웅크리기")]
    [SerializeField] private bool isCrouching = false;

    [Header("효과음")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkSoundInterval = 0.5f;
    [SerializeField] private float dashSoundInterval = 0.2f;  // 대시할 때 더 빠른 간격
    private float lastWalkSoundTime;

    private void Start()
    {
        // 사람 버전 UI 그룹 활성화
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, true);
        // 고양이 버전 UI 그룹 비활성화
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (IsInputBlocked())
        {
            StopMovementAndAnimation(); // ← 다이얼로그 시 정지 처리 추가
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != 0)
            spriteRenderer.flipX = horizontalInput < 0;

        UpdateAnimationState(horizontalInput);
        Crouch();
    }

    void UpdateAnimationState(float horizontalInput)
    {
        // 기본 상태 초기화
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);

        // 대시 상태 체크
        isDashing = Input.GetKey(KeyCode.LeftShift) && !isCrouching && horizontalInput != 0;

        if (isDashing)
        {
            animator.SetBool("Dash", true);
            // 대시 효과음 재생 (더 짧은 간격으로)
            if (Time.time - lastWalkSoundTime >= dashSoundInterval)
            {
                audioSource.PlayOneShot(footstepSound);
                lastWalkSoundTime = Time.time;
            }
        }
        else if (isCrouching)
        {
            animator.SetBool("Crouch", true);
            audioSource.Stop();
        }
        else if (horizontalInput != 0)
        {
            animator.SetBool("Moving", true);
            // 걷기 효과음 재생
            if (Time.time - lastWalkSoundTime >= walkSoundInterval)
            {
                audioSource.PlayOneShot(footstepSound);
                lastWalkSoundTime = Time.time;
            }
        }
        else
        {
            audioSource.Stop();
        }
    }
    private void FixedUpdate()
    {
        if (IsInputBlocked())
        {
            rb.velocity = Vector2.zero;  // ← 강제 정지
            return;
        }

        Move();
    }

    // 이동 및 애니메이션 모두 멈추는 메서드
    private void StopMovementAndAnimation()
    {
        rb.velocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            animator.SetBool("Crouch", false);
        }
    }

    // 다이얼로그 출력 중, 씬 로딩 중이면 입력을 받지 않음.
    bool IsInputBlocked()
    {
        return DialogueManager.Instance.isDialogueActive ||
               (GameManager.Instance != null && GameManager.Instance.IsSceneLoading);
    }

    void Move()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentPower = movePower;

        if (isCrouching)
        {
            currentPower = 0;
        }
        else if (isDashing)
        {
            currentPower = dashPower;
        }

        float targetVelocityX = horizontalInput * currentPower;
        float smoothSpeed = 0.05f;
        float newVelocityX = Mathf.Lerp(rb.velocity.x, targetVelocityX, smoothSpeed / Time.deltaTime);
        rb.velocity = new Vector2(newVelocityX, rb.velocity.y);

    }

    void Crouch()
    {
        // S키 입력으로 인한 웅크리기
        if (Input.GetKeyDown(KeyCode.S))
        {
            // 웅크리기 시작
            isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            // 웅크리기 종료
            isCrouching = false;
        }
    }
}
