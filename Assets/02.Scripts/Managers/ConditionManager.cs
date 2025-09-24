using System.Collections.Generic;
using UnityEngine;

public class ConditionManager : MonoBehaviour
{
    public static ConditionManager Instance { get; private set; }

    private TextAsset conditionsCSV;

    // conditions: dictionary of "Condition"s indexed by string "Condition ID"
    public Dictionary<string, Condition> conditions = new Dictionary<string, Condition>();

    void Awake()
    {
        if (Instance == null)
        {
            conditionsCSV = Resources.Load<TextAsset>("Datas/conditions");
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ParseConditions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // conditions.csv 파일 파싱
    public void ParseConditions()
    {
        string[] lines = conditionsCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');

            if ((string.IsNullOrWhiteSpace(lines[i])) || (fields[0] == "" && fields[1] == "")) continue;

            Condition condition = new Condition(
                fields[0].Trim(), // Condition ID
                fields[1].Trim(), // Condition Description
                fields[2].Trim(), // Variable Name
                fields[3].Trim(), // Logic
                fields[4].Trim() // Value
                );

            conditions[condition.ConditionID] = condition;
        }
    }

    // Condition ID를 받아서 개별 조건의 true/false 판단
    public bool IsCondition(string conditionID)
    {
        if (!conditions.TryGetValue(conditionID, out Condition condition))
        {
            Debug.LogWarning($"ConditionManager: '{conditionID}'에 해당하는 조건을 찾을 수 없습니다!");
            return false;
        }

        string variableName = condition.VariableName;
        string logic = condition.Logic;
        string value = conditions[conditionID].Value;

        object variableValue = GameManager.Instance.GetVariable(variableName);

        if (variableValue is int intVal)
        {
            int targetValue = int.Parse(value);
            return logic switch
            {
                "==" => intVal == targetValue,
                "!=" => intVal != targetValue,
                "<" => intVal < targetValue,
                ">" => intVal > targetValue,
                "<=" => intVal <= targetValue,
                ">=" => intVal >= targetValue,
                _ => false,
            };
        }
        else if (variableValue is bool boolVal)
        {
            bool targetValue = value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            return logic switch
            {
                "==" => boolVal == targetValue,
                "!=" => boolVal != targetValue,
                _ => false,
            };
        }

        return false;
    }
}
