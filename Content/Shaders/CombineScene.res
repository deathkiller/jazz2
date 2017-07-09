{
    "Fragment": "
        //uniform sampler2D blurFullTex;
        uniform sampler2D blurHalfTex;
        uniform sampler2D blurQuarterTex;

        uniform sampler2D mainTex;
        uniform sampler2D lightTex;

        uniform float ambientLight;

        void main() {
            //vec4 blur0 = texture2D(blurFullTex, gl_TexCoord[0].st);
            vec4 blur1 = texture2D(blurHalfTex, gl_TexCoord[0].xy);
            vec4 blur2 = texture2D(blurQuarterTex, gl_TexCoord[0].xy);

            vec4 main = texture2D(mainTex, gl_TexCoord[0].xy);
            vec4 light = texture2D(lightTex, gl_TexCoord[0].xy);

            vec4 blur = (blur1 + blur2) * vec4(0.5);

            float gray = dot(blur.rgb, vec3(0.299, 0.587, 0.114));
            blur = vec4(gray, gray, gray, blur.a);

            gl_FragColor = mix(mix(
                                    main * (1.0 + /*floor(*/light.g/* * 10) / 10*/),
                                    blur,
                                    vec4(clamp((1.0 - light.r) / sqrt(max(ambientLight, 0.35)), 0.0, 1.0))
                                  ), vec4(0.0, 0.0, 0.0, 1.0), vec4(1.0 - light.r));
        }"
}