using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleButton : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 이 부분은 잠시 수정할 예정!!!
    public void LoadScene_Temporary()
    {
        SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
    }
}
