{
    "BlendMode": "Alpha",
    "VertexFormat": "C1P3T4A1",

    "Vertex": "#inherit SmoothAnim",
    "Fragment": "
uniform sampler2D mainTex;
uniform sampler2D normalTex;
uniform vec2 normalMultiplier;

varying float animBlendVar;

void main() {
    // Retrieve frames
    vec4 texClrOld = texture2D(mainTex, gl_TexCoord[0].st);
    vec4 texClrNew = texture2D(mainTex, gl_TexCoord[0].pq);

    // This code prevents nasty artifacts when blending between differently masked frames
    float accOldNew = (texClrOld.w - texClrNew.w) / (texClrOld.w + texClrNew.w);
    accOldNew *= mix(min(min(animBlendVar, 1.0 - animBlendVar) * 4.0, 1.0), 1.0, abs(accOldNew));
    texClrNew.xyz = mix(texClrNew.xyz, texClrOld.xyz, max(accOldNew, 0.0));
    texClrOld.xyz = mix(texClrOld.xyz, texClrNew.xyz, max(-accOldNew, 0.0));

    // Blend between frames
    gl_FragData[0] = gl_Color * mix(texClrOld, texClrNew, animBlendVar);

    vec4 normal = texture2D(normalTex, gl_TexCoord[0].st);
    normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
    gl_FragData[1] = normal;
}"

}