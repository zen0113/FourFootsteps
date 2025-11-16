using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public bool isUsabilityTest = false;

    [Header("Lobby UI Components")]
    [SerializeField] private GameObject LoadGameButton;


    private void Awake()
    {
        if (isUsabilityTest)
            LoadGameButton.SetActive(false);

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
        System.Random rand = new System.Random();

        GameManager.Instance.SetVariable("CurrentMemoryPuzzleCount", score);
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        for (int i = 0; i < score; i++)
        {
            puzzleStates[i] = true;
        }
        GameManager.Instance.SetVariable("ResponsibilityScore", score);
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