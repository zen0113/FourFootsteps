using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SleepEffectTutorialStep : TutorialBase
{
    [Header("수면 효과 설정")]
    [SerializeField] private float effectDuration = 3f; // 효과 지속 시간
    [SerializeField] private bool autoNextStep = true;
    [SerializeField] private float delayBeforeNext = 0.8f;

    [Header("일렁임 효과 설정")]
    [SerializeField] private float maxWaveStrength = 0.05f;
    [SerializeField] private float waveSpeed = 2f;

    [Header("페이드 효과 설정")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("페이드 완료 후 나타낼 오브젝트")]
    private GameObject objectToShowAfterFade;

    [Header("UI 숨김 설정")]
    [SerializeField] private bool useFadeOutForUI = true; // GuideUIController 페이드 아웃 사용 여부

    private bool hasRequestedNext = false;
    private Coroutine autoNextCoroutine;
    private Coroutine sleepEffectCoroutine;
    private TutorialController currentTutorialController;

    // UI 페이드용 캔버스와 이미지
    private Canvas fadeCanvas;
    private Image fadeImage;

    // 카메라 일렁임용 머티리얼
    private Material waveMaterial;
    private Camera mainCamera;

    // 원래 카메라 위치를 저장하기 위한 변수
    private Vector3 originalCameraPosition;

    public override void Enter()
    {
        hasRequestedNext = false;

        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
        }

        if (sleepEffectCoroutine != null)
        {
            StopCoroutine(sleepEffectCoroutine);
        }

        currentTutorialController = TutorialController.Instance;

        if (currentTutorialController != null && currentTutorialController.BlackFadeObject != null)
        {
            // 컨트롤러에서 찾은 'black' 오브젝트를 'objectToShowAfterFade'에 할당
            objectToShowAfterFade = currentTutorialController.BlackFadeObject;
            Debug.Log($"[SleepEffectTutorialStep] 'objectToShowAfterFade'에 TutorialController의 '{objectToShowAfterFade.name}'를 성공적으로 할당했습니다.");
        }
        else
        {
            if (currentTutorialController == null)
            {
                Debug.LogWarning("[SleepEffectTutorialStep] TutorialController.Instance를 찾을 수 없습니다.");
            }
            else
            {
                Debug.LogWarning("[SleepEffectTutorialStep] TutorialController.Instance.BlackFadeObject가 null입니다.");
            }
        }

        // GuideUIController의 가이드 UI 숨기기
        if (GuideUIController.Instance != null) // GuideUIController의 싱글톤은 유지
        {
            if (useFadeOutForUI)
            {
                GuideUIController.Instance.FadeOutGuide();
            }
            else
            {
                GuideUIController.Instance.HideGuideInstant();
            }
        }
        else
        {
            Debug.LogWarning("[SleepEffectTutorialStep] GuideUIController.Instance를 찾을 수 없습니다. UI 숨김을 건너뜜니다.");
        }

        // 수면 효과 시작
        sleepEffectCoroutine = StartCoroutine(StartSleepEffect());

        // autoNextStep이 true일 때만 자동 진행 (효과 완료 후)
        if (autoNextStep)
        {
            autoNextCoroutine = StartCoroutine(AutoNextStep(effectDuration + delayBeforeNext, currentTutorialController));
        }
    }

    public override void Execute(TutorialController controller)
    {

    }

    public override void Exit()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }

        if (sleepEffectCoroutine != null)
        {
            StopCoroutine(sleepEffectCoroutine);
            sleepEffectCoroutine = null;
        }

        // 효과 정리
        CleanupEffects();
    }

    private IEnumerator StartSleepEffect()
    {
        // 효과 초기화
        SetupEffects();

        // 오브젝트를 처음에 비활성화
        if (objectToShowAfterFade != null)
        {
            objectToShowAfterFade.SetActive(false);
        }

        float timer = 0f;

        while (timer < effectDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / effectDuration;

            // 일렁임 효과 업데이트
            UpdateWaveEffect(progress);

            // 페이드 효과 업데이트
            UpdateFadeEffect(progress);

            yield return null;
        }

        // 효과 완료 - 최종 상태로 설정
        UpdateWaveEffect(1f);
        UpdateFadeEffect(1f);

        // 새롭게 추가된 부분: 페이드 효과 완료 후 오브젝트 활성화
        if (objectToShowAfterFade != null)
        {
            objectToShowAfterFade.SetActive(true);
            Debug.Log($"페이드 효과 완료! '{objectToShowAfterFade.name}' 오브젝트 활성화.");
        }

        sleepEffectCoroutine = null;
    }

    private void SetupEffects()
    {
        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        // 카메라의 원래 위치 저장
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }

        // 일렁임 효과용 셰이더 생성 (간단한 버전)
        CreateWaveMaterial();

        // 페이드 효과용 UI 생성
        CreateFadeCanvas();
    }

    private void CreateWaveMaterial()
    {
        // 간단한 일렁임 셰이더를 사용하는 머티리얼 생성
        Shader waveShader = Shader.Find("Hidden/WaveDistortion"); // Hidden/WaveDistortion 셰이더는 예시입니다. 실제 프로젝트에 맞게 사용하세요.
        if (waveShader == null)
        {
            // 기본 셰이더가 없으면 Unlit/Texture 사용
            waveShader = Shader.Find("Unlit/Texture");
            if (waveShader == null)
            {
                Debug.LogWarning("[SleepEffectTutorialStep] 'Hidden/WaveDistortion' 또는 'Unlit/Texture' 셰이더를 찾을 수 없습니다. 일렁임 효과가 제대로 작동하지 않을 수 있습니다.");
                return;
            }
        }
        waveMaterial = new Material(waveShader);
    }

    private void CreateFadeCanvas()
    {
        // 페이드용 캔버스 생성
        GameObject canvasGO = new GameObject("SleepFadeCanvas");
        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000; // 최상위에 렌더링

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 기준 해상도 설정 (필요에 따라 변경)

        canvasGO.AddComponent<GraphicRaycaster>();

        // 페이드용 이미지 생성
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(fadeCanvas.transform, false);

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 초기에는 투명

        // 전체 화면을 덮도록 설정
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void UpdateWaveEffect(float progress)
    {
        if (waveMaterial == null || mainCamera == null) return;

        // 진행도에 따른 일렁임 강도 계산 (페이드 인에 맞춰 강도 증가)
        float currentWaveStrength = progress * maxWaveStrength;

        // 시간에 따른 일렁임 변화
        // Camera.main.transform.position에 직접 접근하여 흔들림 효과 적용
        // originalCameraPosition이 설정되어 있는지 확인
        if (progress > 0 && mainCamera != null)
        {
            // Sin 함수를 사용하여 부드러운 움직임 생성
            float waveX = Mathf.Sin(Time.time * waveSpeed) * currentWaveStrength;
            float waveY = Mathf.Cos(Time.time * waveSpeed * 0.8f) * currentWaveStrength;

            // 원래 위치에서 흔들림 적용
            mainCamera.transform.position = originalCameraPosition + new Vector3(waveX, waveY, 0);
        }
    }

    private void UpdateFadeEffect(float progress)
    {
        if (fadeImage == null) return;

        // 페이드 커브를 사용해서 부드럽게 페이드 (0에서 1로)
        float fadeAlpha = fadeCurve.Evaluate(progress);
        Color currentColor = fadeImage.color;
        currentColor.a = fadeAlpha;
        fadeImage.color = currentColor;
    }

    private void CleanupEffects()
    {
        // 카메라 위치 원복
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition; // 저장된 원래 위치로 복귀
        }

        // 머티리얼 정리
        if (waveMaterial != null)
        {
            // Material.DestroyImmediate는 에디터에서 사용, 런타임에서는 Destroy
            if (Application.isPlaying)
            {
                Destroy(waveMaterial);
            }
            else
            {
                DestroyImmediate(waveMaterial);
            }
            waveMaterial = null;
        }

        // 페이드 캔버스 정리
        if (fadeCanvas != null)
        {
            // GameObject.DestroyImmediate는 에디터에서 사용, 런타임에서는 Destroy
            if (Application.isPlaying)
            {
                Destroy(fadeCanvas.gameObject);
            }
            else
            {
                DestroyImmediate(fadeCanvas.gameObject);
            }
            fadeCanvas = null;
            fadeImage = null;
        }
    }

    private IEnumerator AutoNextStep(float delay, TutorialController controllerToUse)
    {
        yield return new WaitForSeconds(delay);

        if (!hasRequestedNext)
        {
            hasRequestedNext = true;
            if (controllerToUse != null)
            {
                controllerToUse.SetNextTutorial();
            }
            else
            {
                Debug.LogWarning("[SleepEffectTutorialStep] 유효한 TutorialController 참조를 찾을 수 없어 다음 튜토리얼 단계로 진행할 수 없습니다.");
            }
        }

        autoNextCoroutine = null;
    }

    private void OnDestroy()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }

        if (sleepEffectCoroutine != null)
        {
            StopCoroutine(sleepEffectCoroutine);
            sleepEffectCoroutine = null;
        }

        CleanupEffects();
    }
}