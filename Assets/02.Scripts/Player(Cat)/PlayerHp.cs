using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private bool isGameOverLoading = false;

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
    }

    // create 5 hearts on screen on Player UI Canvas 
    public void CreateHearts()
    {
        int heartCount = currentHp;

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

    public void TakeDamage(int damage)
    {
        if (isInvincible || isGameOverLoading) return; // 무적 상태면 데미지 무시

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

        // 다이얼로그 재생 중이면 끝냄
        DialogueManager.Instance.EndDialogue();

        Debug.Log("플레이어 사망!");
        // 페이드 인 효과와 함께 게임오버 씬 로드
        SceneLoader.Instance.LoadScene("GameOver");
    }

    // 외부 접근용 프로퍼티
    public int CurrentHp => currentHp;

    public void SetIsInvincible(bool isInvincible) { this.isInvincible = isInvincible; }
}
