using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PuzzlePiece : MonoBehaviour
{
    public int id = 0;
    public Transform snapTarget;
    public float snapPosTolerance = 0.35f;
    public float snapAngleTolerance = 10f;
    public float rotateStep = 90f;
    private float clickedScale = 1.05f;
    public AudioClip sfxSnap;

    private bool placed = false;
    public bool IsPlaced => placed;
    private Vector3 grabOffset;
    private Camera cam;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PuzzlePieceShadow shadow;

    // --- Sorting 설정 ---
    private const int MIN_ORDER = 5;
    private const int MAX_ORDER = 9; // 총 5단계 (5,6,7,8,9)
    private static readonly List<PuzzlePiece> puzzleList_all = new List<PuzzlePiece>();

    // 현재 선택 여부 (A/D 회전용)
    private static PuzzlePiece current;

    void Awake()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
        shadow = GetComponent<PuzzlePieceShadow>();

        if (!puzzleList_all.Contains(this)) puzzleList_all.Add(this);
    }

    private void OnDestroy()
    {
        puzzleList_all.Remove(this);
    }


    void OnMouseDown()
    {
        if (placed) return;

        // 퍼즐 선택 연출
        transform.localScale = transform.localScale * clickedScale;
        shadow.SetSelected(true);

        // 정렬: 이 조각을 맨 위(9)로, 나머지는 한 칸씩 뒤로
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
        shadow.SetSelected(false);
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

    void Rotate(float delta)
    {
        var z = Mathf.Round((transform.eulerAngles.z + delta) / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0, 0, z % 360f);
    }

    void TrySnap()
    {
        if (snapTarget == null) return;

        float dist = Vector2.Distance(transform.position, snapTarget.position);
        float angle = AngleDelta(transform.eulerAngles.z, snapTarget.eulerAngles.z);

        if (dist <= snapPosTolerance && angle <= snapAngleTolerance)
        {
            // 정답 스냅
            transform.position = snapTarget.position;
            transform.rotation = Quaternion.Euler(0, 0, snapTarget.eulerAngles.z);
            placed = true;

            // 더 이상 상호작용 안 되게
            GetComponent<Collider2D>().enabled = false;

            // 스냅 소리
            if (sfxSnap) AudioSource.PlayClipAtPoint(sfxSnap, transform.position, 0.8f);

            PuzzleManager.Instance.NotifyPlaced(this);
        }
    }

    float AngleDelta(float a, float b)
    {
        float d = Mathf.Abs(Mathf.DeltaAngle(a, b));
        return d;
    }


    // 현재 오브젝트가 맨 위(9)가 되도록 재배치
    private static void BringToTop(PuzzlePiece selected)
    {
        // 1) 현재 리스트를 sortingOrder 기준으로 정렬(낮은→높은)
        puzzleList_all.Sort((a, b) => a.sr.sortingOrder.CompareTo(b.sr.sortingOrder));

        // 2) 선택 항목을 제거 후 맨 뒤로 삽입
        puzzleList_all.Remove(selected);
        puzzleList_all.Add(selected);

        // 3) 5~9로 일괄 재할당 (리스트 순서대로 5,6,7,8,9)
        int count = puzzleList_all.Count;
        int slots = MAX_ORDER - MIN_ORDER + 1;
        if (count > slots)
        {
            Debug.LogWarning($"[PuzzlePiece] 조각 수({count})가 정렬 슬롯({slots})보다 큽니다. 초과분은 같은 오더를 가질 수 있어요.");
        }
        for (int i = 0; i < puzzleList_all.Count; i++)
        {
            int order = MIN_ORDER + Mathf.Min(i, slots - 1); // 초과 시 MAX_ORDER로 고정
            puzzleList_all[i].sr.sortingOrder = order;
        }
    }

}
