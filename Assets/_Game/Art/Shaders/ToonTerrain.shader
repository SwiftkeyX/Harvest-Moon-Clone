// Toon shading for Unity Terrain. The colour comes from the shape, so there's nothing to paint:
// flat ground is grass in three tones (dark, base, light) whose shapes follow the land: dark pools round props and at
// the foot of slopes, light sits on ledge tops (from a baked grass shape map), with angular edges, stretched along the
// land and breaking up into small pieces at their borders; plus a few dry straw patches,
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

        [Header(Grass Tones)]
        _GrassColor ("Base Grass", Color) = (0.57, 0.72, 0.29, 1)
        _GrassLush ("Dark Grass (round props, foot of slopes)", Color) = (0.46, 0.66, 0.33, 1)
        _GrassWarm ("Light Grass (open ground, ledge tops)", Color) = (0.69, 0.75, 0.31, 1)

        [Header(Grass Shapes)]
        [NoScaleOffset] _ShapeMap ("Grass Shape Map (baked: Farm, Art, Bake Grass Shape Map)", 2D) = "black" {}
        _ShapeRect ("Shape Map Area (x, z, width, depth)", Vector) = (0, 0, 1, 1)
        _PropPull ("Dark Round Props", Range(0, 2)) = 1
        _FootPull ("Dark At Slope Feet", Range(0, 2)) = 0.8
        _TopPull ("Light On Ledge Tops", Range(0, 2)) = 0.5
        _ShapeScale ("Shape Scale", Float) = 0.2
        _ShapeStretch ("Stretch Along The Land", Range(1, 4)) = 2.2
        _ShapeNoise ("Shape Randomness", Range(0, 1)) = 0.55
        _DarkLevel ("Dark Grass Above", Range(0, 1)) = 0.62
        _LightLevel ("Light Grass Below", Range(0, 1)) = 0.26
        _BreakupSize ("Break-up Piece Spacing (m)", Float) = 1.2
        _BreakupReach ("Break-up Reach", Range(0, 0.5)) = 0.3

        [Header(Dry Grass)]
        _DryColor ("Dry Grass", Color) = (0.72, 0.73, 0.37, 1)
        _DryScale ("Dry Patch Scale", Float) = 0.5
        _DryCover ("Dry Patch Cover", Range(0, 0.5)) = 0.2

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
            float4 _ShapeRect;
            half _PropPull;
            half _FootPull;
            half _TopPull;
            float _ShapeScale;
            half _ShapeStretch;
            half _ShapeNoise;
            half _DarkLevel;
            half _LightLevel;
            float _BreakupSize;
            half _BreakupReach;
            half4 _DryColor;
            float _DryScale;
            half _DryCover;
            half4 _RockColor;
            half4 _RockDark;
            half4 _RockMid;
            half4 _RockLight;
            float _StreakScale;
            half _StreakCover;
            half4 _SandColor;
            half4 _SnowColor;
            half _SlopeThreshold;
            float _SandHeight;
            float _SnowHeight;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _AmbientStrength;
        CBUFFER_END

        TEXTURE2D(_ShapeMap);
        SAMPLER(sampler_ShapeMap);

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

            // Value noise with straight (linear) blending between its grid points, so its outlines come out as short straight
            // runs with corners, like cut paper, instead of smooth waves.
            float AngularNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float a = Hash(i), b = Hash(i + float2(1, 0)), c = Hash(i + float2(0, 1)), d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // How the baked map pulls the grass here: up (towards dark) near props and at slope feet, down on ledge tops.
            float ShapePull(float2 xz)
            {
                float4 m = SAMPLE_TEXTURE2D_LOD(_ShapeMap, sampler_ShapeMap, (xz - _ShapeRect.xy) / _ShapeRect.zw, 0);
                return m.r * _PropPull + m.g * _FootPull - m.b * _TopPull;
            }

            // The direction the grass shapes stretch: along the outlines of the baked pull (across its slope).
            float2 ShapeAlong(float2 xz)
            {
                float e = 1.5;
                float2 grad = float2(ShapePull(xz + float2(e, 0)) - ShapePull(xz - float2(e, 0)),
                                     ShapePull(xz + float2(0, e)) - ShapePull(xz - float2(0, e)));
                return length(grad) > 1e-3 ? normalize(float2(-grad.y, grad.x)) : float2(0.94, 0.34);
            }

            // The grass shape value: high = dark grass, low = light grass. The baked pull sets where shapes go;
            // angular noise, stretched along the land, gives them bitten edges.
            // The stretch blends four fixed directions instead of rotating the noise, so shapes never swim.
            float ShapeValue(float2 xz, float2 along)
            {
                float n = 0, wsum = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float ang = k * 0.7854;
                    float2 d = float2(cos(ang), sin(ang));
                    float w = pow(abs(dot(along, d)), 4.0) + 1e-3;
                    float2 q = float2(dot(xz, d) / _ShapeStretch, dot(xz, float2(-d.y, d.x))) * _ShapeScale;
                    n += (AngularNoise(q + k * 17.1) * 0.7 + AngularNoise(q * 2.6 + k * 5.3 + 40.0) * 0.3) * w;
                    wsum += w;
                }
                return 0.45 + ShapePull(xz) * 0.5 + (n / wsum - 0.5) * _ShapeNoise * 1.6;
            }

            // Grass tone, 0 = light, 1 = base, 2 = dark. Just outside a dark or light shape, small chunky pieces break off it,
            // with a gap between, so big shapes don't end on one clean line. Neighbouring cells are checked too,
            // so a piece is never cut off at its cell's border.
            float GrassTone(float2 xz)
            {
                float2 along = ShapeAlong(xz);
                float v = ShapeValue(xz, along);
                float tone = v > _DarkLevel ? 2.0 : (v < _LightLevel ? 0.0 : 1.0);
                if (tone != 1.0) return tone;
                float2 home = floor(xz / _BreakupSize);
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 cell = home + float2(x, y);
                    float2 centreXZ = (cell + 0.3 + float2(Hash(cell + 3.0), Hash(cell + 7.0)) * 0.4) * _BreakupSize;
                    float cv = ShapeValue(centreXZ, along);
                    // Pieces sit a little way off the edge (most at about 40% of the reach), so a gap shows between.
                    float toDark = (_DarkLevel - cv) / _BreakupReach, toLight = (cv - _LightLevel) / _BreakupReach;
                    float nearDark = saturate(1.0 - abs(toDark - 0.4) / 0.4);
                    float nearLight = saturate(1.0 - abs(toLight - 0.4) / 0.4);
                    float chance = max(nearDark, nearLight);
                    if (Hash(cell + 11.0) >= chance) continue;
                    // A chunky piece: two overlapping lumps (rounded, randomly turned squares), so it reads as a clump.
                    float a = Hash(cell + 19.0) * 3.1416;
                    float2 rel = (xz - centreXZ) / _BreakupSize;
                    rel = float2(rel.x * cos(a) - rel.y * sin(a), rel.x * sin(a) + rel.y * cos(a));
                    float radius = lerp(0.18, 0.3, Hash(cell + 23.0)) * lerp(0.6, 1.0, chance);
                    float2 rel2 = rel - float2(radius * 0.9, radius * 0.3);
                    float lump1 = max(abs(rel.x), abs(rel.y)) * 0.45 + length(rel) * 0.55;
                    float lump2 = (max(abs(rel2.x), abs(rel2.y)) * 0.45 + length(rel2) * 0.55) / 0.7;
                    if (min(lump1, lump2) < radius) return nearDark >= nearLight ? 2.0 : 0.0;
                }
                return tone;
            }

            // Dry grass: a few small patches where the autumn grass goes straw-coloured. 1 inside one.
            float DryMask(float2 xz)
            {
                float n = BentNoise(xz, _DryScale, 87.1) * 0.7 + Noise(xz * _DryScale * 3.1 + 5.5) * 0.3;
                return step(1.0 - _DryCover, n);
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

                // Grass: light, base or dark by the grass shapes, then dry patches. All hard-edged.
                float tone = GrassTone(p.xz);
                half3 grass = tone < 0.5 ? _GrassWarm.rgb : (tone < 1.5 ? _GrassColor.rgb : _GrassLush.rgb);
                // Dry patches: the ground only leans towards straw.
                grass = lerp(grass, _DryColor.rgb * 0.9, DryMask(p.xz) * 0.35);

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
