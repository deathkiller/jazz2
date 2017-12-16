{
    "Fragment": "
        #version 100
        precision mediump float;

        uniform sampler2D blurFullTex;
        uniform sampler2D blurHalfTex;
        uniform sampler2D blurQuarterTex;

        uniform sampler2D mainTex;
        uniform sampler2D lightTex;

        uniform float ambientLight;

        varying vec2 vTexcoord0;
        varying vec4 vCornerColor;

        void main() {
            vec4 blur1 = texture2D(blurHalfTex, vTexcoord0);
            vec4 blur2 = texture2D(blurQuarterTex, vTexcoord0);

            vec4 main = texture2D(mainTex, vTexcoord0);
            vec4 light = texture2D(lightTex, vTexcoord0);

            vec4 blur = (blur1 + blur2) * vec4(0.5);
            
            float gray = dot(blur.rgb, vec3(0.299, 0.587, 0.114));
            blur = vec4(gray, gray, gray, blur.a);
            
            gl_FragColor = mix(mix(
                                 main * (1.0 + light.g),
                                 blur,
                                 vec4(clamp((1.0 - light.r) / sqrt(max(ambientLight, 0.35)), 0.0, 1.0))
                               ), vec4(0.0, 0.0, 0.0, 1.0), vec4(1.0 - light.r));
        }"
}