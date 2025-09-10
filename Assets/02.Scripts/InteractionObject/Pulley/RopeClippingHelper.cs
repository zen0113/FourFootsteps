using UnityEngine;

/// <summary>
/// 도르레 시스템과 연동되는 줄 클리핑 헬퍼
/// 도르레의 위치에 따라 자동으로 클리핑 라인을 조정
/// </summary>
public class RopeClippingHelper : MonoBehaviour
{
    [Header("도르레 시스템 연결")]
    [SerializeField] private Transform pulleyTransform; // 도르레 Transform
    [SerializeField] private float pulleyRadius = 0.5f; // 도르레 반지름
    [SerializeField] private float clipOffset = 0.1f; // 클리핑 오프셋 (도르레 중심에서 약간 위)
    
    [Header("줄 오브젝트들")]
    [SerializeField] private RopeClipping[] ropeClippings; // 클리핑할 줄들
    
    [Header("자동 감지")]
    [SerializeField] private bool autoFindRopes = true; // 자동으로 줄 찾기
    [SerializeField] private string ropeTag = "Rope"; // 줄 태그
    
    private void Start()
    {
        if (autoFindRopes)
        {
            FindRopeObjects();
        }
        
        InitializeRopeClipping();
    }
    
    /// <summary>
    /// 자동으로 줄 오브젝트 찾기
    /// </summary>
    private void FindRopeObjects()
    {
        GameObject[] ropeObjects = GameObject.FindGameObjectsWithTag(ropeTag);
        ropeClippings = new RopeClipping[ropeObjects.Length];
        
        for (int i = 0; i < ropeObjects.Length; i++)
        {
            RopeClipping clipping = ropeObjects[i].GetComponent<RopeClipping>();
            if (clipping == null)
            {
                clipping = ropeObjects[i].AddComponent<RopeClipping>();
            }
            ropeClippings[i] = clipping;
        }
    }
    
    /// <summary>
    /// 줄 클리핑 초기화
    /// </summary>
    private void InitializeRopeClipping()
    {
        if (pulleyTransform == null)
        {
            Debug.LogWarning("도르레 Transform이 설정되지 않았습니다!");
            return;
        }
        
        foreach (var ropeClipping in ropeClippings)
        {
            if (ropeClipping != null)
            {
                ropeClipping.SetReferenceTransform(pulleyTransform);
                UpdateClippingPosition(ropeClipping);
            }
        }
    }
    
    private void Update()
    {
        // 도르레가 움직일 때마다 클리핑 위치 업데이트
        if (pulleyTransform != null)
        {
            foreach (var ropeClipping in ropeClippings)
            {
                if (ropeClipping != null)
                {
                    UpdateClippingPosition(ropeClipping);
                }
            }
        }
    }
    
    /// <summary>
    /// 클리핑 위치 업데이트
    /// </summary>
    private void UpdateClippingPosition(RopeClipping ropeClipping)
    {
        // 도르레 중심에서 약간 위쪽으로 클리핑 라인 설정
        float clipY = pulleyTransform.position.y + pulleyRadius + clipOffset;
        ropeClipping.SetClipYPosition(clipY - ropeClipping.transform.position.y);
    }
    
    /// <summary>
    /// 런타임에서 도르레 설정
    /// </summary>
    public void SetPulley(Transform newPulley, float radius = 0.5f)
    {
        pulleyTransform = newPulley;
        pulleyRadius = radius;
        InitializeRopeClipping();
    }
    
    /// <summary>
    /// 줄 추가
    /// </summary>
    public void AddRope(RopeClipping newRope)
    {
        if (newRope == null) return;
        
        // 기존 배열 확장
        RopeClipping[] newArray = new RopeClipping[ropeClippings.Length + 1];
        for (int i = 0; i < ropeClippings.Length; i++)
        {
            newArray[i] = ropeClippings[i];
        }
        newArray[ropeClippings.Length] = newRope;
        ropeClippings = newArray;
        
        // 새 줄 초기화
        newRope.SetReferenceTransform(pulleyTransform);
        UpdateClippingPosition(newRope);
    }
    
    private void OnDrawGizmos()
    {
        if (pulleyTransform == null) return;
        
        // 도르레 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pulleyTransform.position, pulleyRadius);
        
        // 클리핑 라인 표시
        Gizmos.color = Color.red;
        float clipY = pulleyTransform.position.y + pulleyRadius + clipOffset;
        Vector3 leftPoint = pulleyTransform.position + Vector3.left * 2f;
        Vector3 rightPoint = pulleyTransform.position + Vector3.right * 2f;
        leftPoint.y = clipY;
        rightPoint.y = clipY;
        
        Gizmos.DrawLine(leftPoint, rightPoint);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(new Vector3(pulleyTransform.position.x, clipY + 0.5f, pulleyTransform.position.z), 
                                 "클리핑 라인");
        #endif
    }
}