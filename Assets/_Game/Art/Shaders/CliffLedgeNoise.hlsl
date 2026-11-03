// Cliff Ledge noise steps as Shader Graph Custom Function nodes (File mode, precision Float).
// Each function is one node: plug its inputs in the same order as the parameters.
#ifndef CLIFF_LEDGE_NOISE_INCLUDED
#define CLIFF_LEDGE_NOISE_INCLUDED

float CL_Hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float CL_Noise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(CL_Hash(i + float3(0, 0, 0)), CL_Hash(i + float3(1, 0, 0)), f.x),
                     lerp(CL_Hash(i + float3(0, 1, 0)), CL_Hash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(CL_Hash(i + float3(0, 0, 1)), CL_Hash(i + float3(1, 0, 1)), f.x),
                     lerp(CL_Hash(i + float3(0, 1, 1)), CL_Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

// FBM with 1 to 3 layers (big, 2x smaller, 4x smaller). Fewer layers = smoother edges.
float CL_Fbm(float3 p, float layers)
{
    float v = CL_Noise(p) * 0.6, w = 0.6;
    if (layers >= 2) { v += CL_Noise(p * 2.03 + 7.1) * 0.3; w += 0.3; }
    if (layers >= 3) { v += CL_Noise(p * 4.1 + 3.3) * 0.1; w += 0.1; }
    return v / w;
}

// Node 1 "Streak Noise": stretched FBM, optionally domain-warped. Out: 0..1 noise value.
void CliffStreakNoise_float(float3 PositionWS, float3 Stretch, float Layers, float DomainWarp, float WarpStrength,
    float WarpSize, out float Noise)
{
    float warp = DomainWarp > 0.5 ? CL_Noise(PositionWS * WarpSize) * WarpStrength : 0.0;
    Noise = CL_Fbm(PositionWS * Stretch + warp, Layers);
}

// Node 2 "Contour": turns the noise into a streak mask (1 = streak).
// ContourLine: thin lines where the noise crosses Levels levels. Off: fill where the noise is below FillCutoff.
void CliffContour_float(float Noise, float ContourLine, float Levels, float Width, float FillCutoff, out float Mask)
{
    Mask = ContourLine > 0.5 ? step(abs(frac(Noise * Levels) - 0.5), Width) : step(Noise, FillCutoff);
}

// Node 3 "Toon Band": 0 = shade, 1 = lit, 2 = highlight, from the main light and its shadows.
// Shadows need the _MAIN_LIGHT_SHADOWS keywords on the graph; without them every point counts as unshadowed.
void CliffToonBand_float(float3 PositionWS, float3 NormalWS, float ShadeThreshold, float HighlightThreshold, out float Band)
{
#if defined(SHADERGRAPH_PREVIEW)
    float3 dir = normalize(float3(0.6, 0.7, -0.4));
    float atten = 1.0;
#else
    Light light = GetMainLight(TransformWorldToShadowCoord(PositionWS));
    float3 dir = light.direction;
    float atten = light.shadowAttenuation;
#endif
    float ndl = dot(normalize(NormalWS), dir);
    float lit = atten > 0.5 ? ndl : min(ndl, -1.0);
    Band = lit < ShadeThreshold ? 0.0 : (lit < HighlightThreshold ? 1.0 : 2.0);
}

// Node 4 "Toon Colour": picks the band colour, then paints streaks darkened by the same band.
void CliffToonColor_float(float Band, float Mask, float3 Lit, float3 Shade, float3 Highlight, float3 StreakColor,
    out float3 Color)
{
    float3 band = Band < 0.5 ? Shade : (Band < 1.5 ? Lit : Highlight);
    float3 bandScale = band / max(Lit, 0.001);
    Color = Mask > 0.5 ? StreakColor * bandScale : band;
}

// "Toon Band (shadows)": like CliffToonBand, with a switch for receiving cast shadows.
void CliffToonBandShadows_float(float3 PositionWS, float3 NormalWS, float ShadeThreshold, float HighlightThreshold,
    float ReceiveShadows, out float Band)
{
#if defined(SHADERGRAPH_PREVIEW)
    float3 dir = normalize(float3(0.6, 0.7, -0.4));
    float atten = 1.0;
#else
    Light light = GetMainLight(TransformWorldToShadowCoord(PositionWS));
    float3 dir = light.direction;
    float atten = ReceiveShadows > 0.5 ? light.shadowAttenuation : 1.0;
#endif
    float ndl = dot(normalize(NormalWS), dir);
    float lit = atten > 0.5 ? ndl : min(ndl, -1.0);
    Band = lit < ShadeThreshold ? 0.0 : (lit < HighlightThreshold ? 1.0 : 2.0);
}

// "Patch Mask": grass patches from world-space FBM on the ground plane (1 = patch).
void CliffPatchMask_float(float3 PositionWS, float PatchesOn, float Scale, float Amount, float Layers, float PatchWarp,
    float WarpStrength, out float Mask)
{
    Mask = 0.0;
    if (PatchesOn > 0.5 && Amount > 0.0)
    {
        float2 pp = PositionWS.xz * Scale;
        if (PatchWarp > 0.5 && WarpStrength > 0.0)
            pp += float2(CL_Fbm(float3(pp, 0.0), Layers), CL_Fbm(float3(pp + float2(5.2, 1.3), 0.0), Layers)) * WarpStrength;
        Mask = CL_Fbm(float3(pp, 0.0), Layers) < Amount * 0.55 ? 1.0 : 0.0;
    }
}

// "Fog Amount": Unity's Unlit graphs always add fog. This pre-compensates the colour so the final result has
// FogAmount of the scene's linear fog (1 = normal fog, 0 = no fog).
void CliffFogAmount_float(float3 Color, float3 PositionWS, float FogAmount, out float3 Out)
{
    Out = Color;
#if !defined(SHADERGRAPH_PREVIEW) && defined(FOG_LINEAR)
    if (FogAmount < 0.999)
    {
        float depth = -TransformWorldToView(PositionWS).z;
        float visible = saturate(depth * unity_FogParams.z + unity_FogParams.w);   // what Unity will keep
        float wanted = lerp(1.0, visible, saturate(FogAmount));
        Out = (unity_FogColor.rgb * (visible - wanted) + Color * wanted) / max(visible, 0.001);
    }
#endif
}

// "Outline Offset" (vertex stage): pushes the mesh out along its normals by Width world units.
// OutlineOn off: the hull collapses onto the mesh and stays hidden behind it.
void CliffOutlineOffset_float(float3 PositionOS, float3 NormalOS, float OutlineOn, float Width, out float3 Out)
{
    float w = OutlineOn > 0.5 ? Width : 0.0;
    float3 ws = TransformObjectToWorld(PositionOS) + normalize(TransformObjectToWorldNormal(NormalOS)) * w;
    Out = TransformWorldToObject(ws);
}

// "Backdrop UV": scale around the centre, then offset, so the painting can be moved or resized.
void CliffBackdropUV_float(float4 UV, float2 Scale, float2 Offset, out float2 Out)
{
    Out = (UV.xy - 0.5) / max(Scale, 0.001) + 0.5 + Offset;
}

// "Backdrop": texture colour times tint, then brightness, saturation and haze toward the fog colour.
void CliffBackdrop_float(float4 Sample, float4 Tint, float Cutoff, float Brightness, float Saturation, float Haze,
    out float3 Color, out float Alpha)
{
    float4 c = Sample * Tint;
    float grey = dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
    float3 rgb = lerp(grey.xxx, c.rgb, Saturation) * Brightness;
    Color = lerp(rgb, unity_FogColor.rgb, saturate(Haze));
    Alpha = c.a >= Cutoff ? 1.0 : 0.0;
}

#endif
