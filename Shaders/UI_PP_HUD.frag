#version 450 core

#include "UI_Functions.glsl"

layout(location = 0) out vec4 outColor;

layout(set=0, binding=0) uniform sampler2D imguiTex; // rendered ImGui Window
layout(location = 0) in struct {
  vec2 Px; // screen pixel coord
  vec2 Uv; // screen uv coord
} In;
layout(location = 4) flat in vec4 PxRect; // bounding pixel rect for window
layout(location = 8) flat in vec4 UvRect; // bounding uv rect for window

layout(set = 1, binding = 0) uniform HudTestBuffer {
  float BufferValue1;
};

// TO DO: expose params to C#
const int blurRadius = 16;
const int blurSpread = 8;
const float threshold = 0.1;
const float intensity = 3.0;

void main()
{
    vec2 curvedUv = CurvedUV_UI(In.Uv, UvRect, -0.15, 1.15);
    vec4 color = ChromaticAberrationUI(imguiTex, curvedUv, UvRect, -0.003);
    
    vec2 texelSize = 1.0 / vec2(textureSize(imguiTex, 0));

//    vec4 glow = GaussianBlurUI(imguiTex, curvedUv, texelSize, blurRadius, blurSpread, threshold);

//    vec4 glow = SpiralGlowUI(imguiTex, curvedUv, texelSize,
//        20.0,   // radius in pixels
//        128,     // samples
//        threshold
//    );
//    color += glow * intensity;

    outColor = vec4(color.rgb, color.a);

//    vec4 color = textureLod(imguiTex, curvedUv, 0);
//    outColor = vec4(vec3(1.0,0.0,0.0), color.a);
}