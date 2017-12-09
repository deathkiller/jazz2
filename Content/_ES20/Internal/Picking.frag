precision mediump float;

uniform sampler2D mainTex;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    gl_FragColor = vec4(vCornerColor.rgb, step(0.5, texture2D(mainTex, vTexcoord0).a));
}