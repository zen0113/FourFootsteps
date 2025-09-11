using UnityEngine;

public class ObjectActivationTutorial : TutorialBase
{
    [Header("오브젝트 설정")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool activateObject = true; // true: 활성화, false: 비활성화

    [Header("효과음 설정")]
    [SerializeField] private AudioSource sound;
    [SerializeField] private bool playSound = true; // true: 재생, false: 재생 안함

    public override void Enter()
    {

    }

    public override void Execute(TutorialController controller)
    {
        // 오브젝트 활성화/비활성화
        if (targetObject != null)
        {
            targetObject.SetActive(activateObject);
        }

        // 효과음 재생
        if (playSound && sound != null)
        {
            sound.Play();
        }

        // 즉시 다음 튜토리얼
        controller.SetNextTutorial();
    }

    public override void Exit()
    {

    }
}