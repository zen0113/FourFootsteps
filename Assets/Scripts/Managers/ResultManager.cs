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

    public void ExecuteResult(string resultID)
    {
        string variableName;

        // ------------------------ 이곳에 모든 동작을 수동으로 추가 ------------------------
        switch (resultID)
        {
            case string when resultID.StartsWith("Result_StartDialogue"):  // 대사 시작
                variableName = resultID["Result_StartDialogue".Length..];
                DialogueManager.Instance.StartDialogue(variableName);
                break;

            // GameManager의 해당 변수를 조정 가능(+1 / -1)
            case string when resultID.StartsWith("Result_Increment"):  // 값++
                variableName = resultID["Result_Increment".Length..];
                GameManager.Instance.IncrementVariable(variableName);
                break;

            case string when resultID.StartsWith("Result_Decrement"):  // 값--
                variableName = resultID["Result_Decrement".Length..];
                GameManager.Instance.DecrementVariable(variableName);
                break;



            default:
                Debug.Log($"Result ID: {resultID} not found!");
                break;
        }


    }

}
