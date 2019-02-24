{
    "BlendMode": "Alpha",

    "Fragment": "
#version 100
precision mediump float;

uniform sampler2D mainTex;
uniform sampler2D paletteTex;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    vec4 index = texture2D(mainTex, vTexcoord0);
    vec4 color = texture2D(paletteTex, vec2(index.r + vCornerColor.g /*PaletteShift*/, vCornerColor.r /*PaletteIndex*/));
    gl_FragColor = vec4(color.rgb * vCornerColor.b /*Intensity*/, color.a * index.a * vCornerColor.a);
}"

}