using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    [Header("바퀴 회전 설정")]
    public float rotationSpeed = 180f;
    public bool clockwise = true;

    public bool isRotating = true;

    [Header("트럭 움직임과 연동")]
    public bool linkToTruckSpeed = true;
    public float speedMultiplier = 30f;
    public BackgroundScroller backgroundRef;

    void Update()
    {
        // [✨ 추가됨] isRotating이 false이면 회전 로직을 실행하지 않습니다.
        if (!isRotating)
        {
            return;
        }

        float currentRotationSpeed = rotationSpeed;

        if (linkToTruckSpeed && backgroundRef != null)
        {
            currentRotationSpeed = backgroundRef.scrollSpeed * speedMultiplier;
        }

        float rotationDirection = clockwise ? -1f : 1f;
        transform.Rotate(0, 0, rotationDirection * currentRotationSpeed * Time.deltaTime);
    }

    public void PauseRotation()
    {
        isRotating = false;
    }

    public void ResumeRotation()
    {
        isRotating = true;
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}