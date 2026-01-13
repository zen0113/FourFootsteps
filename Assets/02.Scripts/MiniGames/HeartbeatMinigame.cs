using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))] // AudioSource 컴포넌트 자동 추가
public class HeartbeatMinigame : MonoBehaviour
{
    [Header("=== UI 요소 ===")]
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private Image targetLineImage;
    [SerializeField] private Image playerLineImage;
    [SerializeField] private Text feedbackText;
    [SerializeField] private TextMeshProUGUI catNameText;

    [SerializeField] private HeartRateDisplay heartRateDisplay;

    [Header("=== 난이도 조절 ===")]
    [SerializeField] private Color guideColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color playerColor = Color.white;
    [SerializeField] private float matchTolerance = 25f;
    [SerializeField] private float successThreshold = 80f;

    [Header("=== 게임플레이 설정 ===")]
    [SerializeField] private float gameDuration = 5.0f;
    [SerializeField] private float showPatternTime = 2.0f;

    [Header("=== 오디오 설정 (New) ===")]
    [SerializeField] private AudioClip minigameBgm; // 인스펙터에서 BGM 파일 연결
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.5f;
    private AudioSource audioSource;

    private struct WaveformParameters
    {
        public int averageBPM;
        public int bpmVariance;

        public float minAmplitude; public float maxAmplitude;
        public float minDip; public float maxDip;
        public float minInterval; public float maxInterval;
        public float peakWidth;

        public float specialBeatChance;
        public float minSpecialAmplitude; public float maxSpecialAmplitude;
        public float minSpecialDip; public float maxSpecialDip;
    }

    private Dictionary<string, WaveformParameters> catWaveformParameters;
    private Dictionary<string, string> catNameTranslation;
    private List<Vector2> currentWaveformPoints;
    private Texture2D targetTexture;
    private Texture2D playerTexture;

    private string currentCatName = "Ttoli";

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
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("Drawing Area의 부모 Canvas가 ScreenSpaceOverlay가 아닙니다. 좌표 오차가 발생할 수 있습니다.");
        }

        targetLineImage.transform.SetParent(drawingArea, false);
        targetLineImage.transform.localPosition = Vector3.zero;
        playerLineImage.transform.SetParent(drawingArea, false);
        playerLineImage.transform.localPosition = Vector3.zero;

        // [오디오 설정 초기화]
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;           // 반복 재생 설정
        audioSource.playOnAwake = false;   // 시작하자마자 켜지는 것 방지 (OnEnable에서 제어)

        InitializeWaveformParameters();
    }

    private void InitializeWaveformParameters()
    {
        catWaveformParameters = new Dictionary<string, WaveformParameters>();

        catWaveformParameters["Ttoli"] = new WaveformParameters
        {
            averageBPM = 80,
            bpmVariance = 5,
            minAmplitude = 50f,
            maxAmplitude = 70f,
            minDip = 40f,
            maxDip = 65f,
            minInterval = 0.5f,
            maxInterval = 1.3f,
            peakWidth = 0.4f,
            specialBeatChance = 0.2f,
            minSpecialAmplitude = 85f,
            maxSpecialAmplitude = 95f,
            minSpecialDip = 30f,
            maxSpecialDip = 40f
        };

        catWaveformParameters["Leo"] = new WaveformParameters
        {
            averageBPM = 130,
            bpmVariance = 10,
            minAmplitude = 70f,
            maxAmplitude = 90f,
            minDip = 55f,
            maxDip = 70f,
            minInterval = 0.4f,
            maxInterval = 1.0f,
            peakWidth = 0.2f,
            specialBeatChance = 0.3f,
            minSpecialAmplitude = 100f,
            maxSpecialAmplitude = 108f,
            minSpecialDip = 45f,
            maxSpecialDip = 55f
        };

        catWaveformParameters["Bogsil"] = new WaveformParameters
        {
            averageBPM = 60,
            bpmVariance = 3,
            minAmplitude = 55f,
            maxAmplitude = 70f,
            minDip = 15f,
            maxDip = 35f,
            minInterval = 0.8f,
            maxInterval = 1.6f,
            peakWidth = 0.8f,
            specialBeatChance = 0.25f,
            minSpecialAmplitude = 60f,
            maxSpecialAmplitude = 80f,
            minSpecialDip = 2f,
            maxSpecialDip = 8f
        };

        catWaveformParameters["Miya"] = new WaveformParameters
        {
            averageBPM = 45,
            bpmVariance = 15,
            minAmplitude = 40f,
            maxAmplitude = 60f,
            minDip = 5f,
            maxDip = 25f,
            minInterval = 0.7f,
            maxInterval = 2.2f,
            peakWidth = 0.15f,
            specialBeatChance = 0.4f,
            minSpecialAmplitude = 65f,
            maxSpecialAmplitude = 85f,
            minSpecialDip = 10f,
            maxSpecialDip = 35f
        };

        catNameTranslation = new Dictionary<string, string>();
        catNameTranslation["Ttoli"] = "똘이";
        catNameTranslation["Leo"] = "레오";
        catNameTranslation["Bogsil"] = "복실이";
        catNameTranslation["Miya"] = "미야";
    }

    public void SetDifficulty(float newThreshold)
    {
        successThreshold = newThreshold;
    }

    public void SetupWaveform(string catName)
    {
        if (catWaveformParameters == null) InitializeWaveformParameters();

        currentCatName = catName;

        if (catNameText != null)
        {
            if (catNameTranslation.ContainsKey(catName))
            {
                catNameText.text = catNameTranslation[catName]; // 한글 이름 표시
            }
            else
            {
                catNameText.text = catName; // 데이터가 없으면 영어 이름 그대로 표시
                Debug.LogWarning($"'{catName}'에 대한 한글 이름 데이터가 없습니다.");
            }
        }

        WaveformParameters parameters;
        if (catWaveformParameters.ContainsKey(catName))
        {
            parameters = catWaveformParameters[catName];
            Debug.Log($"[HeartbeatMinigame] 설정 변경: {catName} (BPM: {parameters.averageBPM})");
        }
        else
        {
            Debug.LogError($"'{catName}' 데이터 없음. 기본값(Ttoli) 사용.");
            parameters = catWaveformParameters["Ttoli"];
            currentCatName = "Ttoli";
        }

        currentWaveformPoints = null;
        currentWaveformPoints = GenerateRandomWaveform(parameters);

        if (heartRateDisplay != null)
        {
            heartRateDisplay.SetHeartRateData(parameters.averageBPM, parameters.bpmVariance);
        }

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(MinigameFlow());
        }
    }

    private List<Vector2> GenerateRandomWaveform(WaveformParameters parameters)
    {
        var points = new List<Vector2> { new Vector2(0, 0) };
        float currentTime = 0f;
        float currentHeight = drawingHeight > 0 ? drawingHeight : drawingArea.rect.height;
        float safeMaxY = (currentHeight / 2f) - 15f;

        while (currentTime < gameDuration)
        {
            float interval = Random.Range(parameters.minInterval, parameters.maxInterval);
            float nextBeatStartTime = currentTime + interval;
            if (nextBeatStartTime + parameters.peakWidth >= gameDuration) break;

            currentTime = nextBeatStartTime;
            points.Add(new Vector2(currentTime - 0.1f, 0));

            float amplitude, dip;
            if (Random.value < parameters.specialBeatChance)
            {
                amplitude = Random.Range(parameters.minSpecialAmplitude, parameters.maxSpecialAmplitude);
                dip = Random.Range(parameters.minSpecialDip, parameters.maxSpecialDip);
            }
            else
            {
                amplitude = Random.Range(parameters.minAmplitude, parameters.maxAmplitude);
                dip = Random.Range(parameters.minDip, parameters.maxDip);
            }

            amplitude = Mathf.Min(amplitude, safeMaxY);
            dip = Mathf.Min(dip, safeMaxY);

            points.Add(new Vector2(currentTime, 0));
            points.Add(new Vector2(currentTime + (parameters.peakWidth / 2f), amplitude));
            points.Add(new Vector2(currentTime + parameters.peakWidth, -dip));

            currentTime += parameters.peakWidth;
            if (currentTime + 0.1f < gameDuration) points.Add(new Vector2(currentTime + 0.1f, 0));
        }
        points.Add(new Vector2(gameDuration, 0));
        return points;
    }

    // [BGM 재생 로직 추가]
    void OnEnable()
    {
        Debug.Log($"게임 활성화됨. '{currentCatName}' 데이터 로드 시작.");

        // BGM 재생
        if (minigameBgm != null && audioSource != null)
        {
            audioSource.clip = minigameBgm;
            audioSource.volume = bgmVolume;
            audioSource.Play();
        }

        SetupWaveform(currentCatName);
    }

    // [BGM 정지 로직 추가]
    void OnDisable()
    {
        isDrawingAllowed = false;
        isDrawing = false;
        StopAllCoroutines();

        // BGM 정지
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (targetTexture != null) Destroy(targetTexture);
        if (playerTexture != null) Destroy(playerTexture);

        currentWaveformPoints = null;
    }

    private IEnumerator MinigameFlow()
    {
        yield return new WaitForEndOfFrame();

        isDrawingAllowed = false;
        isDrawing = false;
        drawingWidth = (int)drawingArea.rect.width;
        drawingHeight = (int)drawingArea.rect.height;

        CreateAndClearTextures();

        if (currentWaveformPoints == null || currentWaveformPoints.Count == 0)
        {
            Debug.LogError("파형 데이터가 없습니다! 재설정 시도.");
            SetupWaveform(currentCatName);
            yield break;
        }

        DrawWaveform(targetTexture, currentWaveformPoints, Color.cyan);
        targetLineImage.sprite = CreateSpriteFromTexture(targetTexture);
        targetLineImage.gameObject.SetActive(true);
        playerLineImage.gameObject.SetActive(false);

        if (feedbackText != null) { feedbackText.text = "↑마음의 소리를 기억하세요↑"; feedbackText.gameObject.SetActive(true); }

        yield return new WaitForSeconds(showPatternTime);

        DrawWaveform(targetTexture, currentWaveformPoints, guideColor);
        targetLineImage.sprite = CreateSpriteFromTexture(targetTexture);

        playerLineImage.sprite = CreateSpriteFromTexture(playerTexture);
        playerLineImage.gameObject.SetActive(true);

        if (feedbackText != null)
        {
            feedbackText.text = $"가이드라인을 따라 그리세요!\n정확도 {successThreshold}% 이상이어야지 성공합니다.";
        }

        isDrawingAllowed = true;
    }

    void Update()
    {
        if (!isDrawingAllowed) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, Input.mousePosition, null, out localPoint))
            {
                isDrawing = true;
                lastMousePosition = localPoint + new Vector2(drawingWidth / 2f, drawingHeight / 2f);
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, Input.mousePosition, null, out localPoint))
            {
                Vector2 currentMousePosition = localPoint + new Vector2(drawingWidth / 2f, drawingHeight / 2f);
                DrawLine(playerTexture, (int)lastMousePosition.x, (int)lastMousePosition.y, (int)currentMousePosition.x, (int)currentMousePosition.y, playerColor, 3);
                playerTexture.Apply();
                lastMousePosition = currentMousePosition;
            }
        }
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
            float time = (float)x / drawingWidth * gameDuration;
            float targetY = GetTargetYAtTime(time);

            totalTargetPixels++;

            float playerY = GetPlayerYAtX_Center(x);

            if (playerY >= 0)
            {
                if (Mathf.Abs(playerY - targetY) <= matchTolerance)
                {
                    matchedPixels++;
                }
            }
        }

        float accuracy = (totalTargetPixels > 0) ?
                         ((float)matchedPixels / totalTargetPixels) * 100f : 0f;

        bool success = accuracy >= successThreshold;

        string displayName = catNameTranslation.ContainsKey(currentCatName) ? catNameTranslation[currentCatName] : currentCatName;
        Debug.Log($"[정확도] {displayName}: {accuracy:F1}% (매칭: {matchedPixels}/{totalTargetPixels})");

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

        // [중요] 게임 오브젝트가 꺼질 때(OnDisable) BGM도 같이 꺼집니다.
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

    private float GetPlayerYAtX_Center(int x)
    {
        List<int> yPositions = new List<int>();

        for (int y = 0; y < drawingHeight; y++)
        {
            if (playerTexture.GetPixel(x, y).a > 0.5f)
            {
                yPositions.Add(y);
            }
        }

        if (yPositions.Count == 0) return -1;

        yPositions.Sort();
        return yPositions[yPositions.Count / 2];
    }

    private float GetTargetYAtX(int x)
    {
        for (int y = 0; y < drawingHeight; y++)
        {
            if (targetTexture.GetPixel(x, y).a > 0) return y;
        }
        return -1;
    }

    private int GetPlayerYAtX(int x)
    {
        for (int y = 0; y < drawingHeight; y++)
        {
            if (playerTexture.GetPixel(x, y).a > 0) return y;
        }
        return -1;
    }

    private void DrawWaveform(Texture2D texture, List<Vector2> points, Color color)
    {
        ClearTexture(texture);
        int maxX = drawingWidth - 1;

        for (int i = 0; i < points.Count - 1; i++)
        {
            int x1 = Mathf.RoundToInt((points[i].x / gameDuration) * drawingWidth);
            int y1 = Mathf.RoundToInt((drawingHeight / 2f) + points[i].y);
            int x2 = Mathf.RoundToInt((points[i + 1].x / gameDuration) * drawingWidth);
            int y2 = Mathf.RoundToInt((drawingHeight / 2f) + points[i + 1].y);

            x1 = Mathf.Clamp(x1, 0, maxX);
            x2 = Mathf.Clamp(x2, 0, maxX);
            y1 = Mathf.Clamp(y1, 0, drawingHeight - 1);
            y2 = Mathf.Clamp(y2, 0, drawingHeight - 1);

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