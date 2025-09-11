using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllPlayerAnim : TutorialBase
{
    public enum PlayerControllerType
    {
        PlayerCatMovement,
        PlayerHumanMovement
    }
    public enum AnimationType
    {
        Idle,
        Crouch,
        CrouchIdle,
        Dash,
        Moving,
        Jump
    }

    [SerializeField]
    private PlayerControllerType playerControllerType = PlayerControllerType.PlayerCatMovement;
    private MonoBehaviour playerController;

    [Header("강제로 전환할 애니메이션 타입")]
    [SerializeField]
    private AnimationType updateAnimationType = AnimationType.Idle;
    private Animator animator;

    private void Awake()
    {
        // 자동으로 Player 찾기
        FindPlayerController();

        animator = playerController.gameObject.GetComponent<Animator>();
    }

    /// <summary>
    /// Player 태그를 가진 오브젝트에서 선택된 타입의 컴포넌트를 찾아서 할당
    /// </summary>
    private void FindPlayerController()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            switch (playerControllerType)
            {
                case PlayerControllerType.PlayerCatMovement:
                    playerController = playerObject.GetComponent<PlayerCatMovement>();
                    break;
                case PlayerControllerType.PlayerHumanMovement:
                    playerController = playerObject.GetComponent<PlayerHumanMovement>();
                    break;
            }

            if (playerController != null)
            {
                Debug.Log($"[{gameObject.name}] Player 자동 찾기 성공: {playerObject.name} ({playerControllerType})", gameObject);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Player 태그를 가진 오브젝트 '{playerObject.name}'에서 {playerControllerType} 컴포넌트를 찾을 수 없습니다!", gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다!", gameObject);
        }
    }

    /// <summary>
    /// 수동으로 Player를 다시 찾는 메서드 (외부에서 호출 가능)
    /// </summary>
    public void RefreshPlayerController()
    {
        FindPlayerController();
    }

    /// <summary>
    /// 플레이어 컨트롤러 타입을 변경하고 다시 찾기
    /// </summary>
    public void SetPlayerControllerType(PlayerControllerType newType)
    {
        playerControllerType = newType;
        FindPlayerController();
    }

    public override void Enter()
    {
        GameManager.Instance.SetVariable("CanMoving", false);
        UpdateAnimation(updateAnimationType);
    }

    private void UpdateAnimation(AnimationType animationType)
    {
        if (animator == null) return;

        // 공통적으로 사용하는 파라미터 초기화
        animator.SetBool("Moving", false);
        animator.SetBool("Dash", false);
        animator.SetBool("Crouch", false);
        animator.SetBool("Jump", false);

        // Cat 전용
        if (playerControllerType == PlayerControllerType.PlayerCatMovement)
        {
            playerController.GetComponent<PlayerCatMovement>().StopDashParticle();
            animator.SetBool("Crouching", false);
            animator.SetBool("Climbing", false);
        }

        switch (animationType)
        {
            case AnimationType.Idle:
                break;

            case AnimationType.Moving:
                animator.SetBool("Moving", true);
                break;

            case AnimationType.Dash:
                animator.SetBool("Dash", true);
                break;

            case AnimationType.Crouch:
                if (playerControllerType == PlayerControllerType.PlayerCatMovement)
                {
                    animator.SetBool("Crouch", true);
                    animator.SetBool("Crouching", false);
                }
                else if (playerControllerType == PlayerControllerType.PlayerHumanMovement)
                {
                    animator.SetBool("Crouch", true);
                }
                break;

            case AnimationType.CrouchIdle:
                if (playerControllerType == PlayerControllerType.PlayerCatMovement)
                {
                    animator.SetBool("Crouching", true);
                    animator.SetBool("Crouch", false);
                }
                break;
        }

        TutorialController controller = FindObjectOfType<TutorialController>();
        controller?.SetNextTutorial();
    }



    public override void Execute(TutorialController controller)
    {

    }

    public override void Exit()
    {
        GameManager.Instance.SetVariable("CanMoving", true);
    }

}
