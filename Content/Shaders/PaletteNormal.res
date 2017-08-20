{
    "BlendMode": "Alpha",

    "Fragment": "
        #extension GL_ARB_draw_buffers : enable 

        uniform sampler2D mainTex;
        uniform sampler2D paletteTex;
        uniform sampler2D normalTex;
        uniform vec2 normalMultiplier;

        void main() {
            vec4 index = texture2D(mainTex, gl_TexCoord[0].st);
            vec4 color = texture2D(paletteTex, vec2(index.r + gl_Color.g /*PaletteShift*/, gl_Color.r /*PaletteIndex*/));
            gl_FragData[0] = vec4(color.rgb * gl_Color.b /*Intensity*/, color.a * index.a * gl_Color.a);

            vec4 normal = texture2D(normalTex, gl_TexCoord[0].st);
            normal.xy = (normal.xy - vec2(0.5)) * normalMultiplier + vec2(0.5);
            gl_FragData[1] = normal;
        }"
}