Shader "Custom/InvisibleOccluder"
{
    SubShader
    {
        // Рисуемся на шаге 1995: ПОСЛЕ 2D-фона, но ДО персонажа (2000)
        Tags { "RenderType"="Opaque" "Queue"="Geometry-5" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ColorMask 0 // Выключаем запись цвета (объект на 100% невидимый!)
            ZWrite On   // Включаем запись глубины (перекрывает всё, что рисуется позже)
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}