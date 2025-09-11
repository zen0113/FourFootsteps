using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 줄(로프) 이미지의 특정 Y 좌표를 넘어가는 부분을 클리핑하는 컴포넌트
/// 도르레 시스템에서 줄이 도르레 연결부를 넘어가면 안 보이게 처리
/// </summary>
public class RopeClipping : MonoBehaviour
{
    [Header("클리핑 설정")]
    [SerializeField] private float clipYPosition = 0f; // 클리핑할 Y 위치 (월드 좌표)
    [SerializeField] private bool useLocalPosition = true; // 로컬 좌표 사용 여부
    [SerializeField] private Transform referenceTransform; // 기준이 되는 Transform (도르레 등)
    
    [Header("마스크 설정")]
    [SerializeField] private SpriteMask spriteMask; // Sprite Mask 컴포넌트
    [SerializeField] private RectMask2D rectMask2D; // UI용 RectMask2D (UI 캔버스일 경우)
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLine = true;
    [SerializeField] private Color debugLineColor = Color.red;
    
    private SpriteRenderer ropeRenderer;
    private Image ropeImage; // UI Image인 경우
    private bool isUIElement = false;
    
    private void Start()
    {
        InitializeComponents();
        SetupMask();
    }
    
    private void InitializeComponents()
    {
        // SpriteRenderer나 Image 컴포넌트 확인
        ropeRenderer = GetComponent<SpriteRenderer>();
        ropeImage = GetComponent<Image>();
        
        if (ropeImage != null)
        {
            isUIElement = true;
        }
        
        // 기준 Transform이 설정되지 않았으면 자신을 기준으로 설정
        if (referenceTransform == null)
        {
            referenceTransform = transform;
            Debug.LogWarning($"{gameObject.name}: referenceTransform이 설정되지 않아 자기 자신을 기준으로 설정했습니다.");
        }
    }
    
    private void SetupMask()
    {
        if (isUIElement)
        {
            SetupUIRectMask();
        }
        else
        {
            SetupSpriteMask();
        }
    }
    
    /// <summary>
    /// UI 요소용 RectMask2D 설정
    /// </summary>
    private void SetupUIRectMask()
    {
        // 부모에 RectMask2D가 없으면 생성
        if (rectMask2D == null)
        {
            GameObject maskObject = new GameObject("RopeMask");
            maskObject.transform.SetParent(transform.parent);
            
            // RectTransform 설정
            RectTransform maskRect = maskObject.AddComponent<RectTransform>();
            rectMask2D = maskObject.AddComponent<RectMask2D>();
            
            // 줄을 마스크 오브젝트의 자식으로 이동
            transform.SetParent(maskObject.transform);
        }
    }
    
    /// <summary>
    /// Sprite용 SpriteMask 설정
    /// </summary>
    private void SetupSpriteMask()
    {
        if (spriteMask == null)
        {
            GameObject maskObject = new GameObject("RopeSpriteMask");
            maskObject.transform.SetParent(transform.parent);
            spriteMask = maskObject.AddComponent<SpriteMask>();
            
            // 마스크 스프라이트 생성 (흰색 사각형)
            CreateMaskSprite();
        }
        
        // 줄의 SpriteRenderer를 마스크와 연동
        if (ropeRenderer != null)
        {
            ropeRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }
    }
    
    /// <summary>
    /// 마스크용 스프라이트 생성
    /// </summary>
    private void CreateMaskSprite()
    {
        // 1x1 흰색 텍스처 생성
        Texture2D maskTexture = new Texture2D(1, 1);
        maskTexture.SetPixel(0, 0, Color.white);
        maskTexture.Apply();
        
        // 스프라이트 생성
        Sprite maskSprite = Sprite.Create(maskTexture, 
                                         new Rect(0, 0, 1, 1), 
                                         new Vector2(0.5f, 0.5f), 
                                         100f);
        
        spriteMask.sprite = maskSprite;
    }
    
    private void Update()
    {
        UpdateClipping();
    }
    
    /// <summary>
    /// 클리핑 업데이트
    /// </summary>
    private void UpdateClipping()
    {
        float currentClipY = GetClipYPosition();
        
        if (isUIElement && rectMask2D != null)
        {
            UpdateUIClipping(currentClipY);
        }
        else if (spriteMask != null)
        {
            UpdateSpriteClipping(currentClipY);
        }
    }
    
    /// <summary>
    /// 클리핑할 Y 위치 계산
    /// </summary>
    private float GetClipYPosition()
    {
        if (useLocalPosition)
        {
            return referenceTransform.localPosition.y + clipYPosition;
        }
        else
        {
            return referenceTransform.position.y + clipYPosition;
        }
    }
    
    /// <summary>
    /// UI 요소 클리핑 업데이트
    /// </summary>
    private void UpdateUIClipping(float clipY)
    {
        RectTransform maskRect = rectMask2D.GetComponent<RectTransform>();
        RectTransform ropeRect = ropeImage.rectTransform;
        
        // 마스크 위치와 크기 조정
        Vector3 ropePos = ropeRect.position;
        Vector3 maskPos = ropePos;
        maskPos.y = clipY + 1000f; // 클리핑 라인 위쪽으로 충분히 높게
        
        maskRect.position = maskPos;
        maskRect.sizeDelta = new Vector2(ropeRect.sizeDelta.x + 100f, 2000f);
    }
    
    /// <summary>
    /// 스프라이트 클리핑 업데이트
    /// </summary>
    private void UpdateSpriteClipping(float clipY)
    {
        // 마스크를 클리핑 라인 위쪽에 배치 (위쪽 부분을 가림)
        Vector3 maskPosition = transform.position;
        maskPosition.y = clipY + 1000f; // 클리핑 라인 위쪽으로 충분히 높게
        
        spriteMask.transform.position = maskPosition;
        
        // 마스크 스케일 조정 (줄보다 넓게)
        float ropeWidth = ropeRenderer.bounds.size.x + 2f;
        spriteMask.transform.localScale = new Vector3(ropeWidth, 2000f, 1f);
    }
    
    /// <summary>
    /// 런타임에서 클리핑 Y 위치 설정
    /// </summary>
    public void SetClipYPosition(float newY)
    {
        clipYPosition = newY;
    }
    
    /// <summary>
    /// 기준 Transform 설정 (도르레 등)
    /// </summary>
    public void SetReferenceTransform(Transform newReference)
    {
        referenceTransform = newReference;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugLine) return;
        
        // 클리핑 라인 표시
        float clipY = GetClipYPosition();
        
        Gizmos.color = debugLineColor;
        Vector3 leftPoint = new Vector3(transform.position.x - 5f, clipY, transform.position.z);
        Vector3 rightPoint = new Vector3(transform.position.x + 5f, clipY, transform.position.z);
        
        Gizmos.DrawLine(leftPoint, rightPoint);
        
        // 클리핑 방향 표시 (화살표)
        Vector3 center = new Vector3(transform.position.x, clipY, transform.position.z);
        Vector3 arrowUp = center + Vector3.up * 0.5f;
        Vector3 arrowDown = center + Vector3.down * 0.5f;
        
        Gizmos.DrawLine(center, arrowUp);
        Gizmos.DrawLine(arrowUp, arrowUp + Vector3.left * 0.2f);
        Gizmos.DrawLine(arrowUp, arrowUp + Vector3.right * 0.2f);
        
        // "보이는 영역" 텍스트는 Scene 뷰에서만 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(arrowUp + Vector3.up * 0.5f, "보이는 영역");
        UnityEditor.Handles.Label(arrowDown + Vector3.down * 0.5f, "안 보이는 영역");
        #endif
    }
}