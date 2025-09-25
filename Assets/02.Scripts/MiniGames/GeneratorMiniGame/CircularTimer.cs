using UnityEngine;
using UnityEngine.UI;

public enum TimingResult
{
    Perfect,
    Good,
    Miss,
    NoInput
}

public class CircularTimer : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image circleBackground;
    public Image successZone;
    public Image needle;
    
    [Header("성공 존 설정")]
    public SuccessZoneSettings[] successZonePresets;
    public int currentPresetIndex = 0;
    public float successZoneSize = 30f; // 고정 성공 존 크기 (도)
    public float perfectZoneSize = 5f;
    
    [Header("디버그")]
    public bool showDebugInfo = true;
    
    [System.Serializable]
    public class SuccessZoneSettings
    {
        [Header("Z 회전 설정")]
        public float zRotation = 315f; // Z축 회전값
        
        [Header("성공 각도 범위 (판정용)")]
        [Range(0f, 360f)]
        public float startAngle = 315f; // 실제 판정 시작 각도
        [Range(0f, 360f)]  
        public float endAngle = 345f;   // 실제 판정 종료 각도
        
        [Header("위치 오프셋")]
        public Vector2 positionOffset = Vector2.zero;
        
        [Header("기즈모 표시")]
        public bool showGizmo = true;
        public Color gizmoColor = Color.green;
    }
    
    [System.Serializable]
    public class TimingSettings
    {
        public float rotationSpeed = 300f;
    }
    
    private TimingSettings currentSettings;
    private float currentRotation = 0f;
    private bool isRunning = false;
    private SuccessZoneSettings currentZoneSetting;
    
    void Update()
    {
        if (isRunning)
        {
            currentRotation += currentSettings.rotationSpeed * Time.deltaTime;
            if (currentRotation >= 360f)
                currentRotation -= 360f;
                
            if (needle != null)
            {
                RectTransform needleRect = needle.GetComponent<RectTransform>();
                if (needleRect != null)
                {
                    needleRect.localRotation = Quaternion.Euler(0, 0, -(currentRotation - 90f));
                }
            }
        }
    }
    
    public void StartTiming(TimingSettings settings)
    {
        currentSettings = settings;
        isRunning = true;
        currentRotation = 0f;
        
        if (successZonePresets != null && successZonePresets.Length > 0)
        {
            int index = Mathf.Clamp(currentPresetIndex, 0, successZonePresets.Length - 1);
            currentZoneSetting = successZonePresets[index];
        }
        else
        {
            currentZoneSetting = new SuccessZoneSettings();
        }
        
        SetupSuccessZone();
        
        if (needle != null)
            needle.gameObject.SetActive(true);
            
        if (showDebugInfo)
        {
            Debug.Log($"성공 존 설정: Z회전={currentZoneSetting.zRotation}°, 크기={successZoneSize}°");
        }
    }
    
    void SetupSuccessZone()
    {
        if (successZone != null && currentZoneSetting != null)
        {
            RectTransform successRect = successZone.GetComponent<RectTransform>();
            if (successRect != null)
            {
                // 위치 설정
                successRect.anchoredPosition = currentZoneSetting.positionOffset;
                
                // Z축 회전만 설정
                successRect.localRotation = Quaternion.Euler(0, 0, currentZoneSetting.zRotation);
            }
            
            successZone.gameObject.SetActive(true);
        }
    }
    
    public TimingResult GetTimingResult()
    {
        if (!isRunning || currentZoneSetting == null) return TimingResult.Miss;
        
        float needleAngle = currentRotation;
        
        // 현재 프리셋의 실제 성공 각도 범위 사용
        float zoneStartAngle = currentZoneSetting.startAngle;
        float zoneEndAngle = currentZoneSetting.endAngle;
        
        // 각도 정규화
        float normalizedNeedle = NormalizeAngle(needleAngle);
        float normalizedStart = NormalizeAngle(zoneStartAngle);
        float normalizedEnd = NormalizeAngle(zoneEndAngle);
        
        bool inSuccessZone = IsAngleInRange(normalizedNeedle, normalizedStart, normalizedEnd);
        
        if (showDebugInfo)
        {
            Debug.Log($"바늘: {normalizedNeedle:F1}°, 성공존: {normalizedStart:F1}°~{normalizedEnd:F1}°, 성공: {inSuccessZone}");
        }
        
        if (!inSuccessZone)
            return TimingResult.Miss;
            
        // 퍼펙트 존 체크 (성공 구역의 중앙)
        float perfectZoneCenter = NormalizeAngle((zoneStartAngle + zoneEndAngle) / 2f);
        float distanceFromCenter = Mathf.Abs(Mathf.DeltaAngle(normalizedNeedle, perfectZoneCenter));
        
        if (distanceFromCenter <= perfectZoneSize / 2f)
            return TimingResult.Perfect;
        else
            return TimingResult.Good;
    }
    
    bool IsAngleInRange(float angle, float start, float end)
    {
        if (start <= end)
        {
            return angle >= start && angle <= end;
        }
        else
        {
            return angle >= start || angle <= end;
        }
    }
    
    float NormalizeAngle(float angle)
    {
        while (angle < 0f) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }
    
    public void StopTiming()
    {
        isRunning = false;
        
        if (needle != null)
            needle.gameObject.SetActive(false);
            
        if (successZone != null)
            successZone.gameObject.SetActive(false);
    }
    
    void OnDrawGizmos()
    {
        if (successZonePresets == null || successZonePresets.Length == 0) return;
        
        // 현재 프리셋의 기즈모 표시
        int index = Mathf.Clamp(currentPresetIndex, 0, successZonePresets.Length - 1);
        var preset = successZonePresets[index];
        
        if (!preset.showGizmo) return;
        
        Vector3 center = transform.position;
        float radius = 100f; // 기즈모 표시 반지름
        
        // 성공존 각도 범위를 기즈모로 표시
        Gizmos.color = preset.gizmoColor;
        
        // 시작 각도와 종료 각도 선 그리기
        Vector3 startDirection = AngleToDirection(preset.startAngle);
        Vector3 endDirection = AngleToDirection(preset.endAngle);
        
        Gizmos.DrawLine(center, center + startDirection * radius);
        Gizmos.DrawLine(center, center + endDirection * radius);
        
        // 성공존 호(Arc) 그리기
        DrawArcGizmo(center, radius, preset.startAngle, preset.endAngle, preset.gizmoColor);
        
        // 바늘 현재 위치도 표시 (실행 중일 때만)
        if (Application.isPlaying && isRunning)
        {
            Gizmos.color = Color.red;
            Vector3 needleDirection = AngleToDirection(currentRotation);
            Gizmos.DrawLine(center, center + needleDirection * (radius + 20f));
        }
    }
    
    Vector3 AngleToDirection(float angle)
    {
        // Unity의 각도 시스템에 맞춰 변환 (0도 = 12시 방향)
        float rad = (angle - 90f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
    }
    
    void DrawArcGizmo(Vector3 center, float radius, float startAngle, float endAngle, Color color)
    {
        Gizmos.color = color;
        int segments = 20;
        
        float angleDiff = endAngle - startAngle;
        if (angleDiff < 0) angleDiff += 360f;
        
        Vector3 prevPoint = center + AngleToDirection(startAngle) * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = startAngle + (angleDiff * i / segments);
            Vector3 currentPoint = center + AngleToDirection(currentAngle) * radius;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
    
    void OnDisable()
    {
        StopTiming();
    }
}