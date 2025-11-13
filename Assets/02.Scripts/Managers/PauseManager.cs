using System;
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
    public static event Action<bool> OnPauseToggled;

    // [중요] 씬이 로드될 때 퍼즈 상태가 아니도록 초기화
    private void Awake()
    {
        IsGamePaused = false;
        isPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // [수정됨] isPaused가 true일 때(퍼즈 중일 때)의 로직을 모두 삭제
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
}