using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldScroller : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        [Tooltip("이 레이어의 루트(이 루트의 자식 타일들이 좌→우로 이어져 있어야 함)")]
        public Transform root;

        [Tooltip("패럴랙스 계수(1 = 기본 속도, 0.5 = 느리게, 2 = 빠르게)")]
        [Range(0f, 4f)] public float parallax = 1f;

        [Header("루프 설정")]
        [Tooltip("true면 카메라 왼쪽을 벗어난 타일을 맨 오른쪽으로 재배치")]
        public bool loop = true;

        [Tooltip("타일 가로 길이(월드 단위). 비워두면 Start에서 Renderer.bounds로 자동 추정")]
        public float segmentWidth = 0f;

        [Tooltip("루트 아래 타일을 좌→우 순서로 배치했다고 가정. 자동으로 정렬하려면 체크")]
        public bool autoSortByX = true;

        [HideInInspector] public readonly Queue<Transform> tiles = new Queue<Transform>();
        [HideInInspector] public float rightmostX; // 현재 가장 오른쪽 끝 X(다음 붙일 기준)
    }

    [Header("속도")]
    [Tooltip("기본 스크롤 속도(오른쪽→왼쪽)")]
    public float baseSpeed = 8f;
    [Tooltip("초당 가속도(+면 빨라짐). 0이면 고정")]
    public float acceleration = 0f;
    [Tooltip("속도 상한. 0 이하면 무제한")]
    public float maxSpeed = 0f;

    [Tooltip("전체 속도 배율(난이도 상승 등)")]
    public float speedMultiplier = 1f;

    [Header("시간/정지")]
    [Tooltip("스케일 타임 사용(일시정지에 연동). 끄면 실시간 진행")]
    public bool useScaledTime = true;
    [SerializeField, Tooltip("수동 일시정지")]
    public bool paused = true;

    [Header("카메라")]
    [Tooltip("뷰 경계를 계산할 카메라. 비우면 Camera.main")]
    public Camera targetCamera;

    [Header("레이어들")]
    public List<Layer> layers = new List<Layer>();

    float currentSpeed;
    float dt => useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        currentSpeed = baseSpeed;

        foreach (var L in layers)
        {
            if (L.root == null) continue;

            // segmentWidth 자동 추정(필요 시)
            if (L.segmentWidth <= 0f)
            {
                L.segmentWidth = EstimateSegmentWidth(L.root);
                if (L.segmentWidth <= 0f)
                {
                    Debug.LogWarning($"[WorldScroller] {L.root.name}의 segmentWidth 추정 실패. 직접 값 지정 권장.");
                    L.segmentWidth = 10f; // fallback
                }
            }

            // 자식 타일 수집 & 좌표 정렬
            var children = new List<Transform>();
            for (int i = 0; i < L.root.childCount; i++)
                children.Add(L.root.GetChild(i));

            if (L.autoSortByX)
                children.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            L.tiles.Clear();
            foreach (var t in children)
                L.tiles.Enqueue(t);

            // 가장 오른쪽 X 계산
            if (children.Count > 0)
                L.rightmostX = children[children.Count - 1].position.x;
        }
    }

    void Update()
    {
        if (paused) return;

        // 속도 갱신
        if (Mathf.Abs(acceleration) > 0f)
        {
            currentSpeed += acceleration * dt;
            if (maxSpeed > 0f) currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }

        float v = currentSpeed * Mathf.Max(0f, speedMultiplier);

        float move = -v * dt; // 왼쪽(-X) 이동

        // 카메라 왼쪽 경계
        float camLeft = WorldLeftX();

        // 각 레이어 이동 & 루프
        foreach (var L in layers)
        {
            if (L.root == null) continue;

            float layerMove = move * L.parallax;

            // 1) 이동
            L.root.position += new Vector3(layerMove, 0f, 0f);

            // 2) 루프 재배치
            if (!L.loop || L.tiles.Count == 0) continue;

            // 큐의 맨 앞 타일이 왼쪽 경계를 충분히 벗어나면 오른쪽 끝으로 붙임
            // 버퍼를 조금 두면 빈틈 방지
            float recycleThreshold = camLeft - (L.segmentWidth * 0.5f);

            // 한 프레임에 여러 타일이 넘어갈 수도 있어 while
            int safety = 0;
            while (L.tiles.Count > 0 && safety++ < 16)
            {
                var front = L.tiles.Peek();
                if (front.position.x + L.segmentWidth * 0.5f < recycleThreshold)
                {
                    // 꺼내서 오른쪽 끝으로 이동
                    front = L.tiles.Dequeue();
                    float newX = L.rightmostX + L.segmentWidth;
                    front.position = new Vector3(newX, front.position.y, front.position.z);
                    L.tiles.Enqueue(front);
                    L.rightmostX = newX;
                }
                else break;
            }
        }
    }

    float WorldLeftX()
    {
        if (!targetCamera) return float.NegativeInfinity;
        var z = 0f;
        var leftBottom = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, Mathf.Abs(targetCamera.transform.position.z - z)));
        return leftBottom.x;
    }

    float EstimateSegmentWidth(Transform root)
    {
        // 첫 자식의 Renderer 폭 또는 자식 간 X 간격으로 추정
        if (root.childCount == 0) return 0f;

        float widthByRenderer = 0f;
        var r = root.GetChild(0).GetComponentInChildren<Renderer>();
        if (r) widthByRenderer = r.bounds.size.x;

        if (root.childCount >= 2)
        {
            var a = root.GetChild(0).position.x;
            var b = root.GetChild(1).position.x;
            float spacing = Mathf.Abs(b - a);
            // 타일이 약간 겹치거나 간격이 있을 수 있으니 더 신뢰되는 값 선택
            if (widthByRenderer > 0f && Mathf.Abs(widthByRenderer - spacing) < widthByRenderer * 0.5f)
                return spacing; // 거의 동일하면 간격 기준
            if (widthByRenderer > 0f) return widthByRenderer;
            return spacing;
        }

        return widthByRenderer; // 자식 1개면 렌더러 폭
    }

    // ====== 런타임 제어 API ======
    public void SetPaused(bool value) => paused = value;
    public void TogglePaused() => paused = !paused;

    public void SetBaseSpeed(float s) { baseSpeed = s; currentSpeed = s; }
    public void AddSpeed(float delta) { currentSpeed += delta; }
    public void SetAcceleration(float a) => acceleration = a;
    public void SetMaxSpeed(float s) => maxSpeed = s;
    public float GetCurrentSpeed() => currentSpeed;

    /// <summary> 난이도/버프에 따른 전체 배율 </summary>
    public void SetSpeedMultiplier(float mul) => speedMultiplier = Mathf.Max(0f, mul);

    /// <summary> PlayerAutoRunner 등에서 “대시 연출 속도”를 직관적으로 반영하고 싶을 때 </summary>
    public void SetRunSpeed(float runSpeed)
    {
        baseSpeed = runSpeed;
        currentSpeed = runSpeed;
    }
}