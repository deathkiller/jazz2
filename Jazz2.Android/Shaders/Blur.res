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
    //vec2 pixelOffsetBlur = 1.1 * pixelOffset * blurDirection;
    //
    //vec4 samples = vec4(0, 0, 0, 0);
    //samples += texture(mainTex, vTexcoord0.xy - pixelOffsetBlur * vec2(5)) * vec4(1.0);
    //samples += texture(mainTex, vTexcoord0.xy - pixelOffsetBlur * vec2(4)) * vec4(10.0);
    //samples += texture(mainTex, vTexcoord0.xy - pixelOffsetBlur * vec2(3)) * vec4(45.0);
    //samples += texture(mainTex, vTexcoord0.xy - pixelOffsetBlur * vec2(2)) * vec4(120.0);
    //samples += texture(mainTex, vTexcoord0.xy - pixelOffsetBlur * vec2(1)) * vec4(210.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(0)) * vec4(252.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(1)) * vec4(210.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(2)) * vec4(120.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(3)) * vec4(45.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(4)) * vec4(10.0);
    //samples += texture(mainTex, vTexcoord0.xy + pixelOffsetBlur * vec2(5)) * vec4(1.0);
    //
    //vFragColor = vCornerColor * samples / vec4(1024.0);
    
    vec4 color = vec4(0.0);
    vec2 off = vec2(1.3333333333333333) * pixelOffset * blurDirection;
    color += texture(mainTex, vTexcoord0) * 0.29411764705882354;
    color += texture(mainTex, vTexcoord0 + off) * 0.35294117647058826;
    color += texture(mainTex, vTexcoord0 - off) * 0.35294117647058826;
    vFragColor = color; 
}"

}