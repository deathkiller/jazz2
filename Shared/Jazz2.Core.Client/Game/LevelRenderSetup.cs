using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Components;

namespace Jazz2.Game
{
    public class LevelRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

#if PLATFORM_ANDROID
        // Lower resolution for Android, but it's enough for smaller screens
        private const int defaultWidth = 544, defaultHeight = 306;
#else
        private const int defaultWidth = 720, defaultHeight = 405;
#endif
        private Vector2 lastImageSize;

        private const int PyramidSize = 2;

        private readonly LevelHandler levelHandler;
        private readonly ContentRef<Material> lightingMaterial, lightingNoiseMaterial;
        private readonly ContentRef<DrawTechnique> combineSceneShader, combineSceneWaterShader;

#if !PLATFORM_ANDROID && !PLATFORM_WASM
        private readonly ContentRef<DrawTechnique> downsampleShader;
        private readonly ContentRef<DrawTechnique> blurShader;
#endif
        private ContentRef<DrawTechnique> resizeShader;

        private Texture lightingTexture, mainTexture, normalTexture, finalTexture;
        private RenderTarget lightingTarget, mainTarget, finalTarget;

#if !PLATFORM_ANDROID && !PLATFORM_WASM
        private readonly RenderTarget[] targetPingPongA = new RenderTarget[PyramidSize];
        private readonly RenderTarget[] targetPingPongB = new RenderTarget[PyramidSize];
#endif

        private readonly VertexC1P3T4A1[] lightBuffer = new VertexC1P3T4A1[4];

        private readonly ContentRef<Texture> noiseTexture;

        private SettingsCache.ResizeMode lastResizeMode;

        public LevelRenderSetup(LevelHandler levelHandler)
        {
            this.levelHandler = levelHandler;

            // Shaders
            ContentRef<DrawTechnique> lightingShader = ContentResolver.Current.RequestShader("Lighting");
            ContentRef<DrawTechnique> lightingNoiseShader = ContentResolver.Current.RequestShader("LightingNoise");

#if !PLATFORM_ANDROID && !PLATFORM_WASM
            downsampleShader = ContentResolver.Current.RequestShader("Downsample");
            blurShader = ContentResolver.Current.RequestShader("Blur");
#endif
            combineSceneShader = ContentResolver.Current.RequestShader("CombineScene");
            combineSceneWaterShader = ContentResolver.Current.RequestShader("CombineSceneWater");

            lastResizeMode = SettingsCache.Resize;

            try {
                switch (lastResizeMode) {
                    default:
                    case SettingsCache.ResizeMode.None:
                        resizeShader = DrawTechnique.Solid;
                        break;
                    case SettingsCache.ResizeMode.HQ2x:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                        break;
                    case SettingsCache.ResizeMode.xBRZ3:
                        resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                        break;
                    case SettingsCache.ResizeMode.xBRZ4:
                        resizeShader = ContentResolver.Current.RequestShader("Resize4xBRZ");
                        break;
                    case SettingsCache.ResizeMode.CRT:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeCRT");
                        break;
                    case SettingsCache.ResizeMode.GB:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeGB");
                        break;
                }
            } catch {
                resizeShader = DrawTechnique.Solid;
            }

            // Main texture
            mainTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            normalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest, format: TexturePixelFormat.Rgb);
            mainTarget = new RenderTarget(AAQuality.Off, /*true*/false, mainTexture, normalTexture);

            // Lighting texture
            lightingTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest, format: TexturePixelFormat.Dual);
            lightingTarget = new RenderTarget(AAQuality.Off, false, lightingTexture);

            finalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            finalTarget = new RenderTarget(AAQuality.Off, false, finalTexture);

            // Noise texture
            noiseTexture = ContentResolver.Current.RequestGraphicResource("_custom/noise.png").Texture;

            // Materials
            lightingMaterial = new Material(lightingShader, ColorRgba.White);
            lightingMaterial.Res.SetTexture("normalBuffer", normalTexture);

            lightingNoiseMaterial = new Material(lightingNoiseShader, ColorRgba.White);
            lightingNoiseMaterial.Res.SetTexture("normalBuffer", normalTexture);

            // Render steps
            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                DefaultProjection = true,
                VisibilityMask = VisibilityFlag.AllGroups,
                ClearFlags = ClearFlag.All,
                DefaultClearColor = true,

                Output = mainTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "CombineScene",

                Projection = ProjectionMode.Screen,
                VisibilityMask = VisibilityFlag.None,
                ClearFlags = ClearFlag.None,

                Input = new BatchInfo(DrawTechnique.Solid, mainTexture),
                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Projection = ProjectionMode.Screen,
                VisibilityMask = VisibilityFlag.All,
                ClearFlags = ClearFlag.None,

                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Resize",

                Projection = ProjectionMode.Screen,
                VisibilityMask = VisibilityFlag.None,
                ClearFlags = ClearFlag.None
            });
        }

        protected override void OnDisposing(bool manually)
        {
            base.OnDisposing(manually);

            Disposable.Free(ref lightingTarget);
            Disposable.Free(ref lightingTexture);

            Disposable.Free(ref mainTarget);
            Disposable.Free(ref mainTexture);
            Disposable.Free(ref normalTexture);

            Disposable.Free(ref finalTarget);
            Disposable.Free(ref finalTexture);

#if !PLATFORM_ANDROID && !PLATFORM_WASM
            Disposable.FreeContents(targetPingPongA);
            Disposable.FreeContents(targetPingPongB);
#endif
        }

        protected override void OnRenderPointOfView(Scene scene, DrawDevice drawDevice, Rect viewportRect, Vector2 imageSize)
        {
            // Switch between resize modes if necessary
            if (lastResizeMode != SettingsCache.Resize) {
                lastResizeMode = SettingsCache.Resize;

                try {
                    switch (lastResizeMode) {
                        default:
                        case SettingsCache.ResizeMode.None:
                            resizeShader = DrawTechnique.Solid;
                            break;
                        case SettingsCache.ResizeMode.HQ2x:
                            resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                            break;
                        case SettingsCache.ResizeMode.xBRZ3:
                            resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                            break;
                        case SettingsCache.ResizeMode.xBRZ4:
                            resizeShader = ContentResolver.Current.RequestShader("Resize4xBRZ");
                            break;
                        case SettingsCache.ResizeMode.CRT:
                            resizeShader = ContentResolver.Current.RequestShader("ResizeCRT");
                            break;
                        case SettingsCache.ResizeMode.GB:
                            resizeShader = ContentResolver.Current.RequestShader("ResizeGB");
                            break;
                    }
                } catch {
                    resizeShader = DrawTechnique.Solid;
                }
            }

            // Check if resolution changed
            if (lastImageSize != imageSize) {
                lastImageSize = imageSize;

                const float defaultRatio = (float)defaultWidth / defaultHeight;
                float currentRatio = imageSize.X / imageSize.Y;

                int width, height;
                if (currentRatio > defaultRatio) {
                    width = MathF.Min(defaultWidth, (int)imageSize.X);
                    height = (int)(width / currentRatio);
                } else if (currentRatio < defaultRatio) {
                    height = MathF.Min(defaultHeight, (int)imageSize.Y);
                    width = (int)(height * currentRatio);
                } else {
                    width = MathF.Min(defaultWidth, (int)imageSize.X);
                    height = MathF.Min(defaultHeight, (int)imageSize.Y);
                }

                TargetSize = new Point2(width, height);

                ResizeRenderTarget(mainTarget, TargetSize);
                ResizeRenderTarget(lightingTarget, TargetSize);
                ResizeRenderTarget(finalTarget, TargetSize);
            }

            base.OnRenderPointOfView(scene, drawDevice, viewportRect, imageSize);
        }

        protected override void OnRenderSingleStep(RenderStep step, Scene scene, DrawDevice device)
        {
            if (step.Id == "Resize") {
                ProcessResizeStep(device);
            } else if(step.Id == "CombineScene") {
                ProcessCombineSceneStep(device);
            } else {
                base.OnRenderSingleStep(step, scene, device);
            }
        }

        private void ProcessResizeStep(DrawDevice device)
        {
            BatchInfo tempMaterial = device.RentMaterial();
            tempMaterial.Technique = resizeShader;
            tempMaterial.MainTexture = finalTexture;
            tempMaterial.SetValue("mainTexSize", new Vector2(finalTexture.ContentWidth, finalTexture.ContentHeight));
            this.Blit(device, tempMaterial, device.ViewportRect);
        }

        private void ProcessCombineSceneStep(DrawDevice device)
        {
            // ToDo: Split lighting to RGB channels
            // ToDo: Implement dynamic lighting/shadows (https://github.com/mattdesl/lwjgl-basics/wiki/2D-Pixel-Perfect-Shadows)

            Vector2 viewSize = device.TargetSize;
            Vector2 viewOffset = new Vector2(
                device.ViewerPos.X - viewSize.X / 2,
                device.ViewerPos.Y - viewSize.Y / 2
            );

            float ambientLight = levelHandler.AmbientLightCurrent;
            float viewWaterLevel = (levelHandler.WaterLevel - viewOffset.Y);

            // One temporary material is used for all operations
            BatchInfo tempMaterial = device.RentMaterial();

            // Blit ambient light color
            {
                tempMaterial.Technique = DrawTechnique.Solid;
                tempMaterial.MainColor = new ColorRgba(ambientLight, 0, 0);
                this.Blit(device, tempMaterial, lightingTarget);
            }

            // Render lights (target was set in Blit() in previous step)
            device.PrepareForDrawcalls();

            foreach (GameObject actor in levelHandler.ActiveObjects) {
                LightEmitter light = actor.GetComponent<LightEmitter>();
                if (light != null) {
                    // World-space to screen-space position transformation
                    Vector3 pos = actor.Transform.Pos;
                    pos.X -= viewOffset.X;
                    pos.Y -= viewOffset.Y;

                    float left = pos.X - light.RadiusFar;
                    float top = pos.Y - light.RadiusFar;
                    float right = pos.X + light.RadiusFar;
                    float bottom = pos.Y + light.RadiusFar;

                    if (left   < viewSize.X &&
                        top    < viewSize.Y &&
                        right  > 0 &&
                        bottom > 0) {

                        lightBuffer[0].Pos.X = left;
                        lightBuffer[0].Pos.Y = top;

                        lightBuffer[1].Pos.X = left;
                        lightBuffer[1].Pos.Y = bottom;

                        lightBuffer[2].Pos.X = right;
                        lightBuffer[2].Pos.Y = bottom;

                        lightBuffer[3].Pos.X = right;
                        lightBuffer[3].Pos.Y = top;

                        // Use TexCoord X & Y for screen-space Light position
                        lightBuffer[0].TexCoord.X = lightBuffer[1].TexCoord.X = lightBuffer[2].TexCoord.X = lightBuffer[3].TexCoord.X = pos.X;
                        lightBuffer[0].TexCoord.Y = lightBuffer[1].TexCoord.Y = lightBuffer[2].TexCoord.Y = lightBuffer[3].TexCoord.Y = pos.Y;
                        // Use TexCoord Z & W for Light radius
                        lightBuffer[0].TexCoord.Z = lightBuffer[1].TexCoord.Z = lightBuffer[2].TexCoord.Z = lightBuffer[3].TexCoord.Z = light.RadiusNear;
                        lightBuffer[0].TexCoord.W = lightBuffer[1].TexCoord.W = lightBuffer[2].TexCoord.W = lightBuffer[3].TexCoord.W = light.RadiusFar;

                        // Use Red channel for Light intensity
                        lightBuffer[0].Color.R = lightBuffer[1].Color.R = lightBuffer[2].Color.R = lightBuffer[3].Color.R = (byte)(light.Intensity * 255);
                        // Use Green channel for Light brightness
                        lightBuffer[0].Color.G = lightBuffer[1].Color.G = lightBuffer[2].Color.G = lightBuffer[3].Color.G = (byte)(light.Brightness * 255);

                        switch (light.Type) {
                            default:
                            case LightType.Solid:
                                device.AddVertices(lightingMaterial, VertexMode.Quads, lightBuffer);
                                break;

                            case LightType.WithNoise:
                                device.AddVertices(lightingNoiseMaterial, VertexMode.Quads, lightBuffer);
                                break;
                        }
                    }
                }
            }

            device.Render();

#if !PLATFORM_ANDROID && !PLATFORM_WASM
            // Combine lighting with blurred scene (disabled on Android and WebAssembly for better performance)
            SetupTargets((Point2)device.TargetSize);

            // Downsample to half size
            tempMaterial.Technique = downsampleShader;
            tempMaterial.MainTexture = mainTexture;
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / mainTexture.ContentWidth, 1f / mainTexture.ContentHeight));
            this.Blit(device, tempMaterial, targetPingPongA[0]);

            // Downsample to quarter size
            tempMaterial.MainTexture = targetPingPongA[0].Targets[0];
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));
            this.Blit(device, tempMaterial, targetPingPongA[1]);

            // Blur all targets, separating horizontal and vertical blur
            for (int i = 0; i < targetPingPongA.Length; i++) {
                tempMaterial.Technique = blurShader;
                tempMaterial.MainTexture = targetPingPongA[i].Targets[0];
                tempMaterial.SetValue("blurDirection", new Vector2(1f, 0f));
                tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));

                this.Blit(device, tempMaterial, targetPingPongB[i]);

                tempMaterial.MainTexture = targetPingPongB[i].Targets[0];
                tempMaterial.SetValue("blurDirection", new Vector2(0f, 1f));
                tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));

                this.Blit(device, tempMaterial, targetPingPongA[i]);
            }
#endif

            // Blit it into screen
            if (viewWaterLevel < viewSize.Y) {
                // Render lighting with water
                tempMaterial.Technique = combineSceneWaterShader;
                tempMaterial.SetTexture("mainTex", mainTexture);
                tempMaterial.SetTexture("lightTex", lightingTexture);
                tempMaterial.SetTexture("displacementTex", noiseTexture); // Underwater displacement
#if !PLATFORM_ANDROID && !PLATFORM_WASM
                tempMaterial.SetTexture("blurHalfTex", targetPingPongA[0].Targets[0]);
                tempMaterial.SetTexture("blurQuarterTex", targetPingPongA[1].Targets[0]);

                tempMaterial.SetValue("ambientLight", ambientLight);
#endif
                tempMaterial.SetValue("darknessColor", levelHandler.DarknessColor);

                tempMaterial.SetValue("waterLevel", viewWaterLevel / viewSize.Y);

                this.Blit(device, tempMaterial, finalTarget);
            } else {
                // Render lighting without water
                tempMaterial.Technique = combineSceneShader;
                tempMaterial.SetTexture("mainTex", mainTexture);
                tempMaterial.SetTexture("lightTex", lightingTexture);
#if !PLATFORM_ANDROID && !PLATFORM_WASM
                tempMaterial.SetTexture("blurHalfTex", targetPingPongA[0].Targets[0]);
                tempMaterial.SetTexture("blurQuarterTex", targetPingPongA[1].Targets[0]);

                tempMaterial.SetValue("ambientLight", ambientLight);
#endif
                tempMaterial.SetValue("darknessColor", levelHandler.DarknessColor);

                this.Blit(device, tempMaterial, finalTarget);
            }
        }

        public Texture RequestBlurredInGame()
        {
#if PLATFORM_ANDROID || PLATFORM_WASM
            // Blur is disabled in Android and WebAssembly version
            return finalTexture;
#else
            DrawDevice device = new DrawDevice();
            device.Projection = ProjectionMode.Screen;

            // One temporary material is used for all operations
            BatchInfo tempMaterial = device.RentMaterial();

            // Downsample to half size
            tempMaterial.Technique = downsampleShader;
            tempMaterial.MainTexture = finalTexture;
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / finalTexture.ContentWidth, 1f / finalTexture.ContentHeight));
            this.Blit(device, tempMaterial, targetPingPongA[0]);

            // Downsample to quarter size
            tempMaterial.MainTexture = targetPingPongA[0].Targets[0];
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));
            this.Blit(device, tempMaterial, targetPingPongA[1]);

            // Blur last target, separating horizontal and vertical blur
            tempMaterial.Technique = blurShader;
            tempMaterial.MainTexture = targetPingPongA[1].Targets[0];
            tempMaterial.SetValue("blurDirection", new Vector2(1f, 0f));
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));

            this.Blit(device, tempMaterial, targetPingPongB[1]);

            tempMaterial.MainTexture = targetPingPongB[1].Targets[0];
            tempMaterial.SetValue("blurDirection", new Vector2(0f, 1f));
            tempMaterial.SetValue("pixelOffset", new Vector2(1f / tempMaterial.MainTexture.Res.ContentWidth, 1f / tempMaterial.MainTexture.Res.ContentHeight));

            this.Blit(device, tempMaterial, targetPingPongA[1]);

            return targetPingPongA[1].Targets[0].Res;
#endif
        }

#if !PLATFORM_ANDROID && !PLATFORM_WASM
        private void SetupTargets(Point2 size)
        {
            for (int i = 0; i < targetPingPongA.Length; i++) {
                // Downsampling starts at half size of original render target
                size /= 2;
                SetupTarget(ref targetPingPongA[i], size);
                SetupTarget(ref targetPingPongB[i], size);
            }
        }

        private void SetupTarget(ref RenderTarget renderTarget, Point2 size)
        {
            if (renderTarget == null) {
                // Create a new rendering target and backing texture, if not existing yet
                Texture tex = new Texture(
                    size.X,
                    size.Y,
                    TextureSizeMode.NonPowerOfTwo,
                    TextureMagFilter.Linear,
                    TextureMinFilter.Linear);

                renderTarget = new RenderTarget(AAQuality.Off, false, tex);
            } else if (renderTarget.Size != size) {
                // Resize the existing target to match the specified size
                Texture tex = renderTarget.Targets[0].Res;
                tex.Size = size;
                tex.ReloadData();
                renderTarget.SetupTarget();
            }
        }
#endif
    }
}