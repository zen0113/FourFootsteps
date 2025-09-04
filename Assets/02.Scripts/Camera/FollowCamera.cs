using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;              // 추적할 대상 (플레이어)
    public float smoothSpeedX = 0.125f;   // X축 부드러운 이동
    public float smoothSpeedY = 0.05f;    // Y축은 더 부드럽게
    public Vector2 offset = new Vector2(0f, 2f); // Y축으로 2만큼 위에 위치

    [Header("Camera Bounds Settings")]
    public float cameraHalfWidth = 5f;    // 카메라 가로 절반 크기
    public float cameraHalfHeight = 3f;   // 카메라 세로 절반 크기
    public bool showDebugBounds = true;   // 디버그용 카메라 경계 표시
    
    [Header("Y축 제한")]
    public bool useMinYLimit = true;      // 최소 Y 제한 사용
    public float minY = 0f;               // 카메라가 내려갈 수 있는 최소 Y 위치

    private Camera cam;
    private LayerMask boundaryLayer;      // CameraLimit 레이어 마스크

    private void Start()
    {
        cam = GetComponent<Camera>();
        
        // CameraLimit 레이어 마스크 설정
        int cameraLimitLayer = LayerMask.NameToLayer("CameraLimit");
        if (cameraLimitLayer == -1)
        {
            Debug.LogWarning("CameraLimit 레이어가 존재하지 않습니다. Project Settings > Tags and Layers에서 CameraLimit 레이어를 추가해주세요.");
            boundaryLayer = 0; // 빈 레이어 마스크
        }
        else
        {
            boundaryLayer = 1 << cameraLimitLayer;
            Debug.Log($"CameraLimit 레이어 감지됨. 레이어 번호: {cameraLimitLayer}");
        }
        
        // 카메라 크기 자동 계산 (Orthographic 카메라인 경우)
        if (cam != null && cam.orthographic)
        {
            cameraHalfHeight = cam.orthographicSize;
            cameraHalfWidth = cameraHalfHeight * cam.aspect;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 타겟의 위치 + 오프셋 계산
        float targetX = target.position.x + offset.x;
        float targetY = target.position.y + offset.y;

        // 현재 위치 저장 및 Z값 유지
        Vector3 currentPos = transform.position;
        float currentZ = currentPos.z;

        // 부드러운 이동 계산
        float smoothedX = Mathf.Lerp(currentPos.x, targetX, smoothSpeedX * Time.deltaTime);
        float smoothedY = Mathf.Lerp(currentPos.y, targetY, smoothSpeedY * Time.deltaTime);

        Vector3 desiredPosition = new Vector3(smoothedX, smoothedY, currentZ);

        // Y축 최소값 제한 적용
        if (useMinYLimit)
        {
            float minCameraY = minY + cameraHalfHeight; // 카메라 하단이 minY보다 아래로 가지 못하게
            desiredPosition.y = Mathf.Max(desiredPosition.y, minCameraY);
        }

        // 경계 제한 적용 - 각 축을 개별적으로 처리
        Vector3 finalPosition = ClampCameraPosition(currentPos, desiredPosition);

        // 카메라 위치를 이동
        transform.position = finalPosition;
    }

    private Vector3 ClampCameraPosition(Vector3 currentPos, Vector3 desiredPos)
    {
        Vector3 clampedPos = currentPos;

        // X축 이동 체크
        float testX = desiredPos.x;
        if (IsCameraPositionValid(testX, currentPos.y))
        {
            clampedPos.x = testX;
        }
        // X축 이동이 안되면 현재 X 위치 유지

        // Y축 이동 체크
        float testY = desiredPos.y;
        if (IsCameraPositionValid(clampedPos.x, testY))
        {
            clampedPos.y = testY;
        }
        // Y축 이동이 안되면 현재 Y 위치 유지

        return clampedPos;
    }

    private bool IsCameraPositionValid(float x, float y)
    {
        // 카메라의 4개 모서리 위치 계산
        Vector2 topLeft = new Vector2(x - cameraHalfWidth, y + cameraHalfHeight);
        Vector2 topRight = new Vector2(x + cameraHalfWidth, y + cameraHalfHeight);
        Vector2 bottomLeft = new Vector2(x - cameraHalfWidth, y - cameraHalfHeight);
        Vector2 bottomRight = new Vector2(x + cameraHalfWidth, y - cameraHalfHeight);

        // 4개 모서리 중 하나라도 경계 콜라이더 안에 들어가면 유효하지 않은 위치
        if (Physics2D.OverlapPoint(topLeft, boundaryLayer) != null) return false;
        if (Physics2D.OverlapPoint(topRight, boundaryLayer) != null) return false;
        if (Physics2D.OverlapPoint(bottomLeft, boundaryLayer) != null) return false;
        if (Physics2D.OverlapPoint(bottomRight, boundaryLayer) != null) return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugBounds) return;

        // 카메라 경계 표시
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        
        // 카메라 뷰 영역을 박스로 표시
        Vector3 size = new Vector3(cameraHalfWidth * 2, cameraHalfHeight * 2, 0.1f);
        Gizmos.DrawWireCube(pos, size);

        // Min Y 경계선 표시
        if (useMinYLimit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-50, minY, 0), new Vector3(50, minY, 0));
        }

        // 카메라 모서리 점들 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(pos.x - cameraHalfWidth, pos.y + cameraHalfHeight, pos.z), 0.1f); // TopLeft
        Gizmos.DrawSphere(new Vector3(pos.x + cameraHalfWidth, pos.y + cameraHalfHeight, pos.z), 0.1f); // TopRight
        Gizmos.DrawSphere(new Vector3(pos.x - cameraHalfWidth, pos.y - cameraHalfHeight, pos.z), 0.1f); // BottomLeft
        Gizmos.DrawSphere(new Vector3(pos.x + cameraHalfWidth, pos.y - cameraHalfHeight, pos.z), 0.1f); // BottomRight

        // 타겟 연결선
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position + (Vector3)offset);
        }
    }
}