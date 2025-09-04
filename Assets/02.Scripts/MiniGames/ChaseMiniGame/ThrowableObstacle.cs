using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 던질 물체
public class ThrowableObstacle : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 1;
    private float born;

    [Header("Refs")]
    private Rigidbody2D rb;
    [SerializeField] private Collider2D solidCollider;   // isTrigger=false (Ground용)
    [SerializeField] private Collider2D damageTrigger;   // isTrigger=true  (Player 데미지용)

    //[Header("Landing/Physics")]
    //[SerializeField] private LayerMask groundMask;
    //[SerializeField] private PhysicsMaterial2D landedMaterial; // 튀지 않게 마찰/반발력 낮추기

    void Awake() { 
        rb = GetComponent<Rigidbody2D>();

        if (solidCollider) solidCollider.isTrigger = false;
        if (damageTrigger) damageTrigger.isTrigger = true;
    }

    public void Launch(Vector2 pos, Vector2 velocity)
    {
        transform.position = pos;
        gameObject.SetActive(true);
        born = Time.time;
        rb.velocity = velocity;
        rb.angularVelocity = Random.Range(-360f, 360f); // 약간 회전
    }

    //public void LaunchTowards(Transform muzzle, Vector3 worldTarget, float speed, Rigidbody2D carrier = null)
    //{
    //    // 1) 움직이는 부모 영향 제거
    //    transform.SetParent(null, true);

    //    // 2) 시작 위치
    //    transform.position = muzzle.position;

    //    // 3) 월드 방향
    //    Vector2 dir = (worldTarget - muzzle.position).normalized;

    //    // 4) 캐리어 속도 더하기 (스크롤/추격자 속도 보정)
    //    Vector2 carrierV = carrier ? carrier.velocity : Vector2.zero;

    //    // 5) 최종 속도
    //    rb.isKinematic = false;
    //    rb.velocity = dir * speed + carrierV;

    //    rb.angularVelocity = Random.Range(-360f, 360f); // 약간 회전
    //}


    void Update()
    {
        if (Time.time - born > lifeTime) gameObject.SetActive(false);
    }

    //// Ground와 부딪혀 착지하면 땅과 자연스럽게 함께 흘러가게
    //void OnCollisionEnter2D(Collision2D col)
    //{
    //    if (((1 << col.collider.gameObject.layer) & groundMask) != 0)
    //    {
    //        if (solidCollider && landedMaterial)
    //            solidCollider.sharedMaterial = landedMaterial; // 튕김 최소화
    //    }
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(DelayedAction(1f));
        }
        else if(!other.CompareTag("Enemy"))
        {
            StartCoroutine(DelayedAction(4f));
        }
    }

    IEnumerator DelayedAction(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        gameObject.SetActive(false);
    }
}
