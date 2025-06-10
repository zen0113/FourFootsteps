using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownObject : MonoBehaviour
{
    [Header("드롭 설정")]
    [SerializeField] private GameObject[] objectsToDrop;
    [SerializeField] private float dropDelay = 0.3f;
    [SerializeField] private float dropForce = 5f;
    
    [Header("드롭 방향")]
    [SerializeField] private Vector2 dropDirection = Vector2.down;
    
    [Header("깨지는 효과 설정")]
    [SerializeField] private AudioClip breakSound; // 깨지는 소리
    [SerializeField] private Sprite brokenSprite; // 깨진 후 스프라이트
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.3f; // 사운드 볼륨 (0~1)
    
    private bool hasTriggered = false;
    private Coroutine dropRoutine;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            
            if (objectsToDrop.Length > 0)
            {
                dropRoutine = StartCoroutine(DropObjectsSequentially());
            }
            else
            {
                Debug.LogWarning("드롭할 오브젝트가 없습니다. Inspector에서 오브젝트를 추가해주세요.");
            }
        }
    }
    
    private IEnumerator DropObjectsSequentially()
    {
        for (int i = 0; i < objectsToDrop.Length; i++)
        {
            GameObject obj = objectsToDrop[i];
            
            if (obj != null)
            {
                // 기존 물리 설정
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody2D>();
                }
                
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                rb.freezeRotation = true;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                
                BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = obj.AddComponent<BoxCollider2D>();
                }
                
                // 스프라이트 교체 방식 GroundLandingHandler 추가
                SimpleGroundLandingHandler landingHandler = obj.GetComponent<SimpleGroundLandingHandler>();
                if (landingHandler == null)
                {
                    landingHandler = obj.AddComponent<SimpleGroundLandingHandler>();
                }
                
                // 깨지는 효과 설정 전달
                landingHandler.SetBreakEffects(breakSound, brokenSprite, soundVolume);
                
                rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);
                
                yield return new WaitForSeconds(dropDelay);
            }
            else
            {
                Debug.LogWarning($"배열의 {i}번 인덱스에 있는 오브젝트가 null입니다.");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // 트리거 영역 시각화
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D)
            {
                BoxCollider2D boxCollider = col as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
        
        // 드롭 방향 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, dropDirection.normalized * 1.5f);
        
        // 연결된 오브젝트 표시
        Gizmos.color = Color.cyan;
        if (objectsToDrop != null)
        {
            for (int i = 0; i < objectsToDrop.Length; i++)
            {
                if (objectsToDrop[i] != null)
                {
                    Gizmos.DrawLine(transform.position, objectsToDrop[i].transform.position);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(objectsToDrop[i].transform.position + Vector3.up * 0.5f, $"{i+1}");
                    #endif
                }
            }
        }
    }
}

// 간단한 스프라이트 교체 방식 Ground 착지 처리 컴포넌트
public class SimpleGroundLandingHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool hasLanded = false;
    
    // 깨지는 효과 설정
    private AudioClip breakSound;
    private Sprite brokenSprite;
    private Sprite originalSprite; // 원본 스프라이트 보관
    private float soundVolume = 0.3f; // 사운드 볼륨
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 원본 스프라이트 저장
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
        
        // AudioSource 추가 (없을 경우)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public void SetBreakEffects(AudioClip sound, Sprite broken, float volume)
    {
        breakSound = sound;
        brokenSprite = broken;
        soundVolume = volume;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && !hasLanded)
        {
            hasLanded = true;
            
            ContactPoint2D contact = collision.GetContact(0);
            
            if (contact.normal.y > 0.5f)
            {
                // 위치 조정
                float halfHeight = boxCollider.bounds.extents.y;
                Vector3 newPosition = new Vector3(
                    transform.position.x, 
                    contact.point.y + halfHeight, 
                    transform.position.z);
                
                transform.position = newPosition;
                
                // 물리 이동 중지
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                
                // 깨지는 효과 실행
                PlayBreakEffects();
                
                Debug.Log($"{gameObject.name}이(가) Ground 위에 착지하고 깨졌습니다.");
            }
        }
    }
    
    private void PlayBreakEffects()
    {
        // 1. 사운드 재생 (볼륨 조절)
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound, soundVolume);
        }
        
        // 2. 스프라이트 교체
        if (brokenSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = brokenSprite;
            Debug.Log($"{gameObject.name}의 스프라이트가 깨진 모습으로 변경되었습니다.");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 깨진 스프라이트가 설정되지 않았습니다.");
        }
    }
    
    // 스프라이트를 원래대로 되돌리는 함수 (필요시 사용)
    public void RestoreOriginalSprite()
    {
        if (originalSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = originalSprite;
            hasLanded = false; // 다시 떨어뜨릴 수 있도록 설정
            Debug.Log($"{gameObject.name}의 스프라이트가 원래대로 복구되었습니다.");
        }
    }
    
    private void FixedUpdate()
    {
        if (hasLanded)
        {
            return;
        }
        
        // 레이캐스트로 Ground 체크
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            boxCollider.bounds.extents.y + 0.1f,
            LayerMask.GetMask("Ground"));
            
        if (hit.collider != null)
        {
            hasLanded = true;
            
            float halfHeight = boxCollider.bounds.extents.y;
            Vector3 newPosition = new Vector3(
                transform.position.x,
                hit.point.y + halfHeight,
                transform.position.z);
                
            transform.position = newPosition;
            
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            
            // 깨지는 효과 실행
            PlayBreakEffects();
            
            Debug.Log($"{gameObject.name}이(가) Ground 위에 레이캐스트로 착지하고 깨졌습니다.");
        }
    }
}