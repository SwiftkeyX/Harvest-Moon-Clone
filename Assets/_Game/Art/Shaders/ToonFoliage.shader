// Brushy foliage: a cloud of leaf-cluster cards that always face the camera, cut out by a leaf texture.
// Each card's four corners share one position (the card's middle); the vertex shader spreads them out in view space.
// The normal is the direction from the middle of the bush, so the whole cloud shades like one soft ball:
// a light top, a mid tone and a tinted shadow underneath. Meshes come from FoliageCards (editor).
//   uv0 = card corner (0..1), uv1 = (size in object units, spin in radians, shade jitter -1..1, unused)
Shader "Farm/Toon Foliage"
{
    Properties
    {
        _BaseColor ("Leaf Colour", Color) = (0.55, 0.65, 0.25, 1)
        _LeafMap ("Leaf Cluster (alpha cut-out)", 2D) = "white" {}
        _Cutoff ("Alpha Cut-off", Range(0, 1)) = 0.5
        _ShadowColor ("Shadow Tint", Color) = (0.58, 0.56, 0.82, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = -0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001, 0.5)) = 0.06
        _HighlightThreshold ("Highlight Threshold", Range(-1, 1)) = 0.55
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.08
        _CastShadowStrength ("Cast Shadow Strength", Range(0, 1)) = 0.75
        _ShadeJitter ("Per-Card Shade Jitter", Range(0, 0.5)) = 0.08
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35
        [Toggle] _Upright ("Upright (grass: stands on its bottom edge, turns only around Y)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_LeafMap);
        SAMPLER(sampler_LeafMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _LeafMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _HighlightThreshold;
            half _HighlightStrength;
            half _CastShadowStrength;
            half _ShadeJitter;
            half _AmbientStrength;
            half _Upright;
        CBUFFER_END

        struct CardAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 corner : TEXCOORD0;
            float4 card : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // World position of this corner: the card's middle, pushed out along the current view's right and up.
        // In the shadow pass the "view" is the light, so cards turn to face the sun and cast a full shadow.
        // Upright cards (grass) stand on their bottom edge and only turn around the world Y axis.
        float3 CardCornerWS(CardAttributes input)
        {
            float3 centreWS = TransformObjectToWorld(input.positionOS.xyz);
            float scale = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
            if (_Upright > 0.5)
            {
                float3 side = UNITY_MATRIX_V[0].xyz;
                side = normalize(float3(side.x, 0, side.z) + float3(1e-5, 0, 0));
                float lean = input.card.y; // reused as a sideways lean for grass
                float h = input.corner.y * input.card.x * scale;
                return centreWS + side * ((input.corner.x - 0.5) * input.card.x * scale + lean * h) + float3(0, h, 0);
            }
            float2 offset = (input.corner - 0.5) * input.card.x * scale;
            float s, c;
            sincos(input.card.y, s, c);
            offset = float2(offset.x * c - offset.y * s, offset.x * s + offset.y * c);
            float3 right = UNITY_MATRIX_V[0].xyz;
            float3 up = UNITY_MATRIX_V[1].xyz;
            return centreWS + right * offset.x + up * offset.y;
        }

        float2 CardUV(CardAttributes input) { return TRANSFORM_TEX(input.corner, _LeafMap); }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half shade : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(CardAttributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = CardCornerWS(input);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = CardUV(input);
                output.shade = 1.0 + input.card.z * _ShadeJitter;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 leaf = SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv);
                clip(leaf.a - _Cutoff);

                float3 normalWS = normalize(input.normalWS);
                half3 baseColor = _BaseColor.rgb * leaf.rgb * input.shade;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half nDotL = dot(normalWS, mainLight.direction);
                half band = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, nDotL);
                half castShadow = lerp(1.0, smoothstep(0.4, 0.6, mainLight.shadowAttenuation), _CastShadowStrength);
                half lit = band * castShadow;

                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                    lit *= ao.directAmbientOcclusion;
                #endif

                // A third, lighter tone where the sun hits the ball straight on.
                half highlight = smoothstep(_HighlightThreshold - 0.03, _HighlightThreshold + 0.03, nDotL) * lit;
                half3 litSide = baseColor * mainLight.color * (1.0 + highlight * _HighlightStrength * 2.0);
                half3 color = lerp(baseColor * _ShadowColor.rgb, litSide, lit);
                color += baseColor * SampleSH(normalWS) * _AmbientStrength;
                return half4(MixFog(color, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(CardAttributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = CardCornerWS(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                // Cards face the light here, so bias along the light direction instead of the (soft) normal.
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, lightDirectionWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                output.uv = CardUV(input);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(CardAttributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(CardCornerWS(input));
                output.uv = CardUV(input);
                return output;
            }

            half Frag(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(CardAttributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(CardCornerWS(input));
                output.uv = CardUV(input);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_LeafMap, sampler_LeafMap, input.uv).a - _Cutoff);
                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 remapped = saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
