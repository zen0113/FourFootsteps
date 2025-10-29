using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleButton : MonoBehaviour
{
    public void LoadNextScene()
    {
        SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
    }


    public void TestLoadScene(string testLoadSceneName)
    {
        SceneLoader.Instance.LoadScene(testLoadSceneName);
    }

    public void ShowCaseScoreSetting()
    {
        System.Random rand = new System.Random();

        GameManager.Instance.SetVariable("CurrentMemoryPuzzleCount", 2);
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;

        int score = 0;
        for (int i = 0; i < 2; i++)
        {
            puzzleStates[i] = false;
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