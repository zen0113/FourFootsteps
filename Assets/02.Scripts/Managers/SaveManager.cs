using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // --- 게임 데이터 파일이름 설정 --- //
    private string SAVE_DATA_FILE_PATH = "/GameData.json";

    // 멀티스레드 동기화(게임 데이터 저장 동기화)를 위한 공용 락
    private static readonly object _saveLock = new object();

    private SaveData InitData { set; get; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SAVE_DATA_FILE_PATH = Application.persistentDataPath + SAVE_DATA_FILE_PATH;
    }

    // 게임 데이터를 초기화 시킬 값 저장 (게임 시작 직후 1회)
    public void SaveInitGameData()
    {
        if (InitData != null) return;

        InitData = new SaveData(GameManager.Instance.Variables);
    }

    // 엔딩 이후 게임 데이터 초기화
    public void LoadInitGameData()
    {
        // 게임 변수 초기화
        GameManager.Instance.ResetVariables();

        // 초기화용 변수로 저장 후 로드
        SaveGameData(new SaveData(GameManager.Instance.Variables));
        ApplySavedGameData();
    }

    // 게임을 새로 시작할 때, 기존 데이터가 있는지 확인하는 함수
    public bool CheckGameData()
    {
        if (!File.Exists(SAVE_DATA_FILE_PATH))  {
            Debug.LogWarning($"[SaveManager] 경로에 데이터 존재하지 않음!");
            return false; 
        }

        try
        {
            string json = File.ReadAllText(SAVE_DATA_FILE_PATH);
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

            if (saveData == null || saveData.Variables == null)
                return false;

            if (!saveData.Variables.TryGetValue("isPrologueFinished", out object value))
                return false;

            if (value is bool b)
                return b;

            // 타입이 꼬여 있으면 false 취급
            return false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] CheckGameData failed: {e.Message}");
            return false;
        }
    }
    public void check_Debug()
    {
        if (!File.Exists(SAVE_DATA_FILE_PATH))
        {
            Debug.LogWarning($"[SaveManager] 경로에 데이터 존재하지 않음!");
        }
        try
        {
            string json = File.ReadAllText(SAVE_DATA_FILE_PATH);
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

            if (saveData == null || saveData.Variables == null)
            {
                Debug.Log("[SaveManager] : saveData null!");
                return;
            }

            if (!saveData.Variables.TryGetValue("isPrologueFinished", out object value))
            {
                Debug.Log("[SaveManager] : isPrologueFinished 가져오기 실패");
                return;
            }

            if (value is bool b)
            {
                Debug.Log($"[SaveManager] : isPrologueFinished -> {b}");
                return;
            }

            // 타입이 꼬여 있으면 false 취급
            Debug.Log($"[SaveManager] : 타입이 꼬여 있음");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] CheckGameData failed: {e.Message}");
        }
    }

    // 불러오기
    public void ApplySavedGameData()
    {
        if (!File.Exists(SAVE_DATA_FILE_PATH)) return; // no save game data

        try
        {
            // 저장된 파일 읽어오고 Json을 클래스 형식으로 전환해서 할당
            string fromJsonData = File.ReadAllText(SAVE_DATA_FILE_PATH);
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(fromJsonData);

            if (saveData == null)
            {
                Debug.LogWarning("[SaveManager] ApplySavedGameData: saveData is null");
                return;
            }

            var loadedVars = saveData.Variables ?? new Dictionary<string, object>();

            // 🔧 레거시 세이브 복구:
            // 예전 세이브에서 MemoryPuzzleStates가 string 타입("System.Collections.Generic.Dictionary`2[...]")으로
            // 잘못 저장된 경우, 현재 GameManager의 초기값(dict<int,bool>)을 복사해 덮어쓴다.
            if (loadedVars.TryGetValue("MemoryPuzzleStates", out object memVal) && memVal is string)
            {
                if (GameManager.Instance.Variables != null &&
                    GameManager.Instance.Variables.TryGetValue("MemoryPuzzleStates", out object defaultVal) &&
                    defaultVal is Dictionary<int, bool> defaultDict)
                {
                    loadedVars["MemoryPuzzleStates"] = new Dictionary<int, bool>(defaultDict);
                    Debug.Log("[SaveManager] Fixed legacy MemoryPuzzleStates (string -> dict) using GameManager defaults.");
                }
                else
                {
                    loadedVars["MemoryPuzzleStates"] = new Dictionary<int, bool>();
                    Debug.Log("[SaveManager] Fixed legacy MemoryPuzzleStates to empty dict<int,bool>.");
                }
            }

            // 게임 쪽 변수에 반영
            GameManager.Instance.Variables = loadedVars;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] ApplySavedGameData failed: {e}");
        }
    }

    // 저장하기 (전체 저장)
    public void SaveGameData(SaveData newSaveData = null)
    {
        lock (_saveLock)
        {
            try
            {
                // 일반적인 저장의 경우, 현재 게임의 상태를 저장
                if (newSaveData == null)
                    newSaveData = new SaveData(GameManager.Instance.Variables);

                string json = JsonConvert.SerializeObject(newSaveData, Formatting.Indented);
                AtomicWrite(SAVE_DATA_FILE_PATH, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] SaveGameData failed: {e}");
            }
        }
    }

    // ⚠ 부분 저장 로직은 버그 유발 가능성이 커서,
    //    안전하게 전체 세이브로 대체 (성능 이슈 거의 없음)
    public void SaveVariable(string variableName)
    {
        // 필요하면 나중에 최적화 가능. 일단 전체 세이브로 안전하게 처리.
        SaveGameData();
    }

    private static void AtomicWrite(string path, string content)
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content);

        try
        {
            File.Replace(tmp, path, path + ".bak", true);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }

    // 저장된 게임 데이터 삭제 후 초기값으로 새로 생성
    public void CreateNewGameData()
    {
        if (File.Exists(SAVE_DATA_FILE_PATH)) File.Delete(SAVE_DATA_FILE_PATH);

        if (InitData == null)
        {
            // 혹시 InitData가 아직 안 만들어졌으면, 현재 GameManager의 변수를 기준으로 생성
            InitData = new SaveData(GameManager.Instance.Variables);
        }

        string json = JsonConvert.SerializeObject(InitData, Formatting.Indented);
        File.WriteAllText(SAVE_DATA_FILE_PATH, json);
        ApplySavedGameData();
    }
}

[System.Serializable]
public class SaveData
{
    // 게임 데이터와 그 타입
    public string variablesToJson;
    public string variablesTypeToJson;

    // 항상 이 프로퍼티를 통해 Dictionary<string, object>를 가져온다.
    public Dictionary<string, object> Variables => ToDictionary(variablesToJson, variablesTypeToJson);

    public SaveData(Dictionary<string, object> variables)
    {
        if (variables == null)
        {
            variablesToJson = "{}";
            variablesTypeToJson = "{}";
            return;
        }

        var stringVariables = new Dictionary<string, string>();
        var typeVariables = new Dictionary<string, string>();

        foreach (var kv in variables)
        {
            string key = kv.Key;
            object value = kv.Value;

            // Dictionary<int,bool> 타입
            if (value is Dictionary<int, bool> dict)
            {
                stringVariables[key] = JsonConvert.SerializeObject(dict);
                typeVariables[key] = "dict:int-bool";
            }
            else if (value is int i)
            {
                stringVariables[key] = i.ToString();
                typeVariables[key] = "int";
            }
            else if (value is bool b)
            {
                stringVariables[key] = b.ToString();
                typeVariables[key] = "bool";
            }
            else if (value == null)
            {
                stringVariables[key] = null;  // null 허용
                typeVariables[key] = "string";
            }
            else // string 포함 나머지
            {
                stringVariables[key] = value.ToString();
                typeVariables[key] = "string";
            }
        }

        variablesToJson = JsonConvert.SerializeObject(stringVariables, Formatting.Indented);
        variablesTypeToJson = JsonConvert.SerializeObject(typeVariables, Formatting.Indented);
    }

    // type 문자열을 통합 포맷으로 정규화 (레거시 "System.Int32" 등 지원)
    private string NormalizeType(string typeStr)
    {
        if (string.IsNullOrEmpty(typeStr))
            return "string";

        switch (typeStr)
        {
            case "int":
            case "System.Int32":
                return "int";

            case "bool":
            case "System.Boolean":
                return "bool";

            case "string":
            case "System.String":
                return "string";

            case "dict:int-bool":
                return "dict:int-bool";

            default:
                // 모르는 타입은 일단 string으로 취급
                return "string";
        }
    }

    private Dictionary<string, object> ToDictionary(string json, string typeJson)
    {
        var objectVariables = new Dictionary<string, object>();

        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(typeJson))
            return objectVariables;

        Dictionary<string, string> stringVariables;
        Dictionary<string, string> typeVariables;

        try
        {
            stringVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                              ?? new Dictionary<string, string>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveData] Failed to deserialize variablesToJson: {e}");
            return objectVariables;
        }

        try
        {
            typeVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(typeJson)
                            ?? new Dictionary<string, string>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveData] Failed to deserialize variablesTypeToJson: {e}");
            typeVariables = new Dictionary<string, string>();
        }

        foreach (var variable in stringVariables)
        {
            string key = variable.Key;
            string valueStr = variable.Value;
            typeVariables.TryGetValue(key, out string typeStrRaw);

            string typeStr = NormalizeType(typeStrRaw);
            object value;

            try
            {
                switch (typeStr)
                {
                    case "dict:int-bool":
                        // dict는 JSON 형태여야 하는데, 혹시 아니라면 빈 dict로 fallback
                        if (!string.IsNullOrEmpty(valueStr) && valueStr.TrimStart().StartsWith("{"))
                        {
                            var dict = JsonConvert.DeserializeObject<Dictionary<int, bool>>(valueStr);
                            value = dict ?? new Dictionary<int, bool>();
                        }
                        else
                        {
                            value = new Dictionary<int, bool>();
                        }
                        break;

                    case "int":
                        if (int.TryParse(valueStr, out int i))
                            value = i;
                        else
                            value = 0;
                        break;

                    case "bool":
                        if (bool.TryParse(valueStr, out bool b))
                            value = b;
                        else
                            value = false;
                        break;

                    case "string":
                    default:
                        value = valueStr ?? "";
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveData] Failed to parse key='{key}', type='{typeStr}', value='{valueStr}': {e.Message}");
                value = valueStr ?? "";
            }

            objectVariables[key] = value;
        }

        return objectVariables;
    }
}
