Shader "Universal Render Pipeline/2D/Sprite Drop Shadow (Unlit)"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _ShadowColor("Shadow Color (RGBA)", Color) = (0,0,0,0.45)
        _ShadowOffset("Shadow Offset (px)", Vector) = (6,-6,0,0)
        _Softness("Softness Radius (px)", Range(0,16)) = 8
        _ShadowBoost("Shadow Boost (alpha gain)", Range(0,2)) = 1.2
        _ShadowExpand("Shadow Expand (px)", Range(0,12)) = 3
        _ShadowGeomExpand("Shadow Geometry Expand (px)", Range(0,32)) = 12
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
                Name "UNLIT_2D"
                Tags { "LightMode" = "Universal2D" }   // ★ 여기!

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

            // 스프라이트 아틀라스/외부 알파 지원
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            #ifdef ETC1_EXTERNAL_ALPHA
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);
            #endif

            float4 _Color;
            float4 _ShadowColor;
            float4 _ShadowOffset;
            float  _Softness;
            float  _ShadowBoost;
            float _ShadowExpand;
            float _ShadowGeomExpand;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float4 SampleMain(float2 uv)
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #ifdef ETC1_EXTERNAL_ALPHA
                c.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif
                return c;
            }
            float SampleAlpha(float2 uv) { return SampleMain(uv).a; }

            float ShadowAlpha8Tap(float2 uvBase, float2 uvOffset, float2 uvRadius, float2 uvGrow)
            {
                // 중심
                float aCenter = SampleAlpha(uvBase + uvOffset);
                float a = aCenter;

                float2 dirs[8] = {
                    float2(1,  0), float2(-1,  0), float2(0,  1), float2(0, -1),
                    normalize(float2(1,  1)), normalize(float2(-1,  1)),
                    normalize(float2(1, -1)), normalize(float2(-1, -1))
                };

                [unroll] for (int i = 0; i < 8; i++)
                {
                    float2 d = dirs[i];
                    // 원래 반경
                    float a1 = SampleAlpha(uvBase + uvOffset + d * uvRadius);
                    // 확장 반경(더 바깥쪽)
                    float a2 = SampleAlpha(uvBase + uvOffset + d * (uvRadius + uvGrow));
                    a = max(a, max(a1, a2));
                }

                // 약간의 게인
                a = a * _ShadowBoost;
                return saturate(a);
            }


            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SampleMain(IN.uv);
                float4 baseCol = tex * IN.color;

                float2 uvOff = _ShadowOffset.xy * _MainTex_TexelSize.xy;
                float  softPx = max(_Softness, 0.0);
                float2 uvRad = max(softPx, 0.5) * _MainTex_TexelSize.xy;
                float2 uvGrow = max(_ShadowExpand, 0.0) * _MainTex_TexelSize.xy;

                float sAlpha = ShadowAlpha8Tap(IN.uv, uvOff, uvRad, uvGrow);
                float  ba = saturate(baseCol.a);
                float  sA = sAlpha * _ShadowColor.a;

                float outA = saturate(ba + sA * (1.0 - ba));
                float3 outRGB = baseCol.rgb * ba + _ShadowColor.rgb * sA * (1.0 - ba);

                return float4(outRGB, outA);
            }
            ENDHLSL
        }
        }

            FallBack "Hidden/InternalErrorShader"
}
