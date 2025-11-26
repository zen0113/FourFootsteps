using System.Collections;
using UnityEngine;

public class WalkingMiniGameTutorial : TutorialBase
{
    [Header("미니게임 설정")]
    [SerializeField] private StruggleMiniGame miniGame;

    [Header("시작 전 다이얼로그")]
    [SerializeField] private string startDialogueID = ""; // 미니게임 시작 전 다이얼로그

    [Header("자동 진행 설정")]
    [SerializeField] private bool autoProgressOnComplete = true; // 완료 시 자동으로 다음 튜토리얼 진행

    [Header("카메라 설정")]
    [SerializeField] private CameraShakeMinigame cameraShake; // CameraShakeMinigame 스크립트
    [SerializeField] private FollowCamera followCamera;     // FollowCamera 스크립트

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

        Debug.Log("[WalkingMiniGameTutorial] 걷기 미니게임 튜토리얼 시작");

        tutorialController = FindObjectOfType<TutorialController>();
        miniGameStarted = false;
        miniGameCompleted = false;
        hasProgressed = false;

        // Player UI Canvas를 자동으로 찾아서 비활성화
        FindAndDisablePlayerUI();

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
        // 모든 미니게임이 끝났으므로 카메라 설정을 원래대로 복원합니다.
        Debug.Log("[WalkingMiniGameTutorial] 걷기 미니게임 종료. 카메라 설정 복원.");
        if (cameraShake != null)
        {
            Debug.Log("[WalkingMiniGameTutorial] CameraShake 중지 및 비활성화");
            cameraShake.StopShake(); // 진행 중인 쉐이크 즉시 중지
            cameraShake.enabled = false; // 쉐이크 스크립트 비활성화
        }

        if (followCamera != null)
        {
            Debug.Log("[WalkingMiniGameTutorial] FollowCamera 활성화");
            followCamera.enabled = true; // 카메라 팔로우 기능 다시 활성화
        }

        // 이벤트 해제 ...
        if (miniGame != null)
        {
            miniGame.OnMiniGameComplete -= HandleMiniGameComplete;
        }
        StopAllCoroutines();

        // Exit 시 Player UI Canvas 다시 활성화
        RestorePlayerUI();
    }

    /// <summary>
    /// Player UI Canvas를 자동으로 찾아서 비활성화합니다.
    /// </summary>
    private void FindAndDisablePlayerUI()
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
            Debug.Log("[WalkingMiniGameTutorial] Player UI Canvas를 찾아서 비활성화합니다.");
            playerUICanvas.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[WalkingMiniGameTutorial] Player UI Canvas를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 비활성화했던 Player UI Canvas를 다시 활성화합니다.
    /// </summary>
    private void RestorePlayerUI()
    {
        if (playerUICanvas != null)
        {
            Debug.Log("[WalkingMiniGameTutorial] Player UI Canvas를 다시 활성화합니다.");
            playerUICanvas.SetActive(true);
        }
    }

    private IEnumerator StartWithDialogue()
    {
        if (PlayerCatMovement.Instance != null)
        {
            PlayerCatMovement.Instance.SetMiniGameInputBlocked(true);
            PlayerCatMovement.Instance.IsJumpingBlocked = true; // 점프도 확실히 차단
        }
        // 다이얼로그 시작 전 3초 대기
        yield return new WaitForSeconds(3f);

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

    // 미니게임 강제 종료
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