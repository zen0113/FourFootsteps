using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionImageDisplay : MonoBehaviour
{
    [Header("영역 설정")]
    [SerializeField] private Vector2 areaSize = new Vector2(3f, 4f);
    [SerializeField] private Vector2 areaOffset = Vector2.zero;
    
    [Header("이미지 설정")]
    [SerializeField] private Sprite interactionSprite;
    [SerializeField] private Vector2 imagePosition = new Vector2(0f, 1f);
    [SerializeField] private Vector2 imageScale = Vector2.one;
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private string sortingLayerName = "Default";
    
    [Header("기타 설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showGizmosInScene = true;
    
    // 이미지 오브젝트
    private GameObject imageObject;
    private SpriteRenderer imageRenderer;
    [SerializeField] private bool isPlayerInArea = false;
    
    void Start()
    {
        // Collider2D 설정
        SetupCollider();
        // 이미지 오브젝트 생성
        CreateImageObject();
    }
    
    void SetupCollider()
    {
        // BoxCollider2D가 없으면 추가
        BoxCollider2D areaCollider = GetComponent<BoxCollider2D>();
        if (areaCollider == null)
        {
            areaCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Trigger로 설정
        areaCollider.isTrigger = true;
        areaCollider.size = areaSize;
        areaCollider.offset = areaOffset;
    }
    
    void CreateImageObject()
    {
        if (interactionSprite == null) return;
        
        // 자식 오브젝트로 이미지 생성
        imageObject = new GameObject("InteractionImage_" + gameObject.name);
        imageObject.transform.SetParent(transform);
        
        // SpriteRenderer 추가
        imageRenderer = imageObject.AddComponent<SpriteRenderer>();
        imageRenderer.sprite = interactionSprite;
        imageRenderer.sortingOrder = sortingOrder;
        imageRenderer.sortingLayerName = sortingLayerName;
        
        // 위치 및 스케일 설정
        UpdateImageTransform();
        
        // 처음에는 비활성화
        imageObject.SetActive(false);
    }
    
    void UpdateImageTransform()
    {
        if (imageObject == null) return;
        
        // 로컬 위치 설정 (부모 기준)
        imageObject.transform.localPosition = imagePosition;
        imageObject.transform.localScale = imageScale;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInArea = true;
            ShowImage();
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInArea = false;
            HideImage();
        }
    }
    
    void ShowImage()
    {
        if (imageObject != null && interactionSprite != null)
        {
            imageObject.SetActive(true);
        }
    }
    
    void HideImage()
    {
        if (imageObject != null)
        {
            imageObject.SetActive(false);
        }
    }
    
    // 인스펙터에서 값이 변경될 때 실시간 업데이트
    void OnValidate()
    {
        // 게임이 실행 중일 때만 업데이트
        if (Application.isPlaying)
        {
            SetupCollider();
            UpdateImageTransform();
            
            // 이미지나 정렬 설정이 변경되었을 때 업데이트
            if (imageRenderer != null)
            {
                imageRenderer.sprite = interactionSprite;
                imageRenderer.sortingOrder = sortingOrder;
                imageRenderer.sortingLayerName = sortingLayerName;
            }
        }
        
        // 에디터에서도 콜라이더 업데이트
        if (!Application.isPlaying)
        {
            SetupCollider();
        }
    }
    
    // Scene View에서 영역과 이미지 위치를 시각적으로 표시
    void OnDrawGizmos()
    {
        if (!showGizmosInScene) return;
        
        // 영역 표시 (반투명 녹색)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 areaCenter = transform.position + (Vector3)areaOffset;
        Gizmos.DrawCube(areaCenter, areaSize);
        
        // 영역 테두리 (녹색 선)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(areaCenter, areaSize);
        
        // 이미지 위치 표시 (빨간 점)
        Gizmos.color = Color.red;
        Vector3 imagePos = transform.position + (Vector3)imagePosition;
        Gizmos.DrawSphere(imagePos, 0.1f);
        
        // 이미지 위치에서 영역으로 선 연결
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(imagePos, areaCenter);
        
        // 이미지 스프라이트가 있으면 아웃라인 표시
        if (interactionSprite != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 spriteSize = new Vector3(
                interactionSprite.bounds.size.x * imageScale.x,
                interactionSprite.bounds.size.y * imageScale.y,
                0
            );
            Gizmos.DrawWireCube(imagePos, spriteSize);
        }
    }
    
    // 컴포넌트가 제거될 때 생성된 이미지 오브젝트 정리
    void OnDestroy()
    {
        if (imageObject != null)
        {
            if (Application.isPlaying)
                Destroy(imageObject);
            else
                DestroyImmediate(imageObject);
        }
    }
    
    // 공개 메서드들 (필요시 다른 스크립트에서 호출 가능)
    public void ForceShowImage()
    {
        ShowImage();
    }
    
    public void ForceHideImage()
    {
        HideImage();
    }
    
    public bool IsPlayerInArea()
    {
        return isPlayerInArea;
    }
}