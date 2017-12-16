{
    "BlendMode": "Alpha",
    "VertexFormat": "C1P3T4A1",
    
    "Vertex": "
        #version 100
        
        uniform vec4 mainColor;

        uniform mat4 ModelView;
        uniform mat4 Projection;

        attribute vec4 Color;
        attribute vec3 Position;
        attribute vec4 TexCoord;
        attribute float animBlend;

        varying vec4 vTexcoord0;
        varying vec4 vCornerColor;
        varying float animBlendVar;

        void main() {
            gl_Position = Projection * (ModelView * vec4(Position, 1.0));
            vTexcoord0 = TexCoord;
            vCornerColor = Color * mainColor;
            animBlendVar = animBlend;
        }",

    "Fragment": "
        #version 100
        precision mediump float;

        uniform sampler2D mainTex;

        varying vec4 vTexcoord0;
        varying vec4 vCornerColor;
        varying float animBlendVar;

        void main() {
            // Retrieve frames
            vec4 texClrOld = texture2D(mainTex, vTexcoord0.st);
            vec4 texClrNew = texture2D(mainTex, vTexcoord0.pq);

            // This code prevents nasty artifacts when blending between differently masked frames
            float accOldNew = (texClrOld.w - texClrNew.w) / (texClrOld.w + texClrNew.w);
            accOldNew *= mix(min(min(animBlendVar, 1.0 - animBlendVar) * 4.0, 1.0), 1.0, abs(accOldNew));
            texClrNew.xyz = mix(texClrNew.xyz, texClrOld.xyz, max(accOldNew, 0.0));
            texClrOld.xyz = mix(texClrOld.xyz, texClrNew.xyz, max(-accOldNew, 0.0));

            // Blend between frames
            gl_FragColor = vCornerColor * mix(texClrOld, texClrNew, animBlendVar);
        }"
}