using System.Collections;
using UnityEngine;

public class HideGuideTutorialStep : TutorialBase
{
    [Header("UI 숨김 설정")]
    [SerializeField] private bool useFadeOut = true; // 페이드 아웃 사용 여부
    [SerializeField] private float autoNextDelay = 0.2f; // UI 숨김 후 다음 단계로 넘어가는 딜레이 (Inspector에서 설정 가능하도록)

    private bool hasRequestedNext = false;
    private Coroutine autoNextCoroutine;

    // ⭐ 현재 튜토리얼 컨트롤러 인스턴스를 저장할 필드 추가
    private TutorialController tutorialControllerRef;

    public override void Enter()
    {
        Debug.Log("[HideGuideTutorialStep] Enter - 가이드 UI 숨김 시작.");

        // UI 숨김 처리
        if (GuideUIController.Instance != null) // GuideUIController의 싱글톤은 유지
        {
            if (useFadeOut)
            {
                GuideUIController.Instance.FadeOutGuide();
            }
            else
            {
                GuideUIController.Instance.HideGuideInstant();
            }
        }
        else
        {
            Debug.LogWarning("[HideGuideTutorialStep] GuideUIController.Instance를 찾을 수 없습니다. UI 숨김을 건너뜁니다.");
        }

        // 기존 코루틴이 있다면 중지
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
        }

        // hasRequestedNext를 다시 false로 초기화
        hasRequestedNext = false;

        // ⭐ Enter 메서드에서 직접 TutorialController 인스턴스를 찾아 저장합니다.
        // 현재 TutorialBase의 Enter() 시그니처가 매개변수를 받지 않으므로,
        // 이 스텝이 직접 TutorialController를 찾아야 합니다.
        // 가장 이상적인 방법은 TutorialController가 currentTutorial.Enter(this); 로
        // 자신을 넘겨주는 것이지만, 현재 코드 구조에서는 이렇게 하는 것이 인스턴스 사용을 줄이는 방법입니다.
        tutorialControllerRef = FindObjectOfType<TutorialController>();

        // ⭐ AutoNextStep 코루틴에 저장된 TutorialController 참조를 전달
        autoNextCoroutine = StartCoroutine(AutoNextStep(autoNextDelay, tutorialControllerRef));
    }

    public override void Execute(TutorialController controller)
    {
        // UI 숨김 전용 단계이므로 별도 실행 로직 없음
        // ⭐ 여기서 controller를 tutorialControllerRef에 할당하는 것도 가능합니다.
        // if (tutorialControllerRef == null) tutorialControllerRef = controller;
    }

    public override void Exit()
    {
        Debug.Log("[HideGuideTutorialStep] Exit - 가이드 UI 숨김 단계 종료.");
        // 실행 중인 코루틴 정리
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
                Debug.Log($"[HideGuideTutorialStep] {delay}초 후 다음 튜토리얼 단계로 진행.");
                controllerToUse.SetNextTutorial();
            }
            else
            {
                Debug.LogWarning("[HideGuideTutorialStep] 유효한 TutorialController 참조를 찾을 수 없어 다음 튜토리얼 단계로 진행할 수 없습니다. (On Destroying or Scene Change issue)");
            }
        }
        autoNextCoroutine = null; // 코루틴이 완료되었으므로 참조 해제
    }

    // OnDestroy에서 코루틴 정리
    private void OnDestroy()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
        Debug.Log("[HideGuideTutorialStep] 오브젝트 파괴 시 코루틴 정리 완료.");
    }
}