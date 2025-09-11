using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // 카메라가 따라갈 타겟
    public Vector3 offset = new Vector3(0, 0, -10); // 타겟으로부터의 오프셋
    public bool followTarget = true; // 타겟 추적 여부

    [Header("Shake Settings")]
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public float shakeIntensityMultiplier = 1f;

    private Vector3 basePosition; // 기본 위치 (타겟 + 오프셋)
    private Vector3 shakeOffset; // 현재 쉐이크 오프셋
    private Coroutine currentShakeCoroutine;

    private FollowCamera followCamera;
    private Camera cam;

    void Start()
    {
        followCamera = GetComponent<FollowCamera>();

        // 타겟이 설정되지 않은 경우 자동으로 플레이어 찾기
        if (target == null && followTarget)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraShake] 자동으로 Player 타겟을 찾았습니다.");
            }
        }

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // 초기 위치 설정
        UpdateBasePosition();
    }

    void LateUpdate()
    {
        if (followTarget && target != null)
        {
            UpdateBasePosition();
        }

        // 최종 카메라 위치 = 기본 위치 + 쉐이크 오프셋
        Vector3 desired = basePosition + shakeOffset;

        // 최종 좌표에서 Y 하한 클램프
        if (followCamera != null && followCamera.useMinYLimit && cam != null)
        {
            float halfHeight = cam.orthographicSize;            // 매 프레임 계산
            float minCenterY = followCamera.minY + halfHeight;  // 카메라 중심의 최저 Y
            desired.y = Mathf.Max(desired.y, minCenterY);
        }

        transform.position = desired;
    }

    void UpdateBasePosition()
    {
        if (target != null)
        {
            basePosition = target.position + offset;
        }
        else
        {
            // 타겟이 없으면 현재 위치를 기본으로 사용
            basePosition = transform.position - shakeOffset; // 현재 흔들림을 제외한 중심으로 환산
        }
    }

    public void Shake(float duration, float magnitude)
    {
        // 기존 쉐이크가 실행 중이면 중지
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }

        currentShakeCoroutine = StartCoroutine(DoShake(duration, magnitude, false));
    }

    public void ShakeAndDisable(float duration, float magnitude)
    {
        // 기존 쉐이크가 실행 중이면 중지
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }

        currentShakeCoroutine = StartCoroutine(DoShake(duration, magnitude, true));
    }


    private IEnumerator DoShake(float duration, float magnitude, bool doDisable)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;

            // 쉐이크 효과
            float currentShakeIntensity = shakeCurve.Evaluate(normalizedTime) * magnitude * shakeIntensityMultiplier;
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * currentShakeIntensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 원래 상태로 복원
        elapsed = 0f;
        Vector3 beforeOffset = shakeOffset;
        float backDur = duration * 0.5f;        // 복원 시간
        while (elapsed < backDur)
        {
            float t = (backDur <= 0f) ? 1f : (elapsed / backDur); // 정규화 시간
            shakeOffset = new Vector3(
                Mathf.Lerp(beforeOffset.x, 0f, t),
                Mathf.Lerp(beforeOffset.y, 0f, t),
                0f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        currentShakeCoroutine = null;

        if (doDisable) this.enabled = false;
    }

    // 강제로 쉐이크 중지
    public void StopShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        shakeOffset = Vector3.zero;
    }

    // 현재 쉐이크 중인지 확인
    public bool IsShaking()
    {
        return currentShakeCoroutine != null;
    }

    // 타겟 설정
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        UpdateBasePosition();
    }

    // 오프셋 설정
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        UpdateBasePosition();
    }

    // 타겟 추적 활성화/비활성화
    public void SetFollowTarget(bool follow)
    {
        followTarget = follow;
        if (!follow)
        {
            // 추적을 끄면 현재 위치를 기본 위치로 고정
            basePosition = transform.position - shakeOffset;
        }
    }

    void OnDrawGizmosSelected()
    {
        // 에디터에서 타겟과의 관계를 시각적으로 표시
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
        }
    }
}