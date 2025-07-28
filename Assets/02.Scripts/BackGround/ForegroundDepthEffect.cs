using UnityEngine;

public class ForegroundDepthEffect : MonoBehaviour
{
    [Header("Depth 설정")]
    [SerializeField] private float depthDistance;
    [SerializeField] private string sortingLayerName = "Foreground";
    [SerializeField] private int sortingOrder;
    
    [Header("Parallax 설정")]
    [SerializeField] private bool useParallax = false;
    [SerializeField] private float parallaxSpeed;
    [SerializeField] private Transform cameraTransform;
    
    [Header("블러 효과 설정")]
    [SerializeField] private bool applyBlur = true;
    [SerializeField] private Material blurMaterial; // 블러 셰이더 머티리얼
    [SerializeField] private float blurSize = 2f; // 블러 크기
    [SerializeField] private float alphaValue = 0.7f;
    [SerializeField] private Color tintColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Vector3 startPosition;
    private float startCameraX;
    private Material blurMaterialInstance;
    
    void Start()
    {
        InitializeComponents();
        SetupDepthAndSorting();
        SetupBlurEffect();
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
        
        originalMaterial = spriteRenderer.material;
        
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }
    
    void SetupDepthAndSorting()
    {
        Vector3 pos = transform.position;
        pos.z = depthDistance;
        transform.position = pos;
        
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
    }
    
    void SetupBlurEffect()
    {
        if (!applyBlur || blurMaterial == null) return;
        
        // 머티리얼 인스턴스 생성 (각 오브젝트마다 독립적인 설정을 위해)
        blurMaterialInstance = new Material(blurMaterial);
        spriteRenderer.material = blurMaterialInstance;
        
        // 초기 블러 설정
        UpdateBlurSettings(blurSize, alphaValue, tintColor);
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
    
    void UpdateBlurSettings(float blur, float alpha, Color color)
    {
        if (blurMaterialInstance == null) return;
        
        blurMaterialInstance.SetFloat("_BlurSize", blur);
        blurMaterialInstance.SetFloat("_Alpha", alpha);
        blurMaterialInstance.SetColor("_Color", color);
    }
    
    // 런타임에서 블러 조절하는 공개 메서드들
    public void SetBlurSize(float newBlurSize)
    {
        blurSize = newBlurSize;
        if (blurMaterialInstance != null)
            blurMaterialInstance.SetFloat("_BlurSize", blurSize);
    }
    
    public void SetAlpha(float newAlpha)
    {
        alphaValue = newAlpha;
        if (blurMaterialInstance != null)
            blurMaterialInstance.SetFloat("_Alpha", alphaValue);
    }
    
    public void ToggleBlur(bool enable)
    {
        if (enable && blurMaterialInstance != null)
            spriteRenderer.material = blurMaterialInstance;
        else
            spriteRenderer.material = originalMaterial;
    }
    
    void OnDestroy()
    {
        // 메모리 누수 방지
        if (blurMaterialInstance != null)
            DestroyImmediate(blurMaterialInstance);
    }
}