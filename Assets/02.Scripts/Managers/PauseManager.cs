using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using static Constants;


public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject settingsUI;
    public GameObject guideUI;
    public GameObject exitWarningUI;

    private bool isPaused = false;
    public static bool IsGamePaused { get; private set; }
    public static event Action<bool> OnPauseToggled;

    // -----------ShowCase-----------
    [SerializeField] private TMP_Dropdown scoreDropdown;
    [SerializeField] private GameObject scoreConfirmButton;
    private Dictionary<string, int> setMaxResponsibilityScores;
    private const string ENDING_SCENE_NAME = "Ending";
    [SerializeField] private bool isProhibittedScore = false;
    // ------------------------------

    // [중요] 씬이 로드될 때 퍼즈 상태가 아니도록 초기화
    private void Awake()
    {
        IsGamePaused = false;
        isPaused = false;
        Time.timeScale = 1f;

        InitializeShortcuts();
    }

    private void InitializeShortcuts()
    {
        // Stage 씬, Recall 씬에서 최대 설정 가능한 책임감 지수
        setMaxResponsibilityScores = new Dictionary<string, int>
        {
            { "StageScene1", 0 },
            { "StageScene2", 1},
            { "StageScene3", 2},
            { "StageScene3_2", 3},
            { "StageScene4_1", 3 },
            { "StageScene4_2", 4 },
            { "StageScene4_3", 4 },
            { "StageScene5", 4 },
            { "RecallScene1", 1 },
            { "RecallScene2", 2 },
            { "RecallScene3", 3 },
            { "RecallScene4", 4 },
            { "RecallScene5", 5 }
        };
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // isPaused가 true일 때(퍼즈 중일 때)의 로직을 모두 삭제
            if (!isPaused) // isPaused가 false일 때(퍼즈 중이 아닐 때)만
            {
                PauseGame(); // PauseGame()을 호출
            }
            // isPaused가 true이면 ESC를 눌러도 아무 일도 일어나지 않습니다.
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;
        isPaused = false;

        OnPauseToggled?.Invoke(false); // 이벤트 호출
    }

    void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;
        isPaused = true;

        OnPauseToggled?.Invoke(true); // 이벤트 호출

        // -----------ShowCase-----------
        // 시연용 책임감 지수 변경
        // 현재씬이 회상씬인 경우에는 CanInvesigatingRecallObject 이 true일 때만 
        // 시연용 책임감 지수 변경 사용 가능
        bool isToEnding = GameManager.Instance.GetVariable("CurrentSceneName").ToString().Contains(ENDING_SCENE_NAME);

        // 엔딩에선 해당 기능 사용 불가능
        if (isToEnding|| isProhibittedScore) {
            Showcase_SetActiveCurrentScoreOption(false);
            return;
        }

        if (!(bool)GameManager.Instance.GetVariable("isRecalling"))
            Showcase_ChangeScoreOption();
        else if((bool)GameManager.Instance.GetVariable("isRecalling")&&
            (bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject"))
        {
            Showcase_ChangeScoreOption();
        }
        else
        {
            Showcase_SetActiveCurrentScoreOption(false);
        }
        // ------------------------------
    }

    public void TogglePause()
    {
        IsGamePaused = !IsGamePaused;
        Time.timeScale = IsGamePaused ? 0f : 1f;
        OnPauseToggled?.Invoke(IsGamePaused);
    }

    public void ShowExitWarning()
    {
        exitWarningUI.SetActive(true);
    }

    public void ConfirmExitToMain()
    {
        // 다이얼로그 출력 중이면 취소함
        if (DialogueManager.Instance.isDialogueActive)
            DialogueManager.Instance.ForceAbortDialogue();

        // 씬을 떠나기 전에 인게임 UI(PlayerUICanvas 등)를 명시적으로 비활성화합니다.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, false);
            UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
            UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, false);
            UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, false);
            UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, false);
        }

        Time.timeScale = 1f;
        SceneLoader.Instance.LoadScene("TitleScene");
    }

    public void CancelExit()
    {
        exitWarningUI.SetActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
    }

    public void OpenGuide()
    {
        guideUI.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        settingsUI.SetActive(false);
        guideUI.SetActive(false);
    }

    // -----------ShowCase-----------
    // 시연 치트용 : 책임감 지수 토글 설정
    public void Showcase_ChangeScoreOption()
    {
        Showcase_SetActiveCurrentScoreOption(true);

        string currentSceneName = GameManager.Instance.GetVariable("CurrentSceneName").ToString();
        int maxScore = setMaxResponsibilityScores[currentSceneName];

        List<TMP_Dropdown.OptionData> optionList = new();
        for (int i = 1; i <= maxScore; i++)
            optionList.Add(new TMP_Dropdown.OptionData(i.ToString()));

        scoreDropdown.ClearOptions();
        scoreDropdown.AddOptions(optionList);

        Showcase_SetCurrentScoreOption();
    }

    private void Showcase_SetCurrentScoreOption()
    {
        int currentScore = (int)GameManager.Instance.GetVariable("ResponsibilityScore");
        scoreDropdown.value = (currentScore - 1 <= 0) ? 0 : currentScore - 1;
    }

    public void Showcase_ConfirmCurrentScore()
    {
        int fixedScore = scoreDropdown.value + 1;
        GameManager.Instance.SetVariable("ResponsibilityScore", fixedScore);
        ResultManager.Instance.Test();
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        foreach (int key in puzzleStates.Keys.ToList())
            puzzleStates[key] = false;

        for (int i = 0; i < fixedScore; i++)
            puzzleStates[i] = true;

        //SaveManager.Instance.SaveGameData();
    }

    private void Showcase_SetActiveCurrentScoreOption(bool active)
    {
        scoreDropdown.gameObject.SetActive(active);
        scoreConfirmButton.SetActive(active);
    }
}