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
}
