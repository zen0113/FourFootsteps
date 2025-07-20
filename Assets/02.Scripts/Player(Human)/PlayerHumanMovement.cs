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
    [SerializeField] private float movePower = 2f;
    [SerializeField] private float dashPower = 8f;
    private bool isDashing = false;

    [Header("웅크리기")]
    [SerializeField] private bool isCrouching = false;

    [Header("효과음")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkSoundInterval = 0.5f;
    [SerializeField] private float dashSoundInterval = 0.2f;
    private float lastWalkSoundTime;

    private void Start()
    {
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, true);
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
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            StopMovementAndAnimation();
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
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);

        isDashing = Input.GetKey(KeyCode.LeftShift) && !isCrouching && horizontalInput != 0;

        if (isDashing)
        {
            animator.SetBool("Dash", true);
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
        if (IsInputBlocked() || !(bool)GameManager.Instance.GetVariable("CanMoving"))
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Move();
    }

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

    bool IsInputBlocked()
    {
        return PauseManager.IsGamePaused || // ⬅ 추가된 부분
               DialogueManager.Instance.isDialogueActive ||
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
        if (Input.GetKeyDown(KeyCode.S))
        {
            isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            isCrouching = false;
        }
    }
}
