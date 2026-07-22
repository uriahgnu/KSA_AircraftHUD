#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

// layout(set = 1, binding = 0, input_attachment_index = 0) uniform subpassInput Source;
layout(set = 1, binding = 0) uniform sampler2D Source;
layout(set = 1, binding = 1) uniform sampler2D PreviousInput;
layout(set = 1, binding = 2) uniform sampler2D noiseTex;
layout(set = 1, binding = 3) uniform sampler2D dirtMask;

layout(location = 0) in vec2 Uv;


// layout(push_constant, std430) uniform BlurParams {
//   int radius;
//   float weights[21];
// } blur;

void main()
{
	vec2 texelSize = 1.0 / vec2(textureSize(Source, 0));

	vec2 zoomUv = Uv - 0.5;
	zoomUv *= 0.95;
	zoomUv += 0.5;

	// vec4 color = subpassLoad(Source);
	// vec4 color = texture(Source, Uv);
	vec4 color = ChromaticAberration(Source, zoomUv, texelSize, -15.0);

	vec2 curveUv = CurvedUV(zoomUv, -0.1, 1.12);
    vec4 original = texture(PreviousInput, curveUv);

    color = DirtMask(Uv, texelSize, original, color, dirtMask);

	// RGB Blue Noise (STBN) LDR Dithering
	vec3 noise = ScreenNoise(noiseTex, vec2(128.0, 8192.0), 0.0).rgb;
	noise -= 0.5; // -0.5 to 0.5 range
	noise *= 0.15; // strength
	color += vec4(noise, 1) * color;

	outColor = color;
}