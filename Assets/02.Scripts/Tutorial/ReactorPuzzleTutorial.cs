using UnityEngine;
using System.Collections;

/// <summary>
/// [역할] 튜토리얼의 전체적인 흐름(시작, 성공, 실패)을 관리합니다.
/// </summary>
public class ReactorPuzzleTutorial : TutorialBase
{
    #region Variables
    [Header("말풍선 설정")]
    [SerializeField] private Sprite startPortrait;
    [TextArea(2, 4)][SerializeField] private string startMessage;
    [SerializeField] private float startMessageDuration = 2f;

    [Header("가이드 말풍선")]
    [SerializeField] private Sprite guidePortrait;
    [TextArea(2, 4)][SerializeField] private string guideMessage;
    [SerializeField] private float guideMessageDuration = 1.5f;

    [Header("정답/오답 말풍선")]
    [SerializeField] private Sprite correctAnswerPortrait;
    [TextArea(2, 4)][SerializeField] private string correctAnswerMessage;
    [SerializeField] private float correctAnswerDuration = 1f;
    [SerializeField] private Sprite wrongAnswerPortrait;
    [TextArea(2, 4)][SerializeField] private string wrongAnswerMessage;
    [SerializeField] private float wrongAnswerDuration = 1f;

    [Header("성공/실패 말풍선")]
    [SerializeField] private Sprite successPortrait;
    [TextArea(2, 4)][SerializeField] private string successMessage;
    [SerializeField] private float successMessageDuration = 2f;
    [SerializeField] private Sprite failurePortrait;
    [TextArea(2, 4)][SerializeField] private string failureMessage;
    [SerializeField] private float failureMessageDuration = 2f;

    [Header("미니게임 연결")]
    [SerializeField] private ReactorPuzzle reactorPuzzle;
    [SerializeField] private Collider2D interactionTrigger;
    [SerializeField] private ReactorPuzzleEvent puzzleEventTrigger;

    [Header("차량 효과 연결")]
    [SerializeField] private TruckShakeEffect[] truckShakes;
    [SerializeField] private BackgroundScroller[] backgroundScrollers;
    [SerializeField] private AdvancedParallaxScroller[] advancedScrollers;
    [SerializeField] private WheelRotation[] wheelRotations;

    [Header("실패 설정")]
    [SerializeField] private float delayBeforeRetry = 2f;

    private bool isCompleted = false;
    private PlayerCatMovement _playerMovement;
    private Coroutine currentSequence;
    #endregion

    private void OnEnable()
    {
        ReactorPuzzleEvent.OnPlayerInteracted += HandleInteraction;
        if (reactorPuzzle != null)
        {
            reactorPuzzle.OnPuzzleEnd += HandlePuzzleResult;
            reactorPuzzle.OnAnswerSelected += HandleAnswerFeedback;
            reactorPuzzle.OnStageCleared += HandleStageCleared;
        }
    }

    private void OnDisable()
    {
        ReactorPuzzleEvent.OnPlayerInteracted -= HandleInteraction;
        if (reactorPuzzle != null)
        {
            reactorPuzzle.OnPuzzleEnd -= HandlePuzzleResult;
            reactorPuzzle.OnAnswerSelected -= HandleAnswerFeedback;
            reactorPuzzle.OnStageCleared -= HandleStageCleared;
        }
    }

    public override void Enter()
    {
        isCompleted = false;

        // 이전 시퀀스 중지
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        // 콜라이더와 상호작용 상태 리셋
        if (puzzleEventTrigger != null)
        {
            puzzleEventTrigger.ResetInteraction();
        }

        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = true;
        }

        currentSequence = StartCoroutine(SetupSequence());
    }

    private IEnumerator SetupSequence()
    {
        ToggleVehicleEffects(false);
        reactorPuzzle.PrepareForTutorial();
        reactorPuzzle.ShowAndStartTimer();

        if (!string.IsNullOrEmpty(startMessage))
        {
            SpeechBubbleController.Instance.ShowBubble(startMessage, startPortrait);
            yield return new WaitForSeconds(startMessageDuration);
            SpeechBubbleController.Instance.FadeOutBubble();
        }
    }

    private void HandleInteraction(PlayerCatMovement playerMovement)
    {
        ReactorPuzzleEvent.OnPlayerInteracted -= HandleInteraction;
        _playerMovement = playerMovement;

        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        currentSequence = StartCoroutine(InteractionRoutine());
    }

    private IEnumerator InteractionRoutine()
    {
        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = false;
        }

        _playerMovement?.SetMiniGameInputBlocked(true);

        if (!string.IsNullOrEmpty(guideMessage))
        {
            SpeechBubbleController.Instance.ShowBubble(guideMessage, guidePortrait);
            yield return new WaitForSeconds(guideMessageDuration);
            SpeechBubbleController.Instance.FadeOutBubble();
            yield return new WaitForSeconds(0.2f);
        }

        reactorPuzzle.StartPatternPhase();
    }

    private void HandleStageCleared()
    {
        if (!string.IsNullOrEmpty(correctAnswerMessage))
        {
            SpeechBubbleController.Instance.ShowBubbleForDuration(
                correctAnswerMessage,
                correctAnswerPortrait,
                correctAnswerDuration
            );
        }
    }

    private void HandleAnswerFeedback(bool isCorrect)
    {
        if (!isCorrect && !string.IsNullOrEmpty(wrongAnswerMessage))
        {
            SpeechBubbleController.Instance.ShowBubbleForDuration(
                wrongAnswerMessage,
                wrongAnswerPortrait,
                wrongAnswerDuration
            );
        }
    }

    private void HandlePuzzleResult(bool success)
    {
        _playerMovement?.SetMiniGameInputBlocked(false);

        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = false;
        }

        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        if (success)
        {
            currentSequence = StartCoroutine(SuccessSequence());
        }
        else
        {
            currentSequence = StartCoroutine(FailureSequence());
        }
    }

    public override void Exit()
    {
        _playerMovement?.SetMiniGameInputBlocked(false);

        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            currentSequence = null;
        }

        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = false;
        }

        if (reactorPuzzle != null)
        {
            reactorPuzzle.gameObject.SetActive(false);
        }

        ToggleVehicleEffects(true);
    }

    private IEnumerator SuccessSequence()
    {
        Debug.Log("미니게임 성공! 다음 튜토리얼로 넘어갑니다.");

        if (!string.IsNullOrEmpty(successMessage))
        {
            SpeechBubbleController.Instance.ShowBubble(successMessage, successPortrait);
            yield return new WaitForSeconds(successMessageDuration);
            SpeechBubbleController.Instance.FadeOutBubble();
        }

        isCompleted = true;
    }

    private IEnumerator FailureSequence()
    {
        Debug.Log("미니게임 시간 초과 실패! 튜토리얼을 반복합니다.");

        if (!string.IsNullOrEmpty(failureMessage))
        {
            SpeechBubbleController.Instance.ShowBubble(failureMessage, failurePortrait);
            yield return new WaitForSeconds(failureMessageDuration);
            SpeechBubbleController.Instance.FadeOutBubble();
        }

        ToggleVehicleEffects(true);

        Debug.Log($"{delayBeforeRetry}초 후 튜토리얼을 다시 시작합니다.");
        yield return new WaitForSeconds(delayBeforeRetry);

        ReactorPuzzleEvent.OnPlayerInteracted += HandleInteraction;
        Enter();
    }

    public override void Execute(TutorialController controller)
    {
        if (isCompleted)
        {
            controller.SetNextTutorial();
        }
    }

    private void ToggleVehicleEffects(bool isEnabled)
    {
        if (truckShakes != null)
        {
            foreach (var shake in truckShakes)
                if (shake != null) shake.enabled = isEnabled;
        }

        if (backgroundScrollers != null)
        {
            foreach (var scroller in backgroundScrollers)
                if (scroller != null) scroller.enabled = isEnabled;
        }

        if (advancedScrollers != null)
        {
            foreach (var scroller in advancedScrollers)
                if (scroller != null) scroller.enabled = isEnabled;
        }

        if (wheelRotations != null)
        {
            foreach (var wheel in wheelRotations)
            {
                if (wheel != null)
                {
                    if (isEnabled) wheel.ResumeRotation();
                    else wheel.PauseRotation();
                }
            }
        }
    }
}