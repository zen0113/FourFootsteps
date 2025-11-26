using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Header("스크롤 설정")]
    public float scrollSpeed = 2f;          // 스크롤 속도
    public float backgroundWidth = 19.2f;   // 배경 이미지 가로 길이 (Unity 단위)

    [Header("패럴랙스 효과")]
    public float parallaxMultiplier = 1f;   // 패럴랙스 배수 (0~1: 느리게, 1: 일반 속도, 1+: 빠르게)

    private float startPosition;

    void Start()
    {
        startPosition = transform.position.x;
    }

    void Update()
    {
        // 배경을 왼쪽으로 이동
        float distance = scrollSpeed * parallaxMultiplier * Time.deltaTime;
        transform.Translate(Vector3.left * distance);

        // 배경이 화면 밖으로 나가면 오른쪽으로 재배치
        if (transform.position.x <= startPosition - backgroundWidth)
        {
            transform.position = new Vector3(startPosition, transform.position.y, transform.position.z);
        }
    }

    // 스크롤 속도를 동적으로 변경하는 함수
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
}