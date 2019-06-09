{
    "Fragment": "
#version 300 es 
precision highp float;

uniform sampler2D mainTex;

uniform vec2 ViewSize;
uniform vec3 CameraPosition;

uniform vec4 horizonColor;
uniform vec2 shift;
uniform float parallaxStarsEnabled;

#define INV_PI 0.31830988618379067153776752675

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor[2];

vec2 hash2D(in vec2 p) {
    float h = dot(p, vec2(12.9898, 78.233));
    float h2 = dot(p, vec2(37.271, 377.632));
    return -1.0 + 2.0 * vec2(fract(sin(h) * 43758.5453), fract(sin(h2) * 43758.5453));
}

vec3 voronoi(in vec2 p) {
    vec2 n = floor(p);
    vec2 f = fract(p);

    vec2 mg, mr;

    float md = 8.0;
    for (int j = -1; j <= 1; ++j) {
        for (int i = -1; i <= 1; ++i) {
            vec2 g = vec2(float(i), float(j));
            vec2 o = hash2D(n + g);

            vec2 r = g + o - f;
            float d = dot(r, r);

            if (d < md) {
                md = d;
                mr = r;
                mg = g;
            }
        }
    }
	return vec3(md, mr);
}

float addStarField(vec2 samplePosition, float threshold) {
    vec3 starValue = voronoi(samplePosition);
    if (starValue.x < threshold) {
        float power = 1.0 - (starValue.x / threshold);
        return min(power * power * power, 0.5);
    }
    return 0.0;
}

void main() {
    // Position of pixel on screen (between -1 and 1)
    vec2 targetCoord = 2.0 * vTexcoord0 - 1.0;

    // Aspect ratio correction, so display circle instead of ellipse
    targetCoord.x *= ViewSize.x / ViewSize.y;

    // Distance to center of screen
    float distance = length(targetCoord);

    // x-coordinate of tunnel
    float xShift = (targetCoord.x == 0.0 ? sign(targetCoord.y) * 0.5 : atan(targetCoord.y, targetCoord.x) * INV_PI);

    vec2 texturePos = vec2(
        (xShift)       * 1.0 + (shift.x * 0.01),
        (1.0 / distance) * 1.4 + (shift.y * 0.002)
    );

    vec4 texColor = texture(mainTex, texturePos);
    float horizonOpacity = 1.0 - clamp(pow(distance, 1.4) - 0.3, 0.0, 1.0);

    vec4 horizonColorWithStars = horizonColor;
    if (parallaxStarsEnabled > 0.0) {
        vec2 samplePosition = (gl_FragCoord.xy / ViewSize.xx) + CameraPosition.xy * 0.00012;
        horizonColorWithStars += vec4(addStarField(samplePosition * 7.0, 0.00008));
        
        samplePosition = (gl_FragCoord.xy / ViewSize.xx) + CameraPosition.xy * 0.00018 + 0.5;
        horizonColorWithStars += vec4(addStarField(samplePosition * 7.0, 0.00008));
    }
    
    vFragColor[0] = mix(texColor, horizonColorWithStars, horizonOpacity);
    vFragColor[1] = vec4(0.5, 0.5, 1.0, 1.0);
}"

}