using UnityEngine;

public class EventObject : MonoBehaviour
{
    [SerializeField]
    protected string eventId;

    public bool isInteractable = false;

    [SerializeField]
    protected SpriteGlow.SpriteGlowEffect spriteGlowEffect;

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
        //if (!string.IsNullOrEmpty(eventId) && EventManager.Instance && isInteractable)
        //{
        //    EventManager.Instance.CallEvent(eventId);
        //}
        EventManager.Instance.CallEvent(eventId);
    }

    protected void Update()
    {
        // 다이얼로그 출력 중이면 입력을 받지 않음.
        // 플레이어가 범위 내에 있고 E키를 눌렀을 때
        if (!DialogueManager.Instance.isDialogueActive && !string.IsNullOrEmpty(eventId) 
            && EventManager.Instance && isInteractable 
            && Input.GetKeyDown(KeyCode.E))
        {
            Investigate();
        }
    }


    public string GetEventId()
    {
        return eventId;
    }

    // 플레이어가 트리거에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬이고 일반 오브젝트 조사 가능하지 않으면 조사 불가능하게 함.
            if ((bool)GameManager.Instance.GetVariable("isRecalling")
                && !(bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject"))
                return;

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = true;

            isInteractable = true;
        }
    }

    // 플레이어가 트리거에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회상 씬이고 일반 오브젝트 조사 가능하지 않으면 조사 불가능하게 함.
            if ((bool)GameManager.Instance.GetVariable("isRecalling")
                && !(bool)GameManager.Instance.GetVariable("CanInvesigatingRecallObject"))
                return;

            if (gameObject.GetComponent<SpriteGlow.SpriteGlowEffect>() != null)
                spriteGlowEffect.enabled = false;

            isInteractable = false;
        }
    }
}
