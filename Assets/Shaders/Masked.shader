// 메인 텍스쳐와 마스크 텍스쳐를 사용하여 투명도를 조절하는 마스킹 효과를 구현.
// 마스크 텍스쳐의 알파값에 따라 메인 텍스쳐의 투명도가 결정.

Shader "Masked" {
    Properties {
        _MainTex ("Main", 2D) = "white" {} //메인 텍스쳐
        _MaskTex ("Mask", 2D) = "white" {} // 마스크 텍스쳐
    }

    SubShader {
        //렌더링 설정
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off  // z버퍼 쓰기 비활성화
        ZTest Off   // z테스트 비활성화
        Blend SrcAlpha OneMinusSrcAlpha // 알파 블렌딩 설정
        Pass {
            CGPROGRAM
            #pragma vertex vert     // 버텍스 쉐이더 함수 지정
            #pragma fragment frag   // 프래그먼트 쉐이더 함수 지정
            #pragma fragmentoption ARB_precision_hint_fastest   //프래그먼트 쉐이더 최적화 힌트
            #include "UnityCG.cginc"    // 유니티 내장 함수 포함

            //텍스쳐 샘플러와 변환 매개변수 선언
            uniform sampler2D _MainTex;
            uniform sampler2D _MaskTex;
            uniform float4 _MainTex_ST;
            uniform float4 _MaskTex_ST;

            // 버텍스 쉐이더 입력 구조체
            struct app2vert
            {
                float4 position: POSITION;  // 오브젝트 공간 위치
                float2 texcoord: TEXCOORD0; // 텍스쳐 좌표
            };

            // 버텍스 쉐이더 입려 구조체
            struct vert2frag
            {
                float4 position: POSITION;  // 클립 공간 위치
                float2 texcoord: TEXCOORD0; // 텍스쳐 좌표
            };

            // 버텍스 쉐이더 함수
            vert2frag vert(app2vert input)
            {
                vert2frag output;
                output.position = UnityObjectToClipPos(input.position);    //오브젝트 공간에서 클립 공간으로 변환
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex); //텍스쳐 좌표 변환
                return output;
            }

            // 프래그먼트 쉐이더 함수
            fixed4 frag(vert2frag input) : COLOR
            {
                fixed4 main_color = tex2D(_MainTex, input.texcoord); // 메인 텍스쳐 샘플링
                fixed4 mask_color = tex2D(_MaskTex, input.texcoord); // 마스크 텍스쳐 샘플링

                // 최종 색상 계산 : RGB는 메인 텍스쳐에서, 알파는 메인 텍스쳐와 마스크의 조합
                return fixed4(main_color.r, main_color.g, main_color.b, main_color.a * (1.0f - mask_color.a));
            }
            ENDCG
        }
    }
}