using UnityEngine;

public class TruckShakeEffect : MonoBehaviour
{
    [Header("흔들림 설정")]
    public float shakeIntensity = 0.1f;      // 흔들림 강도
    public float shakeSpeed = 2f;            // 흔들림 속도
    public bool enableVerticalShake = true;  // 수직 흔들림 사용
    public bool enableHorizontalShake = true; // 수평 흔들림 사용

    private Vector3 originalPosition;
    private float shakeTimer;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        shakeTimer += Time.deltaTime * shakeSpeed;

        Vector3 shakeOffset = Vector3.zero;

        if (enableVerticalShake)
        {
            shakeOffset.y = Mathf.Sin(shakeTimer) * shakeIntensity;
        }

        if (enableHorizontalShake)
        {
            shakeOffset.x = Mathf.Sin(shakeTimer * 0.7f) * shakeIntensity * 0.5f;
        }

        transform.localPosition = originalPosition + shakeOffset;
    }

    // 흔들림 강도를 동적으로 조절하는 함수
    public void SetShakeIntensity(float intensity)
    {
        shakeIntensity = intensity;
    }
}