using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PuzzlePiece : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer mainRenderer;   // 본체
    [SerializeField] private SpriteRenderer shadowRenderer; // 그림자
    [SerializeField] private SpriteRenderer crackRenderer;  // 크랙
    [SerializeField] private SpriteMask mask; // 본체의 스프라이트 마스크

    [Header("Snap / Rotate")]
    public int id = 0;
    public Transform snapTarget;
    public float snapPosTolerance = 0.35f;
    public float snapAngleTolerance = 10f;
    public float rotateStep = 90f;

    [Header("FX")]
    public AudioClip sfxSnap;
    public AudioClip sfxRotate;
    private float clickedScale = 1.05f;
    [SerializeField, Range(0f, 1f)] private float shadowSelectedAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float shadowIdleAlpha = 0.0f;
    [SerializeField] private Color shadowColor = Color.black;

    private bool placed = false;
    public bool IsPlaced => placed;

    private Vector3 grabOffset;
    private Camera cam;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    // --- Sorting 설정 ---
    // 요청: BASE_ORDER를 4(또는 5)부터 시작. 크랙은 main+1, 그림자는 main-1.
    // 중요: crack(+1)과 shadow(-1)가 인접 조각과 겹치지 않도록 STEP_ORDER=3로 설정.
    private const int BASE_ORDER = 4;  // 시작값 (원하면 5로 변경해도 됨)
    private const int STEP_ORDER = 3;  // 간격 (4,7,10,…)  / shadow: main-1 / crack: main+1
    private const int SLOT_COUNT = 5;  // 조각 수(참고용, 다르면 경고만)

    private static readonly List<PuzzlePiece> puzzleList_all = new List<PuzzlePiece>();

    // 현재 선택 여부 (A/D 회전용)
    private static PuzzlePiece current;

    #region Unity lifecycle

    private void Reset()
    {
        // 에디터에서 컴포넌트 붙일 때 자동 참조 시도
        if (!mainRenderer) mainRenderer = GetComponent<SpriteRenderer>();
        if (!shadowRenderer) shadowRenderer = transform.Find("Shadow")?.GetComponent<SpriteRenderer>();
        if (!crackRenderer) crackRenderer = transform.Find("Crack")?.GetComponent<SpriteRenderer>();
        if (!mask) mask = GetComponent<SpriteMask>();
    }

    void Awake()
    {
        cam = Camera.main;
        if (!mainRenderer) mainRenderer = GetComponent<SpriteRenderer>();
        if (!shadowRenderer) shadowRenderer = transform.Find("Shadow")?.GetComponent<SpriteRenderer>();
        if (!crackRenderer) crackRenderer = transform.Find("Crack")?.GetComponent<SpriteRenderer>();
        if (!mask) mask = GetComponent<SpriteMask>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;

        if (!puzzleList_all.Contains(this)) puzzleList_all.Add(this);

        // 초기 정렬: 그림자/크랙은 본체 기준으로 -1 / +1
        if (mainRenderer)
        {
            if (shadowRenderer) shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
            if (crackRenderer) crackRenderer.sortingOrder = mainRenderer.sortingOrder + 1;
        }

        // 그림자 초기 색/알파
        if (shadowRenderer)
        {
            var c = shadowColor; c.a = shadowIdleAlpha;
            shadowRenderer.color = c;
        }

        if (mask != null)
        {
            int layerID = mainRenderer.sortingLayerID;

            mask.isCustomRangeActive = true;
            mask.backSortingLayerID = layerID;
            mask.frontSortingLayerID = layerID;
            mask.backSortingOrder = mainRenderer.sortingOrder - 1; // main-1
            mask.frontSortingOrder = mainRenderer.sortingOrder + 1; // main+1
        }
    }

    private void Start()
    {
        NormalizeSortingOrders();
        SetCrackSprite();
    }

    public void SetCrackSprite()
    {
        // 퍼즐 피스에 부정 선택인 경우 크랙 오브젝트 활성화
        // 긍정 선택인 경우에는 크랙 오브젝트 비활성화
        var puzzleStates = GameManager.Instance.GetVariable("MemoryPuzzleStates") as Dictionary<int, bool>;
        if (puzzleStates != null && puzzleStates.ContainsKey(id) && !puzzleStates[id])
        {
            if (crackRenderer) crackRenderer.gameObject.SetActive(true);
        }
        else
        {
            if (crackRenderer) crackRenderer.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        puzzleList_all.Remove(this);
    }

    void OnMouseDown()
    {
        if (placed) return;

        // 퍼즐 선택 연출
        transform.localScale *= clickedScale;
        SetShadowAlpha(shadowSelectedAlpha);

        // 정렬: 이 조각을 맨 위로
        BringToTop(this);

        current = this;

        // 마우스-조각 간 오프셋
        var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        grabOffset = transform.position - mouseWorld;
    }

    void OnMouseDrag()
    {
        if (placed) return;

        var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld + grabOffset;
    }

    void OnMouseUp()
    {
        if (placed) return;

        transform.localScale = new Vector3(1, 1, 1);
        SetShadowAlpha(shadowIdleAlpha);

        TrySnap();
    }

    void Update()
    {
        if (placed) return;

        if (current == this)
        {
            if (Input.GetKeyDown(KeyCode.A))
                Rotate(+rotateStep);
            if (Input.GetKeyDown(KeyCode.D))
                Rotate(-rotateStep);
        }
    }
    #endregion

    #region Gameplay

    // sfx를 카메라로부터 z축 +5f 위치에서 재생 (x,y는 원래 값 유지)
    void PlayPuzzleSfxRelativeToCamera(AudioClip sfx, float volume)
    {
        var cam = Camera.main;
        if (!cam || !sfx) return;

        Vector3 p = transform.position;
        p.z = cam.transform.position.z + 5f;
        AudioSource.PlayClipAtPoint(sfx, p, volume);
    }

    void Rotate(float delta)
    {
        if (sfxRotate) PlayPuzzleSfxRelativeToCamera(sfxRotate, 1f);

        var z = Mathf.Round((transform.eulerAngles.z + delta) / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0, 0, z % 360f);
    }

    void TrySnap()
    {
        if (snapTarget == null) return;

        float dist = Vector2.Distance(transform.position, snapTarget.position);
        float angle = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, snapTarget.eulerAngles.z));

        if (dist <= snapPosTolerance && angle <= snapAngleTolerance)
        {
            // 정답 스냅
            transform.position = snapTarget.position;
            transform.rotation = Quaternion.Euler(0, 0, snapTarget.eulerAngles.z);
            placed = true;

            // 더 이상 상호작용 안 되게
            var col = GetComponent<Collider2D>();
            if (col) col.enabled = false;

            // 스냅 소리
            if (sfxSnap) PlayPuzzleSfxRelativeToCamera(sfxSnap, 0.8f);

            if (PuzzleManager.Instance) PuzzleManager.Instance.NotifyPlaced(this);
        }
    }
    #endregion

    #region Sorting (BringToTop + 재배치)

    // index(0..N-1) → main sorting order
    private static int SlotOrder(int index)
    {
        // 예: BASE=4 → main: 4,7,10,13,...
        return BASE_ORDER + STEP_ORDER * index;
    }

    // index(0..N-1) → shadow sorting order (항상 main-1)
    private static int SlotOrderShadow(int index)
    {
        return SlotOrder(index) - 1;
    }

    // index(0..N-1) → crack sorting order (항상 main+1)
    private static int SlotOrderCrack(int index)
    {
        return SlotOrder(index) + 1;
    }

    // 현재 본체 sortingOrder 기준으로 낮은→높은 정렬
    private static void SortByCurrentOrder(List<PuzzlePiece> list)
    {
        list.Sort((a, b) =>
        {
            int ao = a.mainRenderer ? a.mainRenderer.sortingOrder : int.MinValue;
            int bo = b.mainRenderer ? b.mainRenderer.sortingOrder : int.MinValue;
            return ao.CompareTo(bo);
        });
    }

    // 현재 오브젝트가 맨 위가 되도록 재배치
    // 그림자: main-1, 크랙: main+1 유지
    private static void BringToTop(PuzzlePiece selected)
    {
        // 1) 현재 눈에 보이는 순서를 정렬 기준으로 확정(낮은→높은)
        SortByCurrentOrder(puzzleList_all);

        var work = new List<PuzzlePiece>(puzzleList_all);

        // 2) 선택 항목을 제거 후 맨 뒤로 삽입
        work.Remove(selected);
        work.Add(selected);

        if (work.Count != SLOT_COUNT)
        {
            Debug.LogWarning($"[PuzzlePiece] 슬롯 수({SLOT_COUNT})와 활성 조각 수({work.Count})가 다릅니다!");
        }

        // 3) 슬롯 재할당
        for (int i = 0; i < work.Count; i++)
        {
            int mainOrder = SlotOrder(i);
            int shadOrder = SlotOrderShadow(i);
            int crackOrder = SlotOrderCrack(i);

            var p = work[i];
            if (p.mainRenderer) p.mainRenderer.sortingOrder = mainOrder;
            if (p.shadowRenderer) p.shadowRenderer.sortingOrder = shadOrder;
            if (p.crackRenderer) p.crackRenderer.sortingOrder = crackOrder;
            if (p.mask)
            {
                p.mask.backSortingOrder = shadOrder; // main-1
                p.mask.frontSortingOrder = crackOrder; // main+1
            }
        }
    }

    // 에디터 초기화용: 계층 순서대로 BASE, BASE+STEP, ... 할당 (shadow: -1, crack: +1)
    [ContextMenu("Normalize Sorting Orders (BASE, BASE+STEP, … with shadow/crack offsets)")]
    private void NormalizeSortingOrders()
    {
        puzzleList_all.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        for (int i = 0; i < puzzleList_all.Count; i++)
        {
            int mainOrder = SlotOrder(i);
            int shadOrder = SlotOrderShadow(i);
            int crackOrder = SlotOrderCrack(i);

            var p = puzzleList_all[i];
            if (p.mainRenderer) p.mainRenderer.sortingOrder = mainOrder;
            if (p.shadowRenderer) p.shadowRenderer.sortingOrder = shadOrder;
            if (p.crackRenderer) p.crackRenderer.sortingOrder = crackOrder;
            if (p.mask)
            {
                p.mask.backSortingOrder = shadOrder; // main-1
                p.mask.frontSortingOrder = crackOrder; // main+1
            }
        }
    }
    #endregion

    #region Shadow helper
    private void SetShadowAlpha(float a)
    {
        if (!shadowRenderer) return;
        var c = shadowRenderer.color;
        c.r = shadowColor.r; c.g = shadowColor.g; c.b = shadowColor.b;
        c.a = Mathf.Clamp01(a);
        shadowRenderer.color = c;

        // 항상 본체보다 한 단계 아래 유지
        if (mainRenderer)
            shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
    }
    #endregion
}
