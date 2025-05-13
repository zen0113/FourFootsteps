using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHp : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("UI Components")]
    //[SerializeField] private Image[] hearts;
    //[SerializeField] private Sprite fullHeart;
    //[SerializeField] private Sprite emptyHeart;
    [SerializeField] private TextMeshProUGUI healthText;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // HP 텍스트 UI를 업데이트합니다.
    private void UpdateHealthUI()
    {
        //for (int i = 0; i < hearts.Length; i++)
        //{
        //    if (i < currentHealth)
        //        hearts[i].sprite = fullHeart;
        //    else
        //        hearts[i].sprite = emptyHeart;
        //}
        healthText.text = $"x {currentHealth}";
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 사망 처리, 애니메이션, 이동 등
    }

    public int CurrentHealth => currentHealth;
}
