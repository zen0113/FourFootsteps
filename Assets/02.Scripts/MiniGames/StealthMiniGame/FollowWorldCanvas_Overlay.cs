using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FollowWorldCanvas_Overlay : MonoBehaviour
{
    [Header("setting")]
    public bool followX = true;
    public bool followY = false;

    [Header("Refs")]
    [Tooltip("targetCanvas는 꼭 직접 할당!")]
    public Transform targetCanvas;         // World Space(혹은 카메라 자식) 캔버스 Transform
    public Camera targetCamera;             // 보통 Main Camera
    public Canvas overlayCanvas;            // Screen Space - Overlay 캔버스

    [Header("Behavior")]
    public bool clampToCanvas = true;       // 오버레이 캔버스 영역 안에서 클램프
    public float paddingPx = 40f;           // 좌/우 여백(픽셀)
    public bool smooth = false;
    public float smoothSpeed = 10f;         // 부드럽게 이동 시

    [Tooltip("오버레이 좌표계 기준 오프셋(px). +는 위, -는 아래")]
    [Header("Offsets (px)")]
    public float xOffsetPx = 0f;
    public float yOffsetPx = 0f;

    [Header("Init Options")]
    public bool snapOnStart = true;        // Start에서 1회 스냅
    public bool snapOnEnable = true;       // OnEnable에서 1회 스냅
    public bool snapOnBecomeVisible = true;// 활성화 직후(레이아웃 확정 후) 1회 스냅


    private RectTransform _self;            // 이 스크립트가 붙은 오버레이 UI의 RectTransform
    private RectTransform _overlayRect;     // 오버레이 캔버스의 RectTransform

    void Awake()
    {
        _self = GetComponent<RectTransform>();
        if (!overlayCanvas) overlayCanvas = GetComponentInParent<Canvas>();
        if (!targetCamera) targetCamera = Camera.main;
        if (overlayCanvas && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            Debug.LogWarning("overlayCanvas는 Screen Space - Overlay 권장");
        _overlayRect = overlayCanvas ? overlayCanvas.GetComponent<RectTransform>() : null;
    }

    void Start()
    {
        if (snapOnStart) StartCoroutine(SnapAtEndOfFrameOnce());
    }

    void OnEnable()
    {
        if (snapOnEnable) StartCoroutine(SnapAtEndOfFrameOnce());
    }

    // 외부에서 UI를 SetActive(true)로 켜줄 때 호출하면 좋음
    public void RefreshNow(bool instant = true)
    {
        // 즉시 1회 적용 (EndOfFrame 기다리지 않고)
        UpdateOverlayPosition(instant);
        // 혹시 레이아웃이 늦게 반영되면 다음 프레임 말에 한 번 더 스냅
        if (snapOnBecomeVisible) StartCoroutine(SnapAtEndOfFrameOnce());
    }

    IEnumerator SnapAtEndOfFrameOnce()
    {
        // 레이아웃/캔버스 갱신 이후 안전하게 스냅
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        UpdateOverlayPosition(true); // instant=true (보간 없이 스냅)
    }

    //void LateUpdate()
    //{
    //    if (!targetCanvas || !_self || !_overlayRect || !targetCamera) return;

    //    // 1) 월드 → 스크린
    //    Vector3 worldPos = targetCanvas.position;
    //    Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

    //    // 2) 스크린 → 오버레이 캔버스 로컬 좌표
    //    //    Overlay 모드이므로 camera 인자는 null!
    //    Vector2 localOnOverlay;
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        _overlayRect, screenPos, null, out localOnOverlay);

    //    // 3) followX -> x 따라감. followY -> y 따라감.
    //    Vector2 target = _self.anchoredPosition; // 초기값: 현재 값 유지

    //    if (followX) target.x = localOnOverlay.x + xOffsetPx;

    //    if (followY) target.y = localOnOverlay.y + yOffsetPx;

    //    // 4) 오버레이 캔버스 경계로 클램프(옵션)
    //    if (clampToCanvas)
    //    {
    //        Rect r = _overlayRect.rect; // 로컬 좌표계
    //        if (followX)
    //        {
    //            float minX = r.xMin + paddingPx;
    //            float maxX = r.xMax - paddingPx;
    //            target.x = Mathf.Clamp(target.x, minX, maxX);
    //        }

    //        if (followY)
    //        {
    //            float minY = r.yMin + paddingPx;
    //            float maxY = r.yMax - paddingPx;
    //            target.y = Mathf.Clamp(target.y, minY, maxY);
    //        }
    //    }

    //    // 5) 적용
    //    Vector2 ap = _self.anchoredPosition;
    //    ap.x = followX ? (smooth ? Mathf.Lerp(ap.x, target.x, Time.deltaTime * smoothSpeed) : target.x) : ap.x;
    //    ap.y = followY ? (smooth ? Mathf.Lerp(ap.y, target.y, Time.deltaTime * smoothSpeed) : target.y) : ap.y;
    //    _self.anchoredPosition = ap;
    //}

    void LateUpdate()
    {
        UpdateOverlayPosition(false); // 평소엔 기존 설정대로(smooth 플래그에 따름)
    }

    void UpdateOverlayPosition(bool instant)
    {
        if (!targetCanvas || !_self || !_overlayRect || !targetCamera) return;

        // 1) 월드 → 스크린
        Vector3 screenPos = targetCamera.WorldToScreenPoint(targetCanvas.position);
        //if (screenPos.z < 0f) return; // 카메라 뒤면 스킵

        // 2) 스크린 → 오버레이 로컬
        Vector2 localOnOverlay;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, screenPos, null, out localOnOverlay);

        // 3) 타겟 좌표(기본은 현재 값)
        Vector2 target = _self.anchoredPosition;
        if (followX) target.x = localOnOverlay.x + xOffsetPx;
        if (followY) target.y = localOnOverlay.y + yOffsetPx;

        // 4) 클램프
        if (clampToCanvas)
        {
            Rect r = _overlayRect.rect;
            if (followX) target.x = Mathf.Clamp(target.x, r.xMin + paddingPx, r.xMax - paddingPx);
            if (followY) target.y = Mathf.Clamp(target.y, r.yMin + paddingPx, r.yMax - paddingPx);
        }

        // 5) 적용
        if (instant || !smooth)
        {
            _self.anchoredPosition = target;
        }
        else
        {
            Vector2 ap = _self.anchoredPosition;
            ap.x = followX ? Mathf.Lerp(ap.x, target.x, Time.deltaTime * smoothSpeed) : ap.x;
            ap.y = followY ? Mathf.Lerp(ap.y, target.y, Time.deltaTime * smoothSpeed) : ap.y;
            _self.anchoredPosition = ap;
        }
    }

}
