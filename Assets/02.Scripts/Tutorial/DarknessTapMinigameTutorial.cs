using System.Collections;
using UnityEngine;

public class DarknessTapMinigameTutorial : TutorialBase
{
    [Header("미니게임 설정")]
    [SerializeField] private DarknessTapMinigame miniGame;

    [Header("시작 전 다이얼로그)")]
    [SerializeField] private string startDialogueID = ""; // 미니게임 시작 전 다이얼로그

    [Header("시작 시 UI 설정")]
    [SerializeField] private GameObject uiToHideOnStart; // 튜토리얼 시작 시 비활성화할 UI

    [Header("자동 진행 설정")]
    [SerializeField] private bool autoProgressOnComplete = true; // 완료 시 자동으로 다음 튜토리얼 진행

    private TutorialController tutorialController;
    private bool miniGameStarted = false;
    private bool miniGameCompleted = false;
    private bool hasProgressed = false;

    public override void Enter()
    {
        Debug.Log("[DarknessTapMinigameTutorial] 어둠 걷어내기 미니게임 튜토리얼 시작");

        // 튜토리얼 시작 시 지정된 UI가 있다면 비활성화합니다.
        if (uiToHideOnStart != null)
        {
            uiToHideOnStart.SetActive(false);
        }

        tutorialController = FindObjectOfType<TutorialController>();
        miniGameStarted = false;
        miniGameCompleted = false;
        hasProgressed = false;

        if (miniGame == null)
        {
            Debug.LogError("[DarknessTapMinigameTutorial] DarknessTapMinigame이 할당되지 않았습니다!");
            if (autoProgressOnComplete && tutorialController != null)
            {
                tutorialController.SetNextTutorial();
            }
            return;
        }

        miniGame.OnMinigameComplete += HandleMiniGameComplete;

        if (!string.IsNullOrEmpty(startDialogueID))
        {
            StartCoroutine(StartWithDialogue());
        }
        else
        {
            StartMiniGame();
        }
    }

    public override void Execute(TutorialController controller)
    {
        if (miniGameCompleted && autoProgressOnComplete && !hasProgressed)
        {
            Debug.Log("[DarknessTapMinigameTutorial] 미니게임 완료, 다음 튜토리얼로 진행");
            controller?.SetNextTutorial();
            hasProgressed = true;
        }
    }

    public override void Exit()
    {
        Debug.Log("[DarknessTapMinigameTutorial] 어둠 걷어내기 미니게임 튜토리얼 종료");

        if (miniGame != null)
        {
            miniGame.OnMinigameComplete -= HandleMiniGameComplete;
            miniGame.StopMinigame(); // 튜토리얼 종료 시 게임도 함께 정지
        }

        StopAllCoroutines();
    }

    private IEnumerator StartWithDialogue()
    {
        // 다이얼로그 시작 전에 미니게임 일시 정지
        if (miniGame != null)
        {
            miniGame.PauseMinigame();
        }

        // 다이얼로그 시작
        DialogueManager.Instance.StartDialogue(startDialogueID);

        // 다이얼로그가 끝날 때까지 대기
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        Debug.Log("[DarknessTapMinigameTutorial] 다이얼로그 종료, 미니게임 재개");

        // 다이얼로그 완료 후 미니게임 재개
        if (miniGame != null)
        {
            miniGame.ResumeMinigame();
        }
    }

    private void StartMiniGame()
    {
        Debug.Log("[DarknessTapMinigameTutorial] 어둠 걷어내기 미니게임 시작");

        if (miniGame != null)
        {
            miniGameStarted = true;
            miniGame.StartMinigame();
        }
    }

    private void HandleMiniGameComplete()
    {
        Debug.Log("[DarknessTapMinigameTutorial] 미니게임 완료 이벤트 수신");
        miniGameCompleted = true;

        if (!autoProgressOnComplete)
        {
            Debug.Log("[DarknessTapMinigameTutorial] 수동 진행 모드 - 외부에서 다음 단계를 호출해주세요.");
        }
    }

    public void ManualProgressToNext()
    {
        if (miniGameCompleted && tutorialController != null && !hasProgressed)
        {
            Debug.Log("[DarknessTapMinigameTutorial] 수동으로 다음 튜토리얼 진행");
            tutorialController.SetNextTutorial();
            hasProgressed = true;
        }
    }

    public void ForceEndMiniGame()
    {
        if (miniGame != null)
        {
            miniGame.StopMinigame();
            miniGameCompleted = true;
        }
    }

    public float GetMiniGameProgress()
    {
        return miniGame != null ? miniGame.GetProgress() : 0f;
    }

    public int GetCurrentStage()
    {
        return miniGame != null ? miniGame.GetCurrentStage() : -1;
    }

    public bool IsMiniGameActive()
    {
        return miniGame != null ? miniGame.IsGameActive() : false;
    }

    public bool IsMiniGameCompleted()
    {
        return miniGameCompleted;
    }

    public void ResetMiniGame()
    {
        if (miniGame != null)
        {
            miniGame.ResetProgress();
            miniGameStarted = false;
            miniGameCompleted = false;
            hasProgressed = false;
        }
    }
}