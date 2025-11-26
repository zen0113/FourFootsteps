using System.Collections.Generic;
using UnityEngine;

public class AdvancedParallaxScroller : MonoBehaviour
{
    [Tooltip("배경 스프라이트들을 순서대로 넣어주세요 (예: 왼쪽부터 오른쪽으로).")]
    public List<Transform> backgrounds;

    [Tooltip("배경이 움직이는 속도입니다.")]
    public float scrollSpeed = 1f;

    [Tooltip("왼쪽으로 이동할지 여부를 결정합니다. 체크 해제 시 오른쪽으로 이동합니다.")]
    public bool scrollLeft = true;

    [Tooltip("배경이 이 X 좌표를 넘어가면 위치를 재설정합니다.")]
    public float repositionPointX = -10f;

    private float totalWidth; // 배경 스프라이트들의 전체 너비

    void Start()
    {
        // 배경들의 전체 너비를 계산합니다.
        // 모든 배경의 너비가 동일하다고 가정합니다.
        if (backgrounds != null && backgrounds.Count > 0)
        {
            SpriteRenderer sr = backgrounds[0].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                totalWidth = sr.bounds.size.x * backgrounds.Count;
            }
        }
    }

    void Update()
    {
        // 1. 모든 배경 스프라이트를 이동시킵니다.
        foreach (Transform bg in backgrounds)
        {
            // 이동 방향을 결정합니다. scrollLeft가 true이면 왼쪽(-1), 아니면 오른쪽(1)으로 이동합니다.
            float direction = scrollLeft ? -1f : 1f;
            bg.Translate(new Vector2(direction * scrollSpeed * Time.deltaTime, 0));

            // 2. 재배치 지점을 확인합니다.
            // 왼쪽으로 이동할 때
            if (scrollLeft && bg.position.x < repositionPointX)
            {
                RepositionBackground(bg);
            }
            // 오른쪽으로 이동할 때
            else if (!scrollLeft && bg.position.x > repositionPointX)
            {
                RepositionBackground(bg);
            }
        }
    }

    /// <summary>
    /// 지정된 지점을 넘어간 배경을 맨 뒤로 재배치합니다.
    /// </summary>
    /// <param name="bg">재배치할 배경의 Transform</param>
    private void RepositionBackground(Transform bg)
    {
        // 이동 방향에 따라 전체 너비만큼 위치를 더하거나 빼줍니다.
        // 왼쪽으로 이동 중이었다면, 전체 너비만큼 오른쪽으로 이동하여 맨 뒤에 붙습니다.
        // 오른쪽으로 이동 중이었다면, 전체 너비만큼 왼쪽으로 이동하여 맨 뒤에 붙습니다.
        float offset = scrollLeft ? totalWidth : -totalWidth;
        bg.position = new Vector2(bg.position.x + offset - 20, bg.position.y);
    }
}