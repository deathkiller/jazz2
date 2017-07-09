{
    "Fragment": "
        #version 300 es 
        precision mediump float;

        uniform sampler2D blurFullTex;
        uniform sampler2D blurHalfTex;
        uniform sampler2D blurQuarterTex;

        uniform sampler2D mainTex;
        uniform sampler2D lightTex;

        uniform float ambientLight;

        in vec2 vTexcoord0;
        in vec4 vCornerColor;

        out vec4 vFragColor;

        void main() {
            vec4 blur1 = texture(blurHalfTex, vTexcoord0);
            vec4 blur2 = texture(blurQuarterTex, vTexcoord0);

            vec4 main = texture(mainTex, vTexcoord0);
            vec4 light = texture(lightTex, vTexcoord0);

            vec4 blur = (blur1 + blur2) * vec4(0.5);
            
            float gray = dot(blur.rgb, vec3(0.299, 0.587, 0.114));
            blur = vec4(gray, gray, gray, blur.a);
            
            vFragColor = mix(mix(
                                 main * (1.0 + light.g),
                                 blur,
                                 vec4(clamp((1.0 - light.r) / sqrt(max(ambientLight, 0.35)), 0.0, 1.0))
                               ), vec4(0.0, 0.0, 0.0, 1.0), vec4(1.0 - light.r));
        }"
}