{
    "BlendMode": "Alpha",

    "Fragment": "
#extension GL_ARB_draw_buffers : enable 

uniform sampler2D mainTex;
uniform sampler2D normalTex;
uniform vec2 normalMultiplier;

void main() {
    vec4 dye = vec4(1.0) + (gl_Color.rgba - vec4(0.5)) * vec4(4.0);

    vec4 original = texture2D(mainTex, gl_TexCoord[0].st);
    float average = (original.r + original.g + original.b) * 0.5;
    vec4 gray = vec4(average, average, average, original.a);

    gl_FragData[0] = vec4(gray * dye);

    vec4 normal = texture2D(normalTex, gl_TexCoord[0].st);
    normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
    gl_FragData[1] = normal;
}"

}