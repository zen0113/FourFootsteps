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

    // FloatingEffect 스크립트를 제어하기 위한 변수
    private FloatingEffect puzzleFloatingEffect;

    public override void Enter()
    {
        SpawnPuzzlePiece();
    }

    public override void Execute(TutorialController controller)
    {
        tutorialController = controller;

        if (spawnedPuzzlePiece != null)
        {
            if (!hasLanded)
            {
                // 떨어지는 동안 Z축 회전
                spawnedPuzzlePiece.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
            }
        }
    }

    public override void Exit()
    {
        // 이 스크립트의 역할이 끝나도 퍼즐 조각은 계속 둥둥 뜹니다.
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

        // 생성된 퍼즐 조각에서 FloatingEffect 컴포넌트를 가져옵니다.
        puzzleFloatingEffect = spawnedPuzzlePiece.GetComponent<FloatingEffect>();
        if (puzzleFloatingEffect == null)
        {
            Debug.LogWarning("퍼즐 조각 프리펩에 FloatingEffect 스크립트가 없습니다! 추가해주세요.");
        }

        puzzleRigidbody = spawnedPuzzlePiece.GetComponent<Rigidbody>();
        if (puzzleRigidbody == null)
        {
            puzzleRigidbody = spawnedPuzzlePiece.AddComponent<Rigidbody>();
        }

        Collider puzzleCollider = spawnedPuzzlePiece.GetComponent<Collider>();
        if (puzzleCollider == null)
        {
            puzzleCollider = spawnedPuzzlePiece.AddComponent<BoxCollider>();
        }

        PuzzlePieceGroundDetector detector = spawnedPuzzlePiece.GetComponent<PuzzlePieceGroundDetector>();
        if (detector == null)
        {
            detector = spawnedPuzzlePiece.AddComponent<PuzzlePieceGroundDetector>();
        }
        detector.Initialize(this);

        MemoryPuzzle memoryPuzzle = spawnedPuzzlePiece.GetComponent<MemoryPuzzle>();
        if (memoryPuzzle != null)
        {
            // eventId와 puzzleId를 하나의 if 블록 안에서 모두 설정합니다.
            if (!string.IsNullOrEmpty(eventId))
            {
                memoryPuzzle.eventId = eventId; // EventObject로부터 상속받은 eventId
            }

            if (!string.IsNullOrEmpty(puzzleId))
            {
                memoryPuzzle.puzzleId = puzzleId;
            }
        }

        if (playSound && sound != null)
        {
            sound.Play();
        }
    }

    // 퍼즐 조각이 바닥에 닿았을 때 호출되는 메서드
    public void OnPuzzlePieceLanded()
    {
        if (hasLanded) return;

        Debug.Log("퍼즐 조각이 바닥에 닿았습니다!");
        hasLanded = true;

        if (puzzleRigidbody != null)
        {
            puzzleRigidbody.velocity = Vector3.zero;
            puzzleRigidbody.angularVelocity = Vector3.zero;
            puzzleRigidbody.isKinematic = true;
        }

        // 현재 회전 상태를 그대로 유지 (회전을 멈추기만 함)
        // 별도의 회전 보간이나 리셋 없이, 떨어진 그 각도 그대로 고정

        if (puzzleFloatingEffect != null)
        {
            // FloatingEffect 스크립트 활성화
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