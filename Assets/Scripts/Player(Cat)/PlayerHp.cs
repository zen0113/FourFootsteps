using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHp : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("UI Components")]
    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHeartsUI();
    }

    // 데미지를 받아 체력을 감소시킵니다.
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // 음수 방지
        UpdateHeartsUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // 체력 UI를 갱신합니다.
    private void UpdateHeartsUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }

    // 체력이 0이 되었을 때 실행됩니다.
    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 게임 오버 처리, 리스폰, 씬 이동 등
    }

    // 체력을 외부에서 확인할 수 있도록 속성 제공
    public int CurrentHealth => currentHealth;
}
