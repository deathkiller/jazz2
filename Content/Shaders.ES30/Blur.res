{
    "Fragment": "
#version 300 es 
precision mediump float;

uniform sampler2D mainTex;
uniform vec2 blurDirection;
uniform vec2 pixelOffset;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor;

void main() {
    vec4 color = vec4(0.0);
    vec2 off = vec2(1.3333333333333333) * pixelOffset * blurDirection;
    color += texture(mainTex, vTexcoord0) * 0.29411764705882354;
    color += texture(mainTex, vTexcoord0 + off) * 0.35294117647058826;
    color += texture(mainTex, vTexcoord0 - off) * 0.35294117647058826;
    vFragColor = color; 
}"

}