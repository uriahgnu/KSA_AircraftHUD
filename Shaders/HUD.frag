#version 450 core

layout(location = 0) out vec4 outColor;

layout(set=0, binding=0) uniform sampler2D imguiTex; // rendered ImGui Window
layout(location = 0) in struct {
  vec2 Px; // screen pixel coord
  vec2 Uv; // screen uv coord
} In;
layout(location = 4) flat in vec4 PxRect; // bounding pixel rect for window
layout(location = 8) flat in vec4 UvRect; // bounding uv rect for window

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

void main()
{
    vec2 uv = CurvedUV(In.Uv, UvRect, -0.15, 1.15);
    vec4 c = ChromaticAberration(imguiTex, uv, UvRect, 0.003);
    // vec4 c = textureLod(imguiTex, uv, 0);
    outColor = vec4(c.rgb, c.a);
}
