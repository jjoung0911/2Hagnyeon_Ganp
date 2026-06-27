Shader "Custom/AttackTelegraphDecal"
{
    // 깊이 텍스처로 바닥 표면을 재구성해 투영하는 디퍼드 데칼 텔레그래프.
    // 큐브 볼륨을 그리되, 각 픽셀의 씬 깊이로 월드 위치를 복원해 큐브 내부에 들어오는
    // 바닥 픽셀에만 원/부채꼴을 그린다 → 지형 굴곡을 따라 바닥에 칠해진다.
    // 원본 Custom/AttackTelegraph의 원/콘 절차 로직(_AngleDeg/_Edge*)을 그대로 포팅.
    Properties
    {
        _Color ("Color", Color) = (1, 0.15, 0.1, 1)
        _Alpha ("Alpha", Range(0,1)) = 1
        _AngleDeg ("Angle Degrees", Range(0,360)) = 360
        _EdgeWidth ("Edge Width", Range(0.001,0.5)) = 0.06
        _EdgeIntensity ("Edge Intensity", Range(1,8)) = 4
        _FillIntensity ("Fill Intensity", Range(0,1)) = 0.18
        // 지면(GroundLayer) 월드 Y. 이 높이 + 허용치보다 위로 솟은 표면(에너미 등)에는 그리지 않는다.
        // 기본값을 매우 크게 둬서 미설정 시(레거시)엔 컷이 동작하지 않도록 한다.
        _GroundY ("Ground World Y", Float) = 100000
        _HeightTolerance ("Height Tolerance", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            // 볼륨 박스는 깊이에 가려지지 않게 항상 그리고, 카메라가 박스 안에 있어도 보이도록 앞면 컬링
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Alpha;
                float _AngleDeg;
                float _EdgeWidth;
                float _EdgeIntensity;
                float _FillIntensity;
                float _GroundY;
                float _HeightTolerance;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 화면 픽셀의 씬 깊이로 바닥 월드 좌표 복원
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float depth = SampleSceneDepth(screenUV);
                float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                // 지면 위로 솟은 표면(에너미/플레이어 몸체 등)에는 그리지 않음 — GroundLayer 높이 기준 컷.
                // 깊이 버퍼는 에너미도 포함하므로, 지면(_GroundY) + 허용치보다 높은 픽셀을 제거해 바닥에만 남긴다.
                clip((_GroundY + _HeightTolerance) - worldPos.y);

                // 깊이로 표면 법선을 복원해 바닥(수평면)에만 투영 — 수직면엔 그리지 않음
                float3 surfNormal = normalize(cross(ddy(worldPos), ddx(worldPos)));
                clip(abs(surfNormal.y) - 0.4);

                // 데칼(큐브) 로컬 공간으로 변환 — 단위 큐브 기준 ±0.5 범위
                float3 objPos = TransformWorldToObject(worldPos);
                clip(0.5 - abs(objPos.x));
                clip(0.5 - abs(objPos.y));
                clip(0.5 - abs(objPos.z));

                // 바닥 평면(XZ)에서 원/부채꼴 판정 — 원본 셰이더 로직과 동일
                float2 centered = objPos.xz * 2.0; // -1..1
                float dist = length(centered);
                clip(1.0 - dist);

                if (_AngleDeg < 359.0)
                {
                    float angle = atan2(centered.x, centered.y) * 57.29578; // 로컬 +Z 기준 각도
                    clip(_AngleDeg * 0.5 - abs(angle));
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
