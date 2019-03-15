{
    "Fragment": "
uniform sampler2D mainTex;

void main() {
    vec3 color = texture2D(mainTex, gl_TexCoord[0].st).rgb;
    float gray = dot(((color - vec3(0.5)) * vec3(1.4, 1.2, 1.0)) + vec3(0.5), vec3(0.3, 0.7, 0.1));
    float palette = (abs(1.0 - gray) * 0.75) + 0.125;

    if (palette < 0.25) {
        color = vec3(0.675, 0.710, 0.420);
    } else if (palette < 0.5) {
        color = vec3(0.463, 0.518, 0.283);
    } else if (palette < 0.75) {
        color = vec3(0.247, 0.314, 0.247);
    } else {
        color = vec3(0.141, 0.192, 0.216);
    }

    gl_FragData[0] = vec4(color, 1.0);
}"

}