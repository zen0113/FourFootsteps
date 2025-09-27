using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    [Header("바퀴 회전 설정")]
    public float rotationSpeed = 180f;  // 회전 속도 (도/초)
    public bool clockwise = true;       // 시계방향 회전 여부

    [Header("트럭 움직임과 연동")]
    public bool linkToTruckSpeed = true;     // 트럭 속도와 연동
    public float speedMultiplier = 30f;      // 속도 배수
    public BackgroundScroller backgroundRef; // 배경 스크롤러 참조

    void Update()
    {
        float currentRotationSpeed = rotationSpeed;

        // 트럭 속도와 연동하는 경우
        if (linkToTruckSpeed && backgroundRef != null)
        {
            currentRotationSpeed = backgroundRef.scrollSpeed * speedMultiplier;
        }

        // 바퀴 회전
        float rotationDirection = clockwise ? -1f : 1f;
        transform.Rotate(0, 0, rotationDirection * currentRotationSpeed * Time.deltaTime);
    }

    // 바퀴 회전 속도를 직접 설정하는 함수
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}