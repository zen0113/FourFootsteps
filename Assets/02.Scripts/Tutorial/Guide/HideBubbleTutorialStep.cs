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

    private bool isDone = false;

    public override void Enter()
    {
        isDone = false;
        StartCoroutine(HideAndProceed());
    }

    private IEnumerator HideAndProceed()
    {
        // SpeechBubbleController의 인스턴스가 있는지 확인
        if (SpeechBubbleController.Instance != null)
        {
            if (useFadeOut)
            {
                SpeechBubbleController.Instance.FadeOutBubble();
                // --- 💡 [개선] 페이드아웃 시간만큼 추가로 기다려 자연스러운 전환을 만듭니다. ---
                yield return new WaitForSeconds(0.3f); // SpeechBubbleController의 fadeDuration 값
            }
            else
            {
                // --- 💡 [수정] HideBubbleInstant() 대신 새로 만든 HideBubble()을 사용합니다. ---
                SpeechBubbleController.Instance.HideBubble();
            }
        }

        // 지정된 딜레이 후 다음 단계로 진행하도록 플래그 설정
        yield return new WaitForSeconds(delayBeforeNext);
        isDone = true;
    }

    public override void Execute(TutorialController controller)
    {
        // isDone 플래그가 true가 되면 다음 튜토리얼로 넘어갑니다.
        if (isDone)
        {
            controller.SetNextTutorial();
        }
    }

    public override void Exit()
    {
        // 코루틴이 중복 실행되는 것을 방지하기 위해 모든 코루틴을 중지합니다.
        StopAllCoroutines();
    }
}