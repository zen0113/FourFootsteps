using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHumanMovement : MonoBehaviour
{
    Animator animator;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;

    [Header("이동")]
    [SerializeField] private float movePower = 2f;
    [SerializeField] private float dashPower = 8f;
    private bool isDashing = false;

    [Header("웅크리기")]
    [SerializeField] private bool isCrouching = false;

    [Header("상태")]
    private bool isHoldingCat = false;
    private bool isHoldingCarrier = false;

    [Header("효과음")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkSoundInterval = 0.5f;
    [SerializeField] private float dashSoundInterval = 0.2f;
    private float lastWalkSoundTime;

    private bool isMiniGameInputBlocked = false;

    private void Start()
    {
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);
        bool puzzleBagState = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Ending_Happy";
        UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, puzzleBagState);

        // rb = GetComponent<Rigidbody2D>(); // Rigidbody2D 관련 코드 제거
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
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            StopMovementAndAnimation();
            return;
        }

        // 입력 처리
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != 0)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }

        // 상태 및 애니메이션 업데이트
        Crouch();
        isDashing = Input.GetKey(KeyCode.LeftShift) && !isCrouching && horizontalInput != 0 && !isHoldingCat && !isHoldingCarrier;
        UpdateAnimationState(horizontalInput);

        // 이동 처리
        Move(horizontalInput);

        // 오디오 처리
        UpdateAudioState(horizontalInput);
    }

    void UpdateAnimationState(float horizontalInput)
    {
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);

        if (isDashing)
        {
            animator.SetBool("Dash", true);
        }
        else if (isCrouching)
        {
            animator.SetBool("Crouch", true);
        }
        else if (horizontalInput != 0)
        {
            animator.SetBool("Moving", true);
        }
    }

    public void SetPlayerHoldingCat(bool isActive)
    {
        isHoldingCat = isActive;
        animator.SetBool("With_Cat", isActive);

        if (isActive)
        {
            // 캐리어 들기 상태 강제 해제
            isHoldingCarrier = false;
            animator.SetBool("Holding_Carrier", false);

            // 웅크리기 상태 강제 해제
            isCrouching = false;
            animator.SetBool("Crouch", false);
        }
    }

    public void SetPlayerHoldingCarrier(bool isActive)
    {
        isHoldingCarrier = isActive;
        animator.SetBool("Holding_Carrier", isActive);

        if (isActive)
        {
            // 고양이 들기 상태 강제 해제
            isHoldingCat = false;
            animator.SetBool("With_Cat", false);

            // 웅크리기 상태 강제 해제
            isCrouching = false;
            animator.SetBool("Crouch", false);
        }
    }

    void UpdateAudioState(float horizontalInput)
    {
        if (isDashing)
        {
            if (Time.time - lastWalkSoundTime >= dashSoundInterval)
            {
                audioSource.PlayOneShot(footstepSound);
                lastWalkSoundTime = Time.time;
            }
        }
        else if (isCrouching)
        {
            audioSource.Stop();
        }
        else if (horizontalInput != 0)
        {
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

    private void StopMovementAndAnimation()
    {
        // 물리 이동이 아니므로 속도를 0으로 설정할 필요 없음
        if (animator != null)
        {
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            animator.SetBool("Crouch", false);
        }
    }

    bool IsInputBlocked()
    {
        return PauseManager.IsGamePaused ||
               DialogueManager.Instance.isDialogueActive ||
               (GameManager.Instance != null && GameManager.Instance.IsSceneLoading)
               || isMiniGameInputBlocked;
    }

    void Move(float horizontalInput)
    {
        float currentPower = movePower;

        if (isCrouching)
        {
            currentPower = 0;
        }
        else if (isDashing)
        {
            currentPower = dashPower;
        }

        Vector3 moveDir = new Vector3(horizontalInput, 0, 0);
        transform.Translate(moveDir * currentPower * Time.deltaTime);
    }

    void Crouch()
    {
        if (Input.GetKeyDown(KeyCode.S) && !isHoldingCat && !isHoldingCarrier)
        {
            isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            isCrouching = false;
        }
    }

    public void SetCrouch(bool isActice)
    {
        isCrouching = isActice;
    }

    public void BlockMiniGameInput(bool isBlocked)
    {
        isMiniGameInputBlocked = isBlocked;
    }

    public void SetPlayerPosition(Transform transform)
    {
        this.transform.position = transform.position;
    }

}