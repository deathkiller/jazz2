#version 300 es 
precision mediump float;

uniform sampler2D mainTex;
uniform float smoothness;

const float Gamma = 2.2;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor;

void main() {
    // Retrieve base color
    vec4 texClr = texture(mainTex, vTexcoord0);
    
    // Do some anti-aliazing
    float w = clamp(smoothness * (abs(dFdx(vTexcoord0.s)) + abs(dFdy(vTexcoord0.t))), 0.0, 0.5);
    float a = smoothstep(0.5 - w, 0.5 + w, texClr.a);

    // Perform Gamma Correction to achieve a linear attenuation
    texClr.a = pow(a, 1.0 / Gamma);

    // Compose result color
    vFragColor = vCornerColor * texClr; 
}