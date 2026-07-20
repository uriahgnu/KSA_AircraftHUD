#version 450 core

layout(location = 0) out vec4 outColor;

layout(set=0, binding=0) uniform sampler2D imguiTex; // rendered ImGui Window
layout(location = 0) in struct {
  vec2 Px; // screen pixel coord
  vec2 Uv; // screen uv coord
} In;
layout(location = 4) flat in vec4 PxRect; // bounding pixel rect for window
layout(location = 8) flat in vec4 UvRect; // bounding uv rect for window

// TO DO: expose params to C#
int blurRadius = 10;
int blurSpread = 5;
float threshold = 0.2;
float intensity = 2.0;

vec2 CurvedUV(vec2 uv, vec4 uvRect, float strength, float scale)
{
    vec2 local = (uv - uvRect.xy) / (uvRect.zw - uvRect.xy);

    vec2 p = local * 2.0 - 1.0;
    p *= scale;

    float r2 = dot(p, p);
    p *= 1.0 + strength * r2;

    local = p * 0.5 + 0.5;

    return uvRect.xy + local * (uvRect.zw - uvRect.xy);
}

float Luminance(vec3 c)
{
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

vec4 BrightSample(sampler2D tex, vec2 uv, float threshold)
{
    vec4 c = textureLod(tex, uv, 0);

    float lum = Luminance(c.rgb);

    float mask = smoothstep(threshold, threshold + 0.25, lum);

    return vec4(c.rgb * mask, c.a * mask);
}

vec4 ChromaticAberration(sampler2D tex, vec2 uv, vec4 uvRect, float amount)
{
    vec2 local = (uv - uvRect.xy) / (uvRect.zw - uvRect.xy);
    vec2 dir = local * 2.0 - 1.0;

    float r2 = dot(dir, dir);
    vec2 offset = dir * r2 * amount;

    vec4 center = textureLod(tex, uv, 0);
    vec4 red    = textureLod(tex, uv + offset, 0);
    vec4 blue   = textureLod(tex, uv - offset, 0);

    vec3 baseColor = center.rgb / max(center.a, 1e-5);

    float lumCenter = Luminance(center.rgb / max(center.a, 1e-5));
    float lumRed    = Luminance(red.rgb    / max(red.a,    1e-5));
    float lumBlue   = Luminance(blue.rgb   / max(blue.a,   1e-5));

    float lum = max(lumCenter, max(lumRed, lumBlue));

    float mask = smoothstep(0.15, 0.75, lum);

    vec3 color = center.rgb;
    color.r = mix(center.r, red.r, mask);
    color.b = mix(center.b, blue.b, mask);

    float alpha = mix(center.a, max(center.a, max(red.a, blue.a)), mask);

    return vec4(color, alpha);
}

vec4 GaussianBlur(sampler2D tex, vec2 uv, vec2 texelSize, int radius, int sigma, float threshold)
{
    vec4 result = vec4(0.0);

    float weightSum = 0.0;

    float twoSigmaSq = 2.0 * sigma * sigma;

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            vec2 offset = vec2(x, y) * texelSize;

            float distSq = float(x*x + y*y);

            float weight = exp(-distSq / twoSigmaSq);

            result += BrightSample(tex, uv + offset, threshold) * weight;

            weightSum += weight;
        }
    }

    return result / weightSum;
}

vec4 SpiralGlow(sampler2D tex, vec2 uv, vec2 texelSize, float radius, int samples, float threshold)
{
    vec4 result = vec4(0.0);

    float weightSum = 0.0;

    const float GOLDEN_ANGLE = 2.39996323;

    for (int i = 0; i < samples; i++)
    {
        float fi = float(i);

        float t = fi / float(samples - 1);

        float angle = fi * GOLDEN_ANGLE;

        vec2 dir = vec2(cos(angle), sin(angle));

        vec2 offset = dir * t * radius * texelSize;

        vec4 c = textureLod(tex, uv + offset, 0);

        float lum = Luminance(c.rgb);

        float mask = smoothstep(threshold, threshold + 0.25, lum);

        c.rgb *= mask;
        c.a *= mask;

        float weight = 1.0 - t;

        result += c * weight;
        weightSum += weight;
    }

    return result / weightSum;
}

void main()
{
    vec2 uv = CurvedUV(In.Uv, UvRect, -0.1, 1.12);
    vec4 color = ChromaticAberration(imguiTex, uv, UvRect, -0.003);
    // vec4 color = textureLod(imguiTex, uv, 0);
    
    vec2 texelSize = 1.0 / vec2(textureSize(imguiTex, 0));
    // vec4 glow = GaussianBlur(imguiTex, uv, texelSize, blurRadius, blurSpread, threshold);
    vec4 glow = SpiralGlow(imguiTex, uv, texelSize,
        20.0,   // radius in pixels
        128,     // samples
        threshold
    );
    color += glow * intensity;

    outColor = vec4(color.rgb, color.a);
}
