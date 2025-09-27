using UnityEngine;

public class PuzzlePieceDropTutorial : TutorialBase
{
    [Header("퍼즐 조각 설정")]
    [SerializeField] private GameObject puzzlePiecePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 10, 0);

    [Header("물리 설정")]
    [SerializeField] private float rotationSpeed = 180f; // 회전 속도 (도/초)

    [Header("퍼즐 이벤트 설정")]
    [SerializeField] private string eventId = ""; // 이벤트 아이디
    [SerializeField] private string puzzleId = ""; // 퍼즐 아이디
    [SerializeField] private AudioSource sound;
    [SerializeField] private bool playSound = true;

    private GameObject spawnedPuzzlePiece;
    private Rigidbody puzzleRigidbody;
    public bool hasLanded = false;
    private TutorialController tutorialController;

    private FloatingEffect puzzleFloatingEffect;

    public override void Enter()
    {
        SpawnPuzzlePiece();
    }

    public override void Execute(TutorialController controller)
    {
        tutorialController = controller;

        if (spawnedPuzzlePiece != null && !hasLanded)
        {
            spawnedPuzzlePiece.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        }
    }

    public override void Exit()
    {
        // 역할이 끝나도 퍼즐 조각은 유지
    }

    private void SpawnPuzzlePiece()
    {
        if (puzzlePiecePrefab == null)
        {
            Debug.LogError("퍼즐 조각 프리펩이 설정되지 않았습니다!");
            return;
        }

        Vector3 finalSpawnPosition = spawnPoint != null ? spawnPoint.position : spawnPosition;
        spawnedPuzzlePiece = Instantiate(puzzlePiecePrefab, finalSpawnPosition, Quaternion.identity);

        if (!string.IsNullOrEmpty(eventId))
        {
            spawnedPuzzlePiece.name = eventId;
        }

        puzzleFloatingEffect = spawnedPuzzlePiece.GetComponent<FloatingEffect>();

        PuzzlePieceGroundDetector detector = spawnedPuzzlePiece.GetComponentInChildren<PuzzlePieceGroundDetector>(true);
        if (detector == null)
        {
            detector = spawnedPuzzlePiece.AddComponent<PuzzlePieceGroundDetector>();
        }
        detector.Initialize(this);


        // 자식 오브젝트에서 MemoryPuzzle 컴포넌트를 찾습니다.
        MemoryPuzzle memoryPuzzle = spawnedPuzzlePiece.GetComponentInChildren<MemoryPuzzle>();
        if (memoryPuzzle != null)
        {
            // eventId 할당
            if (!string.IsNullOrEmpty(eventId))
            {
                memoryPuzzle.eventId = eventId;
            }

            // puzzleId 할당
            if (!string.IsNullOrEmpty(puzzleId))
            {
                memoryPuzzle.puzzleId = puzzleId;
            }
        }
        else
        {
            Debug.LogWarning("생성된 퍼즐 조각 프리펩 또는 그 자식에서 MemoryPuzzle 컴포넌트를 찾을 수 없습니다.");
        }

        if (playSound && sound != null)
        {
            sound.Play();
        }
    }

    public void OnPuzzlePieceLanded()
    {
        if (hasLanded) return;

        Debug.Log("퍼즐 조각이 바닥에 닿았습니다!");
        hasLanded = true;

        puzzleRigidbody = spawnedPuzzlePiece.GetComponent<Rigidbody>();
        if (puzzleRigidbody != null)
        {
            puzzleRigidbody.velocity = Vector3.zero;
            puzzleRigidbody.angularVelocity = Vector3.zero;
            puzzleRigidbody.isKinematic = true;
        }

        if (puzzleFloatingEffect != null)
        {
            puzzleFloatingEffect.enabled = true;
        }

        if (tutorialController != null)
        {
            Invoke(nameof(MoveToNextTutorial), 2f);
        }
    }

    private void MoveToNextTutorial()
    {
        tutorialController.SetNextTutorial();
    }
}