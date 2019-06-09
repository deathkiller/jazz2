{
    "Fragment": "
uniform sampler2D mainTex;
uniform vec2 blurDirection;
uniform vec2 pixelOffset;

void main() {
    vec4 color = vec4(0, 0, 0, 0);
    vec2 off1 = vec2(1.3846153846) * pixelOffset * blurDirection;
    vec2 off2 = vec2(3.2307692308) * pixelOffset * blurDirection;
    color += texture2D(mainTex, gl_TexCoord[0].xy) * 0.2270270270;
    color += texture2D(mainTex, gl_TexCoord[0].xy + off1) * 0.3162162162;
    color += texture2D(mainTex, gl_TexCoord[0].xy - off1) * 0.3162162162;
    color += texture2D(mainTex, gl_TexCoord[0].xy + off2) * 0.0702702703;
    color += texture2D(mainTex, gl_TexCoord[0].xy - off2) * 0.0702702703;
    gl_FragColor = gl_Color * color;
}"

}