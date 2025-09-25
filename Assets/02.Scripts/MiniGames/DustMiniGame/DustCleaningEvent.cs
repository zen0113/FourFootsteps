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
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private int minDustCount = 3;
    [SerializeField] private int maxDustCount = 4;
    [SerializeField] private float spawnRadius = 150f;

    [Header("Custom Cursor Settings")]
    [SerializeField] private Texture2D broomCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private float cursorFlipInterval = 0.1f;

    private Texture2D xCursor;
    private List<GameObject> _activeDustObjects = new List<GameObject>();

    private bool _isEventActive = false; // 이벤트 전체가 진행중인지 (중복 실행 방지)
    private bool _isMinigameRunning = false; // '먼지 닦기' 미니게임이 진행중인지 (커서 제어용)

    private Coroutine _cursorAnimationCoroutine;
    private bool _isMinigameFinished = false;
    private PlayerHumanMovement _playerMovement;

    private void Awake()
    {
        xCursor = CreateFlippedTexture(broomCursor);
    }

    void Update()
    {
        if (_isMinigameRunning)
        {
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
                    Cursor.SetCursor(broomCursor, cursorHotspot, CursorMode.Auto);
                }
            }
            return; // 미니게임 중에는 아래 로직을 실행하지 않음
        }

        // 대화 중이 아닐 때만 E키 입력 감지
        if (DialogueManager.Instance.isDialogueActive) return;

        if (_isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (_isEventActive) return; // [추가] 이벤트가 어떤 단계든 진행중이면 중복 실행 방지

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

    //protected override bool CanInteractInRecallScene()
    //{
    //    // 이미 청소가 끝났다면 더 이상 조사할 수 없음
    //    if (_isMinigameFinished)
    //    {
    //        return false;
    //    }
    //    // GameManager의 CanStartCleaningMinigame 변수 값을 반환
    //    return (bool)GameManager.Instance.GetVariable("CanStartCleaningMinigame");
    //}

    private IEnumerator EventFlow()
    {
        _isEventActive = true;


        // --- 시작 대화 구간 (기본 커서) ---
        EventManager.Instance.CallEvent(startDialogueId);
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        // --- 미니게임 구간 (빗자루 커서) ---
        SetupMinigame();
        yield return new WaitUntil(() => _activeDustObjects.Count == 0);

        _playerMovement?.BlockMiniGameInput(false);
        _isMinigameRunning = false; // 커서 제어 로직 비활성화
        if (_cursorAnimationCoroutine != null)
        {
            StopCoroutine(_cursorAnimationCoroutine);
            _cursorAnimationCoroutine = null;
        }
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 기본 커서로 즉시 복원

        // --- 종료 대화 구간 (기본 커서) ---
        EventManager.Instance.CallEvent(endDialogueId);
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        // --- 최종 정리 ---
        cleaningCanvas.SetActive(false);

        if (!_isMinigameFinished)
        {
            _isMinigameFinished = true;
            // 기존의 IncrementCleanedObjectCount() 대신 IncrementVariable() 사용
            GameManager.Instance.IncrementVariable("CleanedObjectCount");
        }

        _isEventActive = false;
    }

    private void SetupMinigame()
    {
        _playerMovement?.BlockMiniGameInput(true);
        _isMinigameRunning = true;

        cleaningCanvas.SetActive(true);
        Cursor.SetCursor(broomCursor, cursorHotspot, CursorMode.Auto);
        objectImage.sprite = GetComponent<SpriteRenderer>().sprite;
        objectImage.preserveAspect = true;

        _activeDustObjects.Clear();
        int dustCount = Random.Range(minDustCount, maxDustCount + 1);
        for (int i = 0; i < dustCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPosition = objectImage.transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);
            GameObject dust = Instantiate(dustPrefab, spawnPosition, Quaternion.identity, dustSpawnParent);
            _activeDustObjects.Add(dust);
            dust.GetComponent<DustObject>().SetManager(this);
        }
    }

    private IEnumerator CursorAnimationLoop()
    {
        while (true)
        {
            Cursor.SetCursor(xCursor, cursorHotspot, CursorMode.Auto);
            yield return new WaitForSeconds(cursorFlipInterval);
            Cursor.SetCursor(broomCursor, cursorHotspot, CursorMode.Auto);
            yield return new WaitForSeconds(cursorFlipInterval);
        }
    }

    private Texture2D CreateFlippedTexture(Texture2D source)
    {
        Texture2D flippedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] sourcePixels = source.GetPixels();
        Color[] flippedPixels = new Color[sourcePixels.Length];
        int width = source.width;
        int height = source.height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flippedPixels[y * width + x] = sourcePixels[y * width + (width - 1 - x)];
            }
        }
        flippedTexture.SetPixels(flippedPixels);
        flippedTexture.Apply();
        return flippedTexture;
    }

    public void OnDustCleaned(GameObject dust)
    {
        if (_activeDustObjects.Contains(dust))
        {
            _activeDustObjects.Remove(dust);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            _playerMovement = other.GetComponent<PlayerHumanMovement>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            _playerMovement = null;
        }
    }
}