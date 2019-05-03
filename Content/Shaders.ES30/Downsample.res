{
    "Fragment": "
#version 300 es 
precision mediump float;

uniform sampler2D mainTex;
uniform vec2 pixelOffset;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor;

void main() {
    vec4 sample0 = texture(mainTex, vTexcoord0.xy);
    vec4 sample1 = texture(mainTex, vTexcoord0.xy + vec2(pixelOffset.x, 0.0));
    vec4 sample2 = texture(mainTex, vTexcoord0.xy + vec2(0.0, pixelOffset.y));
    vec4 sample3 = texture(mainTex, vTexcoord0.xy + pixelOffset);
    vec4 average = vec4(0.25) * (sample0 + sample1 + sample2 + sample3);

    vFragColor = vCornerColor * average;
}"

}