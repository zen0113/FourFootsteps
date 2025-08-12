using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("카메라 제어 설정")]
    public CameraZoneType zoneType = CameraZoneType.Follow;
    
    [Header("고정 위치 설정 (Fixed 타입만)")]
    public Vector2 fixedPosition;
    public bool useCurrentPositionAsFixed = false;
    
    [Header("줌 설정")]
    public bool changeZoom = false;
    public float targetZoomSize = 5f;
    
    [Header("전환 시간 설정")]
    public float positionTransitionTime = 1f;
    public float zoomTransitionTime = 1f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("디버그")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    
    private FollowCamera followCamera;
    private bool isPlayerInZone = false;
    
    void Start()
    {
        followCamera = FindObjectOfType<FollowCamera>();
        
        if (followCamera == null)
        {
            Debug.LogError("FollowCamera를 찾을 수 없습니다!");
            return;
        }
        
        // 현재 위치를 고정 위치로 설정
        if (useCurrentPositionAsFixed)
        {
            fixedPosition = transform.position;
        }
        
        // 콜라이더 설정 확인
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Collider2D가 없습니다. BoxCollider2D를 추가합니다.");
            gameObject.AddComponent<BoxCollider2D>();
        }
        
        GetComponent<Collider2D>().isTrigger = true;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPlayerInZone)
        {
            isPlayerInZone = true;
            EnterZone();
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isPlayerInZone)
        {
            isPlayerInZone = false;
            ExitZone();
        }
    }
    
    void EnterZone()
    {
        if (followCamera == null) return;
        
        // CameraController에 구역 진입 알림
        CameraController cameraController = followCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.EnterCameraZone(this);
        }
        
        string zoomInfo = changeZoom ? $" (줌: {targetZoomSize})" : "";
        Debug.Log($"카메라 구역 진입: {gameObject.name} ({zoneType}){zoomInfo}");
    }
    
    void ExitZone()
    {
        if (followCamera == null) return;
        
        // CameraController에 구역 이탈 알림
        CameraController cameraController = followCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.ExitCameraZone(this);
        }
        
        Debug.Log($"카메라 구역 이탈: {gameObject.name}");
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        // 콜라이더 영역 표시
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
                
                // 반투명 영역 표시
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
                Gizmos.DrawCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
                
                // 반투명 영역 표시
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
                Gizmos.DrawSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }
        
        // 타입별 아이콘과 정보 표시
        Gizmos.color = Color.white;
        Vector3 iconPos = transform.position + Vector3.up * 0.5f;
        
        switch (zoneType)
        {
            case CameraZoneType.Fixed:
                // 고정 카메라 아이콘
                Gizmos.DrawWireCube(iconPos, Vector3.one * 0.3f);
                
                // 고정 위치 표시
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere((Vector3)fixedPosition, 0.2f);
                Gizmos.DrawLine(transform.position, fixedPosition);
                break;
                
            case CameraZoneType.Follow:
                // 추적 카메라 아이콘
                Gizmos.DrawWireSphere(iconPos, 0.15f);
                break;
        }
        
        // 줌 변경 표시
        if (changeZoom)
        {
            Gizmos.color = Color.cyan;
            Vector3 zoomIconPos = iconPos + Vector3.right * 0.5f;
            
            // 줌 레벨에 따라 아이콘 크기 변경
            float iconScale = targetZoomSize < 5f ? 0.2f : 0.4f; // 줌 인이면 작은 아이콘, 줌 아웃이면 큰 아이콘
            Gizmos.DrawWireCube(zoomIconPos, Vector3.one * iconScale);
        }
        
        // 에디터에서 텍스트 표시
        #if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 1f;
        UnityEditor.Handles.color = Color.yellow;
        
        string label = $"{zoneType}";
        if (changeZoom)
            label += $"\nZoom: {targetZoomSize:F1}";
            
        UnityEditor.Handles.Label(labelPos, label);
        #endif
    }
    
    void OnValidate()
    {
        // 에디터에서 useCurrentPositionAsFixed 체크시 즉시 적용
        if (useCurrentPositionAsFixed)
        {
            fixedPosition = transform.position;
            useCurrentPositionAsFixed = false; // 한 번만 실행되도록
        }
    }
}

public enum CameraZoneType
{
    Follow,     // 플레이어 추적
    Fixed       // 고정 위치
}
