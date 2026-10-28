// Letras de los rótulos: respetan la profundidad (no se ven a través de las paredes), solo se dibujan por su cara
// delantera y se desvanecen con la distancia, de modo que solo se leen al mirar el rótulo de cerca.
Shader "Guadalupe/SignText"
{
    Properties
    {
        _MainTex ("Atlas de la fuente", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FadeStart ("Inicio del desvanecido (m)", Float) = 14
        _FadeEnd ("Fin del desvanecido (m)", Float) = 22
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Back
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float d = distance(world, _WorldSpaceCameraPos);
                o.fade = 1 - saturate((d - _FadeStart) / max(0.01, _FadeEnd - _FadeStart));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = i.color;
                c.a *= tex2D(_MainTex, i.uv).a * i.fade;
                return c;
            }
            ENDCG
        }
    }
}
