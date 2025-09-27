using UnityEngine;

public class PuzzlePieceGroundDetector : MonoBehaviour
{
    private PuzzlePieceDropTutorial parentTutorial;
    [SerializeField] private LayerMask groundLayer;

    public void Initialize(PuzzlePieceDropTutorial tutorial)
    {
        parentTutorial = tutorial;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌한 오브젝트가 groundLayer에 속하는지 확인
        if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            if (parentTutorial != null && !parentTutorial.hasLanded)
            {
                parentTutorial.transform.rotation = Quaternion.identity;
                parentTutorial.OnPuzzlePieceLanded();
            }
        }
    }
}