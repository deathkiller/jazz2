{
    "BlendMode": "Add",

    "Vertex": "#inherit MinimalC1P3T4",
    "Fragment": "
#version 300 es 
precision mediump float;

uniform vec2 ViewSize;

uniform sampler2D normalBuffer;

in vec4 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor;

float light_blend(float t) {
    return t * t;
}

void main() {
    vec2 center = vTexcoord0.xy;
    float radiusNear = vTexcoord0.z;
    float radiusFar = vTexcoord0.w;
    float intensity = vCornerColor.r;
    float brightness = vCornerColor.g;

    float dist = distance(vec2(gl_FragCoord), center);
    if (dist > radiusFar) {
        vFragColor = vec4(0, 0, 0, 0);
        return;
    }

    vec4 clrNormal = texture(normalBuffer, vec2(gl_FragCoord) / ViewSize);
    vec3 normal = normalize(clrNormal.xyz - vec3(0.5, 0.5, 0.5));
    normal.z = -normal.z;

    vec3 lightDir = vec3((center.x - gl_FragCoord.x), (center.y - gl_FragCoord.y), 0);

    // Diffuse lighting
    float diffuseFactor = 1.0 - max(dot(normal, normalize(lightDir)), 0.0);

    float strength = diffuseFactor * light_blend(clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0));
    vFragColor = vec4(strength * intensity, strength * brightness, 0.0, 1.0);
}"

}