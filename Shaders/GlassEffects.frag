#version 450 core

layout(location = 0) out vec4 outColor;

layout(set = 1, binding = 0) uniform sampler2D Source;

layout(location = 0) in vec2 Uv;

// layout(push_constant, std430) uniform BlurParams {
//   int radius;
//   float weights[21];
// } blur;

void main()
{
  vec4 color = texture(Source, Uv);
  outColor = color;
}