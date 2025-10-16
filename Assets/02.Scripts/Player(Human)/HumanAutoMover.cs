using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HumanAutoMover : MonoBehaviour
{
    private PlayerHumanMovement humanMovement;
    public Transform targetPoint;
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;

    public Action OnArrived; // 도착 콜백

    private SpriteRenderer spriteRenderer;
    public Animator animator;
    private AudioSource audioSource; // AudioSource 추가
    [SerializeField] private bool isMoving = false;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip walkSound; // 걷기 소리
    [SerializeField] private float walkSoundInterval = 0.3f; // 걷기 소리 재생 간격
    private float lastWalkSoundTime; // 마지막 걷기 소리 재생 시간

    // --- 엔딩용: 임시 타겟 캐시 ---
    private Transform _runtimeTarget;

    public enum EndingExitSide { Auto, Left, Right, Up, Down, NearestEdge }


    private void Awake()
    {
        humanMovement = TryGetComponent(out PlayerHumanMovement hm) ? hm : null;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); // AudioSource 컴포넌트 가져오기
        if (audioSource == null) // AudioSource가 없으면 추가
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // 무릎 꿇는 애니메이션 재생
    public void SetHumanCrouch(bool isActive, bool isHoldingCarrier=false)
    {
        animator.SetBool("Crouch", isActive);
        animator.SetBool("Holding_Carrier", isHoldingCarrier);
    }

    public void StartMoving(Transform destination, bool isHoldingCarrier = false)
    {
        targetPoint = destination;
        isMoving = true;
        animator?.SetBool("Moving", true); // 이동 시작 시 애니메이션 설정
        animator?.SetBool("Holding_Carrier", isHoldingCarrier);
        lastWalkSoundTime = Time.time; // 이동 시작 시 바로 소리 재생을 위해 초기화
        PlayWalkSound(); // 이동 시작 시 걷는 소리 바로 재생
        humanMovement?.BlockMiniGameInput(true);
    }

    /// <summary>
    /// 엔딩 연출용 자동 이동: 현재 카메라 화면 밖(보정거리 포함)으로 캐릭터를 이동시킵니다.
    /// 기본값은 스프라이트가 보는 방향으로 좌/우 화면 밖으로 나감.
    /// </summary>
    /// <param name="isHoldingCarrier">잡고 있는 애니메이션 여부</param>
    /// <param name="margin">화면 바깥으로 더 나갈 보정 거리(월드좌표 단위)</param>
    /// <param name="side">나갈 방향(기본 Auto: flipX 기준 좌/우, 또는 가장 가까운 가장자리)</param>
    public void StartMovingInEnding(float margin = 0.5f)
    {
        EndingExitSide side = EndingExitSide.Right;
        
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[HumanAutoMover] Camera.main 이 없습니다. StartMovingInEnding 실패.");
            return;
        }
        cam.GetComponent<FollowCamera>().enabled = false;
        
        // 캐릭터와 카메라의 Z 차이를 기준으로 Viewport->World 변환
        float z = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 worldBL = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z)); // bottom-left
        Vector3 worldTR = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z)); // top-right

        float minX = Mathf.Min(worldBL.x, worldTR.x);
        float maxX = Mathf.Max(worldBL.x, worldTR.x);
        float minY = Mathf.Min(worldBL.y, worldTR.y);
        float maxY = Mathf.Max(worldBL.y, worldTR.y);

        // 현재 위치 기준으로 어떤 가장자리로 나갈지 결정
        Vector3 targetPos = transform.position;

        EndingExitSide decided = side;
        if (decided == EndingExitSide.Auto)
        {
            // 스프라이트 방향 우선. flipX=true면 왼쪽을 봄.
            decided = (spriteRenderer != null && spriteRenderer.flipX) ? EndingExitSide.Left : EndingExitSide.Right;
        }
        else if (decided == EndingExitSide.NearestEdge)
        {
            // 가장 가까운 가장자리 선택
            float dxLeft = Mathf.Abs(transform.position.x - minX);
            float dxRight = Mathf.Abs(transform.position.x - maxX);
            float dyDown = Mathf.Abs(transform.position.y - minY);
            float dyUp = Mathf.Abs(transform.position.y - maxY);

            float best = dxLeft;
            decided = EndingExitSide.Left;

            if (dxRight < best) { best = dxRight; decided = EndingExitSide.Right; }
            if (dyDown < best) { best = dyDown; decided = EndingExitSide.Down; }
            if (dyUp < best) { best = dyUp; decided = EndingExitSide.Up; }
        }

        switch (decided)
        {
            case EndingExitSide.Left:
                targetPos.x = minX - margin;
                targetPos.y = Mathf.Clamp(transform.position.y, minY + 0.1f, maxY - 0.1f);
                if (spriteRenderer) spriteRenderer.flipX = true;
                break;
            case EndingExitSide.Right:
                targetPos.x = maxX + margin;
                targetPos.y = Mathf.Clamp(transform.position.y, minY + 0.1f, maxY - 0.1f);
                if (spriteRenderer) spriteRenderer.flipX = false;
                break;
            case EndingExitSide.Up:
                targetPos.y = maxY + margin;
                targetPos.x = Mathf.Clamp(transform.position.x, minX + 0.1f, maxX - 0.1f);
                break;
            case EndingExitSide.Down:
                targetPos.y = minY - margin;
                targetPos.x = Mathf.Clamp(transform.position.x, minX + 0.1f, maxX - 0.1f);
                break;
        }

        // 임시 타겟 Transform 준비
        if (_runtimeTarget == null)
        {
            var go = new GameObject("[HumanAutoMover] RuntimeTarget");
            go.hideFlags = HideFlags.HideInHierarchy;
            _runtimeTarget = go.transform;
        }
        _runtimeTarget.position = targetPos;

        // 이동 시작
        StartMoving(_runtimeTarget);
    }

    private void Update()
    {
        if (!isMoving || targetPoint == null)
        {
            if (!isMoving)
            {
                animator?.SetBool("Moving", false); // 정지 시 애니메이션 끄기
                if (audioSource.isPlaying && audioSource.clip == walkSound) // 걷는 소리 재생 중이면 멈춤
                {
                    audioSource.Stop();
                    humanMovement?.BlockMiniGameInput(false);
                }
            }
            return;
        }

        Vector2 direction = targetPoint.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            isMoving = false;
            animator?.SetBool("Moving", false); // 도착 시 애니메이션 끄기
            if (audioSource.isPlaying && audioSource.clip == walkSound) // 걷는 소리 재생 중이면 멈춤
            {
                audioSource.Stop();
            }
            OnArrived?.Invoke(); // 도착 이벤트 호출
            return;
        }

        // 방향 반전
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;

        Vector2 moveDir = direction.normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);
        animator?.SetBool("Moving", true); // 이동 중 애니메이션 설정

        // 걷는 소리 재생
        if (Time.time - lastWalkSoundTime >= walkSoundInterval)
        {
            PlayWalkSound();
            lastWalkSoundTime = Time.time;
        }
    }

    private void PlayWalkSound()
    {
        if (walkSound != null)
        {
            // 현재 재생 중인 소리가 걷는 소리가 아니거나, 재생 중인 소리가 없으면 재생
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
            {
                audioSource.PlayOneShot(walkSound);
            }
        }
    }

}
