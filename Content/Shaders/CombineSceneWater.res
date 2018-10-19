{
    "Fragment": "
        uniform vec2 ViewSize;
        uniform vec3 CameraPosition;
        uniform float GameTime;
        
        uniform sampler2D blurHalfTex;
        uniform sampler2D blurQuarterTex;

        uniform sampler2D mainTex;
        uniform sampler2D lightTex;
        uniform sampler2D displacementTex;
        
        uniform float waterLevel;
        uniform float ambientLight;
        uniform vec4 darknessColor;

        float wave(float x, float time) {
            float waveOffset = cos((x - time) * 60.0) * 0.004
                                + cos((x - 2.0 * time) * 20.0) * 0.008
                                + sin((x + 2.0 * time) * 35.0) * 0.01
                                + cos((x + 4.0 * time) * 70.0) * 0.001;
            return waveOffset * 0.4;
        }
        
        float aastep(float threshold, float value) {
            float afwidth = length(vec2(dFdx(value), dFdy(value))) * 0.70710678118654757;
            return smoothstep(threshold - afwidth, threshold + afwidth, value); 
        }
        
        // Perlin noise
        vec4 permute(vec4 x) {
            return mod(34.0 * (x * x) + x, 289.0);
        }

        vec2 fade(vec2 t) {
            return 6.0*(t * t * t * t * t)-15.0*(t * t * t * t)+10.0*(t * t * t);
        }

        float perlinNoise2D(vec2 P) {
            vec4 Pi = floor(P.xyxy) + vec4(0.0, 0.0, 1.0, 1.0);
            vec4 Pf = fract(P.xyxy) - vec4(0.0, 0.0, 1.0, 1.0);
            vec4 ix = Pi.xzxz;
            vec4 iy = Pi.yyww;
            vec4 fx = Pf.xzxz;
            vec4 fy = Pf.yyww;
            vec4 i = permute(permute(ix) + iy);
            vec4 gx = fract(i/41.0)*2.0-1.0;
            vec4 gy = abs(gx)-0.5;
            vec4 tx = floor(gx+0.5);
            gx = gx-tx;
            vec2 g00 = vec2(gx.x, gy.x);
            vec2 g10 = vec2(gx.y, gy.y);
            vec2 g01 = vec2(gx.z, gy.z);
            vec2 g11 = vec2(gx.w, gy.w);
            vec4 norm = 1.79284291400159 - 0.85373472095314 * vec4(dot(g00,g00),dot(g01,g01),dot(g10,g10),dot(g11,g11));
            g00 *= norm.x;
            g01 *= norm.y;
            g10 *= norm.z;
            g11 *= norm.w;
            float n00 = dot(g00, vec2(fx.x,fy.x));
            float n10 = dot(g10, vec2(fx.y,fy.y));
            float n01 = dot(g01, vec2(fx.z,fy.z));
            float n11 = dot(g11, vec2(fx.w,fy.w));
            vec2 fade_xy = fade(Pf.xy);
            vec2 n_x = mix(vec2(n00, n01), vec2(n10, n11), fade_xy.x);
            float n_xy = mix(n_x.x, n_x.y, fade_xy.y);
            return 2.3 * n_xy;
        }

        void main() {
            vec3 waterColor = vec3(0.4, 0.6, 0.8);
            float time = GameTime * 0.065;
            vec2 viewSizeInv = (1.0 / ViewSize);

            vec2 uvWorldCenter = (CameraPosition.xy * viewSizeInv.xy);
            vec2 uvWorld = gl_TexCoord[0].xy + uvWorldCenter;

            float waveHeight = wave(uvWorld.x, time);
            float isTexelBelow = aastep(waveHeight, gl_TexCoord[0].y - waterLevel);
            float isTexelAbove = 1.0 - isTexelBelow;

            vec2 disPos = uvWorld.xy * vec2(0.4);
            disPos.x += mod(time, 2.0);
            disPos.y += mod(time, 2.0);
            vec2 dis = (texture2D(displacementTex, disPos).xy - vec2(0.5)) * vec2(0.014);
            
            vec2 uv = gl_TexCoord[0].xy + dis.xy * vec2(isTexelBelow);

            vec4 main = texture2D(mainTex, uv);
            float aberration = abs(gl_TexCoord[0].x - 0.5) * 0.012;
            float mainR = texture2D(mainTex, vec2(uv.x - aberration, uv.y)).r;
            float mainB = texture2D(mainTex, vec2(uv.x + aberration, uv.y)).b;
            
            float waterColBlendFac = isTexelBelow * 0.5;
            main.rgb = mix(main.rgb, waterColor * (0.4 + 1.2 * vec3(mainR, main.g, mainB)), vec3(waterColBlendFac));
            
            
            float noisePos = uvWorld.x * 8.0 + uvWorldCenter.y * 0.5;
            //noisePos += -0.5;
            //noisePos*=_Size;
            noisePos += (1.0 - gl_TexCoord[0].y - gl_TexCoord[0].x) * -5.0;
            //noisePos *= 1.0 / mix(1.0, 10.0, 1.0 - gl_TexCoord[0].y);
            float rays = perlinNoise2D(vec2(noisePos, time * 10.0 + uvWorldCenter.y)) * 0.5 + 0.4;
            
            //val=_Contrast*(val-0.5)+0.5;
            //rays = rays * i.uv.y;
            //color.a=clamp(color.a,0.0,1.0);
            
            main.rgb += vec3(rays * isTexelBelow * max(1.0 - gl_TexCoord[0].y * 1.4, 0.0) * 0.6);
            

            float topDist = abs(gl_TexCoord[0].y - waterLevel - waveHeight);
            float isNearTop = 1.0 - aastep(viewSizeInv.y * 2.8, topDist);
            float isVeryNearTop = 1.0 - aastep(viewSizeInv.y * (0.8 - 100.0 * waveHeight), topDist);

            float topColorBlendFac = isNearTop * isTexelBelow * 0.6;
            main.rgb = mix(main.rgb, texture2D(mainTex, vec2(gl_TexCoord[0].x,
                (waterLevel - gl_TexCoord[0].y + waterLevel) * 0.97 + waveHeight - viewSizeInv.y * 1.0
            )).rgb, vec3(topColorBlendFac));
            main.rgb += vec3(0.2 * isVeryNearTop);
            
            vec4 blur1 = texture2D(blurHalfTex, uv);
            vec4 blur2 = texture2D(blurQuarterTex, uv);
            vec4 light = texture2D(lightTex, uv);
            
            vec4 blur = (blur1 + blur2) * vec4(0.5);

            float gray = dot(blur.rgb, vec3(0.299, 0.587, 0.114));
            blur = vec4(gray, gray, gray, blur.a);

            gl_FragColor = mix(mix(
                                    main * (1.0 + light.g),
                                    blur,
                                    vec4(clamp((1.0 - light.r) / sqrt(max(ambientLight, 0.35)), 0.0, 1.0))
                                  ), darknessColor, vec4(1.0 - light.r));
        }"
}