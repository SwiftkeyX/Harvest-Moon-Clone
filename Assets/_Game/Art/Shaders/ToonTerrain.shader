// Toon shading for Unity Terrain. The colour comes from the shape, so there's nothing to paint:
// flat ground is grass: land tones (lush, base, warm) that follow height and slope, medium light and dark patches,
// big soft bands, a few dry ochre patches, and light and dark tufts on top,
// steep ground is rock (painted streaks in dark and mid grey with light patches),
// low ground near the sea is sand and high flat ground is snow. Every edge between colours is hard.
// Lighting matches Farm/Toon Lit: two flat bands, tinted shadows. Use with Terrain.drawInstanced off.
Shader "Farm/Toon Terrain"
{
    Properties
    {
        [Header(Ground Types)]
        _SlopeThreshold ("Grass Below This Steepness", Range(0, 1)) = 0.72
        _SandHeight ("Sand Below Height (m)", Float) = -86
        _SnowHeight ("Snow Above Height (m)", Float) = 62
        _SandColor ("Sand", Color) = (0.95, 0.84, 0.60, 1)
        _SnowColor ("Snow", Color) = (0.96, 0.97, 1.0, 1)

        [Header(Grass Land Tones)]
        _GrassColor ("Base Grass", Color) = (0.57, 0.72, 0.29, 1)
        _GrassLush ("Lush Grass (low and flat ground)", Color) = (0.46, 0.66, 0.33, 1)
        _GrassWarm ("Warm Grass (high ground and slopes)", Color) = (0.69, 0.75, 0.31, 1)
        _ToneLow ("Lush Below Height (m)", Float) = -80
        _ToneHigh ("Warm Above Height (m)", Float) = 20
        _ToneSlope ("Slopes Pull Warm", Range(0, 2)) = 0.35
        _ToneScale ("Land Tone Shape Scale", Float) = 0.045
        _ToneMix ("Land Tone Shape Strength", Range(0, 1)) = 0.55

        [Header(Grass Patches)]
        _PatchDarkTint ("Dark Patch Tint", Color) = (0.90, 0.93, 0.92, 1)
        _PatchLightTint ("Light Patch Tint", Color) = (1.07, 1.06, 0.95, 1)
        _MidPatchScale ("Patch Scale", Float) = 0.22
        _MidPatchCover ("Patch Cover (each)", Range(0, 0.5)) = 0.26

        [Header(Grass Bands)]
        _BandTint ("Band Tint", Color) = (0.89, 0.90, 0.90, 1)
        _PatchScale ("Grass Band Scale (big)", Float) = 0.12
        _BandScale ("Grass Band Scale (medium)", Float) = 0.4
        _BandCover ("Grass Band Cover", Range(0, 1)) = 0.4

        [Header(Dry Grass)]
        _DryColor ("Dry Grass", Color) = (0.72, 0.73, 0.37, 1)
        _DryScale ("Dry Patch Scale", Float) = 0.5
        _DryCover ("Dry Patch Cover", Range(0, 0.5)) = 0.2

        [Header(Grass Tufts)]
        _TuftDarkTint ("Dark Tuft Tint", Color) = (0.62, 0.68, 0.60, 1)
        _TuftLightTint ("Light Tuft Tint", Color) = (1.10, 1.08, 0.93, 1)
        _LightTuftShare ("Light Tuft Share", Range(0, 1)) = 0.4
        _SpotSize ("Tuft Spacing (m)", Float) = 0.5
        _SpotDensity ("Tuft Density (busiest areas)", Range(0, 1)) = 0.8
        _SpotQuiet ("Tuft Density (quietest areas)", Range(0, 1)) = 0.3
        _TuftAxis ("Tuft Direction (world XZ)", Vector) = (1, 0, 0, 0)

        [Header(Fallen Leaves)]
        _LitterColor ("Fallen Leaves", Color) = (0.74, 0.30, 0.22, 1)
        _LitterColor2 ("Fallen Leaves 2", Color) = (0.86, 0.56, 0.24, 1)
        _LitterSize ("Fallen Leaf Spacing (m)", Float) = 0.4
        _LitterCover ("Fallen Leaf Cover", Range(0, 1)) = 0.42

        [Header(Rock Streaks)]
        _RockColor ("Rock", Color) = (0.86, 0.66, 0.50, 1)
        _RockLight ("Rock Light Patches", Color) = (0.90, 0.88, 0.84, 1)
        _RockMid ("Rock Mid Strokes", Color) = (0.68, 0.65, 0.66, 1)
        _RockDark ("Rock Dark Streaks", Color) = (0.72, 0.50, 0.39, 1)
        _StreakScale ("Rock Streak Scale", Float) = 0.22
        _StreakCover ("Rock Dark Streak Cover", Range(0, 1)) = 0.3

        [Header(Lighting)]
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
            half4 _GrassLush;
            half4 _GrassWarm;
            float _ToneLow;
            float _ToneHigh;
            half _ToneSlope;
            float _ToneScale;
            half _ToneMix;
            half4 _PatchDarkTint;
            half4 _PatchLightTint;
            float _MidPatchScale;
            half _MidPatchCover;
            half4 _BandTint;
            half4 _DryColor;
            float _DryScale;
            half _DryCover;
            half4 _TuftDarkTint;
            half4 _TuftLightTint;
            half _LightTuftShare;
            half _SpotQuiet;
            half4 _RockColor;
            half4 _RockDark;
            half4 _RockMid;
            half4 _RockLight;
            float _StreakScale;
            half _StreakCover;
            float4 _TuftAxis;
            half4 _LitterColor;
            half4 _LitterColor2;
            float _LitterSize;
            half _LitterCover;
            half4 _SandColor;
            half4 _SnowColor;
            half _SlopeThreshold;
            float _SandHeight;
            float _SnowHeight;
            float _PatchScale;
            float _BandScale;
            half _BandCover;
            float _SpotSize;
            half _SpotDensity;
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

            // Bent noise: value noise read through a warped position, so outlines wobble like brush shapes.
            // The bend scales with the shape size, so shapes keep their look at any scale.
            float BentNoise(float2 xz, float scale, float seed)
            {
                float2 warp = float2(Noise(xz * scale * 0.8 + seed), Noise(xz * scale * 0.8 + seed + 17.3)) - 0.5;
                return Noise((xz + warp * (0.9 / scale)) * scale + seed * 1.7);
            }

            // Land tone: lush (cool blue-green) on low, flat ground, warm (yellow-green) on high ground and slopes,
            // base in between. Big bent shapes break up the lines so it doesn't read as contour bands.
            // Hard steps: 0 = lush, 1 = base, 2 = warm.
            float LandTone(float3 p, float3 n)
            {
                float height = saturate((p.y - _ToneLow) / max(_ToneHigh - _ToneLow, 1.0));
                float slope = saturate((1.0 - n.y) / max(1.0 - _SlopeThreshold, 0.01));
                float shapes = BentNoise(p.xz, _ToneScale, 41.2) * 0.65 + BentNoise(p.xz, _ToneScale * 2.7, 12.9) * 0.35;
                float t = 0.5 + (height - 0.5) * (1.0 - _ToneMix) + slope * _ToneSlope * 0.5 + (shapes - 0.5) * _ToneMix * 2.8;
                return step(0.36, t) + step(0.64, t);
            }

            // Medium patches, a few metres across, half darker and half lighter, so no stretch of ground is one flat colour.
            // Returns -1 (dark), 0 (none) or 1 (light).
            float MidPatch(float2 xz)
            {
                float n = BentNoise(xz, _MidPatchScale, 63.5);
                return step(1.0 - _MidPatchCover, n) - step(n, _MidPatchCover);
            }

            // Dry grass: a few small patches where the autumn grass goes straw-coloured. 1 inside one.
            float DryMask(float2 xz)
            {
                float n = BentNoise(xz, _DryScale, 87.1) * 0.7 + Noise(xz * _DryScale * 3.1 + 5.5) * 0.3;
                return step(1.0 - _DryCover, n);
            }

            // Wavy bands: big slow shapes mixed with medium ones, with bent outlines,
            // so even a narrow ledge crosses a few edges. 1 inside a band.
            float BandMask(float2 xz)
            {
                // The outline bend scales with the band size, so bands keep their shape at any scale.
                float2 warp = float2(Noise(xz * _PatchScale * 0.83 + 3.1), Noise(xz * _PatchScale * 0.83 + 7.7)) - 0.5;
                float2 q = xz + warp * (0.72 / _PatchScale);
                float n = Noise(q * _PatchScale) * 0.55 + Noise(q * _BandScale + 11.3) * 0.45;
                return step(1.0 - _BandCover, n);
            }

            // Tufts: quick three-stroke brush flicks, some darker and some lighter than the ground under them.
            // The ground is cut into cells _SpotSize wide; a cell gets a tuft more often where the cluster noise is high,
            // but even the quietest ground keeps a few. Neighbouring cells are checked too, so tufts cross cell borders.
            // Returns 0 (none), 1 (dark tuft) or 2 (light tuft).
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
                    float chance = lerp(_SpotQuiet, _SpotDensity, saturate((cluster - 0.3) * 2.5));
                    float has = step(Hash(cell + 39.0), chance) * (1.0 + step(Hash(cell + 91.0), _LightTuftShare));
                    // A tuft: three short tapered strokes fanning out from one root, like a quick brush flick.
                    float2 c = cell + float2(Hash(cell), Hash(cell + 17.0)) * 0.6 + 0.2;
                    float len = lerp(0.35, 0.9, Hash(cell + 53.0));
                    float lean = (Hash(cell + 71.0) - 0.5) * 1.8; // tufts turn up to about 50 degrees either way
                    float2 axis = normalize(_TuftAxis.xy + 1e-4);
                    float tuft = 0;
                    [unroll] for (int b = -1; b <= 1; b++)
                    {
                        float ang = lean + b * 0.5;
                        float2 d = float2(axis.x * cos(ang) - axis.y * sin(ang), axis.x * sin(ang) + axis.y * cos(ang));
                        float2 rel = g - c;
                        float t = dot(rel, d) / (len * (b == 0 ? 1.0 : 0.75));
                        float side = abs(rel.x * d.y - rel.y * d.x);
                        tuft = max(tuft, step(0.0, t) * step(t, 1.0) * step(side, 0.09 * (1.0 - t * 0.8)));
                    }
                    hit = max(hit, tuft * has);
                }
                // Far away a spot shrinks to a few pixels and flickers, so let them go there.
                float metresPerPixel = length(fwidth(xz));
                return hit * step(metresPerPixel, _SpotSize * 0.2);
            }

            // Fallen leaves: small pointed splats in two autumn colours, in loose drifts.
            // Returns 0 (none), 1 (first colour) or 2 (second colour).
            float LitterMask(float2 xz)
            {
                float2 g = xz / _LitterSize;
                float2 home = floor(g);
                float hit = 0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 cell = home + float2(x, y);
                    float drift = Noise((cell + 0.5) * _LitterSize * 0.11 + 71.0);
                    float has = step(Hash(cell + 3.0), saturate((drift - (1.0 - _LitterCover)) * 3.0));
                    float2 c = cell + float2(Hash(cell + 5.0), Hash(cell + 9.0));
                    float a = Hash(cell + 13.0) * 6.2832;
                    float2 d = float2(cos(a), sin(a));
                    float2 rel = g - c;
                    float t = dot(rel, d) / 0.42;
                    float s = abs(rel.x * d.y - rel.y * d.x);
                    float leaf = step(abs(t), 1.0) * step(s, 0.22 * pow(saturate(1.0 - t * t), 0.6) * (1.0 - max(t, 0.0) * 0.4));
                    hit = max(hit, leaf * has * (1.0 + step(0.5, Hash(cell + 21.0))));
                }
                return hit * step(length(fwidth(xz)), _LitterSize * 0.3);
            }

            half3 SurfaceColor(float3 p, float3 n)
            {
                // Grass on flat ground; the border with rock wobbles so it isn't a clean contour line.
                float wobble = (Noise(p.xz * 0.35) - 0.5) * 0.12;
                float flat = step(_SlopeThreshold, n.y + wobble);

                // Rock: painted streaks. Long, bent, mostly-level strokes in a dark and a mid grey, with a few light
                // patches, all hard-edged. Walls facing X use Z as "along", walls facing Z use X.
                float along = abs(n.x) > abs(n.z) ? p.z : p.x;
                float2 q = float2(along, p.y * 3.2) * _StreakScale;
                q += (float2(Noise(q * 0.35 + 3.7), Noise(q * 0.35 + 9.1)) - 0.5) * 3.0;
                q.y += Noise(q * float2(0.6, 0.2) + 21.0) * 1.5;
                float darkStreak = step(1.0 - _StreakCover, Noise(q * float2(0.55, 1.6)) * 0.7 + Noise(q * float2(1.6, 4.0) + 5.3) * 0.3);
                float midStreak = step(0.6, Noise(q * float2(0.7, 2.2) + 13.1) * 0.6 + Noise(q * float2(2.4, 6.0) + 2.2) * 0.4);
                float lightPatch = step(0.68, Noise(q * float2(0.3, 0.9) + 31.0));
                half3 rock = _RockColor.rgb;
                rock = lerp(rock, _RockLight.rgb, lightPatch);
                rock = lerp(rock, _RockMid.rgb, midStreak);
                rock = lerp(rock, _RockDark.rgb, darkStreak);

                // Grass, bottom to top: land tone, medium patches, bands, dry patches, tufts. All hard-edged.
                float tone = LandTone(p, n);
                half3 grass = tone < 0.5 ? _GrassLush.rgb : (tone < 1.5 ? _GrassColor.rgb : _GrassWarm.rgb);
                float patch = MidPatch(p.xz);
                grass *= patch < -0.5 ? _PatchDarkTint.rgb : (patch > 0.5 ? _PatchLightTint.rgb : half3(1, 1, 1));
                grass *= lerp(half3(1, 1, 1), _BandTint.rgb, BandMask(p.xz));
                float tuft = SpotMask(p.xz);
                grass *= tuft > 1.5 ? _TuftLightTint.rgb : (tuft > 0.5 ? _TuftDarkTint.rgb : half3(1, 1, 1));
                // Dry patches show mostly in the tufts (straw-coloured blades); the ground under them only leans that way.
                grass = lerp(grass, _DryColor.rgb * (tuft > 1.5 ? 1.08 : 0.9), DryMask(p.xz) * (tuft > 0.5 ? 0.85 : 0.35));
                float litter = LitterMask(p.xz);
                grass = lerp(grass, litter > 1.5 ? _LitterColor2.rgb : _LitterColor.rgb, step(0.5, litter));

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
