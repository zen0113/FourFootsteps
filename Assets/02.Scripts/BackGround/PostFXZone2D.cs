using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider2D))]
public class PostFXZone2D : MonoBehaviour
{
    [Header("Player detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;

    [Header("Target Global Volume (URP)")]
    [SerializeField] private Volume globalVolume;

    [Header("Mapping along zone (left->right)")]
    [SerializeField] private AnimationCurve positionToWeight = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private bool clampWeight01 = true;
    [SerializeField] private bool invertDirection = false;

    [Header("Targets")]
    [SerializeField, Range(-100f, 100f)] private float targetContrast = -15f;
    [SerializeField] private Color targetColorFilter = new Color32(255, 209, 189, 255);
    [SerializeField] private bool switchTonemapping = true;

    [Header("Latch / Persistence")]
    [Tooltip("w가 이 임계값을 넘는 순간부터 변화를 시작")]
    [SerializeField] private float startThreshold = 0.02f;

    [Tooltip("존을 떠나도 진행도를 기억(재진입 시 이어서 시작)")]
    [SerializeField] private bool rememberProgressAcrossExit = true;

    [Tooltip("존을 떠날 때 화면 효과를 즉시 0(하양/Contrast 원래)으로 원복. 기본값 False")]
    [SerializeField] private bool visualResetOnExit = false;

    //[Tooltip("필요하면 수동으로 진행도를 초기화")]
    //[SerializeField] private KeyCode debugResetKey = KeyCode.None;

    private Collider2D zoneCol;
    private Transform playerTf;
    private bool playerInside;

    private ColorAdjustments colorAdj;
    private Tonemapping tonemapping;
    private float baseContrast = 0f;
    private Color baseColorFilter = Color.white;
    private TonemappingMode baseToneMode = TonemappingMode.None;

    // 진행 상태
    private bool started = false;     // 현재 존 내부에서 “시작됨” 여부
    private float latchedMaxW = 0f;   // 지금까지 도달한 최대 가중치 (존 밖에서도 유지할 값)

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

        // 런타임 프로필 복제
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;

        // 재진입 시 처리:
        // 진행도를 기억하는 경우(latchedMaxW>0), 바로 그 값부터 재개
        if (rememberProgressAcrossExit && latchedMaxW > 0f)
        {
            started = true;                 // 이미 시작 상태로 간주
            ApplyWithWeight(latchedMaxW);   // 시각적으로도 이어서 보이게
        }
        else
        {
            // 처음 진입이거나 기억한 진행도 없음 → 아직 시작 전
            started = false;
            // (latchedMaxW는 유지. 기억을 끄고 완전 초기화를 원하면 여기서 0으로)
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;

        // 화면은 원복하되, 진행도(latchedMaxW)는 남겨둠 → 재진입 시 이어서 시작
        if (visualResetOnExit)
        {
            ApplyWithWeight(0f);
        }

        // rememberProgressAcrossExit가 꺼져 있으면 진행도도 지움
        if (!rememberProgressAcrossExit)
        {
            latchedMaxW = 0f;
            started = false;
        }
        else
        {
            // 기억 모드에서는 진행도 유지. 단, 존을 벗어나면 현재 세션은 끝났다고 표기.
            started = false;
        }
    }

    private void Update()
    {
        //if (debugResetKey != KeyCode.None && Input.GetKeyDown(debugResetKey))
        //    ResetProgress();

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
            // 재진입이어도 latchedMaxW가 더 크면 그 값 유지
            if (w > latchedMaxW) latchedMaxW = w;
        }

        if (started)
        {
            if (w > latchedMaxW) latchedMaxW = w; // 최대치 갱신
            w = latchedMaxW;                      // 감소 금지(모노톤 증가)
        }
        else
        {
            // 아직 시작 전이면 0 유지
            w = 0f;
        }

        ApplyWithWeight(w);
    }

    private void ApplyWithWeight(float w)
    {
        colorAdj.contrast.value = Mathf.Lerp(baseContrast, targetContrast, w);
        colorAdj.colorFilter.value = Color.Lerp(baseColorFilter, targetColorFilter, w);

        if (switchTonemapping)
        {
            if (w > 0.001f) tonemapping.mode.value = TonemappingMode.Neutral;
            else tonemapping.mode.value = baseToneMode; // 보통 None
        }
    }

    /// <summary>진행도(래치) 강제 초기화 API (예: 체크포인트, 챕터 리셋 등에서 호출)</summary>
    public void ResetProgress()
    {
        latchedMaxW = 0f;
        started = false;
        // 화면도 0으로 돌리고 싶으면 아래 줄 유지
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
