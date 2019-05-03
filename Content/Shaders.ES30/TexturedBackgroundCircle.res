{
    "Fragment": "
#version 300 es 
precision highp float;

uniform sampler2D mainTex;

uniform vec2 ViewSize;

uniform vec4 horizonColor;
uniform vec2 shift;

#define INV_PI 0.31830988618379067153776752675

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 vFragColor[2];

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

    vFragColor[0] = mix(texColor, horizonColor, horizonOpacity);
    vFragColor[1] = vec4(0.5, 0.5, 1.0, 1.0);
}"

}