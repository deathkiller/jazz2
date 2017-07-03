{
    "Fragment": "
        #extension GL_ARB_draw_buffers : enable 

        uniform sampler2D mainTex;

        uniform vec2 ViewSize;

        uniform vec4 horizonColor;
        uniform vec2 shift;

        #define INV_PI 0.31830988618379067153776752675

        void main() {
            // Position of pixel on screen (between -1 and 1)
            vec2 targetCoord = vec2(2.0) * gl_TexCoord[0].xy - vec2(1.0);

            // Aspect ratio correction, so display circle instead of ellipse
            targetCoord.x *= ViewSize.x / ViewSize.y;

            // Distance to center of screen
            float distance = length(targetCoord);

            // x-coordinate of tunnel
            float xShift = (targetCoord.x == 0 ? sign(targetCoord.y) * 0.5 : atan(targetCoord.y, targetCoord.x) * INV_PI);

            vec2 texturePos = vec2(
                (xShift)         * 1.0 + (shift.x * 0.01),
                (1.0 / distance) * 1.4 + (shift.y * 0.002)
            );

            vec4 texColor = texture2D(mainTex, texturePos);
            float horizonOpacity = 1.0 - clamp(pow(distance, 1.4) - 0.3, 0.0, 1.0);

            gl_FragData[0] = mix(texColor, horizonColor, horizonOpacity);
            gl_FragData[1] = vec4(0.5, 0.5, 1.0, 1.0);
        }"
}