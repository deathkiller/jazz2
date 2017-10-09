{
    "BlendMode": "Alpha",
    "VertexFormat": "C1P3T4A1",
    
    "Vertex": "
        #version 300 es 

        uniform vec4 mainColor;

        uniform mat4 ModelView;
        uniform mat4 Projection;

        in vec4 Color;
        in vec3 Position;
        in vec4 TexCoord;
        in float animBlend;

        out vec4 vTexcoord0;
        out vec4 vCornerColor;
        out float animBlendVar;

        void main() {
            gl_Position = Projection * (ModelView * vec4(Position, 1.0));
            vTexcoord0 = TexCoord;
            vCornerColor = Color * mainColor;
            animBlendVar = animBlend;
        }",

    "Fragment": "
        #version 300 es 
        precision mediump float;

        uniform sampler2D mainTex;

        in vec4 vTexcoord0;
        in vec4 vCornerColor;
        in float animBlendVar;

        out vec4 vFragColor;

        void main() {
            // Retrieve frames
            vec4 texClrOld = texture(mainTex, vTexcoord0.st);
            vec4 texClrNew = texture(mainTex, vTexcoord0.pq);

            // This code prevents nasty artifacts when blending between differently masked frames
            float accOldNew = (texClrOld.w - texClrNew.w) / (texClrOld.w + texClrNew.w);
            accOldNew *= mix(min(min(animBlendVar, 1.0 - animBlendVar) * 4.0, 1.0), 1.0, abs(accOldNew));
            texClrNew.xyz = mix(texClrNew.xyz, texClrOld.xyz, max(accOldNew, 0.0));
            texClrOld.xyz = mix(texClrOld.xyz, texClrNew.xyz, max(-accOldNew, 0.0));

            // Blend between frames
            vFragColor = vCornerColor * mix(texClrOld, texClrNew, animBlendVar);
        }"
}