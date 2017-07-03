{
    "BlendMode": "Alpha",

    "Fragment": "
        #version 300 es 
        precision highp float;

        uniform sampler2D mainTex;
        uniform sampler2D normalTex;
        uniform vec2 normalMultiplier;

        in vec2 vTexcoord0;
        in vec4 vCornerColor;

        out vec4 vFragColor[2];

        void main() {
            vec4 dye = vec4(1.0) + (vCornerColor.rgba - vec4(0.5)) * vec4(4.0);

            vec4 original = texture(mainTex, vTexcoord0);
            float average = (original.r + original.g + original.b) * 0.5;
            vec4 gray = vec4(average, average, average, original.a);

            vFragColor[0] = vec4(gray * dye);

            vec4 normal = texture(normalTex, vTexcoord0);
            normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
            vFragColor[1] = normal;
        }"
}