{
    "Vertex": "
        uniform vec2 mainTexSize;

        void main() {
            gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

            float dx = 1.0 / mainTexSize.x;
            float dy = 1.0 / mainTexSize.y;

            vec2 texCoord = gl_MultiTexCoord0.xy + vec2(0.0000001, 0.0000001);

            gl_TexCoord[0].xy = texCoord;
            gl_TexCoord[1] = texCoord.xxxy + vec4( -dx, 0, dx, -2.0*dy);  // A1 B1 C1
            gl_TexCoord[2] = texCoord.xxxy + vec4( -dx, 0, dx, -dy);      // A  B  C
            gl_TexCoord[3] = texCoord.xxxy + vec4( -dx, 0, dx, 0);        // D  E  F
            gl_TexCoord[4] = texCoord.xxxy + vec4( -dx, 0, dx, dy);       // G  H  I
            gl_TexCoord[5] = texCoord.xxxy + vec4( -dx, 0, dx, 2.0*dy);   // G5 H5 I5
            gl_TexCoord[6] = texCoord.xyyy + vec4(-2.0*dx, -dy, 0, dy);   // A0 D0 G0
            gl_TexCoord[7] = texCoord.xyyy + vec4( 2.0*dx, -dy, 0, dy);   // C4 F4 I4
        }",

    "Fragment": "
        #version 120

        #define BLEND_NONE 0
        #define BLEND_NORMAL 1
        #define BLEND_DOMINANT 2
        #define LUMINANCE_WEIGHT 1.0
        #define EQUAL_COLOR_TOLERANCE 30.0/255.0
        #define STEEP_DIRECTION_THRESHOLD 2.2
        #define DOMINANT_DIRECTION_THRESHOLD 3.6

        const float one_third = 1.0 / 3.0;
        const float two_third = 2.0 / 3.0;

        uniform sampler2D mainTex;
        uniform vec2 mainTexSize;

        float Reduce(vec3 color) {
            return dot(color, vec3(65536.0, 256.0, 1.0));
        }

        float DistYCbCr(vec3 pixA, vec3 pixB) {
            const vec3 w = vec3(0.2627, 0.6780, 0.0593);
            const float scaleB = 0.5 / (1.0 - w.b);
            const float scaleR = 0.5 / (1.0 - w.r);
            vec3 diff = pixA - pixB;
            float Y = dot(diff, w);
            float Cb = scaleB * (diff.b - Y);
            float Cr = scaleR * (diff.r - Y);

            return sqrt( ((LUMINANCE_WEIGHT * Y) * (LUMINANCE_WEIGHT * Y)) + (Cb * Cb) + (Cr * Cr) );
        }

        bool IsPixEqual(vec3 pixA, vec3 pixB) {
            return (DistYCbCr(pixA, pixB) < EQUAL_COLOR_TOLERANCE);
        }

        bool IsBlendingNeeded(ivec4 blend) {
            return any(notEqual(blend, ivec4(BLEND_NONE)));
        }

        void ScalePixel(ivec4 blend, vec3 k[9], inout vec3 dst[9]) {
            float v0 = Reduce(k[0]);
            float v4 = Reduce(k[4]);
            float v5 = Reduce(k[5]);
            float v7 = Reduce(k[7]);
            float v8 = Reduce(k[8]);

            float dist_01_04 = DistYCbCr(k[1], k[4]);
            float dist_03_08 = DistYCbCr(k[3], k[8]);
            bool haveShallowLine = (STEEP_DIRECTION_THRESHOLD * dist_01_04 <= dist_03_08) && (v0 != v4) && (v5 != v4);
            bool haveSteepLine   = (STEEP_DIRECTION_THRESHOLD * dist_03_08 <= dist_01_04) && (v0 != v8) && (v7 != v8);
            bool needBlend = (blend[2] != BLEND_NONE);
            bool doLineBlend = (  blend[2] >= BLEND_DOMINANT ||
                               !((blend[1] != BLEND_NONE && !IsPixEqual(k[0], k[4])) ||
                                 (blend[3] != BLEND_NONE && !IsPixEqual(k[0], k[8])) ||
                                 (IsPixEqual(k[4], k[3]) &&  IsPixEqual(k[3], k[2]) && IsPixEqual(k[2], k[1]) && IsPixEqual(k[1], k[8]) && !IsPixEqual(k[0], k[2]))));

            vec3 blendPix = (DistYCbCr(k[0], k[1]) <= DistYCbCr(k[0], k[3])) ? k[1] : k[3];
            dst[1] = mix(dst[1], blendPix, (needBlend && doLineBlend) ? ((haveSteepLine) ? 0.750 : ((haveShallowLine) ? 0.250 : 0.125)) : 0.000);
            dst[2] = mix(dst[2], blendPix, (needBlend) ? ((doLineBlend) ? ((!haveShallowLine && !haveSteepLine) ? 0.875 : 1.000) : 0.4545939598) : 0.000);
            dst[3] = mix(dst[3], blendPix, (needBlend && doLineBlend) ? ((haveShallowLine) ? 0.750 : ((haveSteepLine) ? 0.250 : 0.125)) : 0.000);
            dst[4] = mix(dst[4], blendPix, (needBlend && doLineBlend && haveShallowLine) ? 0.250 : 0.000);
            dst[8] = mix(dst[8], blendPix, (needBlend && doLineBlend && haveSteepLine) ? 0.250 : 0.000);
        }

        //---------------------------------------
        // Input Pixel Mapping:  --|21|22|23|--
        //                       19|06|07|08|09
        //                       18|05|00|01|10
        //                       17|04|03|02|11
        //                       --|15|14|13|--
        //
        // Output Pixel Mapping:    06|07|08
        //                          05|00|01
        //                          04|03|02

        void main() {
            vec2 f = fract(gl_TexCoord[0].xy*mainTexSize);
            vec3 src[25];

            src[21] = texture2D(mainTex, gl_TexCoord[1].xw).rgb;
            src[22] = texture2D(mainTex, gl_TexCoord[1].yw).rgb;
            src[23] = texture2D(mainTex, gl_TexCoord[1].zw).rgb;
            src[ 6] = texture2D(mainTex, gl_TexCoord[2].xw).rgb;
            src[ 7] = texture2D(mainTex, gl_TexCoord[2].yw).rgb;
            src[ 8] = texture2D(mainTex, gl_TexCoord[2].zw).rgb;
            src[ 5] = texture2D(mainTex, gl_TexCoord[3].xw).rgb;
            src[ 0] = texture2D(mainTex, gl_TexCoord[3].yw).rgb;
            src[ 1] = texture2D(mainTex, gl_TexCoord[3].zw).rgb;
            src[ 4] = texture2D(mainTex, gl_TexCoord[4].xw).rgb;
            src[ 3] = texture2D(mainTex, gl_TexCoord[4].yw).rgb;
            src[ 2] = texture2D(mainTex, gl_TexCoord[4].zw).rgb;
            src[15] = texture2D(mainTex, gl_TexCoord[5].xw).rgb;
            src[14] = texture2D(mainTex, gl_TexCoord[5].yw).rgb;
            src[13] = texture2D(mainTex, gl_TexCoord[5].zw).rgb;
            src[19] = texture2D(mainTex, gl_TexCoord[6].xy).rgb;
            src[18] = texture2D(mainTex, gl_TexCoord[6].xz).rgb;
            src[17] = texture2D(mainTex, gl_TexCoord[6].xw).rgb;
            src[ 9] = texture2D(mainTex, gl_TexCoord[7].xy).rgb;
            src[10] = texture2D(mainTex, gl_TexCoord[7].xz).rgb;
            src[11] = texture2D(mainTex, gl_TexCoord[7].xw).rgb;

            float v[9];
            v[0] = Reduce(src[0]);
            v[1] = Reduce(src[1]);
            v[2] = Reduce(src[2]);
            v[3] = Reduce(src[3]);
            v[4] = Reduce(src[4]);
            v[5] = Reduce(src[5]);
            v[6] = Reduce(src[6]);
            v[7] = Reduce(src[7]);
            v[8] = Reduce(src[8]);

            ivec4 blendResult = ivec4(BLEND_NONE);

            // Preprocess corners
            // Pixel Tap Mapping: --|--|--|--|--
            //                    --|--|07|08|--
            //                    --|05|00|01|10
            //                    --|04|03|02|11
            //                    --|--|14|13|--

            // Corner (1, 1)
            if (!((v[0] == v[1] && v[3] == v[2]) || (v[0] == v[3] && v[1] == v[2]))) {
                float dist_03_01 = DistYCbCr(src[ 4], src[ 0]) + DistYCbCr(src[ 0], src[ 8]) + DistYCbCr(src[14], src[ 2]) + DistYCbCr(src[ 2], src[10]) + (4.0 * DistYCbCr(src[ 3], src[ 1]));
                float dist_00_02 = DistYCbCr(src[ 5], src[ 3]) + DistYCbCr(src[ 3], src[13]) + DistYCbCr(src[ 7], src[ 1]) + DistYCbCr(src[ 1], src[11]) + (4.0 * DistYCbCr(src[ 0], src[ 2]));
                bool dominantGradient = (DOMINANT_DIRECTION_THRESHOLD * dist_03_01) < dist_00_02;
                blendResult[2] = ((dist_03_01 < dist_00_02) && (v[0] != v[1]) && (v[0] != v[3])) ? ((dominantGradient) ? BLEND_DOMINANT : BLEND_NORMAL) : BLEND_NONE;
            }

            // Pixel Tap Mapping: --|--|--|--|--
            //                    --|06|07|--|--
            //                    18|05|00|01|--
            //                    17|04|03|02|--
            //                    --|15|14|--|--
            // Corner (0, 1)
            if (!((v[5] == v[0] && v[4] == v[3]) || (v[5] == v[4] && v[0] == v[3]))) {
                float dist_04_00 = DistYCbCr(src[17]  , src[ 5]) + DistYCbCr(src[ 5], src[ 7]) + DistYCbCr(src[15], src[ 3]) + DistYCbCr(src[ 3], src[ 1]) + (4.0 * DistYCbCr(src[ 4], src[ 0]));
                float dist_05_03 = DistYCbCr(src[18]  , src[ 4]) + DistYCbCr(src[ 4], src[14]) + DistYCbCr(src[ 6], src[ 0]) + DistYCbCr(src[ 0], src[ 2]) + (4.0 * DistYCbCr(src[ 5], src[ 3]));
                bool dominantGradient = (DOMINANT_DIRECTION_THRESHOLD * dist_05_03) < dist_04_00;
                blendResult[3] = ((dist_04_00 > dist_05_03) && (v[0] != v[5]) && (v[0] != v[3])) ? ((dominantGradient) ? BLEND_DOMINANT : BLEND_NORMAL) : BLEND_NONE;
            }

            // Pixel Tap Mapping: --|--|22|23|--
            //                    --|06|07|08|09
            //                    --|05|00|01|10
            //                    --|--|03|02|--
            //                    --|--|--|--|--
            // Corner (1, 0)
            if (!((v[7] == v[8] && v[0] == v[1]) || (v[7] == v[0] && v[8] == v[1]))) {
                float dist_00_08 = DistYCbCr(src[ 5], src[ 7]) + DistYCbCr(src[ 7], src[23]) + DistYCbCr(src[ 3], src[ 1]) + DistYCbCr(src[ 1], src[ 9]) + (4.0 * DistYCbCr(src[ 0], src[ 8]));
                float dist_07_01 = DistYCbCr(src[ 6], src[ 0]) + DistYCbCr(src[ 0], src[ 2]) + DistYCbCr(src[22], src[ 8]) + DistYCbCr(src[ 8], src[10]) + (4.0 * DistYCbCr(src[ 7], src[ 1]));
                bool dominantGradient = (DOMINANT_DIRECTION_THRESHOLD * dist_07_01) < dist_00_08;
                blendResult[1] = ((dist_00_08 > dist_07_01) && (v[0] != v[7]) && (v[0] != v[1])) ? ((dominantGradient) ? BLEND_DOMINANT : BLEND_NORMAL) : BLEND_NONE;
            }

            // Pixel Tap Mapping: --|21|22|--|--
            //                    19|06|07|08|--
            //                    18|05|00|01|--
            //                    --|04|03|--|--
            //                    --|--|--|--|--
            // Corner (0, 0)
            if (!((v[6] == v[7] && v[5] == v[0]) || (v[6] == v[5] && v[7] == v[0]))) {
                float dist_05_07 = DistYCbCr(src[18], src[ 6]) + DistYCbCr(src[ 6], src[22]) + DistYCbCr(src[ 4], src[ 0]) + DistYCbCr(src[ 0], src[ 8]) + (4.0 * DistYCbCr(src[ 5], src[ 7]));
                float dist_06_00 = DistYCbCr(src[19], src[ 5]) + DistYCbCr(src[ 5], src[ 3]) + DistYCbCr(src[21], src[ 7]) + DistYCbCr(src[ 7], src[ 1]) + (4.0 * DistYCbCr(src[ 6], src[ 0]));
                bool dominantGradient = (DOMINANT_DIRECTION_THRESHOLD * dist_05_07) < dist_06_00;
                blendResult[0] = ((dist_05_07 < dist_06_00) && (v[0] != v[5]) && (v[0] != v[7])) ? ((dominantGradient) ? BLEND_DOMINANT : BLEND_NORMAL) : BLEND_NONE;
            }

            vec3 dst[9];
            dst[0] = src[0];
            dst[1] = src[0];
            dst[2] = src[0];
            dst[3] = src[0];
            dst[4] = src[0];
            dst[5] = src[0];
            dst[6] = src[0];
            dst[7] = src[0];
            dst[8] = src[0];

            // Scale pixel
            if (IsBlendingNeeded(blendResult)) {
                vec3 k[9];
                vec3 tempDst8;
                vec3 tempDst7;

                k[8] = src[8];
                k[7] = src[7];
                k[6] = src[6];
                k[5] = src[5];
                k[4] = src[4];
                k[3] = src[3];
                k[2] = src[2];
                k[1] = src[1];
                k[0] = src[0];
                ScalePixel(blendResult.xyzw, k, dst);

                k[8] = src[6];
                k[7] = src[5];
                k[6] = src[4];
                k[5] = src[3];
                k[4] = src[2];
                k[3] = src[1];
                k[2] = src[8];
                k[1] = src[7];
                tempDst8 = dst[8];
                tempDst7 = dst[7];
                dst[8] = dst[6];
                dst[7] = dst[5];
                dst[6] = dst[4];
                dst[5] = dst[3];
                dst[4] = dst[2];
                dst[3] = dst[1];
                dst[2] = tempDst8;
                dst[1] = tempDst7;
                ScalePixel(blendResult.wxyz, k, dst);

                k[8] = src[4];
                k[7] = src[3];
                k[6] = src[2];
                k[5] = src[1];
                k[4] = src[8];
                k[3] = src[7];
                k[2] = src[6];
                k[1] = src[5];
                tempDst8 = dst[8];
                tempDst7 = dst[7];
                dst[8] = dst[6];
                dst[7] = dst[5];
                dst[6] = dst[4];
                dst[5] = dst[3];
                dst[4] = dst[2];
                dst[3] = dst[1];
                dst[2] = tempDst8;
                dst[1] = tempDst7;
                ScalePixel(blendResult.zwxy, k, dst);

                k[8] = src[2];
                k[7] = src[1];
                k[6] = src[8];
                k[5] = src[7];
                k[4] = src[6];
                k[3] = src[5];
                k[2] = src[4];
                k[1] = src[3];
                tempDst8 = dst[8];
                tempDst7 = dst[7];
                dst[8] = dst[6];
                dst[7] = dst[5];
                dst[6] = dst[4];
                dst[5] = dst[3];
                dst[4] = dst[2];
                dst[3] = dst[1];
                dst[2] = tempDst8;
                dst[1] = tempDst7;
                ScalePixel(blendResult.yzwx, k, dst);

                // Rotate the destination pixels back to 0 degrees.
                tempDst8 = dst[8];
                tempDst7 = dst[7];
                dst[8] = dst[6];
                dst[7] = dst[5];
                dst[6] = dst[4];
                dst[5] = dst[3];
                dst[4] = dst[2];
                dst[3] = dst[1];
                dst[2] = tempDst8;
                dst[1] = tempDst7;
            }

            vec3 res = mix(mix(dst[6], mix(dst[7], dst[8], step(two_third, f.x)), step(one_third, f.x)),
                                   mix(mix(dst[5], mix(dst[0], dst[1], step(two_third, f.x)), step(one_third, f.x)),
                                       mix(dst[4], mix(dst[3], dst[2], step(two_third, f.x)), step(one_third, f.x)), step(two_third, f.y)),
                                                                                                                     step(one_third, f.y));
            gl_FragColor = vec4(res, 1.0);
        }"
}