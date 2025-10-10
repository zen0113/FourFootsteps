using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeartbeatMinigame : MonoBehaviour
{
    [Header("=== UI 요소 ===")]
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private Image targetLineImage;
    [SerializeField] private Image playerLineImage;
    [SerializeField] private Text feedbackText;

    [Header("=== 난이도 조절 ===")]
    [Tooltip("가이드라인으로 사용할 색상")]
    [SerializeField] private Color guideColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [Tooltip("플레이어 라인 색상")]
    [SerializeField] private Color playerColor = Color.white;
    [Tooltip("성공 판정 범위 (값이 작을수록 어려워짐)")]
    [SerializeField] private float matchTolerance = 25f;
    [Tooltip("성공으로 인정되는 정확도(%)의 최소값")]
    [SerializeField] private float successThreshold = 80f;

    [Header("=== 게임플레이 설정 ===")]
    [SerializeField] private float gameDuration = 5.0f;
    [SerializeField] private float showPatternTime = 2.0f;

    private struct WaveformParameters
    {
        // 일반 박동
        public float minAmplitude;
        public float maxAmplitude;
        public float minDip;
        public float maxDip;
        public float minInterval;
        public float maxInterval;
        public float peakWidth;

        // 특별한 박동
        [Tooltip("특별한 박동이 나타날 확률 (0.0 ~ 1.0)")]
        public float specialBeatChance;
        public float minSpecialAmplitude;
        public float maxSpecialAmplitude;
        public float minSpecialDip;
        public float maxSpecialDip;
    }

    private Dictionary<string, WaveformParameters> catWaveformParameters;
    private List<Vector2> currentWaveformPoints;
    private Texture2D targetTexture;
    private Texture2D playerTexture;

    private bool isDrawingAllowed = false;
    private bool isDrawing = false;
    private Vector2 lastMousePosition;
    private int drawingWidth;
    private int drawingHeight;

    public delegate void MinigameEndHandler(bool success);
    public event MinigameEndHandler OnMinigameEnd;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("Drawing Area의 부모 Canvas가 ScreenSpaceOverlay가 아닙니다. 마우스 좌표가 정확하지 않을 수 있습니다.");
        }

        targetLineImage.transform.SetParent(drawingArea, false);
        targetLineImage.transform.localPosition = Vector3.zero;
        playerLineImage.transform.SetParent(drawingArea, false);
        playerLineImage.transform.localPosition = Vector3.zero;

        InitializeWaveformParameters();
    }

    private void InitializeWaveformParameters()
    {
        catWaveformParameters = new Dictionary<string, WaveformParameters>();

        // 똘이 (활발함): 가끔 더 강하게 뜀
        catWaveformParameters["Ttoli"] = new WaveformParameters
        {
            minAmplitude = 55f,
            maxAmplitude = 75f,
            minDip = 25f,
            maxDip = 35f,
            minInterval = 0.8f,
            maxInterval = 1.1f,
            peakWidth = 0.1f,
            specialBeatChance = 0.2f, // 20% 확률로 특별한 박동
            minSpecialAmplitude = 90f,
            maxSpecialAmplitude = 100f, // 더 강하게
            minSpecialDip = 40f,
            maxSpecialDip = 50f
        };

        // 레오 (강렬함): 더 자주, 훨씬 강렬하게 뜀
        catWaveformParameters["Leo"] = new WaveformParameters
        {
            minAmplitude = 80f,
            maxAmplitude = 100f,
            minDip = 50f,
            maxDip = 60f,
            minInterval = 0.6f,
            maxInterval = 0.9f,
            peakWidth = 0.08f,
            specialBeatChance = 0.3f, // 30% 확률
            minSpecialAmplitude = 110f,
            maxSpecialAmplitude = 120f, // 훨씬 강하게
            minSpecialDip = 60f,
            maxSpecialDip = 70f
        };

        // 복실이 (그리움): 가끔 박동을 건너뛰는 듯 약하게 뜀
        catWaveformParameters["Bogsil"] = new WaveformParameters
        {
            minAmplitude = 35f,
            maxAmplitude = 50f,
            minDip = 10f,
            maxDip = 15f,
            minInterval = 1.4f,
            maxInterval = 1.8f,
            peakWidth = 0.2f,
            specialBeatChance = 0.25f, // 25% 확률
            minSpecialAmplitude = 15f,
            maxSpecialAmplitude = 25f, // 더 약하게
            minSpecialDip = 5f,
            maxSpecialDip = 10f
        };

        // 미야 (절망): 거의 멈출 것처럼 매우 불안정하게 뜀
        catWaveformParameters["Miya"] = new WaveformParameters
        {
            minAmplitude = 10f,
            maxAmplitude = 20f,
            minDip = 2f,
            maxDip = 8f,
            minInterval = 1.8f,
            maxInterval = 2.5f,
            peakWidth = 0.15f,
            specialBeatChance = 0.4f, // 40% 확률로 불안정
            minSpecialAmplitude = 25f,
            maxSpecialAmplitude = 35f, // 갑자기 조금 강해지거나
            minSpecialDip = 0f,
            maxSpecialDip = 5f // 거의 평탄하게 지나감
        };
    }

    private List<Vector2> GenerateRandomWaveform(WaveformParameters parameters)
    {
        var points = new List<Vector2> { new Vector2(0, 0) };
        float currentTime = 0f;

        while (currentTime < gameDuration)
        {
            float interval = Random.Range(parameters.minInterval, parameters.maxInterval);
            currentTime += interval;

            if (currentTime >= gameDuration) break;

            points.Add(new Vector2(currentTime - 0.1f, 0));

            float amplitude;
            float dip;

            // 확률 체크!
            if (Random.value < parameters.specialBeatChance)
            {
                // "특별한 박동"의 수치를 사용
                amplitude = Random.Range(parameters.minSpecialAmplitude, parameters.maxSpecialAmplitude);
                dip = Random.Range(parameters.minSpecialDip, parameters.maxSpecialDip);
            }
            else
            {
                // "일반 박동"의 수치를 사용
                amplitude = Random.Range(parameters.minAmplitude, parameters.maxAmplitude);
                dip = Random.Range(parameters.minDip, parameters.maxDip);
            }

            // 뾰족한 심장박동 모양(QRS파)을 만듦
            points.Add(new Vector2(currentTime, 0));
            points.Add(new Vector2(currentTime + (parameters.peakWidth / 2f), amplitude));
            points.Add(new Vector2(currentTime + parameters.peakWidth, -dip));

            currentTime += parameters.peakWidth;
            if (currentTime < gameDuration)
            {
                points.Add(new Vector2(currentTime + 0.1f, 0));
            }
        }

        points.Add(new Vector2(gameDuration, 0));
        return points;
    }


    public void SetupWaveform(string catName)
    {
        if (catWaveformParameters == null) InitializeWaveformParameters();

        if (catWaveformParameters.ContainsKey(catName))
        {
            WaveformParameters parameters = catWaveformParameters[catName];
            currentWaveformPoints = GenerateRandomWaveform(parameters);
        }
        else
        {
            Debug.LogError($"{catName}의 파형 데이터가 없습니다! 기본 파형(Ttoli)으로 실행합니다.");
            WaveformParameters parameters = catWaveformParameters["Ttoli"];
            currentWaveformPoints = GenerateRandomWaveform(parameters);
        }
    }

    void OnEnable()
    {
        if (currentWaveformPoints == null)
        {
            Debug.LogWarning("미니게임 파형이 설정되지 않았습니다! 기본 파형(Ttoli)을 사용합니다.");
            SetupWaveform("Ttoli");
        }
        StartCoroutine(MinigameFlow());
    }

    void OnDisable()
    {
        isDrawingAllowed = false;
        isDrawing = false;
        StopAllCoroutines();
        if (targetTexture != null) Destroy(targetTexture);
        if (playerTexture != null) Destroy(playerTexture);
    }

    private IEnumerator MinigameFlow()
    {
        isDrawingAllowed = false;
        isDrawing = false;
        drawingWidth = (int)drawingArea.rect.width;
        drawingHeight = (int)drawingArea.rect.height;

        CreateAndClearTextures();

        // 1단계: 패턴 보여주기
        DrawWaveform(targetTexture, currentWaveformPoints, Color.cyan);
        targetLineImage.sprite = CreateSpriteFromTexture(targetTexture);
        targetLineImage.gameObject.SetActive(true);
        playerLineImage.gameObject.SetActive(false);

        if (feedbackText != null) { feedbackText.text = "패턴을 기억하세요!"; feedbackText.gameObject.SetActive(true); }

        yield return new WaitForSeconds(showPatternTime);

        // 2단계: 그리기 준비
        DrawWaveform(targetTexture, currentWaveformPoints, guideColor);
        targetLineImage.sprite = CreateSpriteFromTexture(targetTexture);

        playerLineImage.sprite = CreateSpriteFromTexture(playerTexture);
        playerLineImage.gameObject.SetActive(true);

        if (feedbackText != null) { feedbackText.text = "가이드라인을 따라 그리세요!"; }

        isDrawingAllowed = true;
    }

    void Update()
    {
        if (!isDrawingAllowed) return;

        // 마우스 클릭 시 그리기 시작
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, Input.mousePosition, null, out localPoint))
            {
                isDrawing = true;
                lastMousePosition = localPoint + new Vector2(drawingWidth / 2f, drawingHeight / 2f);
            }
        }
        // 마우스 드래그 중 그리기
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, Input.mousePosition, null, out localPoint))
            {
                Vector2 currentMousePosition = localPoint + new Vector2(drawingWidth / 2f, drawingHeight / 2f);
                DrawLine(playerTexture, (int)lastMousePosition.x, (int)lastMousePosition.y, (int)currentMousePosition.x, (int)currentMousePosition.y, playerColor, 5);
                playerTexture.Apply();
                lastMousePosition = currentMousePosition;
            }
        }
        // 마우스 버튼 뗄 때 그리기 종료 및 정확도 계산
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            isDrawingAllowed = false;
            CalculateAccuracyAndEnd();
        }
    }

    private void CalculateAccuracyAndEnd()
    {
        int matchedPixels = 0;
        int totalTargetPixels = 0;

        for (int x = 0; x < drawingWidth; x++)
        {
            float targetY = GetTargetYAtX(x);

            // 해당 x 좌표에 목표 라인이 있는지 확인
            if (targetY >= 0)
            {
                totalTargetPixels++;
                // 플레이어가 그린 라인이 있는지 확인
                int playerY = GetPlayerYAtX(x);
                if (playerY >= 0)
                {
                    if (Mathf.Abs(playerY - targetY) <= matchTolerance)
                    {
                        matchedPixels++;
                    }
                }
            }
        }

        float accuracy = (totalTargetPixels > 0) ? ((float)matchedPixels / totalTargetPixels) * 100f : 0f;
        bool success = accuracy >= successThreshold;

        EndMinigame(success, accuracy);
    }

    private void EndMinigame(bool success, float accuracy)
    {
        if (feedbackText != null)
        {
            feedbackText.text = success ?
                $"성공! (정확도: {accuracy:F1}%)" :
                $"실패... (정확도: {accuracy:F1}%)";
        }
        StartCoroutine(DelayedEnd(success));
    }

    private IEnumerator DelayedEnd(bool success)
    {
        yield return new WaitForSeconds(1.5f);
        OnMinigameEnd?.Invoke(success);
        gameObject.SetActive(false);
    }

    #region Texture Drawing Functions
    private void CreateAndClearTextures()
    {
        if (targetTexture != null) Destroy(targetTexture);
        if (playerTexture != null) Destroy(playerTexture);

        targetTexture = new Texture2D(drawingWidth, drawingHeight);
        playerTexture = new Texture2D(drawingWidth, drawingHeight);

        ClearTexture(targetTexture);
        ClearTexture(playerTexture);
    }

    private void ClearTexture(Texture2D texture)
    {
        Color[] clearColors = new Color[texture.width * texture.height];
        for (int i = 0; i < clearColors.Length; i++) { clearColors[i] = Color.clear; }
        texture.SetPixels(clearColors);
        texture.Apply();
    }

    private Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private float GetTargetYAtTime(float time)
    {
        if (currentWaveformPoints == null || currentWaveformPoints.Count < 2) return drawingHeight / 2f;
        for (int i = 0; i < currentWaveformPoints.Count - 1; i++)
        {
            if (time >= currentWaveformPoints[i].x && time <= currentWaveformPoints[i + 1].x)
            {
                float t = (time - currentWaveformPoints[i].x) / (currentWaveformPoints[i + 1].x - currentWaveformPoints[i].x);
                float yOffset = Mathf.Lerp(currentWaveformPoints[i].y, currentWaveformPoints[i + 1].y, t);
                return (drawingHeight / 2f) + yOffset;
            }
        }
        return drawingHeight / 2f;
    }

    // X 좌표를 기반으로 목표 라인의 Y 좌표를 찾는 함수
    private float GetTargetYAtX(int x)
    {
        // GetPixel은 비용이 높으므로, 이 방식 대신 텍스처에서 직접 찾는 것이 더 효율적
        for (int y = 0; y < drawingHeight; y++)
        {
            if (targetTexture.GetPixel(x, y).a > 0)
            {
                return y;
            }
        }
        return -1; // 해당 x에 그려진 픽셀이 없음
    }

    // X 좌표를 기반으로 플레이어 라인의 Y 좌표를 찾는 함수
    private int GetPlayerYAtX(int x)
    {
        for (int y = 0; y < drawingHeight; y++)
        {
            if (playerTexture.GetPixel(x, y).a > 0)
            {
                return y;
            }
        }
        return -1; // 해당 x에 그려진 픽셀이 없음
    }

    private void DrawWaveform(Texture2D texture, List<Vector2> points, Color color)
    {
        ClearTexture(texture);
        for (int i = 0; i < points.Count - 1; i++)
        {
            int x1 = Mathf.RoundToInt((points[i].x / gameDuration) * drawingWidth);
            int y1 = Mathf.RoundToInt((drawingHeight / 2f) + points[i].y);
            int x2 = Mathf.RoundToInt((points[i + 1].x / gameDuration) * drawingWidth);
            int y2 = Mathf.RoundToInt((drawingHeight / 2f) + points[i + 1].y);
            DrawLine(texture, x1, y1, x2, y2, color, 3);
        }
        texture.Apply();
    }

    private void DrawLine(Texture2D texture, int x1, int y1, int x2, int y2, Color color, int thickness)
    {
        int dx = Mathf.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Mathf.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            DrawPixel(texture, x1, y1, color, thickness);
            if (x1 == x2 && y1 == y2) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }

    private void DrawPixel(Texture2D texture, int x, int y, Color color, int thickness)
    {
        int half = thickness / 2;
        for (int i = -half; i <= half; i++)
        {
            for (int j = -half; j <= half; j++)
            {
                if (i * i + j * j > half * half) continue;
                int px = x + i;
                int py = y + j;
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
    #endregion
}