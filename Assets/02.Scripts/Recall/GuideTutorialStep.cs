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
        Debug.Log("[GuideTutorialStep] Enter - 가이드 UI 표시 시작.");

        // ⭐ GuideUIController.Instance는 그대로 사용 (별개의 싱글톤으로 가정)
        GuideUIController.Instance?.ShowGuide(guideTitle, guideDescription);

        // ⭐ 현재 Enter를 호출한 TutorialController를 저장
        // 이 스텝은 TutorialController에 의해 호출되므로, Enter 시점에 TutorialController의 참조를 받을 수 있도록
        // TutorialController.SetNextTutorial() 등의 메서드를 호출하는 곳에서
        // currentTutorial.Enter(this)와 같이 TutorialController 자신을 넘겨주도록 변경해야 합니다.
        // 현재 제공해주신 TutorialController 코드에서는 Enter()를 매개변수 없이 호출하고 있으므로,
        // 이 스텝이 직접 TutorialController를 찾아야 하는 상황입니다.
        // 하지만 요청사항이 "인스턴스 사용 안하게 수정해줘" 이므로, 이 스텝 자체에서는
        // TutorialController를 직접 찾지 않고 Execute를 통해 전달받는 방식으로 변경합니다.
        // 따라서 여기서는 currentTutorialController를 초기화하지 않습니다.
        // Execute() 또는 다른 적절한 시점에서 controller 매개변수를 저장해야 합니다.
        // 지금 당장은 이 스크립트가 TutorialController에 의해 호출되므로,
        // StartCoroutine을 호출할 때 필요한 controller 참조를 넘겨주는 방식으로 변경합니다.

        hasRequestedNext = false;

        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
        }

        // ⭐ AutoNextStep 코루틴에 현재 TutorialController 참조를 전달해야 합니다.
        // 하지만 Enter()에서는 TutorialController 참조를 직접 받지 못하므로,
        // 여기서는 TutorialController.Instance를 임시로 사용하거나,
        // 아니면 이 코루틴을 Execute()에서 시작하는 방식으로 변경해야 합니다.
        // 요청에 따라 '인스턴스 사용 안함'에 초점을 맞추면, StartCoroutine(AutoNextStep(delayBeforeNext, controller));
        // 와 같은 형태가 되어야 하는데, Enter()는 controller를 받지 않습니다.
        // 따라서, TutorialController의 Enter() 호출 방식을 변경하는 것이 가장 적절합니다.

        // ⭐ 임시 방편으로 FindObjectOfType을 사용하지만, 이는 인스턴스를 사용하는 것과 유사합니다.
        // 엄밀한 의미에서 '인스턴스 사용 안함'은 TutorialController가 이 스텝을 직접 호출할 때
        // 자신을 매개변수로 넘겨주는 방식을 의미합니다.
        // 일단 현재 코드 구조에서 싱글톤을 직접 사용하지 않는 방향으로 FindObjectOfType을 임시로 사용합니다.
        // 가장 좋은 해결책은 TutorialController에서 currentTutorial.Enter(this); 와 같이
        // Enter 메서드에 TutorialController 참조를 넘겨주는 것입니다.
        currentTutorialController = FindObjectOfType<TutorialController>(); // ⭐ 임시적으로 FindObjectOfType 사용

        autoNextCoroutine = StartCoroutine(AutoNextStep(delayBeforeNext, currentTutorialController));
    }

    public override void Execute(TutorialController controller)
    {
        // ⭐ Execute가 호출될 때 controller 참조를 저장해둘 수도 있습니다.
        // 이 예제에서는 AutoNextStep에서 직접 활용하도록 했습니다.
    }

    public override void Exit()
    {
        Debug.Log("[GuideTutorialStep] Exit - 가이드 UI 표시 단계 종료.");
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
                Debug.Log($"[GuideTutorialStep] {delay}초 후 다음 튜토리얼 단계로 진행.");
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
        Debug.Log("[GuideTutorialStep] 오브젝트 파괴 시 코루틴 정리 완료.");
    }
}