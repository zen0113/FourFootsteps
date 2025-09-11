using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("패럴랙스 설정")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float parallaxSpeed = 0.5f; // 0~1 사이 값
    [SerializeField] private bool infiniteScroll = true;
    
    [Header("무한 스크롤 설정")]
    [SerializeField] private float backgroundWidth;
    
    private Vector3 startPosition;
    private float startCameraX;
    
    void Start()
    {
        // 카메라 자동 찾기
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        startPosition = transform.position;
        startCameraX = cameraTransform.position.x;
        
        // 배경 너비 자동 계산
        if (backgroundWidth == 0)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                backgroundWidth = spriteRenderer.bounds.size.x;
        }
    }
    
    void Update()
    {
        // 카메라 이동량 계산
        float cameraMovement = cameraTransform.position.x - startCameraX;
        
        // 패럴랙스 효과 적용
        float parallaxMovement = cameraMovement * parallaxSpeed;
        
        // 배경 위치 업데이트
        transform.position = new Vector3(
            startPosition.x + parallaxMovement,
            transform.position.y,
            transform.position.z
        );
        
        // 무한 스크롤 처리
        if (infiniteScroll)
        {
            HandleInfiniteScroll();
        }
    }
    
    void HandleInfiniteScroll()
    {
        float distanceFromCamera = transform.position.x - cameraTransform.position.x;
        
        // 화면 밖으로 너무 멀리 나갔을 때 위치 재조정
        if (distanceFromCamera > backgroundWidth)
        {
            transform.position = new Vector3(
                transform.position.x - backgroundWidth * 2,
                transform.position.y,
                transform.position.z
            );
        }
        else if (distanceFromCamera < -backgroundWidth)
        {
            transform.position = new Vector3(
                transform.position.x + backgroundWidth * 2,
                transform.position.y,
                transform.position.z
            );
        }
    }
}
