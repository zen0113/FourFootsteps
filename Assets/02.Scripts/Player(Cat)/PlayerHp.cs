using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class PlayerHp : MonoBehaviour
{
    [Header("플레이어 체력 변수")]
    [SerializeField] private int maxHp;
    [SerializeField] private int currentHp;

    [Header("UI HP 컴포넌트")]
    public GameObject heartPrefab;
    [SerializeField] protected GameObject heartParent;  // Grid Layout Group

    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;
    //[SerializeField] private Image[] hearts;
    //[SerializeField] private TextMeshProUGUI healthText;

    [Header("플레이어 무적상태")]
    [SerializeField] private float invincibilityDuration = 1.5f; // 무적 시간 (초)
    public bool isInvincible = false;

    [Header("Camera Shake Effect")]
    [SerializeField] private CameraShake cameraShake;

    [Header("Game Over Settings")] // 추가
    [SerializeField] private bool useGameOverScene = false; // false면 스테이지 재시작, true면 GameOver 씬
    [SerializeField] private float restartDelay = 1.5f; // 재시작 전 대기 시간

    private bool isGameOverLoading = false;

    // Stage3의 Chase 페이즈의 무적용 변수
    private bool isChasePhaseInvincible = false;

    private void Awake()
    {
        // GameManager가 Title에서 부터 있게 만들면 밑의 두줄로 되게 하기

        //maxHp = (int)GameManager.Instance.GetVariable("MaxHP");
        //currentHp = (int)GameManager.Instance.GetVariable("CurrentHP");
    }

    // Start is called before the first frame update
    void Start()
    {
        cameraShake = Camera.main.GetComponent<CameraShake>();
        heartParent = UIManager.Instance.heartParent;

        maxHp = (int)GameManager.Instance.GetVariable("MaxHP");
        currentHp = (int)GameManager.Instance.GetVariable("CurrentHP");

        currentHp = maxHp;
        CreateHearts();
        cameraShake.enabled = false;

        isChasePhaseInvincible = false;
    }

    // create 5 hearts on screen on Player UI Canvas 
    // 이게 매 스테이지마다 계속 Start때마다 호출되어서
    // 스테이지1->회상1->스테이지2 에 왔을 때 UI의 HP가 5개->10개로 불어나있는 오류 발생
    // Destroy(child.gameObject); 으로 UI에 남아있는 하트 삭제 후 생성되게 함
    public void CreateHearts()
    {
        int heartCount = currentHp;

        if (heartParent.transform.childCount > 0)
        {
            foreach(Transform child in heartParent.transform)
                Destroy(child.gameObject);
        }

        // create heart on screen by creating instances of heart prefab under heart parent
        for (int i = 0; i < heartCount; i++)
        {
            Instantiate(heartPrefab, heartParent.transform);
        }
    }

    public void DecrementHealthPoint()
    {
        //int presentHeartIndex = currentHp - 1;

        //// pop heart on screen
        //GameObject heart = heartParent.transform.GetChild(presentHeartIndex).gameObject;

        //// animate heart by triggering "break" animation
        //heart.GetComponent<Animator>().SetTrigger("Break");

        for (int i = 0; i < heartParent.transform.childCount; i++)
        {
            if (i < currentHp)
            {
                heartParent.transform.GetChild(i).GetComponent<Image>().sprite = fullHeart;
            }
            else
            {
                heartParent.transform.GetChild(i).GetComponent<Image>().sprite = emptyHeart;
            }
        }

        var catMovement = gameObject.GetComponent<PlayerCatMovement>();

        if (catMovement != null && catMovement.enabled)
        {
            catMovement.PlayHurtSound();
        }
        else
        {
            var autoRunner = gameObject.GetComponent<PlayerAutoRunner>();
            if (autoRunner != null)
            {
                autoRunner.PlayHurtSound();
            }
            else
            {
                Debug.LogWarning("Neither PlayerCatMovement nor PlayerAutoRunner is active!");
            }
        }

        // 빨간 비네팅 실행
        Warning();
        // 카메라 흔들림 효과
        cameraShake.enabled = true;
        cameraShake.ShakeAndDisable(0.5f, 0.25f);
    }

    protected void Warning()
    {
        StartCoroutine(UIManager.Instance.WarningCoroutine());
    }

    
    public void SetChasePhaseInvincible(bool isActive)
    {
        isChasePhaseInvincible = isActive;
    }


    public void TakeDamage(int damage)
    {
        if (isInvincible || isGameOverLoading|| isChasePhaseInvincible) return; // 무적 상태면 데미지 무시

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        GameManager.Instance.SetVariable("CurrentHP", currentHp);

        DecrementHealthPoint();

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(ActivateInvincibility());
        }
    }

    private IEnumerator ActivateInvincibility()
    {
        isInvincible = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            sr.enabled = false;
            yield return new WaitForSeconds(0.1f);
            sr.enabled = true;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }

        isInvincible = false;
    }

    //// HP 텍스트 UI를 업데이트합니다.
    //private void UpdateHealthUI()
    //{
    //    for (int i = 0; i < hearts.Length; i++)
    //    {
    //        if (i < currentHp)
    //            hearts[i].sprite = fullHeart;
    //        else
    //            hearts[i].sprite = emptyHeart;
    //    }
    //    //healthText.text = $"x {currentHealth}";
    //}

   private void Die()
    {
        isGameOverLoading = true;

        // UI 비활성화
        UIManager.Instance.SetUI(eUIGameObjectName.CatVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.HumanVersionUIGroup, false);
        UIManager.Instance.SetUI(eUIGameObjectName.PuzzleBagButton, false);
        UIManager.Instance.SetUI(eUIGameObjectName.PlaceUI, false);
        UIManager.Instance.SetUI(eUIGameObjectName.ResponsibilityGroup, false);

        // 다이얼로그 재생 중이면 끝냄
        DialogueManager.Instance.EndDialogue();

        Debug.Log("플레이어 사망!");

        // useGameOverScene이 true면 GameOver 씬으로, false면 현재 스테이지 재시작
        if (useGameOverScene)
        {
            // 페이드 인 효과와 함께 게임오버 씬 로드
            SceneLoader.Instance.LoadScene("GameOver");
        }
        else
        {
            // SoundPlayer를 통해 StageScene3 사운드 재생
            if (SoundPlayer.Instance != null)
            {
                SoundPlayer.Instance.ChangeSceneBGM("StageScene3");
            }
            
            // 현재 스테이지를 재시작
            StartCoroutine(RestartCurrentStage());
        }
    }

    // 추가된 메서드: 현재 스테이지 재시작
    private IEnumerator RestartCurrentStage()
    {
        // 플레이어 동작 중지
        var catMovement = GetComponent<PlayerCatMovement>();
        var autoRunner = GetComponent<PlayerAutoRunner>();
        
        if (catMovement != null) catMovement.enabled = false;
        if (autoRunner != null) autoRunner.enabled = false;
        
        // 물리 처리 중지
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // 다른 모든 활성 상태를 false로 설정하여 충돌 방지
            animator.SetBool("Moving", false);
            animator.SetBool("Dash", false);
            // animator.SetBool("Jump", false); // Jump는 Bool이 아닐 수 있으므로 확인 필요 (PlayerCatMovement에 Jump Bool이 없음)
            animator.SetBool("Climbing", false);
            animator.SetBool("Crouching", false); // 웅크린 채 이동 (false)

            // 'idle crouch' 상태 (Crouch)를 true로 설정
            animator.SetBool("Crouch", true);
        }


        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(restartDelay);

        // GameManager의 변수들 리셋 (HP를 최대치로)
        GameManager.Instance.SetVariable("CurrentHP", maxHp);
        GameManager.Instance.SetVariable("MaxHP", maxHp);

        if (SoundPlayer.Instance != null)
        {
            SoundPlayer.Instance.StopAllBGM();
        }

        // 현재 씬을 다시 로드
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // SceneLoader를 사용하여 페이드 효과와 함께 재로드
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(currentSceneName);
        }
        else
        {
            // SceneLoader가 없으면 직접 로드
            SceneManager.LoadScene(currentSceneName);
        }
    }

    // 외부 접근용 프로퍼티
    public int CurrentHp => currentHp;

    public void SetIsInvincible(bool isInvincible) { this.isInvincible = isInvincible; }
}
