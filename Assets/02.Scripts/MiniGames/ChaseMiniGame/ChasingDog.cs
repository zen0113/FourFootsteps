using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChasingDog : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform ChasersGroup;
    [SerializeField] private Animator animator;
    [SerializeField] private ChaserFollower chaserFollower;
    private string tagName = "Enemy";

    [SerializeField] private float startBufferTime = 1f;
    public bool startChasing = false;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ChasersGroup = GameObject.Find("Chasers").transform;
        animator = GetComponent<Animator>();
        chaserFollower = GetComponent<ChaserFollower>();

        spriteRenderer.flipX = true;
        animator.SetBool("Moving", false);
        animator.speed = 0f;
        chaserFollower.enabled = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!startChasing && other.CompareTag("Player"))
        {
            StartCoroutine(StartChasePlayer());
        }
    }

    IEnumerator StartChasePlayer()
    {
        yield return new WaitForSeconds(startBufferTime);

        Debug.Log("Dog has detected the player. Chase begins.");

        transform.SetParent(ChasersGroup);
        gameObject.tag = tagName;
        spriteRenderer.flipX = false;
        animator.speed = 1f;
        animator.SetBool("Moving", true);
        chaserFollower.enabled = true;
        startChasing = true;
    }

}
