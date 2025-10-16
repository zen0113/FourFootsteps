using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HideObject : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private StealthSettingsSO settings;
    private StealthSettingsSO _settings;                 // 런타임 복제본

    [Header("Anchor (optional)")]
    [Tooltip("플레이어를 정렬할 기준점 (미지정 시 자기 Transform 사용)")]
    public Transform anchor;

    [Header("Visuals / Prompt")]
    [SerializeField] private SpriteGlow.SpriteGlowEffect spriteGlowEffect;
    [SerializeField] private GameObject keyPrompt; // 'Key Icon' 등 프롬프트 오브젝트

    // 내부 캐시
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private Collider2D areaCollider;
    public Collider2D AreaCollider => areaCollider ? areaCollider : (areaCollider = GetComponent<Collider2D>());

    /// <summary>앵커의 월드 X좌표(없으면 자기 Transform 사용)</summary>
    public float AnchorX => (anchor ? anchor : transform).position.x;

    public bool isGoalObject = false;

    private float HidingAlphaValue = 0.8f;

    void Awake()
    {
        // 컴포넌트/자식 자동 캐시
        if (!spriteGlowEffect)
            spriteGlowEffect = GetComponent<SpriteGlow.SpriteGlowEffect>();
        if (!renderer)
            renderer = GetComponent<SpriteRenderer>();

        if (!keyPrompt)
        {
            var t = transform.Find("Key Icon");
            if (t) keyPrompt = t.gameObject;
        }

        if (!settings)
        {
            Debug.LogWarning("[CatStealthController] StealthSettings가 지정되지 않았습니다. 기본값으로 동작합니다.");
            settings = ScriptableObject.CreateInstance<StealthSettingsSO>();
        }
        _settings = Instantiate(settings); // 공유 SO 상태오염 방지
        _settings.ResetRuntime();
    }
    private void Start()
    {
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
        if (!renderer) renderer = GetComponent<SpriteRenderer>();

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

    /// <summary>
    /// 컨트롤러(플레이어)가 호출: 오브젝트 알파값 조절
    /// <param name="isActive"> true일 시, 0.8f로 변경.</param>
    /// <param name="isActive"> false일 시, 원래 1로 돌아옴</param>
    /// </summary>
    /// 
    public void SetHidingAlpha(bool isActive)
    {
        if (isActive)
            StartCoroutine(ChangeAlphaValue(_settings.HidingAlphaValue));
        else
            StartCoroutine(ChangeAlphaValue(1f));
    }

    private IEnumerator ChangeAlphaValue(float finalValue)
    {
        float elapsedTime = 0f;
        float duration = 1f;

        Color currentColor = renderer.color;
        float startValue = renderer.color.a;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentColor.a = Mathf.Lerp(startValue, finalValue, (elapsedTime / duration));
            renderer.color = currentColor;
            yield return null;
        }

        currentColor.a = finalValue;
        renderer.color = currentColor;
    }

}
