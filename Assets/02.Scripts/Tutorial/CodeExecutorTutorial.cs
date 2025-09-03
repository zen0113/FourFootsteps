using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 액션 딕셔너리 방식
public class CodeExecutorTutorial : TutorialBase
{
    [SerializeField] private List<string> executedCodes = new List<string>();

    // 실행 가능한 코드들을 미리 등록
    private Dictionary<string, System.Action> codeActions;

    private void Awake()
    {
        InitializeCodeActions();
    }

    private void InitializeCodeActions()
    {
        codeActions = new Dictionary<string, System.Action>
        {
            ["StealthSFX.Instance.StopEnterSFX();"] = () => {
                if (StealthSFX.Instance != null)
                    StealthSFX.Instance.StopEnterSFX();
            },
            ["CatStealthController.Instance.Chase_CourchingDisabled();"] = () => {
                if (CatStealthController.Instance != null)
                    CatStealthController.Instance.Chase_CourchingDisabled();
            },
            // 필요한 코드들을 여기에 추가
            ["Debug.Log('Hello World');"] = () => {
                Debug.Log("Hello World");
            },
            ["GameObject.FindGameObjectWithTag('Player').SetActive(false);"] = () => {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.SetActive(false);
            }
        };
    }

    public override void Enter()
    {
        Debug.Log($"[CodeExecutorTutorial] {executedCodes.Count}개의 코드를 실행합니다.");

        foreach (string code in executedCodes)
        {
            ExecuteCode(code);
        }
    }

    private void ExecuteCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[CodeExecutorTutorial] 빈 코드입니다.");
            return;
        }

        if (codeActions.ContainsKey(code))
        {
            try
            {
                codeActions[code].Invoke();
                Debug.Log($"[CodeExecutorTutorial] 코드 실행 성공: {code}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CodeExecutorTutorial] 코드 실행 실패: {code}\nError: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[CodeExecutorTutorial] 등록되지 않은 코드: {code}");
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 필요시 구현
    }

    public override void Exit()
    {
        Debug.Log("[CodeExecutorTutorial] 코드 실행 튜토리얼 종료");
    }
}
