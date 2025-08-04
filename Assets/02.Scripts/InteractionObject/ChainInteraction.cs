using System.Collections;
using UnityEngine;

public class ChainInteraction : MonoBehaviour
{
    [Header("사슬 설정")]
    [SerializeField] private float interactionRange = 2f;        // 상호작용 범위
    [SerializeField] private GameObject connectedObject;         // 연결된 오브젝트 (쓰레기통 등)
    [SerializeField] private GameObject chainVisual;             // 사슬 시각적 오브젝트 (회색 박스)
    [SerializeField] private AudioClip chainBreakSound;         // 사슬 끊어지는 소리
    
    [Header("이펙트 설정")]
    [SerializeField] private ParticleSystem breakEffect;        // 사슬 끊어지는 파티클 (선택사항)
    [SerializeField] private float breakAnimationTime = 0.5f;   // 끊어지는 애니메이션 시간
    
    private bool isChainBroken = false;                          // 사슬이 끊어졌는지 여부
    private bool playerInRange = false;                          // 플레이어가 범위 내에 있는지
    private AudioSource audioSource;                            // 오디오 소스
    
    // 연결된 오브젝트의 원본 상태
    private PushableBox pushableBoxComponent;                    // 기존 PushableBox 컴포넌트
    
    private void Start()
    {
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 연결된 오브젝트의 PushableBox 컴포넌트 가져오기
        if (connectedObject != null)
        {
            pushableBoxComponent = connectedObject.GetComponent<PushableBox>();
            
            // PushableBox 컴포넌트가 없다면 추가하지 말고 경고만 출력
            if (pushableBoxComponent == null)
            {
                Debug.LogWarning($"{connectedObject.name}에 PushableBox 컴포넌트가 없습니다. 미리 추가해주세요!");
            }
            else
            {
                // 사슬이 연결된 상태에서는 박스를 밀 수 없도록 비활성화
                pushableBoxComponent.enabled = false;
            }
            
            // Rigidbody2D를 kinematic으로 설정 (움직이지 않도록)
            Rigidbody2D connectedRb = connectedObject.GetComponent<Rigidbody2D>();
            if (connectedRb != null)
            {
                connectedRb.isKinematic = true;
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
        
        // 사운드 재생
        if (chainBreakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(chainBreakSound);
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
    }
    
    private IEnumerator ChainBreakAnimation()
    {
        if (chainVisual != null)
        {
            Vector3 originalScale = chainVisual.transform.localScale;
            float elapsedTime = 0f;
            
            // 사슬이 작아지면서 사라지는 애니메이션
            while (elapsedTime < breakAnimationTime)
            {
                float progress = elapsedTime / breakAnimationTime;
                float scale = Mathf.Lerp(1f, 0f, progress);
                
                chainVisual.transform.localScale = originalScale * scale;
                
                // 약간의 회전 효과 추가
                chainVisual.transform.Rotate(0, 0, 360f * Time.deltaTime);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 애니메이션 완료 후 비활성화
            chainVisual.SetActive(false);
        }
    }
    
    private void ReleaseConnectedObject()
    {
        if (connectedObject != null)
        {
            // Rigidbody2D를 Dynamic으로 변경
            Rigidbody2D connectedRb = connectedObject.GetComponent<Rigidbody2D>();
            if (connectedRb != null)
            {
                connectedRb.isKinematic = false;
            }
            
            // 기존 PushableBox 컴포넌트 활성화 (기존 박스 밀기 시스템 사용)
            if (pushableBoxComponent != null)
            {
                pushableBoxComponent.enabled = true;
            }
            
            // 연결된 오브젝트에 "Box" 태그 추가 (PlayerBoxInteraction에서 인식하도록)
            if (!connectedObject.CompareTag("Box"))
            {
                connectedObject.tag = "Box";
            }
            
            Debug.Log($"{connectedObject.name}이(가) 이제 밀 수 있습니다! (기존 박스 시스템 활용)");
        }
    }
    
    // TODO: UI 표시 로직 나중에 구현
    private void ShowInteractionUI(bool show)
    {
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
        }
    }
}