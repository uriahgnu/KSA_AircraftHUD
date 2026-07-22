#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

// layout(set = 1, binding = 0, input_attachment_index = 0) uniform subpassInput Source;
layout(set = 1, binding = 0) uniform sampler2D Source;
layout(set = 1, binding = 1) uniform sampler2D noiseTex;

layout(location = 0) in vec2 Uv;

// layout(push_constant, std430) uniform BlurParams {
//   int radius;
//   float weights[21];
// } blur;

void main()
{
	// vec4 color = subpassLoad(Source);
	vec3 color = texture(Source, Uv).rgb;

	// RGB Blue Noise (STBN) LDR Dithering
	vec3 noise = ScreenNoise(noiseTex, vec2(128.0, 8192.0), 0.0).rgb;
	noise -= 0.5; // -0.5 to 0.5 range
	noise *= 0.15; // strength
	color += noise * color;

	outColor = vec4(color, 1);
}