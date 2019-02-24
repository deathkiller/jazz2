{
    "BlendMode": "Alpha",

    "Fragment": "
#version 100
precision mediump float;

uniform sampler2D mainTex;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    gl_FragColor = vCornerColor * texture2D(mainTex, vTexcoord0);
}"

}