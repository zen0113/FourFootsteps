using UnityEngine;


public class ObjectActivationTutorial : TutorialBase
{
    [Header("활성화 오브젝트")]
    [SerializeField] private GameObject objectToActivate;

    [Header("효과음")]
    [SerializeField] private AudioSource activationSound;

    /// <summary>

    public override void Enter()
    {
        
    }


    public override void Execute(TutorialController controller)
    {
        // 지정된 게임 오브젝트를 활성화
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        // 지정된 효과음을 재생
        if (activationSound != null)
        {
            activationSound.Play();
        }

        // 즉시 다음 튜토리얼
        controller.SetNextTutorial();
    }


    public override void Exit()
    {
        
    }
}