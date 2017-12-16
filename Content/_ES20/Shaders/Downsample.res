{
    "Fragment": "
        #version 100
        precision mediump float;

        uniform sampler2D mainTex;
        uniform vec2 pixelOffset;

        varying vec2 vTexcoord0;
        varying vec4 vCornerColor;

        void main() {
            vec4 sample0 = texture2D(mainTex, vTexcoord0.xy);
            vec4 sample1 = texture2D(mainTex, vTexcoord0.xy + vec2(pixelOffset.x, 0.0));
            vec4 sample2 = texture2D(mainTex, vTexcoord0.xy + vec2(0.0, pixelOffset.y));
            vec4 sample3 = texture2D(mainTex, vTexcoord0.xy + pixelOffset);
            vec4 average = vec4(0.25) * (sample0 + sample1 + sample2 + sample3);

            gl_FragColor = vCornerColor * average;
        }"
}