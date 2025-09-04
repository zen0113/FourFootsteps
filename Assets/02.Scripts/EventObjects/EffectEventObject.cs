using UnityEngine;
using System.Collections; // 코루틴을 사용하기 위해 추가

public class EffectEventObject : MonoBehaviour
{
    [SerializeField]
    protected string eventId;
    [SerializeField]
    protected SpriteGlow.SpriteGlowEffect spriteGlowEffect;

    [Header("One Time Investigation Settings")]
    [SerializeField]
    private bool isOneTimeOnly = false; // 한번만 조사 가능한지 설정
    [SerializeField]
    private GameObject keyImage; // 키 이미지 오브젝트 (E키 UI 등)
    private bool hasBeenInvestigated = false; // 이미 조사했는지 상태 저장

    [Header("Cat Carrier Shake Effect Settings")]
    [SerializeField]
    private bool useShakeEffect = false; // 이동장 흔들림 효과 사용 여부
    [SerializeField]
    private AudioClip shakeSound; // 흔들릴 때 재생할 효과음
    [SerializeField]
    private float shakeDuration = 0.5f; // 흔들림 지속 시간
    [SerializeField]
    private float shakeMagnitude = 0.05f; // 흔들림 강도

    private AudioSource audioSource; // 효과음 재생기
    private bool isShaking = false; // 현재 흔들리는 중인지 확인

    // 플레이어가 오브젝트 범위 내에 있는지 확인하는 변수
    private bool _isPlayerInRange = false;

    protected void Start()
    {
        if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
        {
            spriteGlowEffect = gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>();
            spriteGlowEffect.enabled = false;
        }

        if (keyImage != null)
        {
            keyImage.SetActive(false);
        }

        // 흔들림 효과를 사용한다면 AudioSource 컴포넌트 준비
        if (useShakeEffect)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // AudioSource가 없으면 새로 추가
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    protected void Investigate()
    {
        EventManager.Instance.CallEvent(eventId);
        Debug.Log($"{eventId} 호출");

        // 흔들림 효과 사용이 활성화되어 있고, 현재 흔들리는 중이 아니라면 효과 시작
        if (useShakeEffect && !isShaking)
        {
            StartCoroutine(ShakeCarrier());
        }

        if (isOneTimeOnly)
        {
            hasBeenInvestigated = true;
            if (keyImage != null)
            {
                keyImage.SetActive(false);
            }
        }
    }

    // 이동장을 흔드는 효과를 처리하는 코루틴
    private IEnumerator ShakeCarrier()
    {
        isShaking = true; // 흔들림 시작
        Vector3 originalPosition = transform.position; // 원래 위치 저장

        // 효과음이 설정되어 있다면 재생
        if (audioSource != null && shakeSound != null)
        {
            audioSource.PlayOneShot(shakeSound);
        }

        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            // 작은 범위 내에서 무작위로 위치 변경
            float xOffset = Random.Range(-0.5f, 0.5f) * shakeMagnitude;
            float yOffset = Random.Range(-0.5f, 0.5f) * shakeMagnitude;

            transform.position = originalPosition + new Vector3(xOffset, yOffset, 0);

            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        transform.position = originalPosition; // 원래 위치로 복원
        isShaking = false; // 흔들림 종료
    }

    protected void Update()
    {
        if (DialogueManager.Instance.isDialogueActive)
            return;

        if (!CanInteractInRecallScene())
            return;

        if (isOneTimeOnly && hasBeenInvestigated)
            return;

        if (!string.IsNullOrEmpty(eventId)
            && EventManager.Instance && _isPlayerInRange
            && Input.GetKeyDown(KeyCode.E))
        {
            Investigate();
        }
    }

    // (이하 기존 코드와 동일)

    public string GetEventId()
    {
        return eventId;
    }

    public bool CanBeInvestigated()
    {
        if (isOneTimeOnly && hasBeenInvestigated)
            return false;
        return true;
    }

    public void ResetInvestigationState()
    {
        hasBeenInvestigated = false;
        if (keyImage != null)
        {
            keyImage.SetActive(false);
        }
    }

    private bool CanInteractInRecallScene()
    {
        if (RecallManager.Instance == null)
            return true;
        return (bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!CanInteractInRecallScene())
                return;

            if (isOneTimeOnly && hasBeenInvestigated)
                return;

            if (spriteGlowEffect != null)
                spriteGlowEffect.enabled = true;

            if (keyImage != null)
            {
                keyImage.SetActive(true);
            }

            _isPlayerInRange = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!CanInteractInRecallScene())
            {
                if (spriteGlowEffect != null)
                    spriteGlowEffect.enabled = false;
                if (keyImage != null)
                {
                    keyImage.SetActive(false);
                }
                _isPlayerInRange = false;
                return;
            }

            if (isOneTimeOnly && hasBeenInvestigated)
            {
                if (spriteGlowEffect != null)
                    spriteGlowEffect.enabled = false;
                if (keyImage != null)
                {
                    keyImage.SetActive(false);
                }
                _isPlayerInRange = false;
                return;
            }

            if (spriteGlowEffect != null)
                spriteGlowEffect.enabled = true;

            if (keyImage != null)
            {
                keyImage.SetActive(true);
            }

            _isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (spriteGlowEffect != null)
                spriteGlowEffect.enabled = false;

            if (keyImage != null)
            {
                keyImage.SetActive(false);
            }

            _isPlayerInRange = false;
        }
    }
}