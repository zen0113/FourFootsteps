using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class LobbyManager : MonoBehaviour
{
    public bool isUsabilityTest = false;

    [Header("Lobby UI Components")]
    [SerializeField] private GameObject LoadGameButton;
    [SerializeField] private GameObject NewGamePanel;
    [SerializeField] private GameObject NoGameDataPanel;


    private void Awake()
    {
        if (isUsabilityTest)
            LoadGameButton.SetActive(false);

    }
    private void Start()
    {
        SaveManager.Instance.ApplySavedGameData();
    }

    public void StartNewGame()
    {
        if ( SaveManager.Instance.CheckGameData())  // 저장된 게임 데이터가 있는 경우
            NewGamePanel.SetActive(true);
        else
        {
            SaveManager.Instance.LoadInitGameData();    // 게임 데이터 초기화
            SceneLoader.Instance.LoadScene(Constants.SceneType.SET_PLAYERNAME.ToSceneName());
        }
    }

    public void YesNewGameButton()
    {
        SceneLoader.Instance.LoadScene(Constants.SceneType.SET_PLAYERNAME.ToSceneName());
    }

    public void LoadGame()
    {
        if (SaveManager.Instance.CheckGameData())
        {
            string savedSceneName = GameManager.Instance.GetVariable("SavedSceneName") as string;

            Constants.SceneType savedScene =
                string.IsNullOrEmpty(savedSceneName)
                    ? Constants.SceneType.TITLE   // 기본값
                    : savedSceneName.ToSceneType();

            if (savedScene == Constants.SceneType.TITLE)
                SceneLoader.Instance.LoadScene(Constants.SceneType.SET_PLAYERNAME.ToSceneName());
            else
            {
                SceneLoader.Instance.LoadScene(savedScene.ToSceneName());
                ResponsibilityManager.Instance.ChangeResponsibilityGauge();
            }
        }
        else
            NoGameDataPanel.SetActive(true);
    }

    public void LoadNextScene()
    {
        SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
    }


    public void TestLoadScene(string testLoadSceneName)
    {
        SceneLoader.Instance.LoadScene(testLoadSceneName);
    }

    public void ShowCaseScoreSetting(int score)
    {
        //System.Random rand = new System.Random();
        GameManager.Instance.SetVariable("CurrentMemoryPuzzleCount", 5);
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        foreach (int key in puzzleStates.Keys.ToList())
        {
            puzzleStates[key] = false;
        }

        for (int i = 0; i < score; i++)
        {
            puzzleStates[i] = true;
        }
        GameManager.Instance.SetVariable("ResponsibilityScore", score);
        GameManager.Instance.SetVariable("isPrologueFinished", true);
        SaveManager.Instance.SaveGameData();
        ResultManager.Instance.Test();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 경우, 에디터 재생을 중지합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 애플리케이션(.exe 등)일 경우, 프로그램을 종료합니다.
        Application.Quit();
#endif
    }

}