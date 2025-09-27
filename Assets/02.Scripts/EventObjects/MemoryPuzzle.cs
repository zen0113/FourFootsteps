using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MemoryPuzzle : EventObject, IResultExecutable
{
    [Header("puzzle setting")]
    public string puzzleId;
    [SerializeField] 
    private GameObject puzzleObject;
    [SerializeField]
    private RectTransform puzzleBagButton;

    [Header("MoveToUIPosition Animating")]
    public float durationTime = 2f;     // 이동 시간
    [SerializeField]
    private AnimationCurve animationCurve =
    AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    private new void Start()
    {
        base.Start();
        if (!string.IsNullOrEmpty(eventId))
        {
            ResultManager.Instance.RegisterExecutable(eventId, this);
        }
        else
        {
            // eventId가 설정되지 않은 경우를 대비한 경고 메시지
            Debug.LogWarning($"'{gameObject.name}' 오브젝트의 eventId가 비어있어 ResultManager에 등록할 수 없습니다.");
        }

        puzzleObject = this.gameObject;

        if (puzzleBagButton == null)
        {
            puzzleBagButton = UIManager.Instance.puzzleBagButton.GetComponent<RectTransform>();
        }
    }

    private new void Update()
    {
        base.Update();
    }

    public new void Investigate()
    {
        base.Investigate();
    }

    public void ExecuteAction()
    {
        StartCoroutine(MoveToUIPosition(puzzleObject.transform, puzzleBagButton, durationTime));
    }

    private IEnumerator MoveToUIPosition(Transform puzzleObj, RectTransform uiTarget, float duration)
    {
        GameManager.Instance.SetVariable("isPuzzleMoving", true);
        GameManager.Instance.SetVariable("CanMoving",false);        // 플레이어 동작 제한

        //PlayerCatMovement.Instance.RestAnimationState();

        // 1) UI용 카메라 결정 (Overlay면 null, 나머지는 canvas.worldCamera)
        Canvas canvas = uiTarget.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        // 2) UI 타깃의 스크린 좌표 (Pivot 기준)
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCam, uiTarget.position);

        // 3) 퍼즐 오브젝트의 z(깊이)는 기존 그대로 유지
        Camera worldCam = Camera.main;
        Vector3 startWorld = puzzleObj.position;
        Vector3 startScreen3 = worldCam.WorldToScreenPoint(startWorld);
        Vector2 startScreen = new Vector2(startScreen3.x, startScreen3.y);
        float depthZ = startScreen3.z; // 애니 중 내내 유지할 z(깊이)

        Vector3 startScale = puzzleObj.localScale;
        Vector3 endScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tNormalized = Mathf.Clamp01(elapsed / duration);
            float t = animationCurve.Evaluate(tNormalized);

            // 스크린 공간에서 보간(카메라가 움직여도 스크린 포인트는 안정적)
            Vector2 curScreen = Vector2.Lerp(startScreen, targetScreen, t);

            // 매 프레임 현재 카메라로 스크린→월드 환산
            Vector3 curWorld = worldCam.ScreenToWorldPoint(new Vector3(curScreen.x, curScreen.y, depthZ));

            puzzleObj.position = curWorld;
            puzzleObj.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }
        Vector3 finalWorld = worldCam.ScreenToWorldPoint(new Vector3(targetScreen.x, targetScreen.y, depthZ));
        puzzleObj.position = finalWorld;
        puzzleObj.localScale = endScale;
        puzzleObj.gameObject.SetActive(false);

        GameManager.Instance.SetVariable("isPuzzleMoving", false);
        GameManager.Instance.SetVariable("CanMoving", true);// 플레이어 동작 제한 해제
    }

}
