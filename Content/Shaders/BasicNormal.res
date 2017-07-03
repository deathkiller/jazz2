{
    "BlendMode": "Alpha",

    "Fragment": "
        #extension GL_ARB_draw_buffers : enable 

        uniform sampler2D mainTex;
        uniform sampler2D normalTex;
        uniform vec2 normalMultiplier;

        void main() {
            gl_FragData[0] = gl_Color * texture2D(mainTex, gl_TexCoord[0].st);

            //gl_FragData[1] = texture2D(normalTex, gl_TexCoord[0].st);
            vec4 normal = texture2D(normalTex, gl_TexCoord[0].st);
            normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
            gl_FragData[1] = normal;
        }"
}