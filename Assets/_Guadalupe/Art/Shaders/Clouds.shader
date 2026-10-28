// Capa de nubes sobre una cúpula: textura de nubes generada que se desplaza despacio, transparente, sin niebla,
// teñida según la hora (blancas de día, doradas al atardecer, grises de noche). De noche la cúpula pinta además el
// fondo: un degradado azul marino con estrellas, que se funde según _Night (0 de día, 1 de noche cerrada).
Shader "Guadalupe/Clouds"
{
    Properties
    {
        _MainTex ("Nubes", 2D) = "white" {}
        _Tint ("Tinte", Color) = (1, 1, 1, 1)
        _Speed ("Velocidad", Vector) = (0.004, 0.0015, 0, 0)
        _Opacity ("Opacidad", Range(0, 1)) = 0.85

        [Header(Noche)]
        _Night ("Noche (0-1)", Range(0, 1)) = 0
        _NightHorizon ("Cielo de noche en el horizonte", Color) = (0.07, 0.095, 0.18, 1)
        _NightZenith ("Cielo de noche en el cénit", Color) = (0.015, 0.022, 0.065, 1)
        _StarCells ("Celdas de estrellas", Float) = 120
        _StarChance ("Proporción de celdas con estrella", Range(0, 1)) = 0.05
        _StarBrightness ("Brillo de las estrellas", Range(0, 4)) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-200" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        // Color premultiplicado: las nubes y el fondo de noche se componen en el mismo pase.
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float4 _Speed;
            float _Opacity;
            float _Night;
            float4 _NightHorizon;
            float4 _NightZenith;
            float _StarCells;
            float _StarChance;
            float _StarBrightness;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float horizon : TEXCOORD1;
                float3 dir : TEXCOORD2;
            };

            float3 Hash3(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float Stars(float3 dir)
            {
                float3 p = dir * _StarCells;
                float3 cell = floor(p);
                float3 rnd = Hash3(cell);
                if (rnd.x > _StarChance)
                {
                    return 0;
                }

                // Un punto por celda en un sitio al azar, con brillo variado (casi todas tenues) y un leve parpadeo.
                float3 center = cell + 0.2 + Hash3(cell + 17.0) * 0.6;
                float star = smoothstep(0.09, 0.0, length(p - center));
                float twinkle = 0.8 + 0.2 * sin(_Time.y * (1.5 + rnd.y * 2.0) + rnd.z * 40.0);
                return star * (0.25 + 0.75 * rnd.z * rnd.z) * twinkle;
            }

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex) + _Speed.xy * _Time.y;
                // Las nubes se desvanecen hacia el horizonte para fundirse con el cielo.
                o.horizon = saturate(v.vertex.y * 6);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float3 cloud = _Tint.rgb * lerp(0.78, 1.0, c.r);
                float cloudAlpha = c.a * _Opacity * i.horizon * _Tint.a;

                float3 dir = normalize(i.dir);
                float height = saturate(dir.y);
                float3 night = lerp(_NightHorizon.rgb, _NightZenith.rgb, sqrt(height));
                night += Stars(dir) * _StarBrightness * smoothstep(0.04, 0.25, height);

                float3 color = cloud * cloudAlpha + night * _Night * (1 - cloudAlpha);
                return fixed4(color, cloudAlpha + _Night * (1 - cloudAlpha));
            }
            ENDCG
        }
    }
}
