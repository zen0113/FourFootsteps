using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 프레임 말미에 실행되도록 우선순위 크게 (원한다면 더 키워도 됨)
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Collider2D))]
public class PostFXZone2D : MonoBehaviour
{
    [Header("Player detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;

    [Header("Target Global Volume (URP)")]
    [SerializeField] private Volume globalVolume;

    [Header("Target Sky Object (Sun Or Moon)")]
    [SerializeField] private bool useSkyObject = false;
    [SerializeField] private Transform skyObject;
    [Tooltip("하늘 오브젝트 Y를 로컬좌표로 제어(권장). 끄면 월드좌표로 제어.")]
    [SerializeField] private bool skyUseLocalSpace = true;

    [Header("Mapping along zone (left->right)")]
    [SerializeField] private AnimationCurve positionToWeight = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private bool clampWeight01 = true;
    [SerializeField] private bool invertDirection = false;

    [Header("Targets")]
    [SerializeField, Range(-100f, 100f)] private float targetContrast = -15f;
    [SerializeField] private Color targetColorFilter = new Color32(255, 209, 189, 255);
    [SerializeField] private bool switchTonemapping = true;
    [SerializeField] private float targetSkyObjectYPos = 0f; // 로컬/월드 선택에 따라 해석

    [Header("Latch / Persistence")]
    [Tooltip("w가 이 임계값을 넘는 순간부터 변화를 시작")]
    [SerializeField] private float startThreshold = 0.02f;

    [Tooltip("존을 떠나도 진행도를 기억(재진입 시 이어서 시작)")]
    [SerializeField] private bool rememberProgressAcrossExit = true;

    [Tooltip("존을 떠날 때 화면 효과를 즉시 0(하양/Contrast 원래)으로 원복")]
    [SerializeField] private bool visualResetOnExit = false;

    private Collider2D zoneCol;
    private Transform playerTf;
    private bool playerInside;

    private ColorAdjustments colorAdj;
    private Tonemapping tonemapping;
    private float baseContrast = 0f;
    private Color baseColorFilter = Color.white;
    private TonemappingMode baseToneMode = TonemappingMode.None;

    private float baseSkyY = 0f; // skyUseLocalSpace에 따라 localY 또는 worldY 저장

    // 진행 상태
    private bool started = false;     // 현재 존 내부에서 “시작됨” 여부
    private float latchedMaxW = 0f;   // 지금까지 도달한 최대 가중치 (존 밖에서도 유지할 값)

    // LateUpdate에서 최종 1회 적용하기 위한 캐시
    private float lastComputedW = 0f;

    private void Awake()
    {
        zoneCol = GetComponent<Collider2D>();
        zoneCol.isTrigger = true;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }
        playerTf = player;

        if (globalVolume == null)
        {
            foreach (var v in FindObjectsOfType<Volume>())
                if (v.isGlobal) { globalVolume = v; break; }
        }
        if (globalVolume == null) { Debug.LogError("[PostFXZone2D] Global Volume 없음"); enabled = false; return; }

        // 런타임 프로필 복제(원본 보호)
        if (globalVolume.profile == null && globalVolume.sharedProfile != null)
            globalVolume.profile = Instantiate(globalVolume.sharedProfile);
        else if (globalVolume.profile != null && ReferenceEquals(globalVolume.profile, globalVolume.sharedProfile))
            globalVolume.profile = Instantiate(globalVolume.sharedProfile);

        if (!globalVolume.profile.TryGet(out colorAdj))
            colorAdj = globalVolume.profile.Add<ColorAdjustments>(true);
        if (!globalVolume.profile.TryGet(out tonemapping))
            tonemapping = globalVolume.profile.Add<Tonemapping>(true);

        baseContrast = colorAdj.contrast.value;
        baseColorFilter = colorAdj.colorFilter.value;
        baseToneMode = tonemapping.mode.value;

        // SkyObject 가드 + 기준값 설정
        if (useSkyObject)
        {
            if (skyObject == null)
            {
                Debug.LogWarning("[PostFXZone2D] useSkyObject=true 이지만 skyObject가 비어 있습니다. sky 기능을 비활성화합니다.");
                useSkyObject = false;
            }
            else
            {
                baseSkyY = skyUseLocalSpace ? skyObject.localPosition.y : skyObject.position.y;

                // 참고: 물리 영향 방지(선택 사항)
                var rb2d = skyObject.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.gravityScale = 0f;
                    rb2d.isKinematic = true;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;

        if (rememberProgressAcrossExit && latchedMaxW > 0f)
        {
            started = true;
            lastComputedW = latchedMaxW;
            ApplyWithWeight(lastComputedW); // 즉시 반영
        }
        else
        {
            started = false;
            lastComputedW = 0f;
            ApplyWithWeight(lastComputedW);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;

        if (visualResetOnExit)
        {
            lastComputedW = 0f;
            ApplyWithWeight(0f); // 즉시 원복
        }

        if (!rememberProgressAcrossExit)
        {
            latchedMaxW = 0f;
            started = false;
        }
        else
        {
            started = false; // 진행도 유지
        }
    }

    private void Update()
    {
        if (!playerInside || playerTf == null) return;

        Bounds b = zoneCol.bounds;
        float left = b.min.x;
        float right = b.max.x;
        if (Mathf.Approximately(right - left, 0f)) return;

        float raw = Mathf.InverseLerp(left, right, playerTf.position.x);
        if (invertDirection) raw = 1f - raw;

        float w = positionToWeight.Evaluate(raw);
        if (clampWeight01) w = Mathf.Clamp01(w);

        // 시작 판정
        if (!started && w > startThreshold)
        {
            started = true;
            if (w > latchedMaxW) latchedMaxW = w; // 재진입이어도 더 크면 갱신
        }

        if (started)
        {
            if (w > latchedMaxW) latchedMaxW = w;
            w = latchedMaxW; // 감소 금지
        }
        else
        {
            w = 0f;
        }

        lastComputedW = w; // 계산만
    }

    private void LateUpdate()
    {
        // 프레임 마지막에 최종 1회 덮어쓰기 → 다른 스크립트와 충돌 방지
        ApplyWithWeight(lastComputedW);
    }

    private void ApplyWithWeight(float w)
    {
        // 안전 보정
        w = clampWeight01 ? Mathf.Clamp01(w) : w;

        // PostFX 보간
        colorAdj.contrast.value = Mathf.Lerp(baseContrast, targetContrast, w);
        colorAdj.colorFilter.value = Color.Lerp(baseColorFilter, targetColorFilter, w);

        // 하늘 오브젝트(Y) 이동
        if (useSkyObject && skyObject != null)
        {
            float yBase = baseSkyY;
            float yTarget = targetSkyObjectYPos;
            float yLerp = Mathf.Lerp(yBase, yTarget, w);

            // 오버슈트 절대 방지
            float minY = Mathf.Min(yBase, yTarget);
            float maxY = Mathf.Max(yBase, yTarget);
            yLerp = Mathf.Clamp(yLerp, minY, maxY);

            if (skyUseLocalSpace)
            {
                var lp = skyObject.localPosition;
                lp.y = yLerp;
                skyObject.localPosition = lp;
            }
            else
            {
                var p = skyObject.position;
                p.y = yLerp;
                skyObject.position = p;
            }
        }

        // Tonemapping
        if (switchTonemapping)
        {
            tonemapping.mode.value = (w > 0.001f) ? TonemappingMode.Neutral : baseToneMode;
        }
    }

    /// <summary>진행도(래치) 강제 초기화</summary>
    public void ResetProgress()
    {
        latchedMaxW = 0f;
        started = false;
        lastComputedW = 0f;
        ApplyWithWeight(0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var c = Gizmos.color;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            var b = col.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
        Gizmos.color = c;
    }
#endif
}
