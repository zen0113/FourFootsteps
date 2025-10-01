Shader "Universal Render Pipeline/2D/Sprite Grayscale (Unlit)"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor]   _Tint("Tint Color", Color) = (1,1,1,1)
        _GrayAmount("Grayscale Amount", Range(0,1)) = 1
    }

        SubShader
        {
            Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderPipeline" = "UniversalPipeline"
                "UniversalMaterialType" = "Unlit"
                "CanUseSpriteAtlas" = "True"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            Pass
            {
                Name "SpriteUnlit2D"
                Tags { "LightMode" = "Universal2D" }

                HLSLPROGRAM
                #pragma vertex   vert
                #pragma fragment frag
                #pragma target   2.0
                #pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv         : TEXCOORD0;
                    float4 color      : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4 positionHCS : SV_Position;
                    float2 uv          : TEXCOORD0;
                    float4 color       : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                    float4 _Tint;
                    float  _GrayAmount;
                CBUFFER_END

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    UNITY_SETUP_INSTANCE_ID(IN);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    OUT.color = IN.color * _Tint;

                    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                    return OUT;
                }

                float3 ToLuma(float3 rgb)
                {
                    return dot(rgb, float3(0.2126, 0.7152, 0.0722)).xxx;
                }

                float4 frag(Varyings IN) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(IN);

                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                    float4 src = tex * IN.color;

                    float3 gray = ToLuma(src.rgb);
                    float3 finalRgb = lerp(src.rgb, gray, saturate(_GrayAmount));

                    return float4(finalRgb, src.a);
                }
                ENDHLSL
            }
        }

            FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
