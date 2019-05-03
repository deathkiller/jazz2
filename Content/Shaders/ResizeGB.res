{
    "Fragment": "
uniform sampler2D mainTex;

float dither4x4(vec2 position, float brightness) {
  int x = int(mod(position.x, 4.0));
  int y = int(mod(position.y, 4.0));
  int index = x + y * 4;
  float limit = 0.0;

  if (x < 8) {
    if (index == 0) limit = 0.0625;
    if (index == 1) limit = 0.5625;
    if (index == 2) limit = 0.1875;
    if (index == 3) limit = 0.6875;
    if (index == 4) limit = 0.8125;
    if (index == 5) limit = 0.3125;
    if (index == 6) limit = 0.9375;
    if (index == 7) limit = 0.4375;
    if (index == 8) limit = 0.25;
    if (index == 9) limit = 0.75;
    if (index == 10) limit = 0.125;
    if (index == 11) limit = 0.625;
    if (index == 12) limit = 1.0;
    if (index == 13) limit = 0.5;
    if (index == 14) limit = 0.875;
    if (index == 15) limit = 0.375;
  }

  return brightness + (brightness < limit ? -0.05 : 0.1);
}

void main() {
    vec3 color = texture2D(mainTex, gl_TexCoord[0].st).rgb;
    float gray = dot(((color - vec3(0.5)) * vec3(1.4, 1.2, 1.0)) + vec3(0.5), vec3(0.3, 0.7, 0.1));
    gray = dither4x4(gl_FragCoord.xy, gray);
    float palette = (abs(1.0 - gray) * 0.75) + 0.125;

    if (palette < 0.25) {
        color = vec3(0.675, 0.710, 0.420);
    } else if (palette < 0.5) {
        color = vec3(0.463, 0.518, 0.283);
    } else if (palette < 0.75) {
        color = vec3(0.247, 0.314, 0.247);
    } else {
        color = vec3(0.1, 0.134, 0.151);
    }

    gl_FragData[0] = vec4(color, 1.0);
}"

}