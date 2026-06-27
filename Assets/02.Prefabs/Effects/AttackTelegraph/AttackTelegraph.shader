Shader "Custom/AttackTelegraph"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.15, 0.1, 1)
        _Alpha ("Alpha", Range(0,1)) = 1
        _AngleDeg ("Angle Degrees", Range(0,360)) = 360
        _EdgeWidth ("Edge Width", Range(0.001,0.5)) = 0.06
        _EdgeIntensity ("Edge Intensity", Range(1,8)) = 4
        _FillIntensity ("Fill Intensity", Range(0,1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        // Terrain 높낮이에 가려 텔레그래프가 보이지 않는 문제 방지: 깊이 테스트 없이 항상 위에 그린다
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Alpha;
                float _AngleDeg;
                float _EdgeWidth;
                float _EdgeIntensity;
                float _FillIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centered = (IN.uv - 0.5) * 2.0; // -1..1
                float dist = length(centered);

                clip(1.0 - dist);

                if (_AngleDeg < 359.0)
                {
                    float angle = atan2(centered.x, centered.y) * 57.29578; // degrees from forward (+v)
                    float halfAngle = _AngleDeg * 0.5;
                    clip(halfAngle - abs(angle));
                }

                float edge = smoothstep(1.0 - _EdgeWidth, 1.0, dist);
                float intensity = _FillIntensity + edge * _EdgeIntensity;

                half4 col = _Color;
                col.a *= _Alpha * saturate(intensity);
                return col;
            }
            ENDHLSL
        }
    }
}
