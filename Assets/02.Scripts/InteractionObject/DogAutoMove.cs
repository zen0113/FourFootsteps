using System.Collections;
using UnityEngine;
using System;

public class DogAutoMove : MonoBehaviour
{
    [Header("순찰 설정")]
    public float moveSpeed = 2f;
    public float patrolRange = 5f;

    [Header("플레이어 추적 설정")]
    public Transform player;
    public float eyeContactDistance = 8f;
    public float chaseSpeed = 4f;
    public float chaseRange = 12f;
    [SerializeField] private GameObject exclamationMark;

    [Header("공격 설정")]
    public int attackDamage = 1; // 공격 데미지

    [Header("구역 설정")]
    public Transform areaCenter;
    public float areaRadius = 10f;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isMoving = false;
    private bool isChasing = false;
    private bool isReturning = false;
    private bool movingRight = true;
    private Vector3 initialPosition;
    private Vector3 leftBoundary;
    private Vector3 rightBoundary;
    private Vector3 exclamationInitialLocalPos;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private float walkSoundInterval = 0.3f;
    private float lastWalkSoundTime;

    [SerializeField] private Animator animator;

    public Action OnArrived;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        initialPosition = transform.position;
        SetPatrolBoundaries();
        isMoving = true;

        if (exclamationMark != null)
        {
            exclamationInitialLocalPos = exclamationMark.transform.localPosition;
        }

        animator.SetBool("Moving", true);
    }

    private void SetPatrolBoundaries()
    {
        leftBoundary = initialPosition + Vector3.left * patrolRange;
        rightBoundary = initialPosition + Vector3.right * patrolRange;
    }

    private void Update()
    {
        CheckEyeContact();

        if (isChasing) ChasePlayer();
        else if (isReturning) ReturnToPatrolPoint();
        else PatrolLeftRight();

        PlaySoundIfMoving();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 'Player' 태그를 가진 오브젝트와 부딪혔다면
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어에게 데미지를 줌
            PlayerHp playerHp = collision.gameObject.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(attackDamage);
            }

            // 추적을 멈추고 즉시 복귀 상태로 전환
            StopChasing();
        }
    }

    private void CheckEyeContact()
    {
        if (player == null) return;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInArea = IsPlayerInArea();
        if (!isChasing && !isReturning && playerInArea && distanceToPlayer <= eyeContactDistance && IsFacingPlayer())
        {
            StartChasing();
        }
        else if (isChasing && (!playerInArea || distanceToPlayer > chaseRange))
        {
            StopChasing();
        }
    }

    private bool IsDogInArea()
    {
        if (areaCenter == null) return true;
        return Vector2.Distance(transform.position, areaCenter.position) <= areaRadius;
    }

    private bool IsPlayerInArea()
    {
        if (player == null || areaCenter == null) return false;
        return Vector2.Distance(player.position, areaCenter.position) <= areaRadius;
    }

    private bool IsFacingPlayer()
    {
        if (player == null) return false;
        Vector2 directionToPlayer = player.position - transform.position;
        if (!spriteRenderer.flipX && directionToPlayer.x > 0) return true;
        if (spriteRenderer.flipX && directionToPlayer.x < 0) return true;
        return false;
    }

    private void StartChasing()
    {
        isChasing = true;
        isMoving = true;
        isReturning = false;
        if (exclamationMark != null) exclamationMark.SetActive(true);
    }

    private void StopChasing()
    {
        isChasing = false;
        isReturning = true;
    }

    private void ChasePlayer()
    {
        if (!IsDogInArea())
        {
            StopChasing();
            return;
        }
        if (player == null) return;
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * chaseSpeed * Time.deltaTime);
        UpdateSpriteDirection(direction);
        isMoving = true;
    }

    private void ReturnToPatrolPoint()
    {
        if (exclamationMark != null) exclamationMark.SetActive(false);
        float distanceToInitial = Vector2.Distance(transform.position, initialPosition);
        if (distanceToInitial > 0.1f)
        {
            Vector2 direction = (initialPosition - transform.position).normalized;
            transform.Translate(direction * moveSpeed * Time.deltaTime);
            UpdateSpriteDirection(direction);
            isMoving = true;
        }
        else
        {
            transform.position = initialPosition;
            isReturning = false;
            isMoving = true;
        }
    }

    private void PatrolLeftRight()
    {
        Vector3 targetPosition;
        Vector2 moveDirection;
        if (movingRight)
        {
            targetPosition = rightBoundary;
            moveDirection = Vector2.right;
        }
        else
        {
            targetPosition = leftBoundary;
            moveDirection = Vector2.left;
        }
        if (Vector2.Distance(transform.position, targetPosition) <= 0.1f)
        {
            movingRight = !movingRight;
        }
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        UpdateSpriteDirection(moveDirection);
        isMoving = true;
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (direction.x < -0.01f)
            spriteRenderer.flipX = true;

        if (exclamationMark != null)
        {
            float newX = spriteRenderer.flipX ? -Mathf.Abs(exclamationInitialLocalPos.x) : Mathf.Abs(exclamationInitialLocalPos.x);
            exclamationMark.transform.localPosition = new Vector3(newX, exclamationInitialLocalPos.y, exclamationInitialLocalPos.z);
        }
    }

    private void PlaySoundIfMoving()
    {
        if (isMoving && Time.time - lastWalkSoundTime >= walkSoundInterval)
        {
            if (walkSound != null && audioSource != null) audioSource.PlayOneShot(walkSound);
            lastWalkSoundTime = Time.time;
        }
    }

private void OnDrawGizmosSelected()
    {
        if (areaCenter != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(areaCenter.position, areaRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, eyeContactDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(leftBoundary, rightBoundary);
        }
        else
        {
            Vector3 previewLeft = transform.position + Vector3.left * patrolRange;
            Vector3 previewRight = transform.position + Vector3.right * patrolRange;
            Gizmos.DrawLine(previewLeft, previewRight);
        }
    }
}