using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Constants;
using Random = UnityEngine.Random;

public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }

    [Header("Stage 4 미니게임 참조")]
    public HeartbeatMinigame heartbeatMinigame;
    private Dictionary<string, float[]> catBeatTimings;
    private string currentMinigameCat;

    private TextAsset resultsCSV;

    // results: dictionary of "Results"s indexed by string "Result ID"
    public Dictionary<string, Result> results = new Dictionary<string, Result>();

    // 이벤트 오브젝트 참조
    private Dictionary<string, IResultExecutable> executableObjects = new Dictionary<string, IResultExecutable>();

    void Awake()
    {
        if (Instance == null)
        {
            resultsCSV = Resources.Load<TextAsset>("Datas/results");
            Instance = this;
            InitializeCatBeatTimings(); // 고양이별 비트 데이터 초기화
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 고양이별 미니게임 비트 데이터 초기화
    private void InitializeCatBeatTimings()
    {
        catBeatTimings = new Dictionary<string, float[]>()
        {
            { "Ttoli", new float[] { 1.0f, 1.4f, 1.9f, 2.2f, 2.8f, 3.1f } }, // 불안 (빠르고 불규칙)
            { "Leo", new float[] { 1.0f, 1.6f, 2.2f, 2.8f, 3.4f } },          // 분노 (강하고 일정)
            { "Bogsil", new float[] { 1.2f, 2.4f, 3.6f, 4.8f } },             // 그리움 (느리고 깊게)
            { "Miya", new float[] { 1.5f, 3.0f, 4.5f, 6.0f } }              // 절망 (매우 느리고 간헐적)
        };
    }


    public void RegisterExecutable(string objectName, IResultExecutable executable)
    {
        Debug.Log($"registered {objectName}");

        if (!executableObjects.ContainsKey(objectName))
            executableObjects[objectName] = executable;
    }

    public void InitializeExecutableObjects()
    {
        Debug.Log("############### unregistered all executable objects ###############");

        executableObjects = new Dictionary<string, IResultExecutable>();
    }

    public void ParseResults()
    {
        results = new Dictionary<string, Result>();

        string[] lines = resultsCSV.text.Split('\n');
        Debug.Log($"--- Parsing results.csv: {lines.Length - 1} 개의 데이터를 발견 ---");

        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');

            if ((string.IsNullOrWhiteSpace(lines[i])) || (fields[0] == "" && fields[1] == "")) continue;

            Result result = new Result(
                fields[0].Trim(),   // Result ID
                fields[1].Trim(),   // Result Description
                fields[2].Trim()    // Dialogue ID
            );


            if (results.ContainsKey(result.ResultID))
            {
                Debug.LogWarning($"중복된 Result ID 발견! Key: {result.ResultID}");
            }
            else
            {
                results[result.ResultID] = result;
            }
        }
        Debug.Log("--- 파싱 완료 ---");
    }

    public void Test()
    {
        GameManager.Instance.IncrementVariable("ResponsibilityScore",3);
        ResponsibilityManager.Instance.ChangeResponsibilityGauge();
    }

    public void ExecuteResult(string resultID)
    {
        StartCoroutine(ExecuteResultCoroutine(resultID));
    }

    public IEnumerator ExecuteResultCoroutine(string resultID)
    {
        string variableName;
        // Result ID별로 실행 로직 분기
        switch (resultID)
        {
            case string when resultID.StartsWith("Result_StartDialogue"):  // 대사 시작
                variableName = resultID["Result_StartDialogue".Length..];
                DialogueManager.Instance.StartDialogue(variableName);
                //Debug.Log($"다이얼로그 {variableName} 시작");
                // 비동기 대기 (대사 끝날 때까지)
                while (DialogueManager.Instance.isDialogueActive)
                    yield return null;
                break;

            // GameManager의 해당 변수를 조정 가능(+1 / -1)
            case string when resultID.StartsWith("Result_Increment"):  // 값++
                variableName = resultID["Result_Increment".Length..];
                GameManager.Instance.IncrementVariable(variableName);

                // 증가시킨게 책임감 점수면 ChangeResponsibilityGauge 호출
                if (variableName == "ResponsibilityScore")
                {
                    if (ResponsibilityManager.Instance)
                        ResponsibilityManager.Instance.ChangeResponsibilityGauge();
                }
                yield return null; // 바로 실행이지만 코루틴 일관성 유지
                break;

            case string when resultID.StartsWith("Result_Decrement"):  // 값--
                variableName = resultID["Result_Decrement".Length..];
                GameManager.Instance.DecrementVariable(variableName);
                yield return null;
                break;

            case string when resultID.StartsWith("Result_Inverse"):  // !값
                variableName = resultID["Result_Inverse".Length..];
                GameManager.Instance.InverseVariable(variableName);
                yield return null;
                break;

            // 회상씬 이동
            case string when resultID.StartsWith("Result_GoToRecall"):  // 회상씬 N 이동
                InitializeExecutableObjects();
                variableName = $"RecallScene{resultID["Result_GoToRecall".Length..]}";
                GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
                SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
                Debug.Log($"회상씬 {variableName} 으로 이동");
                yield return new WaitForSeconds(1f);
                break;

            // 책임지수 토스트 텍스트 효과
            // Result_RespScoreToastText1 -> 긍정적 토스트 효과
            // Result_RespScoreToastText0 -> 부정적 토스트 효과
            case string when resultID.StartsWith("Result_RespScoreToastText"):
                int score;
                if(int.TryParse(resultID["Result_RespScoreToastText".Length..],out score))
                {
                    var (msg, mood) = ToastTextSpawner.GetToastForDelta(score);
                    ToastTextSpawner.Instance.ShowToast(msg, Vector2.zero, mood);
                }
                else
                {
                    Debug.LogError("Result_RespScoreToastText 뒤에 0이나 1의 숫자 입력 필요!!");
                }
                yield return null;
                break;

            case "ResultCloseEyes": // 눈 깜빡이는 효과
                yield return UIManager.Instance.OnFade(null, 0, 1, 1, true, 1, 0);
                break;

            case "Result_FadeOut":  // fade out
                float fadeOutTime = 2f;
                yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutTime);
                break;

            case "Result_FadeIn":  // fade int
                float fadeInTime = 2f;
                yield return UIManager.Instance.OnFade(null, 1, 0, fadeInTime);
                break;

            case "Result_FastFadeOut":  // Fast fade out
                fadeOutTime = 1.5f;
                yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutTime);
                break;

            case "Result_FastFadeIn":  // Fast fade int
                fadeInTime = 1.5f;
                yield return UIManager.Instance.OnFade(null, 1, 0, fadeInTime);
                break;

            // 다이얼로그 캔버스까지 안 보이게 하는 Fade Out/In
            case "Result_DialogueFadeOut":  // fade out
                Debug.Log("Result_DialogueFadeOut");
                fadeOutTime = 2f;
                yield return UIManager.Instance.OnFade(UIManager.Instance.dialogueCoverPanel, 0, 1, fadeOutTime);
                break;

            case "Result_DialogueFadeIn":  // fade int
                Debug.Log("Result_DialogueFadeIn");
                fadeInTime = 2f;
                yield return UIManager.Instance.OnFade(UIManager.Instance.dialogueCoverPanel, 1, 0, fadeInTime);
                break;

            // 프롤로그 다음 스텝으로 넘김
            case "Result_NextPrologueStep":
                Debug.Log("Result_NextPrologueStep");
                StartCoroutine(PrologueManager.Instance.ProceedToNextStep());
                yield return null;
                break;

            // 퍼즐 획득 애니메이션 재생 후 회상씬으로 이동
            case "Result_GetMemoryPuzzle":
                Debug.Log("Execute [Result_GetMemoryPuzzle]");
                executableObjects["MemoryPuzzle"].ExecuteAction();
                // 비동기 대기(애니메이션 끝날 때까지)
                while ((bool)GameManager.Instance.GetVariable("isPuzzleMoving"))
                    yield return null;
                if (!(bool)GameManager.Instance.GetVariable("CanMoving"))
                    GameManager.Instance.SetVariable("CanMoving", true);
                break;

            // ##################################################################
            // #################### Stage 4 설득 로직 시작 ####################
            // ##################################################################

            case "Result_DecideTtoliPath":
                Debug.Log("Decide Ttoli Path");
                yield return StartCoroutine(DecideCatPath("Ttoli"));
                break;
            case "Result_DecideLeoPath":
                yield return StartCoroutine(DecideCatPath("Leo"));
                break;
            case "Result_DecideBogsilPath":
                yield return StartCoroutine(DecideCatPath("Bogsil"));
                break;
            case "Result_DecideMiyaPath":
                yield return StartCoroutine(DecideCatPath("Miya"));
                break;

            case "Result_SetTtoliInteracted":
                GameManager.Instance.SetVariable("Ttoli_Interacted", true);
                yield return StartCoroutine(CheckForAllInteracted());
                break;
            case "Result_SetLeoInteracted":
                GameManager.Instance.SetVariable("Leo_Interacted", true);
                yield return StartCoroutine(CheckForAllInteracted());
                break;
            case "Result_SetBogsilInteracted":
                GameManager.Instance.SetVariable("Bogsil_Interacted", true);
                yield return StartCoroutine(CheckForAllInteracted());
                break;
            case "Result_SetMiyaInteracted":
                GameManager.Instance.SetVariable("Miya_Interacted", true);
                yield return StartCoroutine(CheckForAllInteracted());
                break;

            case "Result_SetTtoliPersuaded":
                GameManager.Instance.SetVariable("Ttoli_Persuaded", true);
                break;
            case "Result_SetLeoPersuaded":
                GameManager.Instance.SetVariable("Leo_Persuaded", true);
                break;
            case "Result_SetBogsilPersuaded":
                GameManager.Instance.SetVariable("Bogsil_Persuaded", true);
                break;
            case "Result_SetMiyaPersuaded":
                GameManager.Instance.SetVariable("Miya_Persuaded", true);
                break;

            case string when resultID.StartsWith("Result_SetupAndStartMinigame"):
                currentMinigameCat = resultID.Split('_').Last(); // "Result_SetupAndStartMinigame_Ttoli" -> "Ttoli"
                if (catBeatTimings.ContainsKey(currentMinigameCat))
                {
                    heartbeatMinigame.SetupWaveform(currentMinigameCat);
                    heartbeatMinigame.OnMinigameEnd += HandleMinigameResult;
                    heartbeatMinigame.gameObject.SetActive(true);
                }
                break;

            //// 낡은 소파 조사 시, 회상1 씬으로 이동.
            //case "Result_GoToRecall1":
            //    InitializeExecutableObjects();
            //    GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
            //    SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
            //    yield return new WaitForSeconds(1f);
            //    break;

            //// 동물 병원 앞 퍼즐 조사 시, 회상2 씬으로 이동.
            //case "Result_GoToRecall2":
            //    InitializeExecutableObjects();
            //    GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
            //    SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
            //    yield return new WaitForSeconds(1f);
            //    break;

            //// 정자 밑 퍼즐 조사 시, 회상3 씬으로 이동.
            //case "Result_GoToRecall3":
            //    InitializeExecutableObjects();
            //    GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
            //    SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
            //    yield return new WaitForSeconds(1f);
            //    break;

            // 웜홀 최초 등장
            case "Result_FirstWormholeActivation":
                executableObjects["WormholeActivation"].ExecuteAction();
                // 아래 코드는 임시!!! 나중에 RecallManager 제대로 만들면 수정될 것
                if (RecallManager.Instance != null)
                {
                    Debug.Log("Recall Manger 호출");
                    RecallManager.Instance.SetInteractKeyGroup(true);
                }
                yield return null;
                break;

            // 웜홀 사용 시, 다음 씬으로 이동
            case "Result_WormholeNextScene":
                //Debug.Log("웜홀 사용 ");
                SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
                yield return new WaitForSeconds(1f);
                break;

            case "Result_SetNextTutorial":
                // 다이얼로그 출력 완료 시 다음 트리거로 이동
                TutorialController.Instance.SetNextTutorial();
                yield return null;
                break;

            case string when resultID.StartsWith("Result_JumpToTutorial"):
                string tutorialIndexStr = resultID["Result_JumpToTutorial".Length..];
                if (int.TryParse(tutorialIndexStr, out int tutorialIndex))
                {
                    Debug.Log($"Result_JumpToTutorial{tutorialIndex}");
                    TutorialController.Instance.JumpToTutorial(tutorialIndex);

                    // 여기에 튜토리얼 점프 시 호출할 RecallManager 로직 추가
                    if (RecallManager.Instance != null)
                    {
                        Debug.Log("JumpToTutorial에서 RecallManager 호출");
                        GameManager.Instance.SetVariable("CanInvesigatingRecallObject", true);
                        RecallManager.Instance.SetInteractKeyGroup(true);
                    }
                }
                yield return null;
                break;

            // 같은 씬 내에서 방 간 이동 (페이드 효과와 함께)
            case string when resultID.StartsWith("Result_MoveToRoom"):
                string roomName = resultID["Result_MoveToRoom".Length..];
                yield return StartCoroutine(MoveToRoomCoroutine(roomName));
                break;

            // 은신 게임: 들켰을 때 연출
            case "Result_StartChaseGameIntro":
                const float INITIAL_FADE_TIME = 1.5f;
                const float FINAL_FADE_TIME = 1f;

                // 플레이어 오브젝트 및 컴포넌트 참조 획득
                GameObject player = GameObject.FindWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("Player object not found!");
                    break;
                }

                PlayerCatMovement playerMovement = player.GetComponent<PlayerCatMovement>();
                CatAutoMover autoMover = player.GetComponent<CatAutoMover>();
                PlayerAutoRunner ChaseManager = player.GetComponent<PlayerAutoRunner>();

                if (playerMovement == null || autoMover == null)
                {
                    Debug.LogError("Required player components not found!");
                    break;
                }

                // 플레이어 상태 복구
                playerMovement.ForceCrouch = false;
                playerMovement.IsJumpingBlocked = false;

                // 플레이어 자동 이동 시작 (시야에서 벗어나게)
                autoMover.enabled = true;
                autoMover.StartMoving(autoMover.targetPoint);

                // 화면 페이드 아웃
                yield return UIManager.Instance.OnFade(null, 0, 1, INITIAL_FADE_TIME);

                // 추격 게임 위치로 플레이어 텔레포트
                if (!ChaseManager.TeleportPlayerToChaseStage(player))
                {
                    Debug.LogError("Failed to teleport player to chase stage!");
                    break;
                }

                // 페이드 아웃 대기
                yield return new WaitForSeconds(INITIAL_FADE_TIME);

                // 카메라 설정
                if (!ChaseManager.SetupChaseCamera())
                {
                    Debug.LogError("Failed to setup chase camera!");
                    break;
                }
                // 화면 페이드 인
                yield return UIManager.Instance.OnFade(null, 1, 0, FINAL_FADE_TIME);
                // 튜토리얼 진행
                TutorialController.Instance?.SetNextTutorial();
                break;

            case "Result_MiniGameFailed":
                // 페이드 인 효과와 함께 게임오버 씬 로드
                SceneLoader.Instance.LoadScene("GameOver");
                yield return null;
                break;


            default:
                Debug.LogError($"Result ID: '{resultID}' not found! (Length: {resultID.Length})");
                yield return null;
                break;
        }
    }

    /// <summary>
    /// 고양이와의 상호작용 분기를 처리하는 핵심 함수
    /// </summary>
    private IEnumerator DecideCatPath(string catName)
    {
        var gm = GameManager.Instance;
        Debug.Log($"Deciding path for {catName}");

        // GameManager에서 현재 상태 변수들을 가져옴
        int responsibilityScore = (int)gm.GetVariable("ResponsibilityScore");
        bool interacted = (bool)gm.GetVariable($"{catName}_Interacted");
        bool persuaded = (bool)gm.GetVariable($"{catName}_Persuaded");
        bool allCatsInteracted = (gm.GetVariable("AllCatsInteracted") is bool val) ? val : false;

        string dialogueToStart = "";

        if (persuaded)
        {
            // 이미 설득된 고양이에게 다시 말을 걸었을 때의 공통 대사
            dialogueToStart = "Stage04_Generic_Persuaded";
        }
        else
        {
            // 아직 설득되지 않았다면
            if (responsibilityScore >= 3)
            {
                dialogueToStart = $"Stage04_{catName}_High_001";
            }
            else
            {
                if (allCatsInteracted)
                {
                    // 모든 고양이와 상호작용 완료 -> 미니게임 도전
                    dialogueToStart = $"Stage04_{catName}_Low_Choice_001";
                }
                else if (!interacted)
                {
                    // 첫 상호작용
                    dialogueToStart = $"Stage04_{catName}_Low_Initial_001";
                }
                else
                {
                    // 상호작용은 했지만, 아직 모두와 하지는 않음
                    dialogueToStart = "Stage04_Generic_Wait";
                }
            }
        }

        if (!string.IsNullOrEmpty(dialogueToStart))
        {
            DialogueManager.Instance.StartDialogue(dialogueToStart);
            while (DialogueManager.Instance.isDialogueActive)
                yield return null;
        }
    }

    /// <summary>
    /// 모든 고양이와 상호작용했는지 확인하고, 조건 충족 시 독백 출력
    /// </summary>
    private IEnumerator CheckForAllInteracted()
    {
        var gm = GameManager.Instance;
        bool monologueShown = (bool)gm.GetVariable("Monologue_Shown");

        if (!monologueShown)
        {
            bool allInteracted = (bool)gm.GetVariable("Ttoli_Interacted") &&
                                 (bool)gm.GetVariable("Leo_Interacted") &&
                                 (bool)gm.GetVariable("Bogsil_Interacted") &&
                                 (bool)gm.GetVariable("Miya_Interacted");

            if (allInteracted)
            {
                gm.SetVariable("AllCatsInteracted", true);
                DialogueManager.Instance.StartDialogue("Stage04_Monologue_001");
                while (DialogueManager.Instance.isDialogueActive)
                    yield return null;
                gm.SetVariable("Monologue_Shown", true);
            }
        }
        yield return null;
    }


    /// <summary>
    /// 미니게임 종료 시 호출될 콜백 함수
    /// </summary>
    private void HandleMinigameResult(bool success)
    {
        heartbeatMinigame.OnMinigameEnd -= HandleMinigameResult;

        if (success)
        {
            DialogueManager.Instance.StartDialogue($"Stage04_{currentMinigameCat}_Low_Success_001");
        }
        else
        {
            DialogueManager.Instance.StartDialogue($"Stage04_{currentMinigameCat}_Low_Fail_001");
        }
    }


    private bool _isMovingRoom = false;

    /// <summary>
    /// 같은 씬 내에서 지정된 방으로 플레이어와 카메라를 이동시키는 코루틴
    /// </summary>
    /// <param name="roomName">이동할 방의 이름 (Transform 오브젝트 이름과 일치해야 함)</param>
    /// <param name="useFade">페이드 효과 사용 여부</param>
    private IEnumerator MoveToRoomCoroutine(string roomName, bool useFade = true)
    {
        if (_isMovingRoom)
        {
            Debug.LogWarning($"Already moving to a room! (Requested: {roomName})");
            yield break;
        }

        _isMovingRoom = true;

        Debug.Log($"Moving to room: {roomName}");

        GameObject playerSpawnPoint = GameObject.Find($"{roomName}_PlayerSpawn");
        GameObject cameraTargetPoint = GameObject.Find($"{roomName}_CameraTarget");

        if (playerSpawnPoint == null)
        {
            Debug.LogError($"Player spawn point not found: {roomName}_PlayerSpawn");
            _isMovingRoom = false;
            yield break;
        }

        if (cameraTargetPoint == null)
        {
            Debug.LogError($"Camera target point not found: {roomName}_CameraTarget");
            _isMovingRoom = false;
            yield break;
        }

        // 페이드 아웃
        if (useFade)
        {
            yield return UIManager.Instance.OnFade(null, 0, 1, 1f);
        }

        // 플레이어 위치, 회전 이동 (컨트롤러 enable/disable 제거)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnPoint.transform.position;
            player.transform.rotation = playerSpawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogError("Player object not found with tag 'Player'");
        }

        // 카메라 이동
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = cameraTargetPoint.transform.position;
            mainCamera.transform.rotation = cameraTargetPoint.transform.rotation;
        }
        else
        {
            Debug.LogError("Main camera not found");
        }

        // 짧은 대기
        yield return new WaitForSeconds(0.1f);

        // 페이드 인
        if (useFade)
        {
            yield return UIManager.Instance.OnFade(null, 1, 0, 1f);
        }

        Debug.Log($"Successfully moved to room: {roomName}");

        _isMovingRoom = false;
    }

}