using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mirror : MonoBehaviour
{
    Camera mirrorCam;
    Texture2D mirrorTexture;
    public SpriteRenderer sr;
    float alphaValue = 150f / 255f;

    // Start is called before the first frame update
    //void Start()
    //{
    //    mirrorCam = GetComponent<Camera>();
    //    mirrorTexture = new Texture2D(mirrorCam.targetTexture.width, mirrorCam.targetTexture.height, TextureFormat.ARGB32, false);
    //    StartCoroutine(camrender());
    //}

    // 기존의 mirrorTexture 만드는 것 결과가 색상이 짙게 되어서 원본 색감 그대로 출력되게 하는 방식 교체
    // 기존의 방식은 주석처리해둠
    void Start()
    {
        mirrorCam = GetComponent<Camera>();

        // 1) HDR 끄기 → LDR sRGB RT를 쓰게 만듦
        mirrorCam.allowHDR = false;

        // 2) sRGB 컬러 RT를 직접 만들어 연결(인스펙터 대신 코드로 보장)
        var desc = new RenderTextureDescriptor(mirrorCam.pixelWidth, mirrorCam.pixelHeight);
        desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB; // sRGB LDR
        desc.depthBufferBits = 0;
        var rt = new RenderTexture(desc);
        rt.name = "MirrorRT_sRGB";
        rt.Create();
        mirrorCam.targetTexture = rt;

        // 3) sRGB Texture2D (linear:false)
        mirrorTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, /*linear*/ false);

        StartCoroutine(camrender());
    }

    WaitForEndOfFrame WaitForEnd = new WaitForEndOfFrame();

    IEnumerator camrender()
    {
        while (true)
        {
            yield return WaitForEnd;

            RenderTexture.active = mirrorCam.targetTexture;

            mirrorCam.Render();

            mirrorTexture.ReadPixels(new Rect(0,0,mirrorTexture.width,mirrorTexture.height),0,0);
            mirrorTexture.Apply();

            sr.sprite = Sprite.Create(mirrorTexture, new Rect(0, 0, mirrorTexture.width, mirrorTexture.height), new Vector2(0.5f, 0.5f), mirrorTexture.width);

            // 알파 150/255 로 낮추기 (약 0.588)
            var c = sr.color;
            c.a = alphaValue;
            sr.color = c;
        }
    }
}
