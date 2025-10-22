using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [SerializeField] private GameObject PuzzleGameSet;
    [SerializeField] private CanvasGroup PuzzleExplainCanvas;

    [SerializeField] private List<PuzzlePiece> pieces; // 맞춰야 할 5개만 등록
    //[SerializeField] private ParticleSystem completeFX;
    [SerializeField] private AudioClip sfxComplete;
    [SerializeField] private GameObject fullImageOverlayGroup;
    // 완성 시 나타날 원본 이미지(초기 알파 0, 액자 안에 정답 이미지 한 장)
    [SerializeField] private SpriteRenderer[] fullImageSprites;

    private int placedCount = 0;
    private bool completed = false;

    public bool isBadEnding = false;
    [SerializeField] private int ResponsibilityScore;
    [SerializeField] private TutorialController tutorialController;

    void Awake()
    {
        Instance = this;
        // overlay는 처음에 숨김
        if (fullImageOverlayGroup)
        {
            fullImageSprites = fullImageOverlayGroup.GetComponentsInChildren<SpriteRenderer>();

            foreach(SpriteRenderer spriteRenderer in fullImageSprites)
            {
                var c = spriteRenderer.color;
                c.a = 0f;
                spriteRenderer.color = c;
            }
            
        }
        foreach (var sprite in fullImageSprites)
        {
            sprite.gameObject.SetActive(false);
        }

        if (tutorialController == null)
            tutorialController = FindObjectOfType<TutorialController>();
        tutorialController.gameObject.SetActive(false);
    }

    private void Start()
    {
        int score = (int)GameManager.Instance.GetVariable("ResponsibilityScore");

        isBadEnding = (score < 3) ? true : false;
        ResponsibilityScore = score;
        UIManager.Instance.SetAllUI(false);
    }

    public void NotifyPlaced(PuzzlePiece piece)
    {
        placedCount++;
        if (placedCount >= pieces.Count && !completed)
        {
            StartCoroutine(Complete());
        }
    }

    IEnumerator Complete()
    {
        // 아래 3줄은 씬별로 엔딩 테스트하려고 추가
        // 빌드 시, 이 부분은 삭제
        int score = (int)GameManager.Instance.GetVariable("ResponsibilityScore");

        isBadEnding = (score < 3) ? true : false;
        ResponsibilityScore = score;

        // 반짝 FX
        //if (completeFX) completeFX.Play();
        // 효과음
        if (sfxComplete) AudioSource.PlayClipAtPoint(sfxComplete, Camera.main.transform.position, 0.6f);

        completed = true;
        StartCoroutine(UIManager.Instance.FadeCanvasGroup(PuzzleExplainCanvas, false, 1f));

        // 선이 사라지고 사진이 또렷해지는 연출(페이드 인)
        yield return StartCoroutine(FadeInOverlay());

        yield return new WaitForSeconds(2f);
        // 2초 후 화이트 아웃 발생(tutorialController 활성화)
        tutorialController.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        PuzzleGameSet.SetActive(false);
    }

    IEnumerator FadeInOverlay(float dur = 0.6f)
    {
        // 모두 보이게
        foreach (var sr in fullImageSprites)
            sr.gameObject.SetActive(true);

        // 대상 인덱스 확인
        if (fullImageSprites == null || fullImageSprites.Length <= 2)
            yield break;

        SpriteRenderer grayTarget = fullImageSprites[1];

        // ── MPB 준비
        var mpb = new MaterialPropertyBlock();
        int GrayID = Shader.PropertyToID("_GrayAmount");

        // (선택) 셰이더에 _GrayAmount 속성이 있는지 안전 확인
        var mat = grayTarget.sharedMaterial;
        if (mat != null && !mat.HasProperty(GrayID))
            Debug.LogWarning("[FadeInOverlay] Target material has no _GrayAmount. Check shader & property name.");

        // 색상 보간: #FFFFFF → #939393
        Color startRGB = Color.white;                        // #FFFFFF
        Color endRGB = new Color32(0x93, 0x93, 0x93, 255); // #939393

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);

            // 부드러운 곡선(같은 커브로 알파/그레이스케일/색상 보간)
            float a = Mathf.SmoothStep(0f, 1f, u); // 알파
            float grayV = a;                           // 그레이스케일 정도 (0→1)

            if (isBadEnding)
            {
                // 1) 다른 스프라이트들은 알파만 페이드
                for (int i = 0; i < fullImageSprites.Length; i++)
                {
                    if (i == 1) continue; // 타깃은 아래에서 개별 처리
                    var sr = fullImageSprites[i];
                    var c = sr.color;
                    c.a = a;
                    sr.color = c;
                }

                // 2) 타깃: 색상 보간 + 알파 적용
                Color rgb = Color.Lerp(startRGB, endRGB, a);
                rgb.a = a;
                grayTarget.color = rgb;

                // 3) 타깃: 그레이스케일(셰이더) 적용 — MPB
                grayTarget.GetPropertyBlock(mpb);
                mpb.SetFloat(GrayID, grayV);
                grayTarget.SetPropertyBlock(mpb);
            }
            else
            {
                // 해피엔딩: 기존 로직 — index 1만 페이드
                if (fullImageSprites.Length > 1)
                {
                    var c = fullImageSprites[1].color;
                    c.a = a;
                    fullImageSprites[1].color = c;
                }
            }

            yield return null;
        }

        // 종료 상태 고정
        if (isBadEnding)
        {
            // 나머지 전부 불투명
            for (int i = 0; i < fullImageSprites.Length; i++)
            {
                var c = fullImageSprites[i].color;
                c.a = 1f;
                fullImageSprites[i].color = c;
            }

            // 타깃 색상 최종값 고정
            var final = endRGB; final.a = 1f;
            grayTarget.color = final;

            // 그레이스케일 1 고정
            grayTarget.GetPropertyBlock(mpb);
            mpb.SetFloat(GrayID, 1f);
            grayTarget.SetPropertyBlock(mpb);
        }
        else
        {
            if (fullImageSprites.Length > 1)
            {
                var c = fullImageSprites[1].color; c.a = 1f; fullImageSprites[1].color = c;
            }
        }
    }
}
