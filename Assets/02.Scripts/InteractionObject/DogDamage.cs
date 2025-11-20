using UnityEngine;

public class DogDamage : MonoBehaviour
{
    // DogAutoMove에서 데미지 값과 복귀 로직을 가져오기 위한 참조
    private DogAutoMove dogMove;

    [Header("피해 쿨다운")]
    // 이 값으로 밀려나지 않으면서도 연달아 피해를 입지 않게 제어합니다.
    [SerializeField] private float damageCooldown = 0.5f;
    private float lastDamageTime;

    private void Awake()
    {
        dogMove = GetComponent<DogAutoMove>();
        if (dogMove == null)
        {
            Debug.LogError("DogDamage requires DogAutoMove component on the same GameObject.");
        }
    }

    // Is Trigger가 True인 콜라이더끼리 접촉 시 호출됩니다.
    private void OnTriggerStay2D(Collider2D other)
    {
        // 1. 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            // 2. 쿨다운 확인: 피해 간격이 지났을 때만 피해를 줍니다.
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                // 3. 데미지 처리
                PlayerHp playerHp = other.GetComponent<PlayerHp>();
                if (playerHp != null && dogMove != null)
                {
                    // DogAutoMove의 attackDamage 값을 사용
                    playerHp.TakeDamage(dogMove.attackDamage);
                    lastDamageTime = Time.time;

                    // 4. 데미지를 준 후 추적을 멈추고 복귀 (기존의 OnCollisionEnter2D 역할)
                    dogMove.StopChasing();

                    // (선택) 플레이어를 강제로 살짝 밀어내거나 멈추는 효과를 주어 충돌 피드백 강화
                }
            }
        }
    }
}