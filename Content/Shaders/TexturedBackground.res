{
    "Fragment": "
#extension GL_ARB_draw_buffers : enable 

uniform sampler2D mainTex;

uniform vec2 ViewSize;
uniform vec3 CameraPosition;

uniform vec4 horizonColor;
uniform vec2 shift;
uniform float parallaxStarsEnabled;

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
    // Position of pixel to write (between 0 and 1 both directions)
    //vec2 targetCoord = gl_TexCoord[0].xy / ViewSize * vec2(256.0 * 2.5, 256.0 * 1.6);
    vec2 targetCoord = gl_FragCoord.xy / ViewSize;

    // Distance to center of screen from top or bottom (1: center of screen, 0: edge of screen)
    float distance = 1.3 - abs(2.0 * targetCoord.y - 1.0);
    float horizonDepth = pow(distance, 2.0);

    float yShift = (targetCoord.y > 0.5 ? 1.0 : 0.0);

    vec2 texturePos = vec2(
        (shift.x / 256.0) + (targetCoord.x - 0.5   ) * (0.5 + (1.5 * horizonDepth)),
        (shift.y / 256.0) + (targetCoord.y - yShift) * 2.0 * distance
    );

    vec4 texColor = texture2D(mainTex, texturePos);
    float horizonOpacity = clamp(pow(distance, 1.8) - 0.4, 0.0, 1.0);
    
    vec4 horizonColorWithStars = horizonColor;
    if (parallaxStarsEnabled > 0.0) {
        vec2 samplePosition = (gl_FragCoord.xy / ViewSize.xx) + CameraPosition.xy * 0.00012;
        horizonColorWithStars += vec4(addStarField(samplePosition * 7.0, 0.00008));
        
        samplePosition = (gl_FragCoord.xy / ViewSize.xx) + CameraPosition.xy * 0.00018 + 0.5;
        horizonColorWithStars += vec4(addStarField(samplePosition * 7.0, 0.00008));
    }

    gl_FragData[0] = mix(texColor, horizonColorWithStars, horizonOpacity);
    gl_FragData[1] = vec4(0.5, 0.5, 1.0, 1.0);
}"

}