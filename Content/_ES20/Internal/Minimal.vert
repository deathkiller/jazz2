uniform mat4 ModelView;
uniform mat4 Projection;

uniform vec4 mainColor;

attribute vec4 Color;
attribute vec3 Position;
attribute vec2 TexCoord;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    gl_Position = Projection * (ModelView * vec4(Position, 1.0));

    vTexcoord0 = TexCoord;
    vCornerColor = Color * mainColor;
}