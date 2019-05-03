#version 300 es 
precision mediump float;

uniform sampler2D mainTex;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor;

void main() {
    vFragColor = vCornerColor * texture(mainTex, vTexcoord0);
}