using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;              // 따라갈 대상 (플레이어)
    public float smoothSpeedX = 0.125f;   // X축 부드러움 정도
    public float smoothSpeedY = 0.05f;    // Y축은 더 부드럽게
    public Vector2 offset = new Vector2(0f, 2f); // Y축을 2만큼 위로 올림

    private void LateUpdate()
    {
        if (target == null) return;

        // 타겟의 위치 + 오프셋 적용
        float targetX = target.position.x + offset.x;
        float targetY = target.position.y + offset.y;

        // 현재 위치 유지할 Z축 값
        float currentZ = transform.position.z;

        // 부드러운 보간
        float smoothedX = Mathf.Lerp(transform.position.x, targetX, smoothSpeedX);
        float smoothedY = Mathf.Lerp(transform.position.y, targetY, smoothSpeedY);

        // 최종 위치로 이동
        transform.position = new Vector3(smoothedX, smoothedY, currentZ);
    }
}
