using UnityEngine;

public class EventObject : MonoBehaviour
{
    [SerializeField]
    protected string eventId;

    [SerializeField]
    protected SpriteGlow.SpriteGlowEffect spriteGlowEffect;

    // 플레이어가 오브젝트 범위 내에 있는지 확인하는 변수
    private bool _isPlayerInRange = false;

    protected void Start()
    {
        if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
        {
            spriteGlowEffect = gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>();
            spriteGlowEffect.enabled = false;
        }
    }

    protected void Investigate()
    {
        EventManager.Instance.CallEvent(eventId);
    }

    protected void Update()
    {
        // 다이얼로그 진행 중일 때는 조사 금지
        if (DialogueManager.Instance.isDialogueActive)
            return;

        // 회상 씬에서 조사 불가능하면 리턴 (추가된 조건)
        if (!CanInteractInRecallScene())
            return;

        // 조사 조건: 플레이어가 범위 내에 있고 E키를 눌렀을 때
        if (!string.IsNullOrEmpty(eventId)
            && EventManager.Instance && _isPlayerInRange // isInteractable 대신 새로운 변수 사용
            && Input.GetKeyDown(KeyCode.E))
        {
            Investigate();
        }
    }

    public string GetEventId()
    {
        return eventId;
    }

    // 회상 씬에서 조사 가능한지 확인하는 메서드
    private bool CanInteractInRecallScene()
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

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

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
                _isPlayerInRange = false;
                return;
            }

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

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

            _isPlayerInRange = false; // 플레이어가 범위에서 벗어남
        }
    }
}