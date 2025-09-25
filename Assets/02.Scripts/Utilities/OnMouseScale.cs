using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnMouseScale : MonoBehaviour
{
    public float originalScale = 1f;
    public float scaleMultiplier = 1.2f;
    private RectTransform rectTransform;
    [SerializeField] private bool checkMouseHover;
    private Canvas rootCanvas;      // 이 UI가 속한 최상위 캔버스
    private Camera uiCamera;

    [SerializeField] private float scaleDuration = 0.5f;

    public void PointerEnter()
    {
        transform.localScale = new Vector3(originalScale * scaleMultiplier, originalScale * scaleMultiplier,1f);
    }

    public void PointerExit()
    {
        transform.localScale = new Vector3(originalScale, originalScale, 1f);
    }

    void Start()
    {
        if (!checkMouseHover)
            return;

        rectTransform = GetComponent<RectTransform>();
        if (!rectTransform)
        {
            Debug.LogError("RectTransform not found on the object.");
            enabled = false;
            return;
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (!rootCanvas)
        {
            Debug.LogError("No parent Canvas found.");
            enabled = false;
            return;
        }

        // Canvas 타입에 따라 카메라 설정
        switch (rootCanvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                uiCamera = null; // 중요: Overlay는 null이어야 함
                break;
            case RenderMode.ScreenSpaceCamera:
            case RenderMode.WorldSpace:
                uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
                break;
        }
    }

    void Update()
    {
        if (!checkMouseHover || !rectTransform)
            return;

        // if touching mouse pointer
        bool isPointerOverUI = RectTransformUtility.RectangleContainsScreenPoint(rectTransform,
           Input.mousePosition,
           uiCamera);

        if (isPointerOverUI)
            PointerEnter();
        else
            PointerExit();
    }

    IEnumerator ChangeLocalSize(Vector3 changedSize)
    {
        float elapsedTime = 0f;
        float duration = scaleDuration;

        Vector3 startSize = transform.localScale;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(startSize, changedSize, (elapsedTime/duration));

            yield return null;
        }

        transform.localScale = changedSize;
    }

}
