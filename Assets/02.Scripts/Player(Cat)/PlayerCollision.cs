using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private PlayerHp playerHp;

    // Start is called before the first frame update
    void Start()
    {
        playerHp = GetComponent<PlayerHp>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            playerHp.TakeDamage(1);
        }
    }
}
