using UnityEngine;

public class EventObject : MonoBehaviour
{
    [SerializeField]
    public string eventId;
    [SerializeField]
    protected SpriteGlow.SpriteGlowEffect spriteGlowEffect;

    [Header("One Time Investigation Settings")]
    [SerializeField] private bool isOneTimeOnly = false; // 한번만 조사 가능한지 설정
    [SerializeField] private GameObject keyImage; // 키 이미지 오브젝트 (E키 UI 등)
    private bool hasBeenInvestigated = false; // 이미 조사했는지 상태 저장

    [Header("Interaction Condition")]
    [SerializeField] private string requiredCondition;

    // 플레이어가 오브젝트 범위 내에 있는지 확인하는 변수
    protected bool _isPlayerInRange = false;

    protected virtual void Start()
    {
        if (!spriteGlowEffect)
        {
            spriteGlowEffect = GetComponent<SpriteGlow.SpriteGlowEffect>();
        }
        // 시작 시 Glow는 Off
        SetGlow(false);

        // 키 이미지 초기화
        SetKeyImageActive(false);
    }

    // 비활성화 해도 플레이어가 영역내에 있으면
    // 키 이미지가 뜨는게 부자연스러움
    // => 비활성화 시, Key의 InteractionImageDisplay 컴포넌트 비활성화.
    // eventObejct 활성화 시, Key의 InteractionImageDisplay 컴포넌트 다시 활성화.
    protected void OnDisable()
    {
        if (keyImage != null)
        {
            var comp = keyImage.GetComponent<InteractionImageDisplay>();
            if (comp) comp.enabled = false;
        }
    }

    protected void OnEnable()
    {
        if (keyImage != null)
        {
            var comp = keyImage.GetComponent<InteractionImageDisplay>();
            if (comp) comp.enabled = true;
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
            SetKeyImageActive(false);
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

        // 1. 기본 상호작용 키 (E)
        bool isEKeyDown = Input.GetKeyDown(KeyCode.E);

        // 2. EventID에 'Door'가 포함될 경우, 추가로 W 키 허용
        bool isWKeyDown = false;
        if (!string.IsNullOrEmpty(eventId) && eventId.Contains("Door"))
        {
            isWKeyDown = Input.GetKeyDown(KeyCode.W);
        }

        // 조사 조건: 플레이어가 범위 내에 있고 (E키를 눌렀거나 'Door' 이벤트이면서 W키를 눌렀을 때)
        if (!string.IsNullOrEmpty(eventId)
            && EventManager.Instance && _isPlayerInRange
            && (isEKeyDown || isWKeyDown)) 
        {
            Investigate();
        }
    }

    public string GetEventId() => eventId;

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
        SetKeyImageActive(false);
    }

    // 회상 씬에서 조사 가능한지 확인하는 메서드
    protected virtual bool CanInteractInRecallScene()
    {
        // RecallManager가 없다면 일반 씬이므로, 항상 상호작용 가능합니다.
        if (RecallManager.Instance == null)
        {
            return true;
        }

        // 'Required Condition' 필드가 비어있다면, 지정된 조건이 없으므로 상호작용이 "불가능"합니다.
        if (string.IsNullOrEmpty(requiredCondition))
        {
            return false;
        }

        // 'Required Condition'이 지정되어 있다면, GameManager의 변수 값을 확인하여
        // 실제 조건이 충족되었는지 판단합니다.
        return ConditionManager.Instance.IsCondition(requiredCondition);
    }

    // Glow를 켜고 끄는 실제 동작은 여기서만 처리
    protected virtual void SetGlow(bool enabled)
    {
        if (spriteGlowEffect) spriteGlowEffect.enabled = enabled;
    }

    // Key 이미지 표시/비표시
    protected virtual void SetKeyImageActive(bool enabled)
    {
        if (keyImage) keyImage.SetActive(enabled);
    }

    // ───── 트리거 로직 ─────
    // 플레이어가 트리거에 들어왔을 때
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 회상 씬에서 조사 불가능하면 리턴
        if (!CanInteractInRecallScene()) return;
        // 한번만 조사 가능한 설정이고 이미 조사했으면 글로우 효과 비활성화
        if (isOneTimeOnly && hasBeenInvestigated) return;

        SetGlow(true);
        SetKeyImageActive(true);    // 키 이미지가 있으면 활성화
        _isPlayerInRange = true; // 플레이어가 범위에 들어옴
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 회상 씬에서 조사 불가능하면 리턴
        // Or 한번만 조사 가능한 설정이고 이미 조사했으면 상호작용 해제
        if (!CanInteractInRecallScene() || (isOneTimeOnly && hasBeenInvestigated))
        {
            SetGlow(false);
            SetKeyImageActive(false);
            _isPlayerInRange = false;
            return;
        }

        SetGlow(true);
        SetKeyImageActive(true);
        _isPlayerInRange = true;
    }

    // 플레이어가 트리거에서 나갔을 때
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        SetGlow(false);
        SetKeyImageActive(false);   // 키 이미지가 있으면 비활성화
        _isPlayerInRange = false;   // 플레이어가 범위에서 벗어남
    }
}