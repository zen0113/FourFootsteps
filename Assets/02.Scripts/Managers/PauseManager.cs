using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject settingsUI;
    public GameObject guideUI;
    public GameObject exitWarningUI;

    private bool isPaused = false;
    public static bool IsGamePaused { get; private set; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                // ESC로 Resume 할 수 있는 상태인지 확인
                if (pauseMenuUI.activeSelf && !settingsUI.activeSelf && !guideUI.activeSelf && !exitWarningUI.activeSelf)
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;
        isPaused = false;
    }

    void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;
        isPaused = true;
    }

    public void ShowExitWarning()
    {
        exitWarningUI.SetActive(true);
    }

    public void ConfirmExitToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
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
}
