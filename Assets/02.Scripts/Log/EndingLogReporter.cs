using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingLogReporter : MonoBehaviour
{
    [SerializeField] private EndingLogQueueManager queueManager;

    [Header("Save file name")]
    [SerializeField] private string saveFileName = "GameData.json";

    // “엔딩 보았을 때” 한 번만 보내기 위한 로컬 가드(선택)
    private const string SENT_ENDING_EVENT_PREFIX = "SENT_ENDING_";

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    /// <summary>
    /// 엔딩 진입 시 호출: endingSceneName은 현재 씬 또는 엔딩 타입 구분자
    /// </summary>
    public void ReportEnding(string endingSceneName = null)
    {
        if (queueManager == null)
        {
            Debug.LogWarning("[EndingLogReporter] queueManager is null");
            return;
        }

        string scene = string.IsNullOrEmpty(endingSceneName)
            ? SceneManager.GetActiveScene().name
            : endingSceneName;

        // 중복 전송 방지(같은 엔딩 씬에서 여러 번 호출될 가능성 대비)
        string sentKey = SENT_ENDING_EVENT_PREFIX + scene;
        if (PlayerPrefs.GetInt(sentKey, 0) == 1)
        {
            Debug.Log("[EndingLogReporter] already sent in this device for this ending scene.");
            return;
        }

        // 1) 저장 데이터에서 이름/고양이 이름을 가져오는 방법
        // 현재 제공된 JSON 구조상 Variables.PlayerName / Variables.YourCatName이 존재합니다.
        // 여기서는 “간단 문자열 탐색”으로 가져오고, 실패하면 빈값 처리.
        string raw = File.Exists(SavePath) ? File.ReadAllText(SavePath) : "";

        string playerName = JsonFieldExtractor.TryGetString(raw, "\"PlayerName\"");
        string catName = JsonFieldExtractor.TryGetString(raw, "\"YourCatName\"");

        // 2) MemoryPuzzleStates 정규화 JSON 추출
        string memoryJson = MemoryPuzzleStateExtractor.ExtractNormalizedJsonFromSave(SavePath);

        // 3) eventId 생성(중복 방지 핵심)
        // uuid + scene + utc ticks
        string playerKey = queueManager.GetOrCreatePlayerKey(playerName, catName);
        string eventId = $"{playerKey}_{scene}_{DateTime.UtcNow.Ticks}";

        var payload = new EndingLogPayload
        {
            apiKey = "", // 큐매니저가 주입
            eventId = eventId,
            playerKey = playerKey,
            playerName = playerName,
            catName = catName,
            endingType = scene,
            memoryPuzzleStatesJson = memoryJson
        };

        queueManager.EnqueueAndSend(payload);

        PlayerPrefs.SetInt(sentKey, 1);
        PlayerPrefs.Save();
    }
}

/// <summary>
/// JsonUtility 없이도 간단히 "키":"값" 문자열을 뽑는 최소 도구(정교하진 않지만 Save 형식이 안정적이면 충분)
/// </summary>
public static class JsonFieldExtractor
{
    public static string TryGetString(string json, string keyToken)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(keyToken)) return "";

        int idx = json.IndexOf(keyToken, StringComparison.Ordinal);
        if (idx < 0) return "";

        int colon = json.IndexOf(':', idx);
        if (colon < 0) return "";

        int i = colon + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

        if (i >= json.Length || json[i] != '"') return "";

        // 문자열 슬라이스
        bool escape = false;
        int start = i + 1;
        for (int j = start; j < json.Length; j++)
        {
            char c = json[j];
            if (escape)
            {
                escape = false;
                continue;
            }
            if (c == '\\')
            {
                escape = true;
                continue;
            }
            if (c == '"')
            {
                return json.Substring(start, j - start);
            }
        }
        return "";
    }
}
