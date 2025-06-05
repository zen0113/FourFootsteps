using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using Random = Unity.Mathematics.Random;

public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }

    private TextAsset resultsCSV;

    // results: dictionary of "Results"s indexed by string "Result ID"
    public Dictionary<string, Result> results = new Dictionary<string, Result>();

    // 이벤트 오브젝트 참조
    private Dictionary<string, IResultExecutable> executableObjects = new Dictionary<string, IResultExecutable>();

    void Awake()
    {
        if (Instance == null)
        {
            resultsCSV = Resources.Load<TextAsset>("Datas/results");
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterExecutable(string objectName, IResultExecutable executable)
    {
        Debug.Log($"registered {objectName}");

        if (!executableObjects.ContainsKey(objectName))
            executableObjects[objectName] = executable;
    }

    public void InitializeExecutableObjects()
    {
        Debug.Log("############### unregistered all executable objects ###############");

        executableObjects = new Dictionary<string, IResultExecutable>();
    }

    public void ParseResults()
    {
        string[] lines = resultsCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');

            if ((string.IsNullOrWhiteSpace(lines[i])) || (fields[0] == "" && fields[1] == "")) continue;

            Result result = new Result(
                fields[0].Trim(),   // Result ID
                fields[1].Trim(),   // Result Description
                fields[2].Trim()    // Dialogue ID
                );

            results[result.ResultID] = result;
        }
    }

    public IEnumerator ExecuteResultCoroutine(string resultID)
    {
        string variableName;

        // ------------------------ 이곳에 모든 동작을 수동으로 추가 ------------------------
        switch (resultID)
        {
            case string when resultID.StartsWith("Result_StartDialogue"):  // 대사 시작
                variableName = resultID["Result_StartDialogue".Length..];
                DialogueManager.Instance.StartDialogue(variableName);

                // 비동기 대기 (대사 끝날 때까지)
                while (DialogueManager.Instance.isDialogueActive)
                    yield return null;
                break;

            // GameManager의 해당 변수를 조정 가능(+1 / -1)
            case string when resultID.StartsWith("Result_Increment"):  // 값++
                variableName = resultID["Result_Increment".Length..];
                GameManager.Instance.IncrementVariable(variableName);

                // 증가시킨게 책임감 점수면 ChangeResponsibilityGauge 호출
                if (variableName == "ResponsibilityScore")
                {
                    if (ResponsibilityManager.Instance)
                        ResponsibilityManager.Instance.ChangeResponsibilityGauge();
                }
                yield return null; // 바로 실행이지만 코루틴 일관성 유지
                break;

            case string when resultID.StartsWith("Result_Decrement"):  // 값--
                variableName = resultID["Result_Decrement".Length..];
                GameManager.Instance.DecrementVariable(variableName);

                yield return null; // 바로 실행이지만 코루틴 일관성 유지
                break;

            case string when resultID.StartsWith("Result_Inverse"):  // !값
                variableName = resultID["Result_Inverse".Length..];
                GameManager.Instance.InverseVariable(variableName);

                yield return null; // 바로 실행이지만 코루틴 일관성 유지
                break;

            case "Result_FadeOut":  // fade out
                float fadeOutTime = 3f;
                yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutTime);
                //StartCoroutine(UIManager.Instance.OnFade(null, 0, 1, fadeOutTime));
                break;

            case "Result_FadeIn":  // fade int
                float fadeInTime = 3f;
                yield return UIManager.Instance.OnFade(null, 1, 0, fadeInTime);
                //StartCoroutine(UIManager.Instance.OnFade(null, 1, 0, fadeInTime));
                break;

            case "Result_FastFadeOut":  // Fast fade out
                fadeOutTime = 1.5f;
                yield return UIManager.Instance.OnFade(null, 0, 1, fadeOutTime);
                //StartCoroutine(UIManager.Instance.OnFade(null, 0, 1, fadeOutTime));
                break;

            case "Result_FastFadeIn":  // Fast fade int
                fadeInTime = 1.5f;
                yield return UIManager.Instance.OnFade(null, 1, 0, fadeInTime);
                //StartCoroutine(UIManager.Instance.OnFade(null, 1, 0, fadeInTime));
                break;

            // 낡은 소파 조사 시, 회상1 씬으로 이동.
            case "Result_GoToRecall1":
                InitializeExecutableObjects();
                GameManager.Instance.SetVariable("CanInvesigatingRecallObject", false);
                SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
                yield return new WaitForSeconds(1f);
                break;

            // 웜홀 최초 등장
            case "Result_FirstWormholeActivation":
                executableObjects["WormholeActivation"].ExecuteAction();
                // 아래 코드는 임시!!! 나중에 RecallManager 제대로 만들면 수정될 것
                if (RecallManager.Instance != null)
                {
                    Debug.Log("Recall Manger 호출");
                    RecallManager.Instance.SetInteractKeyGroup(true);
                }
                yield return null;
                break;

            // 웜홀 사용 시, 다음 씬으로 이동
            case "Result_WormholeNextScene":
                //Debug.Log("웜홀 사용 ");
                SceneLoader.Instance.LoadScene(GameManager.Instance.GetNextSceneData().sceneName);
                yield return new WaitForSeconds(1f);
                break;


            default:
                Debug.Log($"Result ID: {resultID} not found!");
                yield return null;
                break;
        }
    }


    //public void ExecuteResult(string resultID)
    //{
    //    string variableName;

    //    // ------------------------ 이곳에 모든 동작을 수동으로 추가 ------------------------
    //    switch (resultID)
    //    {
    //        case string when resultID.StartsWith("Result_StartDialogue"):  // 대사 시작
    //            variableName = resultID["Result_StartDialogue".Length..];
    //            DialogueManager.Instance.StartDialogue(variableName);
    //            break;

    //        // GameManager의 해당 변수를 조정 가능(+1 / -1)
    //        case string when resultID.StartsWith("Result_Increment"):  // 값++
    //            variableName = resultID["Result_Increment".Length..];
    //            GameManager.Instance.IncrementVariable(variableName);

    //            // 증가시킨게 책임감 점수면 ChangeResponsibilityGauge 호출
    //            if (variableName== "ResponsibilityScore")
    //            {
    //                if (ResponsibilityManager.Instance)
    //                    ResponsibilityManager.Instance.ChangeResponsibilityGauge();
    //            }    
    //            break;

    //        case string when resultID.StartsWith("Result_Decrement"):  // 값--
    //            variableName = resultID["Result_Decrement".Length..];
    //            GameManager.Instance.DecrementVariable(variableName);
    //            break;

    //        case "Result_FadeOut":  // fade out
    //            float fadeOutTime = 3f;
    //            StartCoroutine(UIManager.Instance.OnFade(null, 0, 1, fadeOutTime));
    //            break;

    //        case "Result_FadeIn":  // fade int
    //            float fadeInTime = 3f;
    //            StartCoroutine(UIManager.Instance.OnFade(null, 1, 0, fadeInTime));
    //            break;

    //        // 낡은 소파 조사 시, 회상1 씬으로 이동.
    //        case "Result_GoToReminiscence1":
    //            SceneLoader.Instance.LoadScene("Reminiscence1");
    //            break;

    //        default:
    //            Debug.Log($"Result ID: {resultID} not found!");
    //            break;
    //    }
    //}

}
