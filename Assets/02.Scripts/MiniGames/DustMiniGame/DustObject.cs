using UnityEngine;
using UnityEngine.EventSystems; // 마우스 이벤트를 UI에 사용하기 위해 필요

public class DustObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("먼지를 제거하는 데 필요한 시간 (초)")]
    public float timeToClean = 5.0f;

    [Tooltip("클릭하고 있을 때 재생할 효과음")]
    public AudioClip cleaningSound;

    [Tooltip("클릭하고 있을 때 마우스 위치에 생성할 애니메이션 또는 파티클 프리팹")]
    public GameObject cleaningEffectPrefab;

    private float _currentHoldTime = 0f;
    private bool _isBeingCleaned = false;
    private AudioSource _audioSource;
    private GameObject _activeCleaningEffect;

    // 이 먼지를 관리하는 메인 이벤트 스크립트
    private DustCleaningEvent _eventManager;

    void Awake()
    {
        // 효과음 재생을 위한 AudioSource 컴포넌트 추가 및 설정
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = cleaningSound;
        _audioSource.loop = true; // 소리가 반복 재생되도록 설정
    }

    void Update()
    {
        if (_isBeingCleaned)
        {
            // 시간을 누적
            _currentHoldTime += Time.deltaTime;

            // 마우스 위치로 이펙트 이동
            if (_activeCleaningEffect != null)
            {
                // UI 캔버스 좌표계이므로 ScreenToWorldPoint 사용
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0; // 2D이므로 z축은 0으로
                _activeCleaningEffect.transform.position = mousePos;
            }

            // 필요한 시간을 모두 채웠다면
            if (_currentHoldTime >= timeToClean)
            {
                // 매니저에게 내가 제거되었다고 알림
                _eventManager.OnDustCleaned(this.gameObject);
                // 이펙트와 자기 자신을 파괴
                if (_activeCleaningEffect != null) Destroy(_activeCleaningEffect);
                Destroy(gameObject);
            }
        }
    }

    // 이 먼지 오브젝트에 마우스 포인터를 누르기 시작했을 때 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        _isBeingCleaned = true;
        _audioSource.Play();

        if (cleaningEffectPrefab != null)
        {
            _activeCleaningEffect = Instantiate(cleaningEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    // 마우스 포인터에서 손을 뗐을 때 호출
    public void OnPointerUp(PointerEventData eventData)
    {
        _isBeingCleaned = false;
        _currentHoldTime = 0f; // 타이머 리셋
        _audioSource.Stop();

        if (_activeCleaningEffect != null)
        {
            Destroy(_activeCleaningEffect);
        }
    }

    // Event Manager가 자신을 생성할 때 호출해주는 함수
    public void SetManager(DustCleaningEvent manager)
    {
        _eventManager = manager;
    }
}