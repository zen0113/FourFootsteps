using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DustCleaningEvent : EventObject
{
    [Header("Dialogue Settings")]
    [SerializeField] private string startDialogueId;
    [SerializeField] private string endDialogueId;
    [SerializeField] private string alreadyCleanedDialogueId;

    [Header("Minigame UI Settings")]
    [SerializeField] private GameObject cleaningCanvas;
    [SerializeField] private Image objectImage;
    [SerializeField] private Transform dustSpawnParent;

    [Header("Dust Settings")]
    [SerializeField] private GameObject[] dustPrefabs;
    [SerializeField] private int minDustCount = 3;
    [SerializeField] private int maxDustCount = 4;
    [SerializeField] private float spawnRadius = 150f;

    [Header("Custom Cursor Settings")]
    [SerializeField] private Image fakeCursorImage;
    [SerializeField] private Sprite broomSprite;
    [SerializeField] private Sprite xSprite;
    [SerializeField] private float cursorFlipInterval = 0.1f;

    [Header("Feedback Settings")]
    [SerializeField] private GameObject cancelMessagePrefab;
    [SerializeField] private Vector3 messageOffset = new Vector3(0, 50, 0);
    [SerializeField] private float messageDuration = 2.0f; 

    private List<GameObject> _activeDustObjects = new List<GameObject>();
    private bool _isEventActive = false;
    private bool _isMinigameRunning = false;
    private Coroutine _cursorAnimationCoroutine;
    private bool _isMinigameFinished = false;

    private bool _isCleaningSuccess = false;

    // 드래그 중에 먼지에 닿은 적이 있는지 확인하는 플래그
    private bool _wasTouchingDust = false;

    private void OnEnable()
    {
        PauseManager.OnPauseToggled += OnPauseStateChanged;
    }

    private void OnDisable()
    {
        PauseManager.OnPauseToggled -= OnPauseStateChanged;
    }

    void Update()
    {
        if (_isMinigameRunning && !PauseManager.IsGamePaused)
        {
            if (fakeCursorImage != null)
            {
                fakeCursorImage.rectTransform.position = Input.mousePosition;
            }

            // 마우스를 누르고 있는 동안, 커서가 먼지 위에 있는지 체크
            if (Input.GetMouseButton(0))
            {
                // 현재 마우스 위치에 먼지가 있는지 확인
                if (IsMouseOverDust())
                {
                    _wasTouchingDust = true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                _isCleaningSuccess = false;
                _wasTouchingDust = false; // 클릭 시작 시 초기화

                if (_cursorAnimationCoroutine == null)
                {
                    _cursorAnimationCoroutine = StartCoroutine(CursorAnimationLoop());
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_cursorAnimationCoroutine != null)
                {
                    StopCoroutine(_cursorAnimationCoroutine);
                    _cursorAnimationCoroutine = null;
                    if (fakeCursorImage != null)
                    {
                        fakeCursorImage.sprite = broomSprite;
                    }
                }

                // 청소 실패 AND (실제로 먼지를 건드리고 있었을 경우에만) 메시지 출력
                if (!_isCleaningSuccess && _wasTouchingDust)
                {
                    ShowCancelMessage();
                }

                // 상태 초기화
                _wasTouchingDust = false;
            }

            return;
        }

        if (DialogueManager.Instance.isDialogueActive) return;

        if (_isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (_isEventActive) return;

            if (_isMinigameFinished)
            {
                EventManager.Instance.CallEvent(alreadyCleanedDialogueId);
            }
            else
            {
                StartCoroutine(EventFlow());
            }
        }
    }

    // 마우스가 현재 살아있는 먼지 오브젝트 위에 있는지 판별하는 함수
    private bool IsMouseOverDust()
    {
        foreach (var dust in _activeDustObjects)
        {
            if (dust == null) continue;

            // UI RectTransform 기준 범위 체크
            RectTransform rect = dust.GetComponent<RectTransform>();
            if (rect != null)
            {
                // 오버레이 UI나 스크린 스페이스 카메라 UI 모두 대응하기 위한 방식
                // (카메라가 필요하면 Camera.main 등을 사용, 여기선 null로 오버레이 가정하거나 자동 감지 시도)
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null) ||
                    RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, Camera.main))
                {
                    return true;
                }
            }
            else
            {
                // 만약 RectTransform이 없다면 거리로 체크 (백업 로직)
                float dist = Vector3.Distance(Input.mousePosition, dust.transform.position);
                if (dist < 50f) return true; // 임의의 범위
            }
        }
        return false;
    }

    private void ShowCancelMessage()
    {
        if (cancelMessagePrefab == null || cleaningCanvas == null) return;

        GameObject msgObj = Instantiate(cancelMessagePrefab, cleaningCanvas.transform);

        if (msgObj.TryGetComponent<RectTransform>(out RectTransform rect))
        {
            rect.position = Input.mousePosition + messageOffset;
        }

        StartCoroutine(AnimateFeedbackMessage(msgObj));
    }

    private IEnumerator AnimateFeedbackMessage(GameObject msgObj)
    {
        float duration = messageDuration; // 변수 사용
        float timer = 0f;

        CanvasGroup canvasGroup = msgObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = msgObj.AddComponent<CanvasGroup>();

        Vector3 startPos = msgObj.transform.position;
        Vector3 endPos = startPos + new Vector3(0, 50f, 0);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            if (msgObj != null)
            {
                msgObj.transform.position = Vector3.Lerp(startPos, endPos, progress);

                // 애니메이션: 처음 70%는 불투명하다가, 마지막 30% 동안 서서히 사라짐
                if (progress > 0.7f)
                {
                    float alphaProgress = (progress - 0.7f) / 0.3f;
                    canvasGroup.alpha = 1 - alphaProgress;
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
            }
            yield return null;
        }

        if (msgObj != null) Destroy(msgObj);
    }

    public void OnPauseStateChanged(bool isPaused)
    {
        if (!_isMinigameRunning) return;

        if (isPaused)
        {
            Cursor.visible = true;
            if (fakeCursorImage != null)
                fakeCursorImage.gameObject.SetActive(false);

            if (_cursorAnimationCoroutine != null)
            {
                StopCoroutine(_cursorAnimationCoroutine);
                _cursorAnimationCoroutine = null;
            }
        }
        else
        {
            if (fakeCursorImage != null)
            {
                fakeCursorImage.sprite = broomSprite;
                fakeCursorImage.gameObject.SetActive(true);
            }

            StartCoroutine(HideCursorAtEndOfFrame());
        }
    }

    private IEnumerator HideCursorAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        if (_isMinigameRunning && !PauseManager.IsGamePaused)
        {
            Cursor.visible = false;
        }
    }

    private IEnumerator EventFlow()
    {
        _isEventActive = true;
        PlayerHumanMovement currentPlayer = FindObjectOfType<PlayerHumanMovement>();
        if (currentPlayer == null)
        {
            Debug.LogError("EventFlow: PlayerHumanMovement를 찾을 수 없습니다!");
            _isEventActive = false;
            yield break;
        }

        EventManager.Instance.CallEvent(startDialogueId);
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        SetupMinigame(currentPlayer);

        yield return new WaitUntil(() => _activeDustObjects.Count == 0 && !PauseManager.IsGamePaused);

        CleanupMinigame(currentPlayer);

        EventManager.Instance.CallEvent(endDialogueId);
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        cleaningCanvas.SetActive(false);
        if (!_isMinigameFinished)
        {
            _isMinigameFinished = true;
            GameManager.Instance.IncrementVariable("CleanedObjectCount");
        }
        _isEventActive = false;
    }

    private void SetupMinigame(PlayerHumanMovement player)
    {
        player?.BlockMiniGameInput(true);
        _isMinigameRunning = true;
        cleaningCanvas.SetActive(true);

        if (fakeCursorImage != null)
        {
            fakeCursorImage.sprite = broomSprite;
            fakeCursorImage.gameObject.SetActive(true);
        }

        StartCoroutine(HideCursorAtEndOfFrame());

        objectImage.sprite = GetComponent<SpriteRenderer>().sprite;
        objectImage.preserveAspect = true;

        if (dustPrefabs == null || dustPrefabs.Length == 0)
        {
            Debug.LogError("Dust Prefabs 배열이 비어있습니다! 인스펙터에서 설정해주세요.");
            return;
        }

        _activeDustObjects.Clear();
        int dustCount = Random.Range(minDustCount, maxDustCount + 1);
        for (int i = 0; i < dustCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPosition = objectImage.transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);
            int randomIndex = Random.Range(0, dustPrefabs.Length);
            GameObject selectedDustPrefab = dustPrefabs[randomIndex];
            GameObject dust = Instantiate(selectedDustPrefab, spawnPosition, Quaternion.identity, dustSpawnParent);
            _activeDustObjects.Add(dust);
            dust.GetComponent<DustObject>().SetManager(this);
        }
    }

    private void CleanupMinigame(PlayerHumanMovement player)
    {
        player?.BlockMiniGameInput(false);
        _isMinigameRunning = false;

        if (_cursorAnimationCoroutine != null)
        {
            StopCoroutine(_cursorAnimationCoroutine);
            _cursorAnimationCoroutine = null;
        }

        Cursor.visible = true;
        if (fakeCursorImage != null)
        {
            fakeCursorImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator CursorAnimationLoop()
    {
        while (true)
        {
            if (fakeCursorImage != null)
            {
                fakeCursorImage.sprite = xSprite;
            }
            yield return new WaitForSeconds(cursorFlipInterval);

            if (fakeCursorImage != null)
            {
                fakeCursorImage.sprite = broomSprite;
            }
            yield return new WaitForSeconds(cursorFlipInterval);
        }
    }

    public void OnDustCleaned(GameObject dust)
    {
        _isCleaningSuccess = true;

        if (_activeDustObjects.Contains(dust))
        {
            _activeDustObjects.Remove(dust);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
    }
}