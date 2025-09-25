using UnityEngine;

public class GeneratorInteractable : MonoBehaviour
{
    [Header("상호작용 설정")]
    public float interactionRange = 2.5f;
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("발전기 상태")]
    public bool isCompleted = false;
    public AudioClip generatorHumSound;
    
    private Transform player;
    private bool playerInRange = false;
    private MinigameManager minigameManager;
    private AudioSource audioSource;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        minigameManager = FindObjectOfType<MinigameManager>();
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        if (isCompleted) return;
        
        CheckPlayerDistance();
        
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            StartMinigame();
        }
    }
    
    void CheckPlayerDistance()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;
    }
    
    void StartMinigame()
    {
        if (minigameManager != null)
        {
            minigameManager.StartGeneratorMinigame(this);
        }
    }
    
    public void CompleteGenerator()
    {
        isCompleted = true;
        
        // 완료 사운드 재생
        if (audioSource && generatorHumSound)
        {
            audioSource.PlayOneShot(generatorHumSound);
        }
        
        Debug.Log("발전기 수리 완료!");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}