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
    private int selectedPresetIndex = 0; // 현재 선택된 랜덤 프리셋
    
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
                    // 바늘이 반대로 가고 있으므로 180도 추가 보정
                    needleRect.localRotation = Quaternion.Euler(0, 0, -(currentRotation + 120f + 180f));
                }
            }
        }
    }
    
    public void StartTiming(TimingSettings settings)
    {
        currentSettings = settings;
        isRunning = true;
        currentRotation = 0f; // 항상 0도에서 시작
        
        // 랜덤으로 프리셋 선택
        if (successZonePresets != null && successZonePresets.Length > 0)
        {
            selectedPresetIndex = Random.Range(0, successZonePresets.Length);
            currentZoneSetting = successZonePresets[selectedPresetIndex];
        }
        else
        {
            // 기본값 설정
            currentZoneSetting = new SuccessZoneSettings();
            selectedPresetIndex = 0;
        }
        
        SetupSuccessZone();
        
        if (needle != null)
        {
            needle.gameObject.SetActive(true);
            // 바늘을 강제로 0도 위치로 초기화
            RectTransform needleRect = needle.GetComponent<RectTransform>();
            if (needleRect != null)
            {
                needleRect.localRotation = Quaternion.Euler(0, 0, -120f); // 120도 오프셋 적용
            }
        }
            
        if (showDebugInfo)
        {
            Debug.Log($"랜덤 선택된 프리셋 {selectedPresetIndex}: Z회전={currentZoneSetting.zRotation}°, 성공범위={currentZoneSetting.startAngle}°~{currentZoneSetting.endAngle}°");
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
            Debug.Log($"=== 타이밍 판정 ===");
            Debug.Log($"바늘 각도: {normalizedNeedle:F1}°");
            Debug.Log($"성공존 범위: {normalizedStart:F1}° ~ {normalizedEnd:F1}°");
            Debug.Log($"성공존 교차여부: {normalizedStart > normalizedEnd}");
            Debug.Log($"판정 결과: {inSuccessZone}");
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
        // 모든 각도를 0-360 범위로 정규화
        angle = NormalizeAngle(angle);
        start = NormalizeAngle(start);
        end = NormalizeAngle(end);
        
        if (start <= end)
        {
            // 일반적인 경우: start=45, end=75 -> 45~75도
            return angle >= start && angle <= end;
        }
        else
        {
            // 360도를 넘나드는 경우: start=350, end=10 -> 350~360 또는 0~10도
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
        
        // 런타임 중이면 현재 선택된 프리셋, 아니면 첫 번째 프리셋 표시
        int index = 0;
        if (Application.isPlaying && currentZoneSetting != null)
        {
            index = selectedPresetIndex;
        }
        else
        {
            index = 0; // 에디터에서는 첫 번째 프리셋만 미리보기
        }
        
        if (index >= successZonePresets.Length) return;
        
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
        
        // 바늘 기즈모 항상 표시 (에디터와 런타임 모두)
        Gizmos.color = Color.red;
        float needleAngle = 0f; // 기본값
        
        if (Application.isPlaying && isRunning)
        {
            needleAngle = currentRotation; // 실행 중일 때는 실제 각도
        }
        
        Vector3 needleDirection = AngleToDirection(needleAngle);
        Gizmos.DrawLine(center, center + needleDirection * (radius + 20f));
        
        // 0도 위치 표시 (12시 방향)
        Gizmos.color = Color.blue;
        Vector3 zeroDirection = AngleToDirection(0f);
        Gizmos.DrawLine(center, center + zeroDirection * (radius + 40f));
        
        #if UNITY_EDITOR
        // 현재 프리셋과 바늘 각도 정보 표시
        UnityEditor.Handles.color = Color.white;
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(center + Vector3.up * (radius + 60f), $"프리셋 {selectedPresetIndex}");
            UnityEditor.Handles.Label(center + Vector3.up * (radius + 80f), $"바늘: {needleAngle:F1}°");
        }
        else
        {
            UnityEditor.Handles.Label(center + Vector3.up * (radius + 60f), "에디터 모드");
            UnityEditor.Handles.Label(center + Vector3.up * (radius + 80f), "빨간선=바늘(0°), 파란선=0도기준");
        }
        #endif
    }
    
    Vector3 AngleToDirection(float angle)
    {
        // 바늘과 동일한 각도 시스템 사용 (0도 = 12시 방향)
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0);
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