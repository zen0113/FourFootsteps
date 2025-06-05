using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class AnimalDataManager : MonoBehaviour
{
    private static AnimalDataManager _instance;
    public static AnimalDataManager Instance => _instance;
    
    [Header("API 설정")]
    [SerializeField] private string apiKey = "여기에_실제_API_키_입력";
    [SerializeField] private string baseUrl = "http://apis.data.go.kr/1543061/abandonmentPublicSrvc";
    
    private List<AnimalData> animalDataPool = new List<AnimalData>();
    private bool isDataLoaded = false;
    private bool isLoading = false;
    
    public bool IsDataLoadedFromApi { get; private set; } = false;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 시작할 때는 더미 데이터만 추가
        AddDummyData();
    }
    
    public void RequestApiDataIfNeeded()
    {
        if (IsDataLoadedFromApi || isLoading)
            return;
        
        StartCoroutine(LoadAnimalDataFromApi());
    }
    
    private IEnumerator LoadAnimalDataFromApi()
    {
        isLoading = true;
        Debug.Log("API 데이터 로드 시작...");
        
        // URL 인코딩된 API 키 사용 (중요!)
        string encodedApiKey = UnityWebRequest.EscapeURL(apiKey);
        
        // API 요청 URL 구성 (더 많은 파라미터 추가)
        string requestUrl = $"{baseUrl}/abandonmentPublic" +
                          $"?serviceKey={encodedApiKey}" +
                          $"&numOfRows=20" +
                          $"&pageNo=1" +
                          $"&_type=json" +
                          $"&state=protect" +  // 보호중인 동물만
                          $"&neuter_yn=Y";     // 중성화 완료된 동물만
        
        Debug.Log($"API 요청 URL: {requestUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            // 헤더 설정
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("User-Agent", "Unity-Game");
            
            // 타임아웃 설정
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"API 응답 성공. 길이: {responseText.Length}");
                Debug.Log($"응답 내용 일부: {responseText.Substring(0, Mathf.Min(500, responseText.Length))}");
                
                try
                {
                    // JSON 파싱 시도
                    AnimalApiResponse apiResponse = JsonUtility.FromJson<AnimalApiResponse>(responseText);
                    
                    if (apiResponse?.response?.body?.items?.item != null && apiResponse.response.body.items.item.Length > 0)
                    {
                        // 기존 더미 데이터 클리어
                        animalDataPool.Clear();
                        
                        // API 데이터 추가
                        foreach (var item in apiResponse.response.body.items.item)
                        {
                            animalDataPool.Add(item);
                        }
                        
                        IsDataLoadedFromApi = true;
                        Debug.Log($"API 데이터 {animalDataPool.Count}개 로드 성공!");
                    }
                    else
                    {
                        Debug.LogWarning("API 응답에 데이터가 없습니다. 더미 데이터 사용");
                        // 더미 데이터 유지
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON 파싱 오류: {e.Message}");
                    Debug.LogError($"파싱 시도한 JSON: {responseText}");
                    // 더미 데이터 유지
                }
            }
            else
            {
                Debug.LogError($"API 요청 실패: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                
                if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"오류 응답 내용: {request.downloadHandler.text}");
                }
            }
        }
        
        isLoading = false;
    }
    
    private void AddDummyData()
    {
        Debug.Log("더미 데이터 추가");
        animalDataPool.Clear();
        
        for (int i = 0; i < 8; i++)
        {
            animalDataPool.Add(CreateDummyData(i));
        }
        isDataLoaded = true;
    }
    
    public AnimalData GetRandomAnimalData()
    {
        if (!isDataLoaded || animalDataPool.Count == 0)
        {
            Debug.LogWarning("데이터가 로드되지 않았거나 없습니다. 더미 데이터 반환");
            return CreateDummyData(0);
        }
        
        int randomIndex = UnityEngine.Random.Range(0, animalDataPool.Count);
        return animalDataPool[randomIndex];
    }
    
    private AnimalData CreateDummyData(int index)
    {
        string[] sexCodes = { "M", "F", "Q" };
        string[] ages = { "2023(년생)", "2021(년생)", "2020(년생)", "2022(년생)", "추정 1세", "추정 2세", "2019(년생)", "추정 3세" };
        string[] states = { "보호중", "임시보호", "입양가능", "치료중", "보호중", "공고중", "보호중", "입양대기" };
        string[] breeds = { "[개] 믹스견", "[개] 말티즈", "[개] 리트리버", "[개] 비글", "[개] 푸들", "[개] 시츄", "[개] 요크셔테리어", "[개] 진돗개" };
        string[] colors = { "갈색", "흰색", "검정색", "황색", "회색", "갈색+흰색", "검정색+흰색", "갈색+검정색" };
        string[] shelters = { "서울동물복지센터", "경기도동물보호센터", "부산동물보호소", "대구동물보호센터", "인천동물보호센터", "광주동물보호센터", "대전동물보호센터", "울산동물보호센터" };
        string[] places = { "도로변", "공원", "아파트 단지", "상가 앞", "학교 근처", "병원 앞", "시장 주변", "주택가" };
        string[] neuterStatus = { "Y", "N", "U" };
        
        return new AnimalData
        {
            desertionNo = $"202500{1000 + index}",
            happenDt = "20250601",
            happenPlace = places[index % places.Length],
            kindCd = breeds[index % breeds.Length],
            colorCd = colors[index % colors.Length],
            age = ages[index % ages.Length],
            noticeNo = $"서울-2025-00{100 + index}",
            noticeSdt = "20250601",
            noticeEdt = "20250615",
            processState = states[index % states.Length],
            sexCd = sexCodes[index % sexCodes.Length],
            neuterYn = neuterStatus[index % neuterStatus.Length],
            specialMark = index % 3 == 0 ? "온순함, 사람을 잘 따름" : (index % 3 == 1 ? "활발함, 에너지가 많음" : "조용함, 겁이 많음"),
            careNm = shelters[index % shelters.Length],
            careTel = $"02-1234-567{index}"
        };
    }
    
    // 디버깅을 위한 메서드
    [ContextMenu("Test API Connection")]
    public void TestApiConnection()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(TestApiCall());
        }
    }
    
    private IEnumerator TestApiCall()
    {
        Debug.Log("API 연결 테스트 시작...");
        yield return StartCoroutine(LoadAnimalDataFromApi());
    }
}