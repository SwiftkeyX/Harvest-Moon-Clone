// Flat toon shading for the Cliff Ledge scene: three colour bands from the main light (with shadows),
// optional world-space grass patches and rock streaks, and the scene's linear fog.
Shader "CliffLedge/Toon"
{
    Properties
    {
        _BaseColor ("Lit colour", Color) = (0.7, 0.7, 0.4, 1)
        _ShadeColor ("Shade colour", Color) = (0.4, 0.4, 0.25, 1)
        _HighlightColor ("Highlight colour", Color) = (0.8, 0.8, 0.5, 1)
        _ShadeThreshold ("Shade threshold", Range(-1, 1)) = 0.1
        _HighlightThreshold ("Highlight threshold", Range(-1, 1.01)) = 0.85
        _ShadowShade ("Cast shadow uses shade", Range(0, 1)) = 1

        _PatchColor ("Patch colour", Color) = (0.5, 0.5, 0.2, 1)
        _PatchScale ("Patch scale", Float) = 0.35
        _PatchAmount ("Patch amount (0 = off)", Range(0, 1)) = 0

        _StreakColor ("Streak colour", Color) = (0.35, 0.3, 0.28, 1)
        _StreakStretch ("Streak stretch (xyz)", Vector) = (0.6, 3, 0.6, 0)
        _StreakWidth ("Streak width (0 = off)", Range(0, 0.2)) = 0
        _StreakWarp ("Streak warp", Float) = 1.5

        _VertexColorAmount ("Vertex colour amount", Range(0, 1)) = 0
        _FogAmount ("Fog amount", Range(0, 1)) = 1
        _OutlineColor ("Outline colour", Color) = (0.2, 0.17, 0.15, 1)
        _OutlineWidth ("Outline width (world units, 0 = off)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _ShadeColor, _HighlightColor, _PatchColor, _StreakColor, _OutlineColor;
            float _ShadeThreshold, _HighlightThreshold, _ShadowShade;
            float _PatchScale, _PatchAmount;
            float4 _StreakStretch;
            float _StreakWidth, _StreakWarp, _VertexColorAmount, _FogAmount, _OutlineWidth;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),
                                 lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                                 lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float Fbm(float3 p)
            {
                return Noise(p) * 0.6 + Noise(p * 2.03 + 7.1) * 0.3 + Noise(p * 4.1 + 3.3) * 0.1;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = dot(n, light.direction);
                float lit = lerp(ndl, min(ndl, light.shadowAttenuation > 0.5 ? 1.0 : -1.0), _ShadowShade);

                half3 col = lit < _ShadeThreshold ? _ShadeColor.rgb
                          : (lit < _HighlightThreshold ? _BaseColor.rgb : _HighlightColor.rgb);
                // Patches and streaks darken in the same proportion as the band, so they stay visible in shade.
                half3 bandScale = col / max(_BaseColor.rgb, 0.001);

                if (_PatchAmount > 0)
                {
                    float p = Fbm(float3(i.positionWS.xz * _PatchScale, 0.0));
                    if (p < _PatchAmount * 0.55) col = _PatchColor.rgb * bandScale;
                }

                if (_StreakWidth > 0)
                {
                    float3 sp = i.positionWS * _StreakStretch.xyz;
                    float warp = Noise(i.positionWS * 0.15) * _StreakWarp;
                    float s = Fbm(sp + warp);
                    if (abs(frac(s * 3.0) - 0.5) < _StreakWidth) col = _StreakColor.rgb * bandScale;
                }

                col = lerp(col, col * i.color.rgb, _VertexColorAmount);

                #if defined(FOG_LINEAR)
                    // Distance along the view direction, so it works with the orthographic camera.
                    float depth = -TransformWorldToView(i.positionWS).z;
                    float fog = saturate(depth * unity_FogParams.z + unity_FogParams.w);
                    col = lerp(unity_FogColor.rgb, col, lerp(1.0, fog, _FogAmount));
                #endif
                return half4(col, 1);
            }
            ENDHLSL
        }

        // Dark outline: the back faces pushed out along the normals. Off when _OutlineWidth is 0.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalize(TransformObjectToWorldNormal(input.normalOS)) * _OutlineWidth;
                float4 positionCS = TransformWorldToHClip(positionWS);
                if (_OutlineWidth <= 0) positionCS = float4(0, 0, -2, 1); // collapsed, outside the view
                return positionCS;
            }

            half4 frag() : SV_Target { return half4(_OutlineColor.rgb, 1); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
