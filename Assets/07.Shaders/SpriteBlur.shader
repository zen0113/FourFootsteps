Shader "Custom/SpriteBlur"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 20)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _BlurSamples ("Blur Samples", Range(4, 16)) = 9
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurSize;
            float _Alpha;
            int _BlurSamples;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 col = fixed4(0, 0, 0, 0);
                float totalWeight = 0;
                
                // 가우시안 블러 샘플링
                int samples = _BlurSamples;
                int halfSamples = samples / 2;
                
                for (int x = -halfSamples; x <= halfSamples; x++)
                {
                    for (int y = -halfSamples; y <= halfSamples; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        
                        // 가우시안 가중치 계산 (거리 기반)
                        float distance = length(offset);
                        float weight = exp(-distance * distance / (2.0 * _BlurSize * 0.5));
                        
                        // 텍스처 샘플링
                        fixed4 sample = tex2D(_MainTex, i.uv + offset);
                        col += sample * weight;
                        totalWeight += weight;
                    }
                }
                
                // 정규화
                col /= totalWeight;
                
                // 색상과 투명도 적용
                col *= i.color;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
    
    // 최적화된 2패스 블러 (선택사항)
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 50
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        // 수평 블러 패스
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurSize;
            float _Alpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag_horizontal (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float blurSize = _BlurSize;
                
                fixed4 col = tex2D(_MainTex, i.uv) * 0.2270270270;
                
                // 수평 블러 (7 샘플)
                col += tex2D(_MainTex, i.uv + float2(texelSize.x * 1.3846153846 * blurSize, 0)) * 0.3162162162;
                col += tex2D(_MainTex, i.uv - float2(texelSize.x * 1.3846153846 * blurSize, 0)) * 0.3162162162;
                col += tex2D(_MainTex, i.uv + float2(texelSize.x * 3.2307692308 * blurSize, 0)) * 0.0702702703;
                col += tex2D(_MainTex, i.uv - float2(texelSize.x * 3.2307692308 * blurSize, 0)) * 0.0702702703;
                
                col *= i.color;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
        
        // 수직 블러 패스
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vertical
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurSize;
            float _Alpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag_vertical (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float blurSize = _BlurSize;
                
                fixed4 col = tex2D(_MainTex, i.uv) * 0.2270270270;
                
                // 수직 블러 (7 샘플)
                col += tex2D(_MainTex, i.uv + float2(0, texelSize.y * 1.3846153846 * blurSize)) * 0.3162162162;
                col += tex2D(_MainTex, i.uv - float2(0, texelSize.y * 1.3846153846 * blurSize)) * 0.3162162162;
                col += tex2D(_MainTex, i.uv + float2(0, texelSize.y * 3.2307692308 * blurSize)) * 0.0702702703;
                col += tex2D(_MainTex, i.uv - float2(0, texelSize.y * 3.2307692308 * blurSize)) * 0.0702702703;
                
                col *= i.color;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
}