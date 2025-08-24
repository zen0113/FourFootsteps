using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePieceShadow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Material shadowMat; // 위 셰이더로 만든 머티리얼
    [SerializeField] private Color activeShadow = new Color(0, 0, 0, 0.45f);
    [SerializeField] private Color inactiveShadow = new Color(0, 0, 0, 0f);

    Material inst; Color current;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        inst = Instantiate(shadowMat);
        sr.material = inst;
        current = inactiveShadow;
        inst.SetColor("_ShadowColor", current);
    }

    public void SetSelected(bool on)
    {
        StopAllCoroutines();
        StartCoroutine(FadeShadow(on ? activeShadow : inactiveShadow, 0.12f));
    }

    System.Collections.IEnumerator FadeShadow(Color target, float dur)
    {
        float t = 0f;
        Color start = current;
        while (t < dur)
        {
            t += Time.deltaTime;
            current = Color.Lerp(start, target, Mathf.SmoothStep(0, 1, t / dur));
            inst.SetColor("_ShadowColor", current);
            yield return null;
        }
        current = target;
        inst.SetColor("_ShadowColor", current);
    }
}
