// UI_MaskHole.shader
Shader "UI/MaskHole"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend Zero One

        Pass
        {
            // 스텐실 버퍼 설정
            Stencil
            {
                Ref 1             // 스텐실 버퍼에 1이라는 값을 쓸 준비를 합니다.
                Comp Always       // 항상 테스트를 통과시킵니다.
                Pass Replace      // 테스트를 통과하면 Ref 값(1)으로 버퍼를 덮어씁니다.
            }
        }
    }
}