using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mirror : MonoBehaviour
{
    Camera mirrorCam;
    Texture2D mirrorTexture;
    public SpriteRenderer sr;

    // Start is called before the first frame update
    void Start()
    {
        mirrorCam = GetComponent<Camera>();
        mirrorTexture = new Texture2D(mirrorCam.targetTexture.width, mirrorCam.targetTexture.height, TextureFormat.ARGB32, false);
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
