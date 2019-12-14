{
    "BlendMode": "Alpha",
    
    "Fragment": "
#version 300 es 
precision mediump float;

uniform vec2 ViewSize;

uniform float progressTime;

out vec4 vFragColor;

float rand(vec2 xy) {
    return fract(sin(dot(xy.xy, vec2(12.9898,78.233))) * 43758.5453);
}

float ease(float time) {
    time *= 2.0;
    if (time < 1.0)  {
        return 0.5 * time * time;
    }

    time -= 1.0;
    return -0.5 * (time * (time - 2.0) - 1.0);
}
 
void main() {
    vec2 uv = (gl_FragCoord.xy / ViewSize) - vec2(0.5);
    uv = uv * vec2(ViewSize / vec2(max(ViewSize.x, ViewSize.y)));

    float distance = length(uv);

    float progressInner = progressTime - 0.22;
    distance = (clamp(distance, progressInner, progressTime) - progressInner) / (progressTime - progressInner);

    float mixValue = ease(distance);

    float noise = 1.0 + rand(uv) * 0.1;
    vFragColor = vec4(0.0, 0.0, 0.0, mixValue * noise);
}"

}