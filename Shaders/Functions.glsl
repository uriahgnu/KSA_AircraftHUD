#ifndef HUD_FUNC_INCLUDED
#define HUD_FUNC_INCLUDED

const float PI = 3.14159265359;

float Luminance(vec3 c)
{
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

vec3 ScreenNoise(sampler2D tex, vec2 size, float time)
{
    size.y + (fract(time) * 63);
	vec2 screenUv = gl_FragCoord.xy / size;

	vec2 UvFrac = fract(screenUv);
	vec4 UvColor = vec4(UvFrac.x, UvFrac.y, 0, 1);

	return texture(tex, UvFrac).rgb;
}

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

const float radialScale = 1.1;
const float radialFalloff = 1.5;
float RadialGradient(vec2 uv, vec2 texelSize)
{
    vec2 p = uv * 2.0 - 1.0;
    p *= radialScale;

    // Aspect correction
    float aspect = texelSize.y / texelSize.x;
    p.x *= aspect;

    // Normalized radial distance (0 at center, 1 at corners)
    float r = length(p) / length(vec2(aspect, 1.0));

    // Exponential falloff
    float mask = (exp(radialFalloff * r) - 1.0) /
                 (exp(radialFalloff) - 1.0);

    return clamp(mask, 0.0, 1.0);
}

vec4 DirtMask(vec2 uv, vec2 texelSize, vec4 color, vec4 blur, sampler2D dirtMask)
{
    float lum = Luminance(blur.rgb);
	float lumMask = smoothstep(0.05, 0.5, lum);

    float mask = RadialGradient(uv, texelSize);

	float dirt = texture(dirtMask, uv).r;

	dirt *= lumMask;

    //color = blur;
    color = mix(color, blur, mask + dirt);
    color += dirt * (blur * 6.0);

    return color;
}

const int GaussianSamples = 48;      // Samples on each side
const float GaussianSigma = 16.0;    // Gaussian weight falloff
const float BlurRadius = 512.0;      // Blur radius in pixels
const float BlurExponent = 2.0;     // 1=linear, 2=quadratic, 3=cubic

vec4 GaussianBlur(sampler2D source, vec2 uv, vec2 texelSize, vec2 direction)
{
    vec2 minUv = 0.5 * texelSize;
    vec2 maxUv = 1.0 - minUv;

    float twoSigma2 = 2.0 * GaussianSigma * GaussianSigma;

    vec4 blurred = vec4(0.0);
    float weightSum = 0.0;

    for (int i = -GaussianSamples; i <= GaussianSamples; i++)
    {
        float t = float(i) / float(GaussianSamples);

        // Non-linear sample distribution
        float distance = sign(t) * pow(abs(t), BlurExponent);

        float x = distance * BlurRadius;
        float weight = exp(-(x * x) / twoSigma2);

        vec2 offset = direction * distance * BlurRadius * texelSize;

        blurred += texture(
            source,
            clamp(uv + offset, minUv, maxUv)
        ) * weight;

        weightSum += weight;
    }

    return blurred / weightSum;
}

vec3 RadialBlur(sampler2D source, vec2 uv, vec2 texelSize, vec2 radius, int samples, vec2 dither)
{
    vec3 color = vec3(0.0);
    float totalWeight = 0.0;

    // Equivalent to N
    for (int y = 1; y < samples; ++y)
    {
        float fy = float(y);
        float sy = (fy / float(samples) - 0.5) * radius.y + dither.y;

        for (int x = 1; x < samples; ++x)
        {
            float fx = float(x);
            float sx = (fx / float(samples) - 0.5) * radius.x + dither.x;

            vec2 offset = vec2(sx, sy) * texelSize;

            vec2 sampleUV = clamp(uv + offset, vec2(0.0), vec2(1.0));

            // Same weight function UE generates
            float dx = fx - float(samples) * 0.5;
            float dy = fy - float(samples) * 0.5;

            float weight = clamp(
                float(samples) * 0.5 - length(vec2(dx, dy)),
                0.0,
                1.0);

            color += texture(source, sampleUV).rgb * weight;
            totalWeight += weight;
        }
    }

    return color / max(totalWeight, 1e-5);
}

#endif