Shader "Custom/SpriteBlur"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 10)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _BlurSize / _ScreenParams.xy;
                
                fixed4 col = tex2D(_MainTex, i.uv) * 0.16;
                col += tex2D(_MainTex, i.uv + float2(texelSize.x, 0)) * 0.15;
                col += tex2D(_MainTex, i.uv - float2(texelSize.x, 0)) * 0.15;
                col += tex2D(_MainTex, i.uv + float2(0, texelSize.y)) * 0.15;
                col += tex2D(_MainTex, i.uv - float2(0, texelSize.y)) * 0.15;
                col += tex2D(_MainTex, i.uv + texelSize) * 0.12;
                col += tex2D(_MainTex, i.uv - texelSize) * 0.12;
                
                col *= i.color;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
}