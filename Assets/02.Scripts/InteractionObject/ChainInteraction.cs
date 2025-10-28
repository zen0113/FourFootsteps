using System.Collections;
using UnityEngine;

public class ChainInteraction : MonoBehaviour
{
    [Header("사슬 설정")]
    [SerializeField] private float interactionRange = 2f;        // 상호작용 범위
    [SerializeField] private GameObject connectedObject;         // 연결된 오브젝트 (쓰레기통 등)
    [SerializeField] private GameObject chainVisual;             // 사슬 시각적 오브젝트 (회색 박스)
    [SerializeField] private AudioClip chainBreakSound;         // 사슬 끊어지는 소리
    
    [Header("사운드 설정")]
    [SerializeField] private float soundDuration = 4f;          // 사운드 재생 시간 (기본값 4초)
    
    [Header("이펙트 설정")]
    [SerializeField] private ParticleSystem breakEffect;        // 사슬 끊어지는 파티클 (선택사항)
    [SerializeField] private float breakAnimationTime = 0.5f;   // 끊어지는 애니메이션 시간
    [SerializeField] private Transform chainFallPoint;          // 사슬이 떨어질 목표 지점
    [SerializeField] private float chainFallTime = 1.5f;        // 사슬이 떨어지는 시간
    [SerializeField] private float chainWaitTime = 4f;          // 사슬이 땅에 있는 시간
    [SerializeField] private float chainFadeTime = 2f;          // 사슬이 사라지는 페이드 시간
    
    [Header("연결된 플랫폼 트리거")]
    [SerializeField] private PlatformActivationTrigger linkedPlatformTrigger; // 연결된 PlatformActivationTrigger
    
    private bool isChainBroken = false;                          // 사슬이 끊어졌는지 여부
    private bool playerInRange = false;                          // 플레이어가 범위 내에 있는지
    private AudioSource audioSource;                            // 오디오 소스
    private Coroutine soundCoroutine;                           // 사운드 코루틴 참조
    
    // Sprite Glow 효과
    private SpriteGlow.SpriteGlowEffect spriteGlowEffect;       // Sprite Glow 컴포넌트
    
    // 연결된 오브젝트의 원본 상태
    private PushableBox pushableBoxComponent;                    // 기존 PushableBox 컴포넌트
    private Rigidbody2D connectedRb;                            // 연결된 오브젝트의 Rigidbody2D
    private Collider2D connectedCollider;                       // 연결된 오브젝트의 Collider2D
    
    private void Start()
    {
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Sprite Glow 효과 컴포넌트 가져오기
        spriteGlowEffect = GetComponent<SpriteGlow.SpriteGlowEffect>();
        if (spriteGlowEffect != null)
        {
            spriteGlowEffect.enabled = false; // 처음에는 비활성화
            Debug.Log("Sprite Glow 효과 초기화됨 (비활성화)");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}에 SpriteGlowEffect 컴포넌트가 없습니다. 글로우 효과가 적용되지 않습니다.");
        }
        
        // 연결된 오브젝트의 컴포넌트들 가져오기
        if (connectedObject != null)
        {
            pushableBoxComponent = connectedObject.GetComponent<PushableBox>();
            connectedRb = connectedObject.GetComponent<Rigidbody2D>();
            connectedCollider = connectedObject.GetComponent<Collider2D>();
            
            // 연결된 PlatformActivationTrigger 찾기 및 비활성화
            if (linkedPlatformTrigger != null)
            {
                linkedPlatformTrigger.DeactivateTrigger(); // 비활성화 메서드 호출
                Debug.Log($"연결된 PlatformActivationTrigger를 비활성화했습니다.");
            }
            else
            {
                Debug.LogWarning("LinkedPlatformTrigger가 설정되지 않았습니다. Inspector에서 연결해주세요.");
            }
            
            // PushableBox 컴포넌트가 없다면 경고
            if (pushableBoxComponent == null)
            {
                Debug.LogWarning($"{connectedObject.name}에 PushableBox 컴포넌트가 없습니다. 미리 추가해주세요!");
            }
            else
            {
                // 사슬이 연결된 상태에서는 박스를 밀 수 없도록 비활성화
                pushableBoxComponent.enabled = false;
                Debug.Log($"{connectedObject.name} PushableBox 비활성화됨");
            }
            
            // Rigidbody2D를 kinematic으로 설정 (움직이지 않도록)
            if (connectedRb != null)
            {
                connectedRb.isKinematic = true;
                Debug.Log($"{connectedObject.name} Rigidbody2D kinematic으로 설정됨");
            }
            else
            {
                Debug.LogError($"{connectedObject.name}에 Rigidbody2D가 없습니다!");
            }
            
            // 콜라이더 비활성화 (플레이어가 통과할 수 있도록)
            if (connectedCollider != null)
            {
                connectedCollider.enabled = false;
                Debug.Log($"{connectedObject.name} Collider 비활성화됨 - 플레이어가 통과 가능");
            }
            else
            {
                Debug.LogError($"{connectedObject.name}에 Collider2D가 없습니다!");
            }
        }
    }
    
    private void Update()
    {
        // 사슬이 이미 끊어졌다면 처리하지 않음
        if (isChainBroken) return;
        
        // 플레이어가 범위 내에 있고 E키를 눌렀을 때
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            BreakChain();
        }
    }
    
    private void BreakChain()
    {
        if (isChainBroken) return;
        
        Debug.Log("사슬을 끊었습니다!");
        isChainBroken = true;
        
        // 사운드 재생 (4초만 재생되도록)
        if (chainBreakSound != null && audioSource != null)
        {
            soundCoroutine = StartCoroutine(PlaySoundWithDuration(chainBreakSound, soundDuration));
        }
        
        // 파티클 효과 재생
        if (breakEffect != null)
        {
            breakEffect.Play();
        }
        
        // 사슬 시각적 제거 애니메이션
        StartCoroutine(ChainBreakAnimation());
        
        // 연결된 오브젝트 해제 - 기존 시스템 활용
        ReleaseConnectedObject();
        
        // 상호작용 UI 숨기기
        ShowInteractionUI(false);
        
        // 사슬이 끊어진 후 Sprite Glow 완전히 비활성화
        if (spriteGlowEffect != null)
        {
            spriteGlowEffect.enabled = false;
            Debug.Log("사슬이 끊어져서 Sprite Glow 영구 비활성화");
        }
    }
    
    // 사운드를 특정 시간 동안만 재생하는 코루틴
    private IEnumerator PlaySoundWithDuration(AudioClip clip, float duration)
    {
        // AudioClip 설정 및 재생 시작
        audioSource.clip = clip;
        audioSource.Play();
        
        Debug.Log($"체인 브레이크 사운드 재생 시작 (최대 {duration}초)");
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);
        
        // 사운드가 아직 재생 중이면 중지
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"체인 브레이크 사운드 중지 ({duration}초 경과)");
        }
        
        // AudioClip 클리어 (선택사항)
        audioSource.clip = null;
    }
    
    private IEnumerator ChainBreakAnimation()
    {
        if (chainVisual != null)
        {
            Vector3 originalScale = chainVisual.transform.localScale;
            Vector3 originalPosition = chainVisual.transform.position;
            SpriteRenderer spriteRenderer = chainVisual.GetComponent<SpriteRenderer>();
            Color originalColor = Color.white;
            
            // SpriteRenderer가 있으면 원본 색상 저장
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            
            // 목표 지점 설정 (chainFallPoint가 없으면 아래쪽으로 기본 설정)
            Vector3 targetPosition;
            if (chainFallPoint != null)
            {
                targetPosition = chainFallPoint.position;
            }
            else
            {
                // 기본값: 현재 위치에서 아래로 3만큼 떨어뜨리기
                targetPosition = originalPosition + Vector3.down * 3f;
            }
            
            // 1단계: 사슬이 부드럽게 떨어지는 애니메이션 (Lerp 사용)
            float elapsedTime = 0f;
            
            while (elapsedTime < chainFallTime)
            {
                float progress = elapsedTime / chainFallTime;
                
                // 부드러운 곡선 이동 (EaseOut 효과)
                float easedProgress = 1f - (1f - progress) * (1f - progress);
                Vector3 currentPosition = Vector3.Lerp(originalPosition, targetPosition, easedProgress);
                chainVisual.transform.position = currentPosition;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 정확히 목표 지점에 배치
            chainVisual.transform.position = targetPosition;
            
            // 2단계: 땅에 떨어진 후 대기 시간
            yield return new WaitForSeconds(chainWaitTime);
            
            // 3단계: 페이드아웃되며 사라지는 애니메이션
            elapsedTime = 0f;
            
            while (elapsedTime < chainFadeTime)
            {
                float progress = elapsedTime / chainFadeTime;
                
                // 페이드 아웃 효과
                if (spriteRenderer != null)
                {
                    Color fadeColor = originalColor;
                    fadeColor.a = Mathf.Lerp(1f, 0f, progress);
                    spriteRenderer.color = fadeColor;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 애니메이션 완료 후 원본 색상 복원 및 비활성화
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            chainVisual.SetActive(false);
        }
    }
    
    private void ReleaseConnectedObject()
    {
        if (connectedObject != null)
        {
            // 콜라이더 활성화 (이제 충돌 가능)
            if (connectedCollider != null)
            {
                connectedCollider.enabled = true;
                Debug.Log($"{connectedObject.name} Collider 활성화됨 - 이제 충돌 가능");
            }
            
            // Rigidbody2D를 Dynamic으로 변경
            if (connectedRb != null)
            {
                connectedRb.isKinematic = false;
                Debug.Log($"{connectedObject.name} Rigidbody2D Dynamic으로 변경됨");
            }
            
            // 기존 PushableBox 컴포넌트 활성화 (기존 박스 밀기 시스템 사용)
            if (pushableBoxComponent != null)
            {
                pushableBoxComponent.enabled = true;
                Debug.Log($"{connectedObject.name} PushableBox 활성화됨");
            }
            
            // "Box" 태그가 이미 설정되어 있는지 확인
            if (connectedObject.CompareTag("Box"))
            {
                Debug.Log($"{connectedObject.name}에 이미 Box 태그가 설정되어 있습니다.");
            }
            else
            {
                connectedObject.tag = "Box";
                Debug.Log($"{connectedObject.name}에 Box 태그를 추가했습니다.");
            }
            
            // ★ 핵심: 연결된 PlatformActivationTrigger 활성화 (이제 플레이어가 트리거 영역에 들어가면 작동)
            if (linkedPlatformTrigger != null)
            {
                linkedPlatformTrigger.ActivateTrigger(); // 활성화 메서드 호출
                Debug.Log($"연결된 PlatformActivationTrigger가 활성화되었습니다! 이제 플레이어가 트리거 영역에 들어가면 플랫폼이 활성화됩니다.");
            }
            else
            {
                Debug.Log("연결된 PlatformActivationTrigger가 없습니다. 필요하다면 Inspector에서 설정해주세요.");
            }
            
            Debug.Log($"{connectedObject.name}이(가) 이제 밀 수 있습니다! (기존 박스 시스템 활용)");
        }
    }
    
    // TODO: UI 표시 로직 나중에 구현
    private void ShowInteractionUI(bool show)
    {
        // Sprite Glow 효과 제어
        if (spriteGlowEffect != null && !isChainBroken)
        {
            spriteGlowEffect.enabled = show;
            Debug.Log(show ? "Sprite Glow 활성화" : "Sprite Glow 비활성화");
        }
        
        // 나중에 UI 시스템 연동 예정
        if (show)
        {
            Debug.Log("E키 UI 표시");
        }
        else
        {
            Debug.Log("E키 UI 숨김");
        }
    }
    
    // 플레이어가 트리거에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isChainBroken)
        {
            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance <= interactionRange)
            {
                playerInRange = true;
                ShowInteractionUI(true);
                Debug.Log("사슬 근처에 도착했습니다. E키를 눌러 사슬을 끊으세요.");
            }
        }
    }
    
    // 플레이어가 트리거에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionUI(false);
        }
    }
    
    // 오브젝트가 파괴될 때 사운드 중지
    private void OnDestroy()
    {
        // 코루틴이 실행 중이면 중지
        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
        }
        
        // 사운드가 재생 중이면 중지
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    // 기즈모로 상호작용 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 연결된 오브젝트와의 연결선 표시
        if (connectedObject != null)
        {
            Gizmos.color = isChainBroken ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, connectedObject.transform.position);
            
            // 연결된 PlatformActivationTrigger 상태 시각화
            if (linkedPlatformTrigger != null)
            {
                // IsActivated 프로퍼티 사용
                Gizmos.color = linkedPlatformTrigger.IsActivated ? Color.cyan : Color.gray;
                Gizmos.DrawWireCube(linkedPlatformTrigger.transform.position, Vector3.one * 0.8f);
                
                // 연결선 표시
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, linkedPlatformTrigger.transform.position);
            }
        }
        
        // 사슬 떨어뜨릴 지점 시각화
        if (chainFallPoint != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // 주황색
            Gizmos.DrawWireSphere(chainFallPoint.position, 0.3f);
            
            // 사슬에서 떨어뜨릴 지점까지의 경로 표시
            if (chainVisual != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawLine(chainVisual.transform.position, chainFallPoint.position);
                
                // 화살표 방향 표시
                Vector3 direction = (chainFallPoint.position - chainVisual.transform.position).normalized;
                Vector3 arrowPos = chainVisual.transform.position + direction * 0.8f;
                Gizmos.DrawRay(arrowPos, direction * 0.3f);
            }
        }
    }
}