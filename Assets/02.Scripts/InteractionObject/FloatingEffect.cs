using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    [Header("둥둥 떠있는 효과 설정")]
    [Tooltip("위아래로 움직이는 최대 높이")]
    [SerializeField] private float amplitude = 0.2f;
    [Tooltip("위아래로 움직이는 속도")]
    [SerializeField] private float speed = 1.5f;

    // 둥둥 효과의 기준이 될 시작 위치와 회전값
    private Vector3 basePosition;
    private Quaternion initialRotation;
    private float timeOffset;

    // 스크립트가 활성화될 때 호출됩니다.
    void OnEnable()
    {
        // 현재 위치를 기준 위치로 저장
        basePosition = transform.position;
        initialRotation = transform.rotation;
        // 랜덤한 시간 오프셋을 추가하여 여러 오브젝트가 동시에 움직이지 않도록 함
        timeOffset = Random.Range(0f, Mathf.PI * 2);
    }

    // 매 프레임마다 호출됩니다.
    void Update()
    {
        // 시간에 따라 부드럽게 위아래로 움직이는 값을 계산(사인파 사용)
        float yOffset = Mathf.Sin(Time.time * speed + timeOffset) * amplitude;

        // 기준 위치에서 y축으로만 살짝 움직인 새로운 위치를 적용
        transform.position = basePosition + new Vector3(0, yOffset, 0);

        transform.rotation = initialRotation;
    }
}