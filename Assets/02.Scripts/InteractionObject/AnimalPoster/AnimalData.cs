using System;
using UnityEngine;

[Serializable]
public class AnimalData
{
    // 기본 정보
    public string desertionNo;   // 유기번호
    public string happenDt;      // 접수일
    public string happenPlace;   // 발견장소
    public string kindCd;        // 품종
    public string colorCd;       // 색상
    public string age;           // 나이
    public string weight;        // 체중
    public string noticeNo;      // 공고번호
    public string noticeSdt;     // 공고시작일
    public string noticeEdt;     // 공고종료일
    public string processState;  // 상태
    public string sexCd;         // 성별 (M:수컷, F:암컷, Q:미상)
    public string neuterYn;      // 중성화여부 (Y:예, N:아니오, U:미상)
    public string specialMark;   // 특징
    public string careNm;        // 보호소이름
    public string careTel;       // 보호소전화번호
    
    // 이미지 필드들 (API에서 받을 때 사용)
    public string popfile1;      // 대표 이미지 1
    public string popfile2;      // 추가 이미지 2
    public string popfile3;      // 추가 이미지 3
    public string popfile4;      // 추가 이미지 4
    public string popfile5;      // 추가 이미지 5
    public string popfile6;      // 추가 이미지 6
    public string popfile7;      // 추가 이미지 7
    public string popfile8;      // 추가 이미지 8
    
    // 추가 정보들
    public string careAddr;      // 보호소 주소
    public string orgNm;         // 관리기관명
    public string kindFullNm;    // 상세 품종명
    public string upKindCd;      // 축종코드
    public string upKindNm;      // 축종명
    public string kindNm;        // 품종명
    
    // 건강 상태
    public string vaccinationChk; // 예방접종 여부
    public string healthChk;       // 건강상태
    
    // 대표 이미지 URL 가져오기 (첫 번째로 유효한 이미지 반환)
    public string GetMainImageUrl()
    {
        string[] imageUrls = { popfile1, popfile2, popfile3, popfile4, popfile5, popfile6, popfile7, popfile8 };
        
        foreach (string url in imageUrls)
        {
            if (!string.IsNullOrEmpty(url) && url.Trim() != "")
            {
                return url;
            }
        }
        
        return null; // 이미지가 없는 경우
    }
}

// API 응답 구조 업데이트 (올바른 구조)
[Serializable]
public class AnimalApiResponse
{
    public ResponseData response;  // 이게 빠져있었음!
}

[Serializable]
public class ResponseData
{
    public ResponseHeader header;
    public ResponseBody body;
}

[Serializable]
public class ResponseHeader
{
    public string reqNo;
    public string resultCode;
    public string resultMsg;
    public string errorMsg;
}

[Serializable]
public class ResponseBody
{
    public ItemsData items;
    public string numOfRows;
    public string pageNo;
    public string totalCount;
}

[Serializable]
public class ItemsData
{
    public AnimalData[] item;
}