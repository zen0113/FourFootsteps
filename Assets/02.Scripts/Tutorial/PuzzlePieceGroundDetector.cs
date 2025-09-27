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
        if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            if (parentTutorial != null && !parentTutorial.hasLanded)
            {
                parentTutorial.OnPuzzlePieceLanded();
            }
        }
    }
}
