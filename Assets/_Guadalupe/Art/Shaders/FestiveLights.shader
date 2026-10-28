// Luces navideñas: suma su color a lo que hay detrás (sin escribir profundidad) para que las bombillas brillen y
// el bloom las haga resplandecer. La textura define las bombillas; cada celda de la rejilla parpadea suavemente
// con su propia fase. La intensidad global la fija el script de las luces según la hora (0 = apagadas).
Shader "Guadalupe/FestiveLights"
{
    Properties
    {
        _MainTex ("Bombillas", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 0.85, 0.6, 1)
        _Cells ("Bombillas por repetición de la textura", Vector) = (4, 4, 0, 0)
        _Twinkle ("Parpadeo (0-1)", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Cells;
            float _Twinkle;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 cell = floor(i.uv * _Cells.xy);
                float phase = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453) * 6.2832;
                float twinkle = 1 - _Twinkle * 0.5 * (1 + sin(_Time.y * 2.3 + phase));
                float4 bulbs = tex2D(_MainTex, i.uv);
                return float4(bulbs.rgb * bulbs.a * _Color.rgb * twinkle, 0);
            }
            ENDCG
        }
    }
}
