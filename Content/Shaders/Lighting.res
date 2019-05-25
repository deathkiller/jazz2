{
    "BlendMode": "Add",

    "Fragment": "
uniform vec2 ViewSize;

uniform sampler2D normalBuffer;

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
    
    vec3 lightDir = vec3((center.x - gl_FragCoord.x), (center.y - gl_FragCoord.y), 0.0);
    
    // Diffuse lighting
    // Wasn't clamped to 1.0
    float diffuseFactor = 1.0 - /*min(*/max(dot(normal, normalize(lightDir)), 0.0) /*, 1.0)*/;
    //finalColor.rgb += attenFactor * _lightColor[i] * clrDiffuse.rgb * diffuseFactor;
    
    /*float attenuation = 1.0 / (0.4 + (7.0*lightDir) + (30.0*lightDir*lightDir) );

    #define STEP_A 0.4
    #define STEP_B 0.6
    #define STEP_C 0.8
    #define STEP_D 1.0

    //Here is where we apply some toon shading to the light
    if (attenuation < STEP_A) 
        attenuation = 0.0;
    else if (attenuation < STEP_B) 
        attenuation = STEP_B;
    else if (attenuation < STEP_C) 
        attenuation = STEP_C;
    else 
        attenuation = STEP_D;
        
    diffuseFactor = diffuseFactor * (1 - attenuation);*/
    
    // Specular lighting
    //float specularFactor = pow(max(dot(normal, normalize(eyeDir + lightDir)), 0.000001), clrSpecular.a * 64.0);
    //finalColor.rgb += _lightColor[i] * clrSpecular.rgb * specularFactor * diffuseFactor * attenFactor;

    float strength = diffuseFactor * light_blend(clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0));
    gl_FragColor = vec4(strength * intensity, strength * brightness, 0.0, 1.0);
}"

}