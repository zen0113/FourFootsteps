using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // GameManager를 싱글턴으로 생성
    public static GameManager Instance { get; private set; }

    private TextAsset variablesCSV;

    // 이벤트의 실행 조건을 확인하기 위한 변수를 모두 이곳에서 관리.
    // 변수 타입은 int 또는 bool
    private Dictionary<string, object> variables = new Dictionary<string, object>();
    public Dictionary<string, object> Variables // 데이터 저장을 위해 작성
    {
        get => variables;
        set => variables = value;
    }

    public bool IsSceneLoading { get; private set; }

    // 씬 순서
    public List<SceneData> sceneOrder = new List<SceneData>();


    // 디버깅용
    [SerializeField] private TextMeshProUGUI variablesText;
    public bool isDebug = false;

    // --- 청소 카운터 관련 ---
    // variables.csv 파일에 CleanedObjectCount를 추가했기 때문에
    // 별도의 변수나 함수를 추가할 필요 없이, 기존 기능으로 모두 제어 가능합니다.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSceneOrderList();

            // 변수 초기화 로직을 Awake로 이동
            variablesCSV = Resources.Load<TextAsset>("Datas/variables");
            CreateVariables();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 시작 시 현재 씬 이름 자동 등록
        InitCurrentSceneInfo();

        if (isDebug)
            ShowVariables();
    }

    // 씬 순서 초기화
    private void InitSceneOrderList()
    {
        sceneOrder = new List<SceneData>
            {
                new SceneData("TitleScene"),
                new SceneData("SetPlayerName"),
                new SceneData("SetCatName"),
                new SceneData("Prologue"),
                new SceneData("StageScene1"),
                new SceneData("RecallScene1", true),
                new SceneData("StageScene2"),
                new SceneData("RecallScene2", true),
                new SceneData("StageScene3"),
                new SceneData("RecallScene3", true),
                new SceneData("StageScene3_2"),
                new SceneData("StageScene4"),
                new SceneData("RecallScene4", true),
                new SceneData("StageScene5"),
                new SceneData("RecallScene5", true),
                new SceneData("Ending_Happy"),
                new SceneData("Ending_Bad"),
            };
    }

    // 현재 씬 기준으로 CurrentSceneName, NextSceneName 자동 설정
    private void InitCurrentSceneInfo()
    {
        string loadedScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UpdateSceneProgress(loadedScene);
    }


    // 씬 상태 업데이트
    public void UpdateSceneProgress(string loadedSceneName)
    {
        var currentScene = sceneOrder.FirstOrDefault(s => s.sceneName == loadedSceneName);
        if (currentScene != null)
        {
            SetVariable("CurrentSceneName", currentScene.sceneName);

            int index = sceneOrder.IndexOf(currentScene);
            if (index + 1 < sceneOrder.Count)
                SetVariable("NextSceneName", sceneOrder[index + 1].sceneName);
            else
                SetVariable("NextSceneName", null); // 마지막 씬일 경우

            if (currentScene.isRecall)
            {
                SetVariable("isRecalling", true);
            }
            else
                SetVariable("isRecalling", false);
        }
    }

    public SceneData GetCurrentSceneData()
    {
        return sceneOrder.Find(s => s.sceneName == (string)GetVariable("CurrentSceneName"));
    }

    public SceneData GetNextSceneData()
    {
        return sceneOrder.Find(s => s.sceneName == (string)GetVariable("NextSceneName"));
    }


    // variablesCSV 파일 분리해서 variable 변수와 값을 variables에 키와 value로 저장
    private void CreateVariables()
    {
        string[] variableLines = variablesCSV.text.Split('\n');

        for (int i = 1; i < variableLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(variableLines[i])) continue;

            string[] fields = variableLines[i].Split(',');

            string variableName = fields[0].Trim();
            string variableValue = fields[1].Trim();
            string variableType = fields[2].Trim();

            switch (variableType)
            {
                case "int":
                    variables.Add(variableName, int.Parse(variableValue));
                    break;
                case "bool":
                    variables.Add(variableName, bool.Parse(variableValue));
                    break;
                case "string":
                    variables.Add(variableName, variableValue);
                    break;
                case "dict:int-bool":
                    int count = int.Parse(variableValue);
                    Dictionary<int, bool> dict = new Dictionary<int, bool>();
                    for (int j = 0; j < count; j++)
                        dict.Add(j, false); // 초기값 전부 false
                    variables.Add(variableName, dict);
                    break;
                default:
                    Debug.Log($"Unknown variable type : {variableType}");
                    break;
            }
        }
    }

    public void LoadTitleScene()
    {
        SceneLoader.Instance.LoadScene("TitleScene");
    }

    // Variable 값 설정
    public void SetVariable(string variableName, object value)
    {
        if (variables.ContainsKey(variableName))
        {
            variables[variableName] = value;
        }
        else
        {
            Debug.Log($"variable \"{variableName}\" dose not exist!");
        }
    }

    // Variable 값 가져옴
    public object GetVariable(string variableName)
    {
        if (variables.ContainsKey(variableName))
        {
            return variables[variableName];
        }
        else
        {
            Debug.Log($"variable \"{variableName}\" dose not exist!");
            return null;
        }
    }

    // Variable 값 증가
    public void IncrementVariable(string variableName)
    {
        int cnt = (int)GetVariable(variableName);
        cnt++;
        SetVariable(variableName, cnt);
    }

    public void IncrementVariable(string variableName, int count)
    {
        int cnt = (int)GetVariable(variableName);
        cnt += count;
        SetVariable(variableName, cnt);
    }

    // Variable 값 감소
    public void DecrementVariable(string variableName)
    {
        int cnt = (int)GetVariable(variableName);
        cnt--;
        SetVariable(variableName, cnt);
    }

    public void DecrementVariable(string variableName, int count)
    {
        int cnt = (int)GetVariable(variableName);
        cnt -= count;
        SetVariable(variableName, cnt);
    }

    // Variable 값 반전(bool타입)
    public void InverseVariable(string variableName)
    {
        bool variableValue = (bool)GetVariable(variableName);
        variableValue = !variableValue;
        SetVariable(variableName, variableValue);
    }


    public void StartSceneLoad()
    {
        IsSceneLoading = true;
    }

    public void FinishSceneLoad()
    {
        IsSceneLoading = false;
    }


    // 디버깅 용
    private void Update()
    {
        if (isDebug)
            ShowVariables();
    }

    private void ShowVariables()
    {
        variablesText.text = "";    // 텍스트 초기화

        // 화면에 표시하고 싶은 변수명 추가
        List<string> keysToShow = new List<string>(new string[]
        {
            "PlayerName",
            "YourCatName",
            "CurrentSceneName",
            "NextSceneName",
            "CanMoving",
            "CanInvesigatingRecallObject",
            "CanStartCleaningMinigame",
            "CurrentMemoryPuzzleCount",
            "MemoryPuzzleStates",
            "CleanedObjectCount"
        });

        foreach (var item in variables)
        {
            if (keysToShow.Contains(item.Key))
            {
                if (item.Key == "MemoryPuzzleStates")
                {
                    variablesText.text += $"{item.Key}\n";
                    foreach (var dict in item.Value as Dictionary<int, bool>)
                    {
                        variablesText.text += $"{dict.Key}: {dict.Value}\n";
                    }
                }
                else
                {
                    variablesText.text += $"{item.Key}: {item.Value}\n";
                }
            }
        }
    }
}