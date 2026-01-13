using UnityEngine;
using UnityEngine.UI;

public class UIMaskScanner : MonoBehaviour
{
    [Header("Settings")]
    public RectTransform targetGraph; // 기준이 될 그래프 이미지 (크기 참조용)
    public float speed = 300f;        // 이동 속도 (UI는 픽셀 단위라 숫자가 커야 함)

    private RectTransform myRect;     // 이 스크립트가 달린 이미지(스캐너)의 좌표
    private float startX;
    private float endX;

    void Start()
    {
        myRect = GetComponent<RectTransform>();

        if (targetGraph == null)
        {
            Debug.LogError("대상 그래프(Target Graph)를 연결해 주세요!");
            return;
        }

        // 1. 이동 범위 계산
        float graphWidth = targetGraph.rect.width;

        startX = targetGraph.anchoredPosition.x - (graphWidth / 2);
        endX = targetGraph.anchoredPosition.x + (graphWidth / 2);

        // 2. 시작 위치로 이동
        Vector2 startPos = myRect.anchoredPosition;
        startPos.x = startX;
        myRect.anchoredPosition = startPos;
    }

    void Update()
    {
        if (targetGraph == null) return;

        // 3. 오른쪽으로 이동 (anchoredPosition 사용)
        myRect.anchoredPosition += Vector2.right * speed * Time.deltaTime;

        // 4. 오른쪽 끝에 도달하면 처음으로 리셋
        if (myRect.anchoredPosition.x >= endX)
        {
            Vector2 resetPos = myRect.anchoredPosition;
            resetPos.x = startX;
            myRect.anchoredPosition = resetPos;
        }
    }
}