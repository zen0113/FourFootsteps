using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class AnimalDataManager : MonoBehaviour
{
    private static AnimalDataManager _instance;
    public static AnimalDataManager Instance => _instance;
    
    [SerializeField] private string apiKey;
    [SerializeField] private string apiUrl = "http://apis.data.go.kr/1543061/abandonmentPublicSrvc/abandonmentPublic";
    
    private List<AnimalData> animalDataPool = new List<AnimalData>();
    private bool isDataLoaded = false;
    private bool isLoading = false;
    
    // API 요청 성공 여부 확인용
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
    
    // 포스터 근처에서 호출
    public void RequestApiDataIfNeeded()
    {
        // 이미 API 데이터가 로드되었거나 로딩 중이면 무시
        if (IsDataLoadedFromApi || isLoading)
            return;
        
        // API 데이터 로드 시작
        try 
        {
            StartCoroutine(LoadAnimalDataFromApi());
        }
        catch (Exception e)
        {
            Debug.LogError("API 요청 시작 중 오류: " + e.Message);
            isLoading = false;
        }
    }
    
    private IEnumerator LoadAnimalDataFromApi()
    {
        isLoading = true;
        Debug.Log("API 데이터 로드 시작...");
        
        // API 요청 URL 구성
        string requestUrl = $"{apiUrl}?serviceKey={apiKey}&numOfRows=10&pageNo=1&_type=json";
        Debug.Log($"API 요청 URL: {requestUrl}");
        
        // try-catch 블록 외부에서 yield 사용
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        
        // 타임아웃 설정 (3초)
        request.timeout = 3;
        
        // 요청 시작
        var operation = request.SendWebRequest();
        
        // 진행 상황 모니터링 및 타임아웃 처리
        float timer = 0f;
        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            if (timer > 5f) // 5초 이상 걸리면 타임아웃으로 간주
            {
                Debug.LogWarning("API 요청 타임아웃");
                break;
            }
            yield return null; // 한 프레임 대기
        }
        
        try
        {
            // 결과 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("API 응답 받음: " + responseText.Substring(0, Mathf.Min(100, responseText.Length)));
                
                try
                {
                    // 더미 데이터로 대체 (API 파싱 문제 시)
                    // 실제 파싱은 임시로 비활성화
                    AddDummyData();
                    IsDataLoadedFromApi = true;
                    Debug.Log("API 데이터 로드 성공 (더미 데이터로 대체)");
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON 파싱 오류: {e.Message}");
                    // 파싱 실패 시 더미 데이터 유지
                }
            }
            else
            {
                Debug.LogError($"API 요청 실패: {request.error}");
                // 에러 메시지 상세 출력
                if (request.downloadHandler != null && request.downloadHandler.text != null)
                {
                    Debug.LogError("API 응답 내용: " + request.downloadHandler.text);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"API 응답 처리 중 예외 발생: {e.Message}");
        }
        finally
        {
            // 요청 객체 해제
            request.Dispose();
            isLoading = false;
        }
        
        yield return null;
    }
    
    // 더미 데이터 추가
    private void AddDummyData()
    {
        Debug.Log("더미 데이터 추가");
        animalDataPool.Clear();
        
        for (int i = 0; i < 5; i++)
        {
            animalDataPool.Add(CreateDummyData(i));
        }
        isDataLoaded = true;
    }
    
    // 랜덤 동물 데이터 가져오기
    public AnimalData GetRandomAnimalData()
    {
        if (!isDataLoaded || animalDataPool.Count == 0)
        {
            Debug.LogWarning("데이터가 로드되지 않았거나 없습니다. 더미 데이터 반환");
            return CreateDummyData(0);
        }
        
        // Random 클래스를 명확하게 지정
        int randomIndex = UnityEngine.Random.Range(0, animalDataPool.Count);
        return animalDataPool[randomIndex];
    }
    
    // 더미 데이터 생성
    private AnimalData CreateDummyData(int index)
    {
        string[] sexCodes = { "M", "F", "Q" };
        string[] ages = { "2023(년생)", "2021(년생)", "2020(년생)", "2022(년생)", "추정 1세" };
        string[] weights = { "3.5(Kg)", "4.2(Kg)", "2.8(Kg)", "5.1(Kg)", "6.7(Kg)" };
        string[] states = { "보호중", "임시보호", "입양가능", "치료중", "보호중" };
        
        return new AnimalData
        {
            popfile = "", // 이미지 URL 없음
            sexCd = sexCodes[index % sexCodes.Length],
            age = ages[index % ages.Length],
            weight = weights[index % weights.Length],
            processState = states[index % states.Length]
        };
    }
}