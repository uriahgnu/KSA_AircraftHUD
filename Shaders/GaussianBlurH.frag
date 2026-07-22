#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

layout(set = 1, binding = 0) uniform sampler2D Source;

layout(location = 0) in vec2 Uv;

// layout(push_constant, std430) uniform BlurParams {
//   int radius;
//   float weights[21];
// } blur;

void main()
{
    vec2 texelSize = 1.0 / vec2(textureSize(Source, 0));
    vec2 curveUv = CurvedUV(Uv, -0.1, 1.12);
    vec4 blurred = GaussianBlur(Source, curveUv, texelSize, vec2(1.0, 0.0));
    outColor = blurred;
}
