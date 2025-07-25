using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 프롤로그의 흐름과 상태를 관리
public class PrologueManager : MonoBehaviour
{
    public static PrologueManager Instance { get; private set; }

    private int currentStep;
    private bool isPrologueFinished;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 변수 시트로 통해 초기화
        currentStep = (int)GameManager.Instance.GetVariable("PrologueStep");
        isPrologueFinished = (bool)GameManager.Instance.GetVariable("isPrologueFinished");
    }

    void Start()
    {
        ProceedToNextStep();
    }

    public void ProceedToNextStep()
    {
        if (isPrologueFinished) return;

        switch (currentStep)
        {
            case 0:
                Debug.Log($"프롤로그 {currentStep}");
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 1:
                Debug.Log($"프롤로그 {currentStep}");
                StartCoroutine(ResultManager.Instance.ExecuteResultCoroutine("Result_DialogueFadeIn"));
                EventManager.Instance.CallEvent("EventPrologue");
                break;

            case 2:
                Debug.Log($"프롤로그 {currentStep}");

                break;

            case 3:
                break;

            case 4:
                break;

            case 5:
                break;

            case 6:
                break;

            case 7:
                break;
        }

        currentStep++;
        GameManager.Instance.SetVariable("PrologueStep", currentStep);
    }

    public void EndPrologue()
    {
        isPrologueFinished=true;
        GameManager.Instance.SetVariable("isPrologueFinished", isPrologueFinished);
    }
}
