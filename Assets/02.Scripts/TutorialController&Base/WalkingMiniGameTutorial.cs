using System.Collections;
using UnityEngine;

public class WalkingMiniGameTutorial : TutorialBase
{
    [Header("미니게임 설정")]
    [SerializeField] private StruggleMiniGame miniGame;

    [Header("시작 전 다이얼로그 (선택사항)")]
    [SerializeField] private string startDialogueID = ""; // 미니게임 시작 전 다이얼로그

    [Header("자동 진행 설정")]
    [SerializeField] private bool autoProgressOnComplete = true; // 완료 시 자동으로 다음 튜토리얼 진행


    private TutorialController tutorialController;
    private bool miniGameStarted = false;
    private bool miniGameCompleted = false;
    private bool hasProgressed = false;

    public override void Enter()
    {
        Debug.Log("[WalkingMiniGameTutorial] 걷기 미니게임 튜토리얼 시작");

        tutorialController = FindObjectOfType<TutorialController>();
        miniGameStarted = false;
        miniGameCompleted = false;
        hasProgressed = false;

        if (miniGame == null)
        {
            Debug.LogError("[WalkingMiniGameTutorial] StruggleMiniGame이 할당되지 않았습니다!");
            // 미니게임이 없으면 바로 다음으로 진행
            if (autoProgressOnComplete && tutorialController != null)
            {
                tutorialController.SetNextTutorial();
            }
            return;
        }

        // 미니게임 완료 이벤트 등록
        miniGame.OnMiniGameComplete += HandleMiniGameComplete;

        // 시작 다이얼로그가 있으면 재생 후 미니게임 시작
        if (!string.IsNullOrEmpty(startDialogueID))
        {
            StartCoroutine(StartWithDialogue());
        }
        else
        {
            // 바로 미니게임 시작
            StartMiniGame();
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 미니게임이 완료되었고 자동 진행이 활성화된 경우
        if (miniGameCompleted && autoProgressOnComplete && !hasProgressed) 
        {
            Debug.Log("[WalkingMiniGameTutorial] 미니게임 완료, 다음 튜토리얼로 진행");
            controller?.SetNextTutorial();
            hasProgressed = true; 
        }
    }

    public override void Exit()
    {
        // 이벤트 해제 ...
        if (miniGame != null)
        {
            miniGame.OnMiniGameComplete -= HandleMiniGameComplete;
        }
        StopAllCoroutines();
    }

    private IEnumerator StartWithDialogue()
    {
        // 다이얼로그 시작
        DialogueManager.Instance.StartDialogue(startDialogueID);

        // 다이얼로그가 시작될 때까지 대기
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive);

        // 다이얼로그가 끝날 때까지 대기
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        // 다이얼로그 완료 후 미니게임 시작
        StartMiniGame();
    }

    private void StartMiniGame()
    {
        Debug.Log("[WalkingMiniGameTutorial] 미니게임 시작");

        if (miniGame != null)
        {
            miniGameStarted = true;
            miniGame.StartMiniGame();
        }
    }

    private void HandleMiniGameComplete()
    {
        Debug.Log("[WalkingMiniGameTutorial] 미니게임 완료 이벤트 수신");
        miniGameCompleted = true;

        // 자동 진행이 비활성화된 경우 수동으로 다음 단계 진행 가능
        if (!autoProgressOnComplete)
        {
            Debug.Log("[WalkingMiniGameTutorial] 수동 진행 모드 - 외부에서 다음 단계를 호출해주세요.");
        }
    }

    // 외부에서 수동으로 다음 단계로 진행하고 싶을 때 호출
    public void ManualProgressToNext()
    {
        if (miniGameCompleted && tutorialController != null)
        {
            Debug.Log("[WalkingMiniGameTutorial] 수동으로 다음 튜토리얼 진행");
            tutorialController.SetNextTutorial();
        }
    }

    // 미니게임 강제 종료 (필요시 사용)
    public void ForceEndMiniGame()
    {
        if (miniGame != null)
        {
            miniGame.ForceEndMiniGame();
            miniGameCompleted = true;
        }
    }

    // 현재 진행 상황 확인
    public float GetMiniGameProgress()
    {
        return miniGame != null ? miniGame.GetProgress() : 0f;
    }

    // 미니게임 완료 여부 확인
    public bool IsMiniGameCompleted()
    {
        return miniGameCompleted;
    }
}