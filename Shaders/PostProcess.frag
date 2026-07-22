#version 450 core

#include "Functions.glsl"

layout(location = 0) out vec4 outColor;

// layout(set = 1, binding = 0, input_attachment_index = 0) uniform subpassInput Source;
layout(set = 1, binding = 0) uniform sampler2D Source;
layout(set = 1, binding = 1) uniform sampler2D PreviousInput;
layout(set = 1, binding = 2) uniform sampler2D noiseTex;
layout(set = 1, binding = 3) uniform sampler2D dirtMask;

layout(location = 0) in vec2 Uv;

layout(push_constant, std430) uniform Params {
   int enabled;
   float frame;
} pp;

const float Enabled = 1.0;

void main()
{
	vec4 color;
	if (pp.enabled == 1)
	{
		vec2 zoomUv = Uv - 0.5;
		zoomUv *= 0.975;
		zoomUv += 0.5;

		vec2 texelSize = 1.0 / vec2(textureSize(Source, 0));

		color = ChromaticAberration(Source, zoomUv, texelSize, -15.0);

		vec2 curveUv = CurvedUV(zoomUv, -0.1, 1.12);
		vec4 refracted = RefractionUVs(curveUv, texelSize, dirtMask);

		vec4 original = texture(PreviousInput, refracted.rg);
		color = DirtMask(Uv, texelSize, original, color, refracted.b);

//		color = mix(vec4(0,0,0,1), color, refracted.a);

		outColor = color;
	}
	else
	{
		color = texture(PreviousInput, Uv);
	}

	// RGB Blue Noise (STBN) LDR Dithering
	vec3 noise = ScreenNoise(noiseTex, pp.frame).rgb;
	noise -= 0.5; // -0.5 to 0.5 range
	noise *= 0.25; // strength: 0.15
	color += vec4(noise, 1) * color;

	outColor = color;
}