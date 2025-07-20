using UnityEngine;
using System;

public class CatAutoMover : MonoBehaviour
{
    public Transform targetPoint;
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isMoving = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void StartMoving(Transform destination)
    {
        targetPoint = destination;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving || targetPoint == null) return;

        Vector2 direction = targetPoint.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            isMoving = false;
            animator?.SetBool("Moving", false);
            OnArrived?.Invoke(); // 도착 이벤트 호출
            return;
        }

        // 방향 반전
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;

        Vector2 moveDir = direction.normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);
        animator?.SetBool("Moving", true);
    }
}
