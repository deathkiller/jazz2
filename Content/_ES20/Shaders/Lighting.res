{
    "BlendMode": "Add",

    "Vertex": "#inherit MinimalC1P3T4",
    "Fragment": "
        #version 100
        precision mediump float;

        uniform vec2 ViewSize;

        varying vec4 vTexcoord0;
        varying vec4 vCornerColor;

        void main() {
            vec2 center = vTexcoord0.xy;
            float radiusNear = vTexcoord0.z;
            float radiusFar = vTexcoord0.w;
            float intensity = vCornerColor.r;
            float brightness = vCornerColor.g;

            float dist = distance(vec2(gl_FragCoord), center);
            if (dist > radiusFar) {
                gl_FragColor = vec4(0, 0, 0, 0);
                return;
            }

            float strength = clamp(1.0 - ((dist - radiusNear) / (radiusFar - radiusNear)), 0.0, 1.0);
            gl_FragColor = vec4(strength * intensity, strength * brightness, 0.0, 1.0);
        }"
}