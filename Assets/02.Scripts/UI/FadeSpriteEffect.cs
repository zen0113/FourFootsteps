using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))] // SpriteRenderer가 없으면 자동으로 추가해줌
public class FadeSpriteEffect : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("서서히 나타나는 데 걸리는 시간")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("완전히 나타난 후 유지되는 시간 (요청하신 2초)")]
    [SerializeField] private float stayDuration = 2.0f;

    [Tooltip("서서히 사라지는 데 걸리는 시간")]
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("종료 설정")]
    [Tooltip("페이드 아웃이 끝나면 오브젝트를 비활성화할까요?")]
    [SerializeField] private bool disableAfterFade = true;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 오브젝트가 활성화(SetActive true)될 때마다 실행됨
    private void OnEnable()
    {
        // 시작 전에 투명하게 초기화
        SetAlpha(0f);

        // 연출 코루틴 시작
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // 1. 페이드 인 (서서히 나타나기)
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeInDuration;
            SetAlpha(Mathf.Lerp(0f, 1f, progress)); // 0에서 1로 부드럽게
            yield return null;
        }
        SetAlpha(1f); // 확실하게 불투명하게 설정

        // 2. 대기 (2초)
        yield return new WaitForSeconds(stayDuration);

        // 3. 페이드 아웃 (서서히 사라지기)
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutDuration;
            SetAlpha(Mathf.Lerp(1f, 0f, progress)); // 1에서 0으로 부드럽게
            yield return null;
        }
        SetAlpha(0f); // 확실하게 투명하게 설정

        // 4. 종료 처리
        if (disableAfterFade)
        {
            gameObject.SetActive(false); // 오브젝트 끄기
        }
    }

    // 알파값(투명도)을 변경하는 함수
    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}