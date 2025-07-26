using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForegroundDepthEffect : MonoBehaviour
{
    [Header("Depth 설정")]
    [SerializeField] private float depthDistance = -3f; // 카메라에서의 거리
    [SerializeField] private string sortingLayerName = "Foreground";
    [SerializeField] private int sortingOrder = 100;
    
    [Header("Parallax 설정")]
    [SerializeField] private bool useParallax = true;
    [SerializeField] private float parallaxSpeed = 1.3f; // 1보다 크면 더 빠르게 움직임
    [SerializeField] private Transform cameraTransform;
    
    [Header("블러 효과 설정")]
    [SerializeField] private bool applyBlur = true;
    [SerializeField] private float alphaValue = 0.6f; // 투명도
    [SerializeField] private Color tintColor = Color.black; // 어둡게 만들기
    [SerializeField] private float tintStrength = 0.3f;
    
    [Header("흔들림 효과 (선택사항)")]
    [SerializeField] private bool addSway = false;
    [SerializeField] private float swayAmount = 0.1f;
    [SerializeField] private float swaySpeed = 1f;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float startCameraX;
    private Vector3 originalLocalPosition;
    
    void Start()
    {
        InitializeComponents();
        SetupDepthAndSorting();
        SetupVisualEffects();
        SetupParallax();
    }
    
    void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer가 필요합니다!");
            return;
        }
        
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        originalLocalPosition = transform.localPosition;
    }
    
    void SetupDepthAndSorting()
    {
        // Z Position으로 깊이 설정
        Vector3 pos = transform.position;
        pos.z = depthDistance;
        transform.position = pos;
        
        // Sorting Layer 설정
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
    }
    
    void SetupVisualEffects()
    {
        if (!applyBlur) return;
        
        // 색상과 투명도 조절
        Color originalColor = spriteRenderer.color;
        Color blendedColor = Color.Lerp(originalColor, tintColor, tintStrength);
        blendedColor.a = alphaValue;
        spriteRenderer.color = blendedColor;
        
        // 블러 머티리얼이 있다면 적용
        // (별도의 블러 셰이더 머티리얼을 만들어 적용할 수 있습니다)
    }
    
    void SetupParallax()
    {
        if (!useParallax || cameraTransform == null) return;
        
        startPosition = transform.position;
        startCameraX = cameraTransform.position.x;
    }
    
    void Update()
    {
        if (useParallax)
            ApplyParallaxEffect();
            
        if (addSway)
            ApplySwayEffect();
    }
    
    void ApplyParallaxEffect()
    {
        if (cameraTransform == null) return;
        
        float cameraMovement = cameraTransform.position.x - startCameraX;
        float parallaxMovement = cameraMovement * parallaxSpeed;
        
        transform.position = new Vector3(
            startPosition.x + parallaxMovement,
            transform.position.y,
            transform.position.z
        );
    }
    
    void ApplySwayEffect()
    {
        // 미세한 흔들림 효과 (바람에 흔들리는 느낌)
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(Time.time * swaySpeed * 0.5f) * swayAmount * 0.5f;
        
        Vector3 swayOffset = new Vector3(swayX, swayY, 0);
        transform.localPosition = originalLocalPosition + swayOffset;
    }
    
    void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 깊이 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.forward * 0.1f, Vector3.one * 0.5f);
        
        if (Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cameraPos, transform.position);
        }
    }
}

// 추가: 블러 셰이더용 간단한 머티리얼 컨트롤러
[System.Serializable]
public class BlurSettings
{
    public Material blurMaterial;
    public float blurRadius = 2f;
    public int blurIterations = 3;
}

// 전경 오브젝트 매니저 (여러 오브젝트를 한번에 관리)
public class ForegroundManager : MonoBehaviour
{
    [Header("전경 오브젝트 관리")]
    [SerializeField] private ForegroundDepthEffect[] foregroundObjects;
    [SerializeField] private bool autoFindForegroundObjects = true;
    
    [Header("글로벌 설정")]
    [SerializeField] private float globalAlphaMultiplier = 1f;
    [SerializeField] private bool enableAllParallax = true;
    
    void Start()
    {
        if (autoFindForegroundObjects)
        {
            foregroundObjects = FindObjectsOfType<ForegroundDepthEffect>();
        }
        
        ApplyGlobalSettings();
    }
    
    void ApplyGlobalSettings()
    {
        foreach (var obj in foregroundObjects)
        {
            if (obj != null)
            {
                // 글로벌 설정 적용
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a *= globalAlphaMultiplier;
                    sr.color = color;
                }
            }
        }
    }
    
    // 런타임에서 전경 효과 on/off
    public void ToggleForegroundEffects(bool enable)
    {
        foreach (var obj in foregroundObjects)
        {
            if (obj != null)
                obj.enabled = enable;
        }
    }
}
