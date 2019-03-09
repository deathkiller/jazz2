{
    "BlendMode": "Alpha",

    "Fragment": "
#version 300 es 
precision mediump float;

uniform sampler2D mainTex;
uniform sampler2D normalTex;
uniform vec2 normalMultiplier;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor[2];

void main() {
    vFragColor[0] = vCornerColor * texture(mainTex, vTexcoord0);
    
    vec4 normal = texture(normalTex, vTexcoord0);
    normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
    vFragColor[1] = normal;
}"

}