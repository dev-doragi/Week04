// OutlineInvertedHull.shader

// Unity가 이 파일을 셰이더로 인식하게 하는 선언.
// "Custom/OutlineInvertedHull" 이 경로로 Material에서 셰이더를 찾을 수 있어.
Shader "Custom/OutlineInvertedHull"
{
    // ─────────────────────────────────────────────────────────────
    // Properties 블록: Inspector에서 조절할 수 있는 변수들을 선언하는 곳.
    // 이 값들은 Material 인스턴스마다 개별 저장돼.
    // ─────────────────────────────────────────────────────────────
    Properties
    {
        // _BaseMap: URP의 표준 메인 텍스처 이름. 
        // 이 이름을 쓰면 MeshRenderer의 기존 머티리얼 텍스처를 그대로 연결할 수 있어.
        // 2D 타입, 기본값 "white" (흰 텍스처)
        [MainTexture] _BaseMap    ("Base Map",      2D)    = "white" {}

        // [MainColor]: URP가 이 프로퍼티를 "메인 컬러"로 인식하게 해주는 어트리뷰트
        // Material.color 프로퍼티로 코드에서 접근할 때도 이게 기준이 됨
        [MainColor]   _BaseColor  ("Base Color",    Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color)           = (0, 0, 0, 1)  // 아웃라인 색상. 기본값 검정
        _OutlineWidth ("Outline Width", Range(0, 0.1))   = 0.01          // 아웃라인 두께. 슬라이더로 0~0.1 범위 조절
    }

    SubShader
    {
        // Tags: 이 셰이더가 어떤 렌더링 파이프라인에서, 어떤 방식으로 그려질지 Unity에게 알려줌.
        // RenderType = Opaque  → 불투명 오브젝트로 처리 (반투명 큐에 안 들어감)
        // RenderPipeline = UniversalPipeline → URP 전용임을 명시. Built-in에선 이 SubShader가 무시됨
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }


        // ═════════════════════════════════════════════════════════
        // Pass 1 — 베이스 패스 (실제 오브젝트를 그리는 패스)
        // ═════════════════════════════════════════════════════════
        Pass
        {
            // 이 Pass의 이름. 다른 셰이더에서 UsePass "Custom/OutlineInvertedHull/BASE" 로 재사용 가능
            Name "BASE"

            // LightMode = UniversalForward → URP의 Forward 렌더링 루프에서 이 패스를 실행해줘 라는 뜻.
            // 이 태그가 없으면 URP가 패스를 그냥 무시할 수 있어.
            Tags { "LightMode"="UniversalForward" }

            // Cull Back → 카메라를 등지는 면(뒷면)을 컬링(제거). 일반적인 렌더링의 기본값.
            Cull Back

            HLSLPROGRAM
            // vertex 셰이더 함수 이름을 'vert'로, fragment 셰이더 함수 이름을 'frag'로 지정
            #pragma vertex   vert
            #pragma fragment frag

            // URP의 핵심 유틸리티 함수들 포함.
            // TransformObjectToHClip 같은 좌표 변환 함수가 여기 들어있음
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // CBUFFER 밖에 선언 (텍스처/샘플러는 상수버퍼에 못 들어가)
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // CBUFFER_START/END: GPU 상수 버퍼(Constant Buffer). SRP Batcher 호환을 위해 필수.
            // SRP Batcher는 같은 셰이더를 쓰는 오브젝트를 묶어서 드로우콜을 줄여주는 최적화인데,
            // 이 블록 없이 Properties를 쓰면 SRP Batcher가 작동 안 해.
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;      // Properties에서 선언한 변수를 GPU에서 쓰려면 여기에도 선언해야 함
                float4 _BaseMap_ST;  // ← 이게 빠져있었어. TRANSFORM_TEX가 이 값을 참조함
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            // Attributes: 메시의 각 버텍스마다 CPU(메시 데이터)에서 GPU로 넘어오는 입력 구조체
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0; // 텍스처 좌표. 이게 있어야 텍스처를 샘플링할 수 있어
            };

            // Varyings: vertex 셰이더가 출력하고, fragment 셰이더가 입력으로 받는 구조체.
            // 래스터라이저가 삼각형 내부를 보간해서 각 픽셀에 전달해줌
            // Varyings에도 UV 추가 (vertex → fragment로 전달)
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // ── Vertex Shader ──
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // TransformObjectToHClip: 오브젝트 공간 → 클립 공간 변환 (Model * View * Projection 행렬 곱)
                // 이 변환을 거쳐야 GPU가 화면 어디에 찍을지 알 수 있어
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // ↓ 이 줄 빠져있으면 경고 남. Varyings에 uv 선언했으면 반드시 값 넣어줘야 해
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // ── Fragment Shader ──
            // SV_Target: 이 함수의 반환값을 렌더 타겟(화면 or 렌더 텍스처)에 출력해 라는 시맨틱
            half4 frag(Varyings IN) : SV_Target
            {
                // 텍스처 샘플링 후 _BaseColor와 곱해서 출력
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
                // 단순히 _BaseColor를 그대로 출력. 라이팅 없이 플랫 셰이딩.
                // 실제 게임에선 여기에 조명 계산이나 텍스처 샘플링을 추가하면 돼
                return _BaseColor;
            }
            ENDHLSL
        }


        // ═════════════════════════════════════════════════════════
        // Pass 2 — 아웃라인 패스 (Inverted Hull 핵심 로직)
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            // ↓ Pass 2에도 struct를 똑같이 다시 선언해야 해
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            // ↓ 이게 없어서 "unrecognized identifier 'Varyings'" 에러 난 거야
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

                // ↓ OUT이 선언됐어도 positionHCS에 값을 안 넣으면
                //   "Output value 'vert' is not completely initialized" 경고 나와
                //   clipPos를 반드시 OUT.positionHCS에 넣어줘야 해
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