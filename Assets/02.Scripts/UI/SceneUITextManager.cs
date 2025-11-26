using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneUITextManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneTextData
    {
        public string sceneName;
        [TextArea(3, 5)]
        public string sceneDescription;

        public SceneTextData(string name, string place)
        {
            this.sceneName = name;
            this.sceneDescription = place;
        }
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sceneDescriptionText;
    
    [Header("Scene Text Data")]
    [SerializeField] private List<SceneTextData> sceneTextDataList;
    
    [Header("Default Text")]
    [SerializeField] private string defaultText = "현재 씬에 대한 설명이 없습니다.";

    private void Awake()
    {
        InitScenePlaceData();
    }


    private void Start()
    {
        UpdateSceneText();
    }

    private void InitScenePlaceData()
    {
        sceneTextDataList = new List<SceneTextData>
        {
            new SceneTextData("StageScene1","뒷골목"),
            new SceneTextData("RecallScene1","집 안"),
            new SceneTextData("StageScene2","공사장"),
            new SceneTextData("RecallScene2","동물병원"),
            new SceneTextData("StageScene3","공원"),
            new SceneTextData("RecallScene3","집 안"),
            new SceneTextData("StageScene3_2","공원"),
            new SceneTextData("StageScene4_1","트럭 내부"),
            new SceneTextData("RecallScene4","집 안"),
            new SceneTextData("StageScene4_2","트럭 내부"),
            new SceneTextData("StageScene4_3","트럭 내부"),
            new SceneTextData("StageScene5","도로"),
            new SceneTextData("RecallScene5","차도"),
            new SceneTextData("Ending_Bad","뒷골목"),
            new SceneTextData("Ending_Happy","집 안"),
        };
    }

    private void OnEnable()
    {
        // 씬이 변경될 때마다 텍스트 업데이트
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UpdateSceneText();
    }

    private void UpdateSceneText()
    {
        if (sceneDescriptionText == null)
        {
            Debug.LogWarning("Scene Description Text component is not assigned!");
            return;
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SceneTextData currentSceneData = sceneTextDataList.Find(data => data.sceneName == currentSceneName);

        if (currentSceneData != null)
        {
            sceneDescriptionText.text = currentSceneData.sceneDescription;
        }
        else
        {
            sceneDescriptionText.text = defaultText;
        }
    }

    // 에디터에서 씬 텍스트 데이터를 쉽게 관리할 수 있도록 도움말 메서드
    public void AddNewSceneText(string sceneName, string description)
    {
        SceneTextData newData = new SceneTextData(sceneName, description);
        sceneTextDataList.Add(newData);
    }

    // 해피 엔딩에선 장소가 2번 바뀌기 때문에 (집 안 -> 뒷골목 -> 유기동물 보호소 )
    // 장소가 바뀔 경우 튜토리얼 매니저에서 재설정
    public void ResetSceneText(string sceneName)
    {
        sceneDescriptionText.text = sceneName;
    }
} 