Shader "Custom/OutlineInvertedHullCube"
{
    Properties
    {
        [MainTexture] _BaseMap    ("Base Map",      2D)            = "white" {}
        [MainColor]   _BaseColor  ("Base Color",    Color)         = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color)                     = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1))             = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ═════════════════════════════════════════════════════════
        // Pass 1 — 베이스 패스
        // ═════════════════════════════════════════════════════════
        Pass
        {
            Name "BASE"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ✅ 수정 1: 두 Pass의 CBUFFER는 내용이 완전히 동일해야 해.
            // SRP Batcher가 두 Pass를 같은 머티리얼로 묶으려면 레이아웃이 일치해야 함.
            // _BaseMap_ST, _OutlineColor, _OutlineWidth 모두 여기도 선언해줘야 해.
            TEXTURE2D(_BaseMap);        // 텍스처는 CBUFFER 밖에 선언
            SAMPLER(sampler_BaseMap);   // 샘플러도 CBUFFER 밖에 선언

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;     // ✅ 수정 1: Pass 1에도 추가. TRANSFORM_TEX가 이걸 참조함
                float4 _BaseColor;
                float4 _OutlineColor;   // ✅ 수정 1: 두 Pass CBUFFER 동일하게 맞춰야 해
                float  _OutlineWidth;   // ✅ 수정 1: 두 Pass CBUFFER 동일하게 맞춰야 해
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // ✅ 수정 2: OUT.uv 대입이 빠져있었어.
                // Varyings에 uv 필드를 선언했으면 반드시 값을 채워야 경고 안 남.
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ✅ 수정 3: _BaseColor만 반환하면 텍스처가 무시돼.
                // 텍스처 샘플링 후 _BaseColor와 곱해야 텍스처 색상이 실제로 보여.
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }

        // ═════════════════════════════════════════════════════════
        // Pass 2 — 아웃라인 패스
        // ═════════════════════════════════════════════════════════
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vertOutline
            #pragma fragment fragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;

                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 normalCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0));

                float2 screenNormal = normalize(normalCS.xy / clipPos.w)
                                      * float2(1, _ScreenParams.x / _ScreenParams.y);

                clipPos.xy += screenNormal * _OutlineWidth * clipPos.w;

                OUT.positionHCS = clipPos;
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}