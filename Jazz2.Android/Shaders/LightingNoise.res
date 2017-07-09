{
    "BlendMode": "Add",

    "Fragment": "
        #version 300 es 
        precision highp float;

        uniform vec2 center;
        uniform float intensity;
        uniform float brightness;
        uniform float radiusNear;
        uniform float radiusFar;

        uniform vec2 ViewSize;
        uniform float GameTime;

        //uniform vec2 normalMult;
        uniform sampler2D normalBuffer;
        uniform sampler2D noiseTex;

        in vec2 vTexcoord0;
        in vec4 vCornerColor;

        out vec4 vFragColor;

        void main() {
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

            float strength = clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0);

            float noise = 0.3 + 0.7 * texture(noiseTex, vTexcoord0.st * vec2(0.3) + vec2(GameTime * 1.5, GameTime)).r;

            vFragColor = vec4(diffuseFactor * strength * intensity * noise, strength * brightness * noise, 0, 1);
        }"
}