using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("기본 설정")]
    public Transform playerTransform;
    
    private FollowCamera followCamera;
    private Camera cameraComponent;
    private CameraZone currentZone;
    private CameraState currentState = CameraState.Following;
    
    // 원래 값들 저장
    private float originalZoomSize;
    private Vector2 originalOffset;
    private float originalSmoothSpeedX;
    private float originalSmoothSpeedY;
    
    // 전환 중인지 확인
    private bool isTransitioning = false;
    
    void Start()
    {
        followCamera = GetComponent<FollowCamera>();
        cameraComponent = GetComponent<Camera>();
        
        if (followCamera == null)
        {
            Debug.LogError("FollowCamera 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
        }
        
        // 원래 값들 저장
        SaveOriginalValues();
    }
    
    void SaveOriginalValues()
    {
        if (cameraComponent != null && cameraComponent.orthographic)
        {
            originalZoomSize = cameraComponent.orthographicSize;
        }
        
        if (followCamera != null)
        {
            originalOffset = followCamera.offset;
            originalSmoothSpeedX = followCamera.smoothSpeedX;
            originalSmoothSpeedY = followCamera.smoothSpeedY;
        }
    }
    
    public void EnterCameraZone(CameraZone zone)
    {
        if (isTransitioning) return;
        
        currentZone = zone;
        
        switch (zone.zoneType)
        {
            case CameraZoneType.Fixed:
                StartCoroutine(TransitionToFixedPosition(zone));
                break;
            case CameraZoneType.Follow:
                StartCoroutine(TransitionToFollow(zone));
                break;
        }
    }
    
    public void ExitCameraZone(CameraZone zone)
    {
        if (currentZone == zone)
        {
            currentZone = null;
            
            // 기본 추적 모드로 복귀
            if (currentState != CameraState.Following)
            {
                StartCoroutine(TransitionToFollow(null));
            }
        }
    }
    
    IEnumerator TransitionToFixedPosition(CameraZone zone)
    {
        isTransitioning = true;
        currentState = CameraState.Fixed;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(zone.fixedPosition.x, zone.fixedPosition.y, transform.position.z);
        
        // Y축 제한 적용 (MinY, MaxY)
        targetPosition = ApplyYLimits(targetPosition);
        
        float startZoom = cameraComponent.orthographicSize;
        float targetZoom = zone.changeZoom ? zone.targetZoomSize : originalZoomSize;
        
        // 위치와 줌 전환 시간을 따로 설정
        float positionDuration = zone.positionTransitionTime;
        float zoomDuration = zone.zoomTransitionTime;
        float maxDuration = Mathf.Max(positionDuration, zoomDuration);
        
        float elapsedTime = 0f;
        
        // 카메라 추적 비활성화
        followCamera.enabled = false;
        
        while (elapsedTime < maxDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 위치 전환
            if (elapsedTime < positionDuration)
            {
                float positionT = elapsedTime / positionDuration;
                float positionCurveT = zone.transitionCurve.Evaluate(positionT);
                Vector3 lerpedPosition = Vector3.Lerp(startPosition, targetPosition, positionCurveT);
                
                // Y축 제한 적용 (MinY, MaxY)
                lerpedPosition = ApplyYLimits(lerpedPosition);
                transform.position = lerpedPosition;
            }
            else
            {
                transform.position = ApplyYLimits(targetPosition);
            }
            
            // 줌 전환
            if (zone.changeZoom && elapsedTime < zoomDuration)
            {
                float zoomT = elapsedTime / zoomDuration;
                float zoomCurveT = zone.transitionCurve.Evaluate(zoomT);
                cameraComponent.orthographicSize = Mathf.Lerp(startZoom, targetZoom, zoomCurveT);
                
                // FollowCamera 크기 업데이트
                UpdateFollowCameraSize();
                
                // 줌 변경 후 위치 재조정
                transform.position = ApplyYLimits(transform.position);
            }
            else if (zone.changeZoom)
            {
                cameraComponent.orthographicSize = targetZoom;
                UpdateFollowCameraSize();
                transform.position = ApplyYLimits(transform.position);
            }
            
            yield return null;
        }
        
        // 최종 값 보정
        if (zone.changeZoom)
        {
            cameraComponent.orthographicSize = targetZoom;
            UpdateFollowCameraSize();
        }
        transform.position = ApplyYLimits(targetPosition);
        
        isTransitioning = false;
    }
    
    IEnumerator TransitionToFollow(CameraZone zone)
    {
        isTransitioning = true;
        currentState = CameraState.Following;
        
        Vector3 startPosition = transform.position;
        float startZoom = cameraComponent.orthographicSize;
        
        // 플레이어 위치 계산
        Vector3 targetPosition = Vector3.zero;
        if (playerTransform != null)
        {
            targetPosition = new Vector3(
                playerTransform.position.x + originalOffset.x,
                playerTransform.position.y + originalOffset.y,
                transform.position.z
            );
        }
        
        float targetZoom = zone != null && zone.changeZoom ? zone.targetZoomSize : originalZoomSize;
        float positionDuration = zone != null ? zone.positionTransitionTime : 1f;
        float zoomDuration = zone != null ? zone.zoomTransitionTime : 1f;
        float maxDuration = Mathf.Max(positionDuration, zoomDuration);
        AnimationCurve curve = zone != null ? zone.transitionCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < maxDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 플레이어 위치 다시 계산 (실시간 추적)
            if (playerTransform != null)
            {
                targetPosition = new Vector3(
                    playerTransform.position.x + originalOffset.x,
                    playerTransform.position.y + originalOffset.y,
                    transform.position.z
                );
            }
            
            // 위치 전환
            if (elapsedTime < positionDuration)
            {
                float positionT = elapsedTime / positionDuration;
                float positionCurveT = curve.Evaluate(positionT);
                Vector3 lerpedPosition = Vector3.Lerp(startPosition, targetPosition, positionCurveT);
                
                // Y축 제한 적용 (MinY, MaxY)
                lerpedPosition = ApplyYLimits(lerpedPosition);
                transform.position = lerpedPosition;
            }
            else
            {
                transform.position = ApplyYLimits(targetPosition);
            }
            
            // 줌 전환
            bool shouldChangeZoom = zone != null && zone.changeZoom;
            if (shouldChangeZoom && elapsedTime < zoomDuration)
            {
                float zoomT = elapsedTime / zoomDuration;
                float zoomCurveT = curve.Evaluate(zoomT);
                cameraComponent.orthographicSize = Mathf.Lerp(startZoom, targetZoom, zoomCurveT);
                UpdateFollowCameraSize();
                
                // 줌 변경 후 위치 재조정
                transform.position = ApplyYLimits(transform.position);
            }
            else if (shouldChangeZoom)
            {
                cameraComponent.orthographicSize = targetZoom;
                UpdateFollowCameraSize();
                transform.position = ApplyYLimits(transform.position);
            }
            else if (zone == null) // 기본 상태로 복원
            {
                if (elapsedTime < zoomDuration)
                {
                    float zoomT = elapsedTime / zoomDuration;
                    float zoomCurveT = curve.Evaluate(zoomT);
                    cameraComponent.orthographicSize = Mathf.Lerp(startZoom, originalZoomSize, zoomCurveT);
                    UpdateFollowCameraSize();
                    
                    // 줌 변경 후 위치 재조정
                    transform.position = ApplyYLimits(transform.position);
                }
                else
                {
                    cameraComponent.orthographicSize = originalZoomSize;
                    UpdateFollowCameraSize();
                    transform.position = ApplyYLimits(transform.position);
                }
            }
            
            yield return null;
        }
        
        // 최종 값 보정
        cameraComponent.orthographicSize = targetZoom;
        
        // FollowCamera 다시 활성화
        if (followCamera != null)
        {
            UpdateFollowCameraSize();
            followCamera.enabled = true;
        }
        
        isTransitioning = false;
    }
    
    // FollowCamera의 MinY, MaxY 제한을 모두 적용
    Vector3 ApplyYLimits(Vector3 position)
    {
        if (followCamera == null) return position;
        
        // MinY 제한 적용
        if (followCamera.useMinYLimit)
        {
            float minCameraY = followCamera.minY + followCamera.cameraHalfHeight;
            position.y = Mathf.Max(position.y, minCameraY);
        }
        
        // MaxY 제한 적용
        if (followCamera.useMaxYLimit)
        {
            float maxCameraY = followCamera.maxY - followCamera.cameraHalfHeight;
            position.y = Mathf.Min(position.y, maxCameraY);
        }
        
        return position;
    }
    
    void UpdateFollowCameraSize()
    {
        if (followCamera != null && cameraComponent != null)
        {
            followCamera.cameraHalfHeight = cameraComponent.orthographicSize;
            followCamera.cameraHalfWidth = cameraComponent.orthographicSize * cameraComponent.aspect;
        }
    }
    
    // 줌 관련 함수들 (CameraZoomZone에서 호출)
    public void TransitionZoom(float targetZoom, float duration, AnimationCurve curve = null)
    {
        StartCoroutine(ZoomTransition(targetZoom, duration, curve));
    }
    
    public void RestoreOriginalZoom(float duration)
    {
        StartCoroutine(ZoomTransition(originalZoomSize, duration, null));
    }
    
    IEnumerator ZoomTransition(float targetZoom, float duration, AnimationCurve curve)
    {
        float startZoom = cameraComponent.orthographicSize;
        float elapsedTime = 0f;
        
        if (curve == null)
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float curveT = curve.Evaluate(t);
            
            cameraComponent.orthographicSize = Mathf.Lerp(startZoom, targetZoom, curveT);
            
            // FollowCamera 크기 업데이트
            UpdateFollowCameraSize();
            
            // 줌 변경 후 Y축 제한 적용
            transform.position = ApplyYLimits(transform.position);
            
            yield return null;
        }
        
        cameraComponent.orthographicSize = targetZoom;
        UpdateFollowCameraSize();
        transform.position = ApplyYLimits(transform.position);
    }
    
    // 현재 상태 확인용
    public CameraState GetCurrentState() => currentState;
    public bool IsTransitioning() => isTransitioning;
}

public enum CameraState
{
    Following,
    Fixed
}