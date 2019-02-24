{
    "BlendMode": "Add",

    "Vertex": "#inherit MinimalC1P3T4",
    "Fragment": "
#version 100
precision highp float;

uniform vec2 ViewSize;
uniform float GameTime;

uniform sampler2D noiseTex;

varying vec4 vTexcoord0;
varying vec4 vCornerColor;

void main() {
    vec2 center = vTexcoord0.xy;
    float radiusNear = vTexcoord0.z;
    float radiusFar = vTexcoord0.w;
    float intensity = vCornerColor.r;
    float brightness = vCornerColor.g;

    float dist = distance(vec2(gl_FragCoord), center);
    if (dist > radiusFar) {
        gl_FragColor = vec4(0, 0, 0, 0);
        return;
    }

    float noise = 0.3 + 0.7 * texture2D(noiseTex, (gl_FragCoord.xy / ViewSize.xx) + vec2(GameTime * 1.5, GameTime)).r;

    float strength = noise * clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0);
    gl_FragColor = vec4(strength * intensity, strength * brightness, 0.0, 1.0);
}"

}