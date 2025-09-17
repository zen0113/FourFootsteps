Shader "Universal Render Pipeline/2D/Sprite Drop Shadow (ShadowOnly)"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _ShadowColor("Shadow Color (RGBA)", Color) = (0,0,0,0.45)
        _ShadowOffset("Shadow Offset (px)", Vector) = (6,-6,0,0)
        _Softness("Softness Radius (px)", Range(0,24)) = 8
        _ShadowBoost("Shadow Boost (alpha gain)", Range(0,2)) = 1.2
        _ShadowGeomExpand("Shadow Geometry Expand (px)", Range(0,48)) = 16
    }

        SubShader
        {
            Tags{
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "CanUseSpriteAtlas" = "True"
                "RenderPipeline" = "UniversalPipeline"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            Pass
            {
                Name "SHADOW_2D"
                Tags { "LightMode" = "Universal2D" }

                HLSLPROGRAM
                #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv         : TEXCOORD0;
                    float4 color      : COLOR;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv          : TEXCOORD0;
                    float4 color       : COLOR;
                };

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _MainTex_TexelSize; // (1/w, 1/h, w, h)

                #ifdef ETC1_EXTERNAL_ALPHA
                TEXTURE2D(_AlphaTex);
                SAMPLER(sampler_AlphaTex);
                #endif

                float4 _ShadowColor;
                float4 _ShadowOffset;
                float  _Softness;
                float  _ShadowBoost;
                float  _ShadowGeomExpand;

                float4 SampleMain(float2 uv)
                {
                    float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    #ifdef ETC1_EXTERNAL_ALPHA
                        c.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                    #endif
                    return c;
                }
                float SampleAlpha(float2 uv) { return SampleMain(uv).a; }

                float ShadowAlpha8Tap(float2 uvBase, float2 uvOffset, float2 uvRadius)
                {
                    float a = SampleAlpha(uvBase + uvOffset);

                    float2 dirs[8] = {
                        float2(1,  0), float2(-1,  0), float2(0,  1), float2(0, -1),
                        normalize(float2(1,  1)), normalize(float2(-1,  1)),
                        normalize(float2(1, -1)), normalize(float2(-1, -1))
                    };
                    [unroll] for (int i = 0; i < 8; i++)
                        a += SampleAlpha(uvBase + uvOffset + dirs[i] * uvRadius);

                    a = (a / 9.0) * _ShadowBoost;
                    return saturate(a);
                }

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;

                    // UV 중심기준(-1..1) 방향
                    float2 uvFromCenter = (IN.uv - 0.5) * 2.0;

                    // 픽셀→UV
                    float2 uvExpand = (_ShadowGeomExpand * _MainTex_TexelSize.xy) * uvFromCenter;

                    // 오브젝트→월드 스케일 길이로 보정 (대략적)
                    float2 objScale = float2(
                        length(float3(unity_ObjectToWorld._m00_m10_m20)),
                        length(float3(unity_ObjectToWorld._m01_m11_m21))
                    );

                    float3 posOS = IN.positionOS.xyz;
                    posOS.xy += uvExpand * objScale; // ★ 그림자용 메시 확장

                    OUT.positionHCS = TransformObjectToHClip(posOS);
                    OUT.uv = IN.uv;
                    OUT.color = IN.color; // 필요시 Tint 곱하고 싶으면 별도 추가
                    return OUT;
                }

                float4 frag(Varyings IN) : SV_Target
                {
                    // 픽셀→UV 파라미터
                    float2 uvOff = _ShadowOffset.xy * _MainTex_TexelSize.xy;
                    float2 uvRad = max(_Softness, 0.0) * _MainTex_TexelSize.xy;

                    // 소프트 섀도우 알파
                    float sAlpha = ShadowAlpha8Tap(IN.uv, uvOff, uvRad);
                    float  sA = sAlpha * _ShadowColor.a;

                    return float4(_ShadowColor.rgb, sA); // 그림자만 출력
                }
                ENDHLSL
            }
        }

            FallBack "Hidden/InternalErrorShader"
}
