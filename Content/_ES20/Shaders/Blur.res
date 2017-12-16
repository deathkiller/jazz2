{
    "Fragment": "
        #version 100
        precision mediump float;

        uniform sampler2D mainTex;
        uniform vec2 blurDirection;
        uniform vec2 pixelOffset;

        varying vec2 vTexcoord0;
        varying vec4 vCornerColor;

        void main() {
            vec4 color = vec4(0.0);
            vec2 off = vec2(1.3333333333333333) * pixelOffset * blurDirection;
            color += texture2D(mainTex, vTexcoord0) * 0.29411764705882354;
            color += texture2D(mainTex, vTexcoord0 + off) * 0.35294117647058826;
            color += texture2D(mainTex, vTexcoord0 - off) * 0.35294117647058826;
            gl_FragColor = color; 
        }"
}