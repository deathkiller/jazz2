{
    "Fragment": "
uniform sampler2D mainTex;
uniform vec2 blurDirection;
uniform vec2 pixelOffset;

void main() {
    //ivec2 texSize = textureSize(mainTex, 0);
    //vec2 pixelOffsetBlur = 1.1 * vec2(1.0 / texSize.x, 1.0 / texSize.y) * blurDirection;
    vec2 pixelOffsetBlur = 1.1 * pixelOffset * blurDirection;

    vec4 samples = vec4(0, 0, 0, 0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy - pixelOffsetBlur * vec2(5)) * vec4(1.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy - pixelOffsetBlur * vec2(4)) * vec4(10.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy - pixelOffsetBlur * vec2(3)) * vec4(45.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy - pixelOffsetBlur * vec2(2)) * vec4(120.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy - pixelOffsetBlur * vec2(1)) * vec4(210.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(0)) * vec4(252.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(1)) * vec4(210.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(2)) * vec4(120.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(3)) * vec4(45.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(4)) * vec4(10.0);
    samples += texture2D(mainTex, gl_TexCoord[0].xy + pixelOffsetBlur * vec2(5)) * vec4(1.0);

    gl_FragColor = gl_Color * samples / vec4(1024.0);
}"

}