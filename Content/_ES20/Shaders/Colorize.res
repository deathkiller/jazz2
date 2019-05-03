{
    "BlendMode": "Alpha",

    "Fragment": "
#version 100
precision highp float;

uniform sampler2D mainTex;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    vec4 dye = vec4(1.0) + (vCornerColor.rgba - vec4(0.5)) * vec4(4.0);

    vec4 original = texture2D(mainTex, vTexcoord0);
    float average = (original.r + original.g + original.b) * 0.5;
    vec4 gray = vec4(average, average, average, original.a);

    gl_FragColor = vec4(gray * dye);
}"

}