using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalShelterObject : EventObject, IResultExecutable
{
    [Header("여러 개의 오브젝트 리스트 (자식들)")]
    [SerializeField] private GameObject[] objectList;

    [Header("After 스프라이트 (일괄 교체용)")]
    [SerializeField] private Sprite afterSprite;

    // 캐싱: 여러 오브젝트의 Glow를 한 번에 제어
    private readonly List<SpriteGlow.SpriteGlowEffect> _glowEffects = new();

    private void Awake()
    {
        // ResultManager 등록
        ResultManager.Instance.RegisterExecutable(eventId, this);

        // Glow 컴포넌트 수집 (자식까지 포함)
        if (objectList != null)
        {
            foreach (var obj in objectList)
            {
                if (!obj) continue;

                // 자기 자신 포함 자식들까지 모두 수집
                var effects = obj.GetComponentsInChildren<SpriteGlow.SpriteGlowEffect>(true);
                if (effects != null && effects.Length > 0)
                    _glowEffects.AddRange(effects);
            }
        }
    }

    protected override void Start()
    {
        base.Start();
        // 시작 시 모든 자식 Glow Off 보장
        SetGlow(false);
    }

    // EventObject 훅 오버라이드: 범위 내/외에 따라 모든 자식 Glow를 On/Off
    protected override void SetGlow(bool enabled)
    {
        // 부모에 붙은 개별 Glow가 있다면 부모도 함께 제어(선택)
        base.SetGlow(enabled);

        for (int i = 0; i < _glowEffects.Count; i++)
        {
            var fx = _glowEffects[i];
            if (fx) fx.enabled = enabled;
        }
    }

    // ExecuteAction 은 외부에서 호출됨.
    // ResultManager의 executableObjects["eventId"].ExecuteAction(); 처럼.
    public void ExecuteAction()
    {
        if (objectList == null || afterSprite == null) return;

        foreach (var obj in objectList)
        {
            if (!obj) continue;

            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = afterSprite;
        }
    }

}
