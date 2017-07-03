#version 300 es 

uniform mat4 ModelView;
uniform mat4 Projection;

in vec4 Color;
in vec3 Position;
in vec4 TexCoord;
in float animBlend;

out vec4 vTexcoord0;
out vec4 vCornerColor;
out float animBlendVar;

void main() {
    gl_Position = Projection * (ModelView * vec4(Position, 1.0));
    vTexcoord0 = TexCoord;
    vCornerColor = Color;
    animBlendVar = animBlend;
}