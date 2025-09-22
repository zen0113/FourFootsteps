using UnityEngine;

public class EventObject : MonoBehaviour
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

    // 플레이어가 오브젝트 범위 내에 있는지 확인하는 변수
    protected bool _isPlayerInRange = false;

    protected void Start()
    {
        if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
        {
            spriteGlowEffect = gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>();
            spriteGlowEffect.enabled = false;
        }

        // 키 이미지 초기화 (시작 시 비활성화)
        if (keyImage != null)
        {
            keyImage.SetActive(false);
        }
    }

    protected void Investigate()
    {
        EventManager.Instance.CallEvent(eventId);
        Debug.Log($"{eventId} 호출");

        // 한번만 조사 가능한 설정이면 조사 완료 상태로 변경
        if (isOneTimeOnly)
        {
            hasBeenInvestigated = true;

            // 키 이미지가 있으면 비활성화
            if (keyImage != null)
            {
                keyImage.SetActive(false);
            }
        }
    }

    protected void Update()
    {
        // 다이얼로그 진행 중일 때는 조사 금지
        if (DialogueManager.Instance.isDialogueActive)
            return;

        // 회상 씬에서 조사 불가능하면 리턴
        if (!CanInteractInRecallScene())
            return;

        // 한번만 조사 가능한 설정이고 이미 조사했으면 리턴
        if (isOneTimeOnly && hasBeenInvestigated)
            return;

        // 조사 조건: 플레이어가 범위 내에 있고 E키를 눌렀을 때
        if (!string.IsNullOrEmpty(eventId)
            && EventManager.Instance && _isPlayerInRange
            && Input.GetKeyDown(KeyCode.E))
        {
            Investigate();
        }
    }

    public string GetEventId()
    {
        return eventId;
    }

    // 조사 가능 상태인지 확인하는 메서드 (외부에서 호출 가능)
    public bool CanBeInvestigated()
    {
        if (isOneTimeOnly && hasBeenInvestigated)
            return false;
        return true;
    }

    // 조사 상태 초기화 (필요시 호출)
    public void ResetInvestigationState()
    {
        hasBeenInvestigated = false;

        // 키 이미지가 있으면 다시 활성화 가능하도록 설정
        if (keyImage != null)
        {
            keyImage.SetActive(false); // 리셋 시에는 비활성화 상태로 시작
        }
    }

    // 회상 씬에서 조사 가능한지 확인하는 메서드
    protected virtual bool CanInteractInRecallScene()
    {
        // RecallManager가 없으면 일반 씬이므로 조사 가능
        if (RecallManager.Instance == null)
            return true;
        // RecallManager가 있으면 회상 씬이므로 CanInvesigatingRecallObject 변수 확인
        return (bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject");
    }

    // 플레이어가 트리거에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬에서 조사 불가능하면 리턴
            if (!CanInteractInRecallScene())
                return;

            // 한번만 조사 가능한 설정이고 이미 조사했으면 글로우 효과 비활성화
            if (isOneTimeOnly && hasBeenInvestigated)
                return;

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

            // 키 이미지가 있으면 활성화
            if (keyImage != null)
            {
                keyImage.SetActive(true);
            }

            _isPlayerInRange = true; // 플레이어가 범위에 들어옴
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬에서 조사 불가능하면 리턴
            if (!CanInteractInRecallScene())
            {
                // 조사 불가능한 상태가 되면 상호작용 해제
                if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                    spriteGlowEffect.enabled = false;

                // 키 이미지가 있으면 비활성화
                if (keyImage != null)
                {
                    keyImage.SetActive(false);
                }

                _isPlayerInRange = false;
                return;
            }

            // 한번만 조사 가능한 설정이고 이미 조사했으면 상호작용 해제
            if (isOneTimeOnly && hasBeenInvestigated)
            {
                if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                    spriteGlowEffect.enabled = false;

                // 키 이미지가 있으면 비활성화
                if (keyImage != null)
                {
                    keyImage.SetActive(false);
                }

                _isPlayerInRange = false;
                return;
            }

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

            // 키 이미지가 있으면 활성화
            if (keyImage != null)
            {
                keyImage.SetActive(true);
            }

            _isPlayerInRange = true;
        }
    }

    // 플레이어가 트리거에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = false;

            // 키 이미지가 있으면 비활성화
            if (keyImage != null)
            {
                keyImage.SetActive(false);
            }

            _isPlayerInRange = false; // 플레이어가 범위에서 벗어남
        }
    }
}