#ifndef HUD_FUNC_INCLUDED
#define HUD_FUNC_INCLUDED

const int radius = 12;
const float sigma = 6.0;
const float falloff = 1.0;

vec2 CurvedUV(vec2 uv, float strength, float scale)
{
    // Center at (-1..1)
    vec2 p = uv * 2.0 - 1.0;

    // Scale around the center
    p *= scale;

    // Barrel distortion
    float r2 = dot(p, p);
    p *= 1.0 + strength * r2;

    // Back to 0-1
    return p * 0.5 + 0.5;
}

vec4 GaussianBlur(sampler2D Source, vec2 Uv, vec2 texelSize, vec2 direction)
{
	vec2 minUv = 0.5 * texelSize;
    vec2 maxUv = 1.0 - minUv;

    float twoSigma2 = 2.0 * sigma * sigma;

    vec4 blurred = vec4(0.0);
    float weightSum = 0.0;

    for (int i = -radius; i <= radius; i++)
    {
        float x = float(i);

        float weight = exp(-(x * x) / twoSigma2);

        vec2 offset = direction * vec2(x, x) * texelSize;

        blurred += texture(Source, clamp(Uv + offset, minUv, maxUv)) * weight;

        weightSum += weight;
    }

    blurred /= weightSum;

    return blurred;
}

float RadialGradient(vec2 uv, vec2 texelSize)
{
    vec2 p = uv * 2.0 - 1.0;

    // Aspect correction
    float aspect = texelSize.y / texelSize.x;
    p.x *= aspect;

    // Normalized radial distance (0 at center, 1 at corners)
    float r = length(p) / length(vec2(aspect, 1.0));

    // Exponential falloff
    float mask = (exp(falloff * r) - 1.0) /
                 (exp(falloff) - 1.0);

    return clamp(mask, 0.0, 1.0);
}

#endif