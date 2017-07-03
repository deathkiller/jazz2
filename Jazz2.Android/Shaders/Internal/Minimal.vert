#version 300 es 

uniform mat4 ModelView;
uniform mat4 Projection;

in vec4 Color;
in vec3 Position;
in vec2 TexCoord;

out vec2 vTexcoord0;
out vec4 vCornerColor;

void main() {
    gl_Position = Projection * (ModelView * vec4(Position, 1.0));

    vTexcoord0 = TexCoord;
    vCornerColor = Color;
}