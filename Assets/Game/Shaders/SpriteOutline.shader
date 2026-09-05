// 스프라이트 외곽선 + 피격 플래시.
//
// 엘리트/보스를 "색 곱셈"으로 표시하던 걸 대체한다. 곱셈은 컬러 스프라이트를
// 탁하게 만든다(녹색 좀비 x 보라 = 검정). 외곽선은 원화를 건드리지 않는다.
//
// 알파 팽창(dilation) 방식이다. 자기 알파가 낮은 픽셀에서 주변 8방향을 훑어
// 하나라도 불투명하면 외곽선 색을 칠한다.
//
// ※ 스프라이트 임포트 설정이 Mesh Type = Full Rect 여야 한다.
//    기본값 Tight 는 메시가 알파에 딱 붙어서 외곽선이 그려질 여백이 없다.
//
// 씬의 Light2D 가 Global/흰색/intensity 1 하나뿐이라 Unlit 으로 두어도
// Sprite-Lit-Default 와 결과가 같다. 나중에 조명을 쓰면 이 셰이더도 손봐야 한다.

Shader "VS_LIKE/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color        ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        // 텍스처 픽셀 단위(512px 기준). 0 이면 외곽선이 완전히 꺼진다(일반 몹).
        // 스프라이트가 화면에서 작으므로 12~20 은 되어야 눈에 띈다.
        _OutlineWidth ("Outline Width (px)", Range(0,32)) = 0
        _FlashColor   ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount  ("Flash Amount", Range(0,1)) = 0
        // _MainTex_TexelSize 를 쓰면 2D SRP Batcher 가 이 머티리얼을 통째로 배칭에서
        // 제외한다. 그래서 텍스처 크기를 직접 넘긴다 — 이 값이 실제 텍스처 크기와
        // 다르면 _OutlineWidth 가 "텍셀 수"라는 뜻을 잃는다.
        //
        // ⚠️ 512 로 굳어 있던 게 B1 의 진짜 원인이었다. I-58 이 적을 1024 시트로 바꾸면서
        //    선이 14텍셀 → 28텍셀로 굵어졌고, 프레임 자체가 130~230텍셀뿐이라
        //    실루엣이 통째로 덮였다(키의 2.7% → 13.6~23.8%).
        //    EnemyVisual 이 스프라이트마다 실제 크기를 넘긴다.
        _OutlineTexSize ("Outline Ref Texture Size (px)", Float) = 512

        // 이 스프라이트가 텍스처의 어느 사각형인지 (uv, xy=좌하 zw=우상).
        // 시트에서 잘라 온 프레임은 uv 가 0~1 이 아니다. 이 값이 없으면 외곽선이
        // 옆 칸(다음 걷기 프레임)의 알파를 빨아들인다 (B1).
        // 기본값 (0,0,1,1) = 텍스처 전체 = 낱장 png 일 때의 올바른 값이다.
        _SpriteRect ("Sprite Rect in Texture (uv)", Vector) = (0,0,1,1)

        // ── 걷기 바운스 (버텍스) ──────────────────────────────────
        // 스케일을 코드로 흔들면 CapsuleCollider2D 까지 같이 늘어나 판정이 변한다.
        // 그래서 정점에서만 찌그러뜨린다. 물리와 완전히 분리된다.
        // _AnimSpeed 가 0 이면 애니메이션이 꺼진다(기본값).
        _AnimSpeed  ("Anim Speed", Float) = 0
        _AnimPhase  ("Anim Phase", Float) = 0
        _SquashAmt  ("Squash Amount", Range(0,0.4)) = 0.07
        _BobAmt     ("Bob Amount", Range(0,0.4)) = 0.04

        // 이동 방향으로 몸을 기울인다. 회전이 아니라 전단(shear)이라 발은 땅에 붙어 있고
        // 위쪽만 밀린다. transform.rotation 을 돌리면 Rigidbody2D 와 싸우게 되므로 피한다.
        _LeanAmt    ("Lean (shear)", Range(-0.4,0.4)) = 0

        // 🔴 <b>전단의 기준선</b> (오브젝트 공간 y · D79).
        // 0 이면 피벗(= 스프라이트 한가운데)이 기준이라 위가 오른쪽으로 밀릴 때
        // <b>아래(다리)는 왼쪽으로 밀린다.</b> 화면에서 가장 크게 움직이는 게 다리라
        // 기울기 부호가 맞아도 <b>몸이 가는 쪽의 반대로 가는 것처럼 보인다</b> — 사용자가 두 번 지적했다.
        // 스프라이트 밑변(<c>sprite.bounds.min.y</c>)을 넣으면 발이 고정된다.
        _LeanPivotY ("Lean Pivot Y (object space)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        // Sprites/Default 와 동일한 프리멀티플라이드 알파 블렌딩
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _OutlineColor;
                half   _OutlineWidth;
                float  _OutlineTexSize;
                float4 _SpriteRect;
                half4  _FlashColor;
                half   _FlashAmount;
                float  _AnimSpeed;
                float  _AnimPhase;
                half   _SquashAmt;
                half   _BobAmt;
                half   _LeanAmt;
                float  _LeanPivotY;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                if (_AnimSpeed > 0)
                {
                    // _Time.y 는 timeScale 의 영향을 받는다 → 일시정지/레벨업 중에는
                    // 애니메이션도 같이 멈춘다. 의도한 동작이다.
                    float s = sin(_Time.y * _AnimSpeed + _AnimPhase);

                    // 부피 보존 느낌: 세로로 늘면 가로로 준다
                    pos.y *= 1.0 + s * _SquashAmt;
                    pos.x *= 1.0 - s * _SquashAmt * 0.7;
                    pos.y += s * _BobAmt;
                }

                // 기준선에서 멀수록 많이 밀린다 → 아래는 고정, 위만 기운다.
                // 🔴 <b>_LeanPivotY 가 없으면(0) 피벗 = 스프라이트 한가운데가 기준이 되어
                // 다리가 반대로 밀린다</b> (D79). 코드가 밑변을 넣어 준다.
                pos.x += (pos.y - _LeanPivotY) * _LeanAmt;

                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half SampleAlpha(float2 uv)
            {
                // 이 스프라이트 칸 밖은 시트의 다른 그림이다. 0 으로 막는다.
                //
                // ⚠️ 예전에는 `0~1` 로 검사했는데 그건 틀린 검사였다. IN.uv 는
                //    **텍스처 전체 기준**이라 1024 시트에서 잘라 온 256 프레임의 uv 는
                //    예컨대 x∈[0.25,0.5] 다 → 검사가 한 번도 안 걸리고 외곽선이
                //    옆 칸 알파를 빨아들였다 (B1). 낱장 png 시절엔 우연히 맞았다.
                if (uv.x < _SpriteRect.x || uv.x > _SpriteRect.z ||
                    uv.y < _SpriteRect.y || uv.y > _SpriteRect.w) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * _Color * IN.color;

                // 피격 플래시 — 알파는 유지한 채 색만 밀어 올린다
                col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount * col.a);

                if (_OutlineWidth > 0)
                {
                    float d = _OutlineWidth / max(_OutlineTexSize, 1.0);

                    // 반지름 d 의 원 위에서 16방향을 샘플한다.
                    // 축/대각 8방향만 쓰면 대각선이 sqrt(2) 만큼 멀리 찍혀 모서리가
                    // 부풀고, 폭이 커질수록 외곽선이 네모난 덩어리가 된다.
                    // 각도는 컴파일 타임 상수라 sin/cos 는 폴딩된다.
                    half a = 0;
                    [unroll]
                    for (int j = 0; j < 16; j++)
                    {
                        float ang = 6.28318530718 / 16.0 * j;
                        a = max(a, SampleAlpha(IN.uv + float2(cos(ang), sin(ang)) * d));
                    }

                    // 자기 자신이 비어 있고 이웃이 차 있는 곳 = 외곽선
                    half rim = saturate(a - tex.a) * _OutlineColor.a;
                    col.rgb = lerp(col.rgb, _OutlineColor.rgb, rim);
                    col.a   = saturate(col.a + rim);
                }

                col.rgb *= col.a;   // 프리멀티플라이드
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
