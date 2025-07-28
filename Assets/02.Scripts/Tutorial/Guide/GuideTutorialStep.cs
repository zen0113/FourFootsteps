using System.Collections;
using UnityEngine;

public class GuideTutorialStep : TutorialBase
{
    [Header("가이드 제목과 설명")]
    [SerializeField] private string guideTitle;
    [TextArea(3, 5)]
    [SerializeField] private string guideDescription;

    [SerializeField] private float delayBeforeNext = 0.8f; // Inspector에서 설정 가능

    private bool hasRequestedNext = false;
    private Coroutine autoNextCoroutine;

    // ⭐ Enter 메서드에서 현재 튜토리얼 컨트롤러를 저장할 필드 추가
    private TutorialController currentTutorialController;

    public override void Enter()
    {

        GuideUIController.Instance?.ShowGuide(guideTitle, guideDescription);


        hasRequestedNext = false;

        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
        }

        currentTutorialController = FindObjectOfType<TutorialController>(); // ⭐ 임시적으로 FindObjectOfType 사용

        autoNextCoroutine = StartCoroutine(AutoNextStep(delayBeforeNext, currentTutorialController));
    }

    public override void Execute(TutorialController controller)
    {

    }

    public override void Exit()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }

    // ⭐ AutoNextStep 코루틴이 TutorialController 참조를 매개변수로 받도록 수정
    private IEnumerator AutoNextStep(float delay, TutorialController controllerToUse)
    {
        yield return new WaitForSeconds(delay);

        if (!hasRequestedNext)
        {
            hasRequestedNext = true;
            if (controllerToUse != null) // ⭐ 전달받은 controller 참조를 사용
            {
                controllerToUse.SetNextTutorial();
            }
            else
            {
                Debug.LogWarning("[GuideTutorialStep] 유효한 TutorialController 참조를 찾을 수 없어 다음 튜토리얼 단계로 진행할 수 없습니다.");
            }
        }
        autoNextCoroutine = null;
    }

    private void OnDestroy()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }
}