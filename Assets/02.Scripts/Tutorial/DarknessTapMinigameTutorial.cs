using System.Collections;
using UnityEngine;

public class DarknessTapMinigameTutorial : TutorialBase
{
    [Header("미니게임 설정")]
    [SerializeField] private DarknessTapMinigame miniGame;

    [Header("시작 전 다이얼로그)")]
    [SerializeField] private string startDialogueID = ""; // 미니게임 시작 전 다이얼로그

    [Header("카메라 설정")]
    [SerializeField] private CameraShakeMinigame cameraShake; // CameraShakeMinigame 스크립트
    [SerializeField] private FollowCamera followCamera;     // FollowCamera 스크립트

    [Header("자동 진행 설정")]
    [SerializeField] private bool autoProgressOnComplete = true; // 완료 시 자동으로 다음 튜토리얼 진행

    private TutorialController tutorialController;
    private bool miniGameStarted = false;
    private bool miniGameCompleted = false;
    private bool hasProgressed = false;
    private GameObject playerUICanvas; // 자동으로 찾은 Player UI Canvas 저장

    public override void Enter()
    {
        if (PlayerCatMovement.Instance != null)
        {
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(true);
        }

        if (followCamera != null)
        {
            Debug.Log("[DarknessTap] FollowCamera 비활성화");
            followCamera.enabled = false; // 카메라 팔로우 기능 정지
        }

        if (cameraShake != null)
        {
            Debug.Log("[DarknessTap] CameraShake 활성화 및 시작");
            cameraShake.enabled = true; // 카메라 쉐이크 스크립트 활성화

            // 쉐이크 시작 (지속 시간 2초, 강도 0.15f - 값은 조절하세요)
            cameraShake.Shake(2.0f, 0.15f);
        }
        else
        {
            Debug.LogWarning("[DarknessTap] CameraShake가 할당되지 않았습니다!");
        }

        Debug.Log("[DarknessTapMinigameTutorial] 어둠 걷어내기 미니게임 튜토리얼 시작");

        // Player UI Canvas를 자동으로 찾아서 비활성화
        FindAndDisablePauseUI();

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

    /// <summary>
    /// Player UI Canvas를 자동으로 찾아서 비활성화합니다.
    /// </summary>
    private void FindAndDisablePauseUI()
    {
        // 이미 찾은 경우 재사용
        if (playerUICanvas == null)
        {
            // GameObject.Find는 활성화된 오브젝트만 찾으므로, 모든 Canvas를 검색
            Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.gameObject.name == "Player UI Canvas")
                {
                    playerUICanvas = canvas.gameObject;
                    break;
                }
            }

            // 찾지 못한 경우 일반 GameObject.Find 시도
            if (playerUICanvas == null)
            {
                playerUICanvas = GameObject.Find("Player UI Canvas");
            }
        }

        if (playerUICanvas != null)
        {
            CanvasGroup canvasGroup = playerUICanvas.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            Debug.Log("[DarknessTapMinigameTutorial] Player UI Canvas CanvasGroup alpha를 0으로 설정.");
        }
        else
        {
            Debug.LogWarning("[DarknessTapMinigameTutorial] Player UI Canvas를 찾을 수 없습니다!");
        }
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