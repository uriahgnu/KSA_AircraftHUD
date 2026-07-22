#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

layout(set = 1, binding = 0) uniform sampler2D Source;
layout(set = 1, binding = 1) uniform sampler2D dirtTex;
layout(set = 1, binding = 2) uniform sampler2D noiseTex;

layout(location = 0) in vec2 Uv;
//layout(location = 1) in vec2 Px;
//layout(location = 0) in struct {
//  vec2 Px; // screen pixel coord
//  vec2 Uv; // screen uv coord
//} In;

// layout(push_constant, std430) uniform BlurParams {
//   int radius;
//   float weights[21];
// } blur;

void main()
{
	vec2 texelSize = 1.0 / vec2(textureSize(Source, 0));
    vec2 uv = CurvedUV(Uv, -0.1, 1.12);
    vec4 original = texture(Source, uv);

	vec3 noise = ScreenNoise(noiseTex, vec2(128.0, 8192.0), 0.0);

    // single sample dithered Gaussian Blur
    vec3 blurred = RadialBlur(
        Source,
        uv,
        texelSize,
        vec2(128),
        32,
        noise.rg);

//    outColor = vec4(blurred, 1);
    outColor = DirtMask(Uv, texelSize, original, vec4(blurred, 1), dirtTex);
//	outColor = vec4(noise,1);
}
