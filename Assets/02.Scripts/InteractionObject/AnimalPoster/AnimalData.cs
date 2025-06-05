using System;
using UnityEngine;

[Serializable]
public class AnimalData
{
    // 실제 API에서 제공되는 필드들 (이미지, 체중 제외)
    public string desertionNo;   // 유기번호
    public string happenDt;      // 접수일
    public string happenPlace;   // 발견장소
    public string kindCd;        // 품종
    public string colorCd;       // 색상
    public string age;           // 나이
    public string noticeNo;      // 공고번호
    public string noticeSdt;     // 공고시작일
    public string noticeEdt;     // 공고종료일
    public string processState;  // 상태
    public string sexCd;         // 성별 (M:수컷, F:암컷, Q:미상)
    public string neuterYn;      // 중성화여부 (Y:예, N:아니오, U:미상)
    public string specialMark;   // 특징
    public string careNm;        // 보호소이름
    public string careTel;       // 보호소전화번호
}

// API 응답 구조에 맞게 중첩 클래스 정의
[Serializable]
public class AnimalApiResponse
{
    public ResponseData response;
}

[Serializable]
public class ResponseData
{
    public BodyData body;
}

[Serializable]
public class BodyData
{
    public ItemsData items;
}

[Serializable]
public class ItemsData
{
    public AnimalData[] item;
}