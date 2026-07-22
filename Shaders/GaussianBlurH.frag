#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

layout(set = 1, binding = 0) uniform sampler2D Source;
layout(set = 1, binding = 1) uniform sampler2D dirtMask;

layout(location = 0) in vec2 Uv;

layout(push_constant, std430) uniform Params {
   int enabled;
   float frame;
} pp;

void main()
{
    vec2 curveUv = CurvedUV(Uv, -0.1, 1.12);
    if (pp.enabled == 1)
    {
        vec2 texelSize = 1.0 / vec2(textureSize(Source, 0));
        vec2 refractedUv = RefractionUVs(curveUv, texelSize, dirtMask).rg;
        vec4 blurred = GaussianBlur(Source, refractedUv, texelSize, vec2(1.0, 0.0));
        outColor = blurred;
    }
    else { outColor = texture(Source, curveUv); }
}
