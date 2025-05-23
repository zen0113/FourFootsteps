using System;
using UnityEngine;

[Serializable]
public class AnimalData
{
    // PRD에서 요구한 필수 데이터만 유지
    public string popfile;      // 이미지 URL
    public string sexCd;        // 성별 (M:수컷, F:암컷, Q:미상)
    public string age;          // 나이
    public string weight;       // 체중
    public string processState; // 상태 (보호중, 임시보호 등)
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