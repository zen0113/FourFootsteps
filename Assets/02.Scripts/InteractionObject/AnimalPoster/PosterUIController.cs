using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트를 위한 네임스페이스

public class PosterUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject posterPanel;
    
    // 기본 정보
    [SerializeField] private Text sexText;
    [SerializeField] private Text ageText;
    [SerializeField] private Text processStateText;
    
    // 추가된 정보
    [SerializeField] private Text breedText;          // 품종
    [SerializeField] private Text colorText;          // 색상
    [SerializeField] private Text shelterText;        // 보호소명
    [SerializeField] private Text locationText;       // 발견장소
    [SerializeField] private Text neuterText;         // 중성화여부
    [SerializeField] private Text noticeDateText;     // 공고일자
    [SerializeField] private Text specialMarkText;    // 특징
    
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
        
        // 기본 정보 업데이트
        sexText.text = $"성별: {TranslateSex(animalData.sexCd)}";
        ageText.text = $"나이: {animalData.age}";
        processStateText.text = $"상태: {animalData.processState}";
        
        // 추가 정보 업데이트
        breedText.text = $"품종: {animalData.kindCd}";
        colorText.text = $"색상: {animalData.colorCd}";
        shelterText.text = $"보호소: {animalData.careNm}";
        locationText.text = $"발견장소: {animalData.happenPlace}";
        neuterText.text = $"중성화: {TranslateNeuter(animalData.neuterYn)}";
        noticeDateText.text = $"공고일: {FormatDate(animalData.noticeSdt)} ~ {FormatDate(animalData.noticeEdt)}";
        specialMarkText.text = $"특징: {animalData.specialMark}";
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
    
    // 중성화 여부 변환
    private string TranslateNeuter(string neuterYn)
    {
        switch (neuterYn)
        {
            case "Y": return "완료";
            case "N": return "미완료";
            default: return "미상";
        }
    }
    
    // 날짜 포맷 변환 (20250601 -> 2025-06-01)
    private string FormatDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length != 8)
            return dateString;
            
        try
        {
            string year = dateString.Substring(0, 4);
            string month = dateString.Substring(4, 2);
            string day = dateString.Substring(6, 2);
            return $"{year}-{month}-{day}";
        }
        catch
        {
            return dateString;
        }
    }
}