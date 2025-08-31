using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HideObject : MonoBehaviour
{
    [Header("Anchor (optional)")]
    [Tooltip("플레이어를 정렬할 기준점 (미지정 시 자기 Transform 사용)")]
    public Transform anchor;

    [Header("Visuals / Prompt")]
    [SerializeField] private SpriteGlow.SpriteGlowEffect spriteGlowEffect;
    [SerializeField] private GameObject keyPrompt; // 'Key Icon' 등 프롬프트 오브젝트

    // 내부 캐시
    [SerializeField] private Collider2D areaCollider;
    public Collider2D AreaCollider => areaCollider ? areaCollider : (areaCollider = GetComponent<Collider2D>());

    /// <summary>앵커의 월드 X좌표(없으면 자기 Transform 사용)</summary>
    public float AnchorX => (anchor ? anchor : transform).position.x;

    public bool isGoalObject = false;

    void Awake()
    {
        // 컴포넌트/자식 자동 캐시
        if (!spriteGlowEffect)
            spriteGlowEffect = GetComponent<SpriteGlow.SpriteGlowEffect>();

        if (!keyPrompt)
        {
            var t = transform.Find("Key Icon");
            if (t) keyPrompt = t.gameObject;
        }

        // 시작 시 이펙트/프롬프트 off
        if (spriteGlowEffect) spriteGlowEffect.enabled = false;
        if (keyPrompt) keyPrompt.SetActive(false);

        // 안전장치: 은신영역 콜라이더는 trigger 권장
        if (AreaCollider) AreaCollider.isTrigger = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider2D>();
        if (!spriteGlowEffect) spriteGlowEffect = GetComponent<SpriteGlow.SpriteGlowEffect>();

        // 에디터에서 자식 자동 검색(있으면)
        if (!keyPrompt)
        {
            var t = transform.Find("Key Icon");
            if (t) keyPrompt = t.gameObject;
        }
    }
#endif

    /// <summary>
    /// 컨트롤러(플레이어)가 호출: 프롬프트/글로우 온오프
    /// </summary>
    public void SetEffect(bool isActive)
    {
        if (spriteGlowEffect) spriteGlowEffect.enabled = isActive;
        if (keyPrompt) keyPrompt.SetActive(isActive);
    }
}
