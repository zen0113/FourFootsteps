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

    private List<GameObject> _activeDustObjects = new List<GameObject>();
    private bool _isEventActive = false;
    private bool _isMinigameRunning = false;
    private Coroutine _cursorAnimationCoroutine;
    private bool _isMinigameFinished = false;

    // 이벤트 구독/해제
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
            // 가짜 커서 위치 업데이트
            if (fakeCursorImage != null)
            {
                fakeCursorImage.rectTransform.position = Input.mousePosition;
            }

            // 마우스 클릭 애니메이션
            if (Input.GetMouseButtonDown(0))
            {
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
            }

            return;
        }

        // E키 이벤트 시작 로직 (미니게임 중이 아닐 때만 실행)
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

    public void OnPauseStateChanged(bool isPaused)
    {
        if (!_isMinigameRunning) return;

        if (isPaused)
        {
            // [퍼즈됨]
            Cursor.visible = true; // 진짜 커서 켜기
            if (fakeCursorImage != null)
                fakeCursorImage.gameObject.SetActive(false); // 가짜 커서 끄기

            if (_cursorAnimationCoroutine != null)
            {
                StopCoroutine(_cursorAnimationCoroutine);
                _cursorAnimationCoroutine = null;
            }
        }
        else
        {
            // [퍼즈 해제됨]
            // 가짜 커서 켜기
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
        // 이번 프레임의 모든 렌더링이 끝날 때까지 대기
        yield return new WaitForEndOfFrame();

        // 대기 후에도 여전히 미니게임 중이고 퍼즈가 아니라면 (안전장치)
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

        Cursor.visible = true; // '진짜 커서' 복구
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