{
    "BlendMode": "Add",

    "Fragment": "
uniform vec2 ViewSize;
uniform float GameTime;

uniform sampler2D normalBuffer;
uniform sampler2D noiseTex;

float light_blend(float t) {
    return t * t;
}

void main() {
    vec2 center = gl_TexCoord[0].xy;
    float radiusNear = gl_TexCoord[0].z;
    float radiusFar = gl_TexCoord[0].w;
    float intensity = gl_Color.r;
    float brightness = gl_Color.g;
    
    float dist = distance(vec2(gl_FragCoord), center);
    if (dist > radiusFar) {
        gl_FragColor = vec4(0, 0, 0, 0);
        return;
    }
    
    vec4 clrNormal = texture2D(normalBuffer, vec2(gl_FragCoord) / ViewSize);
    vec3 normal = normalize(clrNormal.xyz - vec3(0.5, 0.5, 0.5));
    normal.z = -normal.z;
    
    vec3 lightDir = vec3((center.x - gl_FragCoord.x), (center.y - gl_FragCoord.y), 0);
    
    // Diffuse lighting
    float diffuseFactor = 1.0 - max(dot(normal, normalize(lightDir)), 0.0);

    float noise = 0.3 + 0.7 * texture2D(noiseTex, (gl_FragCoord.xy / ViewSize.xx) + vec2(GameTime * 5.0, GameTime * 3.0)).r;

    float strength = noise * diffuseFactor * light_blend(clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0));
    gl_FragColor = vec4(strength * intensity, strength * brightness, 0.0, 1.0);
}"

}