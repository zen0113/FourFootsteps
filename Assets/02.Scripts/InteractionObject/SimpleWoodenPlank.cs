using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWoodenPlank : MonoBehaviour
{
    [Header("나무판자 설정")]
    [SerializeField] private GameObject triggerObject;      // 판자를 부술 물체
    [SerializeField] private GameObject brokenPlankPrefab;  // 깨진 나무판자 프리팹 (선택사항)
    [SerializeField] private ParticleSystem woodParticles; // 나무 파편 파티클 (선택사항)
    [SerializeField] private AudioClip breakSound;         // 깨지는 소리 (선택사항)
    [SerializeField] private float shakeIntensity = 0.1f;  // 흔들림 강도
    [SerializeField] private float shakeDuration = 0.3f;   // 흔들림 지속시간
    
    [Header("구멍 생성 설정")]
    [SerializeField] private Vector2 holeSize = new Vector2(2f, 0.5f); // 구멍 크기
    [SerializeField] private bool createHoleAtImpactPoint = true;      // 충격 지점에 구멍 생성
    [SerializeField] private float destroyDelay = 0.5f;    // 판자 제거까지의 지연시간
    
    private Collider2D plankCollider;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isBroken = false;
    private Vector3 originalPosition;
    
    // 플레이어 추적용
    private List<GameObject> playersOnPlank = new List<GameObject>();
    
    private void Awake()
    {
        plankCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.position;
        
        // AudioSource가 없으면 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // triggerObject와 충돌했을 때 즉시 판자 깨기
        if (triggerObject != null && collision.gameObject == triggerObject && !isBroken)
        {
            Vector3 impactPoint = collision.contacts[0].point;
            StartCoroutine(BreakPlank(impactPoint));
            
            Debug.Log("[SimpleWoodenPlank] 지정된 물체와 충돌! 판자가 깨집니다!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 판자에 올라왔을 때
        if (other.CompareTag("Player"))
        {
            if (!playersOnPlank.Contains(other.gameObject))
            {
                playersOnPlank.Add(other.gameObject);
            }
            Debug.Log($"[SimpleWoodenPlank] 플레이어가 판자에 올라옴. 현재 {playersOnPlank.Count}명");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어가 판자에서 내려갔을 때
        if (other.CompareTag("Player"))
        {
            playersOnPlank.Remove(other.gameObject);
            Debug.Log($"[SimpleWoodenPlank] 플레이어가 판자에서 내려감. 현재 {playersOnPlank.Count}명");
        }
    }
    
    private IEnumerator BreakPlank(Vector3 impactPoint)
    {
        if (isBroken) yield break;
        
        isBroken = true;
        Debug.Log("[SimpleWoodenPlank] 판자가 깨집니다!");
        
        // 1. 판자 흔들기 효과
        yield return StartCoroutine(ShakePlank(shakeIntensity, shakeDuration));
        
        // 2. 깨지는 사운드 재생
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
        
        // 3. 파티클 효과 재생 (충격 지점에서)
        if (woodParticles != null)
        {
            if (createHoleAtImpactPoint)
            {
                woodParticles.transform.position = impactPoint;
            }
            woodParticles.Play();
        }
        
        // 4. 구멍 생성
        Vector3 holePosition = createHoleAtImpactPoint ? impactPoint : transform.position;
        CreateHole(holePosition);
        
        // 5. 스프라이트 변경 또는 프리팹 교체
        if (brokenPlankPrefab != null)
        {
            // 깨진 판자 프리팹으로 교체
            GameObject brokenPlank = Instantiate(brokenPlankPrefab, transform.position, transform.rotation);
            brokenPlank.transform.SetParent(transform.parent);
            
            // 기존 판자 스프라이트 비활성화
            spriteRenderer.enabled = false;
        }
        else
        {
            // 프리팹이 없으면 스프라이트 제거
            spriteRenderer.enabled = false;
        }
        
        // 6. 플레이어가 판자 위에 있다면 구멍으로 이동
        if (playersOnPlank.Count > 0)
        {
            DropPlayersThrough(holePosition);
        }
        
        // 7. 판자 완전히 제거
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
    
    private IEnumerator ShakePlank(float intensity, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0
            );
            
            transform.position = originalPosition + randomOffset;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 원래 위치로 복원
        transform.position = originalPosition;
    }
    
    private void CreateHole(Vector3 holeCenter)
    {
        // 기존 콜라이더 비활성화
        if (plankCollider != null)
        {
            plankCollider.enabled = false;
        }
        
        // 구멍 양쪽에 새로운 콜라이더 생성
        CreateSideColliders(holeCenter);
    }
    
    private void CreateSideColliders(Vector3 holeCenter)
    {
        // 월드 좌표를 로컬 좌표로 변환
        Vector3 localHoleCenter = transform.InverseTransformPoint(holeCenter);
        
        // 판자의 원래 크기 가져오기
        Bounds originalBounds = plankCollider.bounds;
        Vector3 localBounds = originalBounds.size;
        
        // 왼쪽 부분 콜라이더
        float leftEdge = -localBounds.x / 2;
        float leftLimit = localHoleCenter.x - holeSize.x / 2;
        
        if (leftLimit > leftEdge)
        {
            GameObject leftPart = new GameObject("LeftPlankPart");
            leftPart.transform.SetParent(transform);
            leftPart.transform.localPosition = Vector3.zero;
            leftPart.transform.localRotation = Quaternion.identity;
            leftPart.transform.localScale = Vector3.one;
            
            BoxCollider2D leftCollider = leftPart.AddComponent<BoxCollider2D>();
            float leftWidth = leftLimit - leftEdge;
            leftCollider.size = new Vector2(leftWidth, localBounds.y);
            leftCollider.offset = new Vector2(leftEdge + leftWidth / 2, 0);
            
            Debug.Log($"[SimpleWoodenPlank] 왼쪽 콜라이더 생성: 너비 {leftWidth:F2}");
        }
        
        // 오른쪽 부분 콜라이더
        float rightEdge = localBounds.x / 2;
        float rightLimit = localHoleCenter.x + holeSize.x / 2;
        
        if (rightLimit < rightEdge)
        {
            GameObject rightPart = new GameObject("RightPlankPart");
            rightPart.transform.SetParent(transform);
            rightPart.transform.localPosition = Vector3.zero;
            rightPart.transform.localRotation = Quaternion.identity;
            rightPart.transform.localScale = Vector3.one;
            
            BoxCollider2D rightCollider = rightPart.AddComponent<BoxCollider2D>();
            float rightWidth = rightEdge - rightLimit;
            rightCollider.size = new Vector2(rightWidth, localBounds.y);
            rightCollider.offset = new Vector2(rightEdge - rightWidth / 2, 0);
            
            Debug.Log($"[SimpleWoodenPlank] 오른쪽 콜라이더 생성: 너비 {rightWidth:F2}");
        }
    }
    
    private void DropPlayersThrough(Vector3 holePosition)
    {
        foreach (GameObject player in playersOnPlank)
        {
            if (player != null)
            {
                // 플레이어를 구멍 위치로 이동
                Vector3 dropPosition = new Vector3(
                    holePosition.x,
                    player.transform.position.y,
                    player.transform.position.z
                );
                
                player.transform.position = dropPosition;
                
                // 플레이어에게 하향 속도 부여
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, -3f);
                }
                
                Debug.Log($"[SimpleWoodenPlank] 플레이어 {player.name}를 구멍으로 떨어뜨림");
            }
        }
        
        // 플레이어 목록 초기화
        playersOnPlank.Clear();
    }
    
    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        // 구멍 크기 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(holeSize.x, holeSize.y, 0.1f));
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "나무판자");
        #endif
    }
    
    // 외부에서 사용할 수 있는 프로퍼티 및 메서드
    public bool IsBroken => isBroken;
    public int PlayersOnPlank => playersOnPlank.Count;
    
    // 테스트용 강제 깨기
    public void ForceBreak()
    {
        if (!isBroken)
        {
            StartCoroutine(BreakPlank(transform.position));
        }
    }
}