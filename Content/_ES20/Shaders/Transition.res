{
    "BlendMode": "Alpha",
    
    "Fragment": "
#version 100
precision mediump float;

uniform vec2 ViewSize;

uniform float progressTime;

float rand(vec2 xy) {
    return fract(sin(dot(xy.xy, vec2(12.9898,78.233))) * 43758.5453);
}

void main() {
    vec2 uv = (gl_FragCoord.xy / ViewSize) - vec2(0.5);
    uv = uv * vec2(ViewSize / vec2(max(ViewSize.x, ViewSize.y)));

    float distance = length(uv);

    float progressInner = progressTime - 0.22;
    distance = (clamp(distance, progressInner, progressTime) - progressInner) / (progressTime - progressInner);

    vec2 uvBlocks = floor(uv.xy * 80.0) / 80.0;

    float mixValue = distance * (1.0 + rand(uvBlocks) * 0.5);
    mixValue = mixValue * mixValue * mixValue * mixValue * mixValue;

    float noise = 1.0 + rand(uv) * 0.2;
    gl_FragColor = vec4(0.0, 0.0, 0.0, mixValue * noise);
}"

}