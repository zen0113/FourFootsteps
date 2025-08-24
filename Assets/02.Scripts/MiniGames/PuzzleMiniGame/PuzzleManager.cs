using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [SerializeField] private List<PuzzlePiece> pieces; // 맞춰야 할 5개만 등록
    //[SerializeField] private ParticleSystem completeFX;
    [SerializeField] private AudioClip sfxComplete;
    [SerializeField] private SpriteRenderer fullImageOverlay;
    // 완성 시 나타날 원본 이미지(초기 알파 0, 액자 안에 정답 이미지 한 장)

    private int placedCount = 0;
    private bool completed = false;

    void Awake()
    {
        Instance = this;
        // overlay는 처음에 숨김
        if (fullImageOverlay)
        {
            var c = fullImageOverlay.color;
            c.a = 0f;
            fullImageOverlay.color = c;
        }
        fullImageOverlay.gameObject.SetActive(false);
    }

    public void NotifyPlaced(PuzzlePiece piece)
    {
        placedCount++;
        if (placedCount >= pieces.Count && !completed)
        {
            Complete();
        }
    }

    void Complete()
    {
        completed = true;

        // 반짝 FX
        //if (completeFX) completeFX.Play();

        // 효과음
        if (sfxComplete) AudioSource.PlayClipAtPoint(sfxComplete, Camera.main.transform.position, 1f);

        // 선이 사라지고 사진이 또렷해지는 연출(페이드 인)
        if (fullImageOverlay) StartCoroutine(FadeInOverlay());

        // 추가 연출이 필요하면 여기서 트리거(예: UI 텍스트 “완성!”)
    }

    System.Collections.IEnumerator FadeInOverlay(float dur = 0.6f)
    {
        fullImageOverlay.gameObject.SetActive(true);

        float t = 0f;
        Color c = fullImageOverlay.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.SmoothStep(0f, 1f, t / dur);
            fullImageOverlay.color = c;
            yield return null;
        }
        c.a = 1f;
        fullImageOverlay.color = c;
    }
}
