using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트를 위한 네임스페이스
using UnityEngine.Networking; // UnityWebRequest를 위한 네임스페이스

public class PosterUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject posterPanel;
    [SerializeField] private RawImage animalImage;
    [SerializeField] private Text sexText;
    [SerializeField] private Text ageText;
    [SerializeField] private Text weightText;
    [SerializeField] private Text processStateText;
    
    // 이미지 로딩용 코루틴 참조 저장
    private Coroutine imageLoadCoroutine;
    
    private void Start()
    {
        // 시작시 UI 숨김
        posterPanel.SetActive(false);
    }
    
    // UI 토글 (켜기/끄기)
    public void ToggleUI(bool isActive)
    {
        posterPanel.SetActive(isActive);
        
        // UI가 활성화될 때 새로운 동물 데이터 표시
        if (isActive)
        {
            DisplayRandomAnimalData();
        }
    }
    
    // 랜덤 동물 데이터 표시
    private void DisplayRandomAnimalData()
    {
        // 데이터 매니저에서 랜덤 동물 데이터 가져오기
        AnimalData animalData = AnimalDataManager.Instance.GetRandomAnimalData();
        
        // UI 업데이트 - PRD에서 요구한 데이터만 표시
        sexText.text = $"성별: {TranslateSex(animalData.sexCd)}";
        ageText.text = $"나이: {animalData.age}";
        weightText.text = $"체중: {animalData.weight}";
        processStateText.text = $"상태: {animalData.processState}";
        
        // 이미지 로드 (이전 로드 작업 취소)
        if (imageLoadCoroutine != null)
        {
            StopCoroutine(imageLoadCoroutine);
        }
        
        // 새 이미지 로드 시작
        imageLoadCoroutine = StartCoroutine(LoadAnimalImage(animalData.popfile));
    }
    
    // 성별 코드 변환
    private string TranslateSex(string sexCd)
    {
        switch (sexCd)
        {
            case "M": return "수컷";
            case "F": return "암컷";
            default: return "미상";
        }
    }
    
    // 이미지 로드 코루틴
    private IEnumerator LoadAnimalImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            // 이미지 URL이 없을 경우 기본 이미지 설정
            animalImage.texture = null;
            yield break;
        }
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                animalImage.texture = texture;
            }
            else
            {
                Debug.LogError($"이미지 로드 실패: {request.error}");
                animalImage.texture = null;
            }
        }
    }
}