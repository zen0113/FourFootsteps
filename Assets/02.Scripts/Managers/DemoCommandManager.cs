using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class DemoCommandManager : MonoBehaviour
{
    public static DemoCommandManager Instance { get; private set; }

    // 치트 키 매핑
    private Dictionary<KeyCode, SceneType> ctrlShiftShortcuts;
    private Dictionary<KeyCode, SceneType> shiftShortcuts;

    private Dictionary<KeyCode, int> fixPuzzleCounts;

    private enum PlayerVersion
    {
        CAT,
        HUMAN
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeShortcuts();
    }

    private void Start()
    {
        if (GameManager.Instance.isReleaseBuild)
            this.enabled = false;
    }

    private void InitializeShortcuts()
    {
        // Ctrl + Shift + 숫자 = Stage 씬들
        ctrlShiftShortcuts = new Dictionary<KeyCode, SceneType>
        {
            { KeyCode.Alpha1, SceneType.STAGE_1 },
            { KeyCode.Alpha2, SceneType.STAGE_2 },
            { KeyCode.Alpha3, SceneType.STAGE_3 },
            { KeyCode.Alpha4, SceneType.STAGE_4_1 },
            { KeyCode.Alpha5, SceneType.STAGE_5 }
        };

        // Shift + 숫자 = Recall 씬들
        shiftShortcuts = new Dictionary<KeyCode, SceneType>
        {
            { KeyCode.Alpha1, SceneType.RECALL_1 },
            { KeyCode.Alpha2, SceneType.RECALL_2 },
            { KeyCode.Alpha3, SceneType.RECALL_3 },
            { KeyCode.Alpha4, SceneType.RECALL_4 },
            { KeyCode.Alpha5, SceneType.RECALL_5 }
        };

        fixPuzzleCounts = new Dictionary<KeyCode, int>
        {
            { KeyCode.Alpha1, 1 },
            { KeyCode.Alpha2, 2 },
            { KeyCode.Alpha3, 3 },
            { KeyCode.Alpha4, 4 },
            { KeyCode.Alpha5, 5 }
        };
    }

    void Update()
    {
        if (GameManager.Instance.isReleaseBuild)
            return;

        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Ctrl + Shift + 숫자 → Stage
        if (ctrlPressed && shiftPressed)
        {
            FixCurrentPuzzleCounts(fixPuzzleCounts, true);
            StartCoroutine(CheckShortcuts(ctrlShiftShortcuts, PlayerVersion.CAT));
        }
        // Shift + 숫자 → Recall
        else if (shiftPressed)
        {
            FixCurrentPuzzleCounts(fixPuzzleCounts, false);
            StartCoroutine(CheckShortcuts(shiftShortcuts, PlayerVersion.HUMAN));
        }
    }

    private IEnumerator CheckShortcuts(Dictionary<KeyCode, SceneType> shortcuts, PlayerVersion version)
    {
        foreach (var shortcut in shortcuts)
        {
            if (Input.GetKeyDown(shortcut.Key))
            {
                GameManager.Instance.SetVariable("isPrologueFinished", true);
                SaveManager.Instance.SaveGameData();
                SceneLoader.Instance.LoadScene(shortcut.Value.ToSceneName());
                yield return new WaitWhile(() => SceneLoader.Instance.IsFading);
                SetActivePlayerUI(version);
                yield break;
            }
        }
    }

    private void FixCurrentPuzzleCounts(Dictionary<KeyCode, int> puzzleCounts, bool isStageScene)
    {
        foreach (var count in puzzleCounts)
        {
            if (Input.GetKeyDown(count.Key))
            {
                int puzzleCount = (isStageScene) ? (count.Value - 1) : count.Value;

                GameManager.Instance.SetVariable("CurrentMemoryPuzzleCount", puzzleCount);
                break;
            }
        }
    }

    private void SetActivePlayerUI(PlayerVersion version)
    {
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, true);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGauge, true);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, true);

        switch (version)
        {
            case PlayerVersion.CAT:
                // UI 상태 설정 (고양이 버전 UI 활성화, 사람 버전 UI 비활성화)
                UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
                UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, true);
                UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, true);
                break;

            case PlayerVersion.HUMAN:
                // UI 상태 설정 (사람 버전 UI 활성화, 고양이 버전 UI 비활성화)
                UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, true);
                UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, false);
                UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, false);
                break;
        }

        // 책임감 지수에 따라 진행바 채움
        ResultManager.Instance.Test();
    }
}
