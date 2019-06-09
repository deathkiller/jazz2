{
    "BlendMode": "Add",

    "Fragment": "
uniform vec2 ViewSize;

uniform sampler2D normalBuffer;

float lightBlend(float t) {
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
    
    vec3 lightDir = vec3((center.x - gl_FragCoord.x), (center.y - gl_FragCoord.y), 0.0);
    
    // Diffuse lighting
    float diffuseFactor = 1.0 - max(dot(normal, normalize(lightDir)), 0.0);
    diffuseFactor = diffuseFactor * 0.8 + 0.2;
    
    // Specular lighting
    //vec3 viewDir = vec3((ViewSize.x * 0.5 - gl_FragCoord.x), (ViewSize.y * 0.5 - gl_FragCoord.y), 0.0);
    //vec3 reflectDir = reflect(-lightDir, normal);  
    //float specularFactor = pow(clamp(dot(viewDir, reflectDir), 0.0, 1.0), 32) * 0.1;

    float strength = diffuseFactor * lightBlend(clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0));
    gl_FragColor = vec4(strength * intensity, strength * brightness /*+ specularFactor*/, 0.0, 1.0);
}"

}