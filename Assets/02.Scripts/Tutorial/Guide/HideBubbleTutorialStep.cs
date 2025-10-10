using UnityEngine;
using System.Collections;

/// <summary>
/// SpeechBubbleController를 사용하여 말풍선을 숨기는 튜토리얼 단계입니다.
/// </summary>
public class HideBubbleTutorialStep : TutorialBase
{
    [Header("UI 숨김 설정")]
    [SerializeField] private bool useFadeOut = true; // 페이드 아웃 효과 사용 여부
    [SerializeField] private float delayBeforeNext = 0.5f; // UI 숨김 후 다음 단계로 넘어가는 딜레이

    private Coroutine autoNextCoroutine;

    public override void Enter()
    {
        // SpeechBubbleController의 인스턴스가 있는지 확인
        if (SpeechBubbleController.Instance != null)
        {
            if (useFadeOut)
            {
                SpeechBubbleController.Instance.FadeOutBubble();
            }
            else
            {
                SpeechBubbleController.Instance.HideBubbleInstant();
            }
        }

        // 지정된 딜레이 후 다음 단계로 자동 진행
        if (autoNextCoroutine != null) StopCoroutine(autoNextCoroutine);
        TutorialController controller = FindObjectOfType<TutorialController>();
        autoNextCoroutine = StartCoroutine(AutoNextAfterDelay(delayBeforeNext, controller));
    }

    public override void Execute(TutorialController controller)
    {
        // 비워둠
    }

    public override void Exit()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }

    private IEnumerator AutoNextAfterDelay(float delay, TutorialController controller)
    {
        // 페이드 아웃 시간 등을 고려하여 딜레이
        yield return new WaitForSeconds(delay);

        controller?.SetNextTutorial();
        autoNextCoroutine = null;
    }
}