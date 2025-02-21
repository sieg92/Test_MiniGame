// 기본 텍스처와 마스크 텍스처를 사용하여 마스킹 효과를 구현.
// 마스크 텍스처의 빨간 채널 값에 따라 기본 텍스처의 알파 값(투명도) 조절. 
// 마스크 텍스처의 빨간 채널 값이 높을 수록 해당 부분 투명 up. 


Shader "MaskConstruction"
{
    //인스펙터에서 조정 가능한 속성 정의
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}  // 기본 텍스쳐
        _MaskTex ("Mask Texture", 2D) = "black" {}  // 마스크 텍스쳐
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }  //투명 객체로 렌더링 큐 설정
        Pass
        {
            ZWrite Off                          // z버퍼 쓰기 비활성화
            Blend SrcAlpha OneMinusSrcAlpha     // 알파 블렌딩 설정

            CGPROGRAM
            #pragma vertex vert                 // 버텍스 쉐이더 함수 지정
            #pragma fragment frag               // 프래그먼트 쉐이더 함수 지정

            #include "UnityCG.cginc"            //유니티 내장 함수 포함

            sampler2D _MainTex;     // 기본 텍스처 샘플러
            sampler2D _MaskTex;     // 마스크 텍스처 샘플러

            // 버텍스 쉐이더 입력 구조체
            struct appdata_t
            {
                float4 vertex : POSITION;   // 오브젝트 공간 위치
                float2 uv : TEXCOORD0;      // 텍스처 좌표
            };

            // 프래그먼트 쉐이더 입력 구조체
            struct v2f
            {
                float2 uv : TEXCOORD0;       // 텍스처 좌표
                float4 vertex : SV_POSITION; // 클립 공간 위치
            };

            // 버텍스 쉐이더 함수
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // 오브젝트 공간에서 클립 공간으로 변환
                o.uv = v.uv;                               // 텍스처 좌표 전달
                return o;
            }

            // 프래그먼트 쉐이더 함수
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.uv); // 기본 텍스처 샘플링
                fixed4 maskColor = tex2D(_MaskTex, i.uv); // 마스크 텍스처 샘플링

                // 마스크를 기본 텍스처에 적용
                baseColor.a *= (1 - maskColor.r); // 마스크 텍스처의 빨간 채널을 기반으로 지우기 효과 적용

                return baseColor; //최종 색상 반환
            }
            ENDCG
        }
    }
}
