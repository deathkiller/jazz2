{
    "BlendMode": "Alpha",
    "VertexFormat": "C1P3T4A1",

    "Vertex": "#inherit SmoothAnim",
    "Fragment": "
#version 300 es 
precision mediump float;

uniform sampler2D mainTex;
uniform sampler2D normalTex;
uniform vec2 normalMultiplier;

in vec4 vTexcoord0;
in vec4 vCornerColor;
in float animBlendVar;

out vec4 vFragColor[2];

void main() {
    // Retrieve frames
    vec4 texClrOld = texture(mainTex, vTexcoord0.st);
    vec4 texClrNew = texture(mainTex, vTexcoord0.pq);

    // This code prevents nasty artifacts when blending between differently masked frames
    float accOldNew = (texClrOld.w - texClrNew.w) / (texClrOld.w + texClrNew.w);
    accOldNew *= mix(min(min(animBlendVar, 1.0 - animBlendVar) * 4.0, 1.0), 1.0, abs(accOldNew));
    texClrNew.xyz = mix(texClrNew.xyz, texClrOld.xyz, max(accOldNew, 0.0));
    texClrOld.xyz = mix(texClrOld.xyz, texClrNew.xyz, max(-accOldNew, 0.0));

    // Blend between frames
    vFragColor[0] = vCornerColor * mix(texClrOld, texClrNew, animBlendVar);

    vec4 normal = texture(normalTex, vTexcoord0.st);
    normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
    vFragColor[1] = normal;
}"

}