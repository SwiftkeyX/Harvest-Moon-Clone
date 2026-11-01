// Toon shading for Unity Terrain. The colour comes from the shape, so there's nothing to paint:
// flat ground is grass in four greens (base, a near-identical base v2, wavy dark bands, clusters of small darker spots),
// steep ground is rock (with wavy dark layer strokes),
// low ground near the sea is sand and high flat ground is snow. Every edge between colours is hard.
// Lighting matches Farm/Toon Lit: two flat bands, tinted shadows. Use with Terrain.drawInstanced off.
Shader "Farm/Toon Terrain"
{
    Properties
    {
        _GrassColor ("Base Grass", Color) = (0.57, 0.72, 0.29, 1)
        _GrassColor2 ("Base Grass v2", Color) = (0.59, 0.72, 0.28, 1)
        _GrassDark ("Grass Bands", Color) = (0.51, 0.65, 0.26, 1)
        _GrassSpot ("Grass Spots", Color) = (0.36, 0.49, 0.19, 1)
        _RockColor ("Rock", Color) = (0.86, 0.66, 0.50, 1)
        _RockDark ("Rock Strokes", Color) = (0.72, 0.50, 0.39, 1)
        _SandColor ("Sand", Color) = (0.95, 0.84, 0.60, 1)
        _SnowColor ("Snow", Color) = (0.96, 0.97, 1.0, 1)
        _SlopeThreshold ("Grass Below This Steepness", Range(0, 1)) = 0.72
        _BandHeight ("Rock Layer Height (m)", Float) = 3.5
        _SandHeight ("Sand Below Height (m)", Float) = -86
        _SnowHeight ("Snow Above Height (m)", Float) = 62
        _PatchScale ("Grass Band Scale (big)", Float) = 0.12
        _BandScale ("Grass Band Scale (medium)", Float) = 0.4
        _BandCover ("Grass Band Cover", Range(0, 1)) = 0.45
        _Base2Scale ("Base Grass v2 Scale", Float) = 0.14
        _Base2Cover ("Base Grass v2 Cover", Range(0, 1)) = 0.5
        _SpotSize ("Grass Spot Spacing (m)", Float) = 0.7
        _SpotDensity ("Grass Spot Density (inside a cluster)", Range(0, 1)) = 0.95
        _SpotCluster ("Grass Spot Cluster Cover", Range(0, 1)) = 0.68
        _ShadowColor ("Shadow Tint", Color) = (0.58, 0.56, 0.82, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001, 0.5)) = 0.04
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-100" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _GrassColor;
            half4 _GrassColor2;
            half4 _GrassDark;
            half4 _GrassSpot;
            half4 _RockColor;
            half4 _RockDark;
            half4 _SandColor;
            half4 _SnowColor;
            half _SlopeThreshold;
            float _BandHeight;
            float _SandHeight;
            float _SnowHeight;
            float _PatchScale;
            float _BandScale;
            half _BandCover;
            float _Base2Scale;
            half _Base2Cover;
            float _SpotSize;
            half _SpotDensity;
            half _SpotCluster;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _AmbientStrength;
        CBUFFER_END

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        // Smooth value noise, 0..1. Only ever used through step(), so the result stays hard-edged.
        float Noise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float a = Hash(i), b = Hash(i + float2(1, 0)), c = Hash(i + float2(0, 1)), d = Hash(i + float2(1, 1));
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            // Green 2: wavy dark bands. Big slow shapes mixed with medium ones, with bent outlines,
            // so even a narrow ledge crosses a few edges. 1 inside a band.
            // Base grass v2: an almost identical green over about half the ground, in large bent shapes of its own
            // (different offsets from the bands, so the two never line up). 1 where it shows.
            float Base2Mask(float2 xz)
            {
                float2 warp = float2(Noise(xz * _Base2Scale * 0.8 + 41.2), Noise(xz * _Base2Scale * 0.8 + 63.5)) - 0.5;
                float2 q = xz + warp * (0.9 / _Base2Scale);
                return step(1.0 - _Base2Cover, Noise(q * _Base2Scale + 27.9));
            }

            float BandMask(float2 xz)
            {
                // The outline bend scales with the band size, so bands keep their shape at any scale.
                float2 warp = float2(Noise(xz * _PatchScale * 0.83 + 3.1), Noise(xz * _PatchScale * 0.83 + 7.7)) - 0.5;
                float2 q = xz + warp * (0.72 / _PatchScale);
                float n = Noise(q * _PatchScale) * 0.55 + Noise(q * _BandScale + 11.3) * 0.45;
                return step(1.0 - _BandCover, n);
            }

            // Green 3: small blobby spots that gather in clusters. The ground is cut into cells _SpotSize wide;
            // a cell gets a spot (a dot plus a smaller one beside it) more often where the cluster noise is high.
            // Neighbouring cells are checked too, so spots can cross cell borders without being cut.
            float SpotMask(float2 xz)
            {
                float2 g = xz / _SpotSize;
                float2 home = floor(g);
                float hit = 0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 cell = home + float2(x, y);
                    // Clusters about 7 m across: small enough that every stretch of ledge crosses a few.
                    float cluster = Noise((cell + 0.5) * _SpotSize * 0.15 + 5.0);
                    float chance = saturate((cluster - (1.0 - _SpotCluster)) * 2.2) * _SpotDensity;
                    float has = step(Hash(cell + 39.0), chance);
                    float2 c = cell + float2(Hash(cell), Hash(cell + 17.0));
                    float r = lerp(0.2, 0.38, Hash(cell + 53.0));
                    float a = Hash(cell + 71.0) * 6.2832;
                    float2 side = c + float2(cos(a), sin(a)) * r * 0.95;
                    float spot = max(step(length(g - c), r), step(length(g - side), r * 0.6));
                    hit = max(hit, spot * has);
                }
                // Far away a spot shrinks to a few pixels and flickers, so let them go there.
                float metresPerPixel = length(fwidth(xz));
                return hit * step(metresPerPixel, _SpotSize * 0.12);
            }

            half3 SurfaceColor(float3 p, float3 n)
            {
                // Grass on flat ground; the border with rock wobbles so it isn't a clean contour line.
                float wobble = (Noise(p.xz * 0.35) - 0.5) * 0.12;
                float flat = step(_SlopeThreshold, n.y + wobble);

                // Rock: wavy horizontal layers, drawn as broken dark strokes.
                float warp = (Noise(p.xz * 0.12 + p.y * 0.05) - 0.5) * 2.5;
                float layer = frac((p.y + warp) / _BandHeight);
                float broken = step(0.35, Noise(float2(p.x + p.z, p.y * 2.0) * 0.25));
                half3 rock = lerp(_RockColor.rgb, _RockDark.rgb, step(layer, 0.16) * broken);

                // Green 1 is the base; bands and spots sit on top, all hard-edged.
                half3 grass = lerp(_GrassColor.rgb, _GrassColor2.rgb, Base2Mask(p.xz));
                grass = lerp(grass, _GrassDark.rgb, BandMask(p.xz));
                grass = lerp(grass, _GrassSpot.rgb, SpotMask(p.xz));

                float sandLine = _SandHeight + (Noise(p.xz * 0.2) - 0.5);
                float sand = step(p.y, sandLine) * step(0.35, n.y);
                float snow = step(_SnowHeight, p.y) * flat;

                half3 color = lerp(rock, grass, flat);
                color = lerp(color, _SandColor.rgb, sand);
                return lerp(color, _SnowColor.rgb, snow);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 baseColor = SurfaceColor(input.positionWS, normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half nDotL = dot(normalWS, mainLight.direction);
                half band = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, nDotL);
                half lit = band * smoothstep(0.4, 0.6, mainLight.shadowAttenuation);

                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                    lit *= ao.directAmbientOcclusion;
                #endif

                half3 color = lerp(baseColor * _ShadowColor.rgb, baseColor * mainLight.color, lit);
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target { return 0; }
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

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half Frag() : SV_Target { return 0; }
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
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
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
