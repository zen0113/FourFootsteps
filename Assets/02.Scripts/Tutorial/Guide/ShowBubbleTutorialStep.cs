using UnityEngine;
using System.Collections;

/// <summary>
/// SpeechBubbleController를 사용하여 말풍선을 표시하는 튜토리얼 단계입니다.
/// </summary>
public class ShowBubbleTutorialStep : TutorialBase
{
    [Header("말풍선 내용")]
    [SerializeField] private Sprite faceSprite; // 인스펙터에서 얼굴 이미지 할당
    [TextArea(3, 5)]
    [SerializeField] private string message; // 표시할 메시지

    [Header("위치 설정")]
    [SerializeField] private bool useCustomPosition = false; // 특정 위치에 표시할지 여부
    [SerializeField] private Vector2 screenPosition; // useCustomPosition이 true일 때 사용될 스크린 좌표

    [Header("자동 진행 설정")]
    [SerializeField] private bool autoNextStep = false; // true면 지정된 시간 후 다음 단계로 자동 진행
    [SerializeField] private float displayDuration = 3f; // 말풍선이 표시될 시간 (autoNextStep이 true일 때)

    private Coroutine autoNextCoroutine;

    public override void Enter()
    {
        // SpeechBubbleController의 인스턴스가 있는지 확인
        if (SpeechBubbleController.Instance == null)
        {
            Debug.LogError("[ShowBubbleTutorialStep] SpeechBubbleController.Instance를 찾을 수 없습니다!");
            return;
        }

        // 설정에 따라 말풍선 표시
        if (useCustomPosition)
        {
            SpeechBubbleController.Instance.ShowBubble(message, faceSprite);
        }
        else
        {
            SpeechBubbleController.Instance.ShowBubble(message, faceSprite);
        }

        // 자동 진행이 활성화된 경우
        if (autoNextStep)
        {
            if (autoNextCoroutine != null) StopCoroutine(autoNextCoroutine);

            // 현재 튜토리얼 컨트롤러를 찾아 코루틴에 전달
            TutorialController controller = FindObjectOfType<TutorialController>();
            autoNextCoroutine = StartCoroutine(AutoNextAfterDelay(displayDuration, controller));
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 이 단계에서는 키 입력 등 프레임 단위 확인이 필요 없으므로 비워둡니다.
        // 만약 '다음' 버튼을 눌러야 진행되게 하려면 여기에 로직을 추가합니다.
    }

    public override void Exit()
    {
        // 튜토리얼 단계를 나갈 때 실행 중인 코루틴이 있다면 중지
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }

    private IEnumerator AutoNextAfterDelay(float delay, TutorialController controller)
    {
        yield return new WaitForSeconds(delay);

        // 딜레이 후 다음 튜토리얼 단계로 진행
        controller?.SetNextTutorial();
        autoNextCoroutine = null;
    }
}