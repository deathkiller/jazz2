using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game
{
    public class LevelRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

#if __ANDROID__
        private const int defaultWidth = 544, defaultHeight = 306;
#else
        private const int defaultWidth = 720, defaultHeight = 405;
#endif
        private Vector2 lastImageSize;

        private static readonly int PyramidSize = 3;

        private readonly LevelHandler levelHandler;
        private readonly ContentRef<Material> lightingMaterial, lightingNoiseMaterial;
        private readonly ContentRef<DrawTechnique> combineSceneShader, combineSceneWaterShader;

#if !__ANDROID__
        private readonly ContentRef<DrawTechnique> downsampleShader;
        private readonly ContentRef<DrawTechnique> blurShader;
#endif
        private ContentRef<DrawTechnique> resizeShader;

        private Texture lightingTexture, mainTexture, normalTexture, finalTexture;
        private RenderTarget lightingTarget, mainTarget, finalTarget;

#if !__ANDROID__
        private readonly RenderTarget[] targetPingPongA = new RenderTarget[PyramidSize];
        private readonly RenderTarget[] targetPingPongB = new RenderTarget[PyramidSize];
#endif

        private readonly VertexC1P3T4A1[] lightBuffer = new VertexC1P3T4A1[4];

        private readonly ContentRef<Texture> noiseTexture;

        private SettingsCache.ResizeMode lastResizeMode;

        public Texture FinalTexture
        {
            get { return finalTexture; }
        }

        public LevelRenderSetup(LevelHandler levelHandler)
        {
            this.levelHandler = levelHandler;

            // Shaders
            ContentRef<DrawTechnique> lightingShader = ContentResolver.Current.RequestShader("Lighting");
            ContentRef<DrawTechnique> lightingNoiseShader = ContentResolver.Current.RequestShader("LightingNoise");

#if !__ANDROID__
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
            lightingNoiseMaterial.Res.SetTexture("noiseTex", noiseTexture);

            // Render steps
            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                DefaultProjection = true,
                VisibilityMask = VisibilityFlag.AllGroups,
                ClearFlags = ClearFlag.All,
                DefaultClearColor = true,

                Output = mainTarget
                //Output = finalTarget
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

#if !__ANDROID__
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
            BatchInfo material = device.RentMaterial();
            material.Technique = resizeShader;
            material.MainTexture = finalTexture;
            material.SetValue("mainTexSize", new Vector2(finalTexture.ContentWidth, finalTexture.ContentHeight));
            this.Blit(device, material, device.ViewportRect);
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

            // Blit ambient light color
            {
                BatchInfo material = device.RentMaterial();
                material.Technique = DrawTechnique.Solid;
                material.MainColor = new ColorRgba(ambientLight, 0, 0);
                this.Blit(device, material, lightingTarget);
            }

            // Render lights (target was set in previous step)
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

#if !__ANDROID__
            // Resize Blur targets
            SetupTargets((Point2)device.TargetSize);

            // Blit it into screen
            {
                BatchInfo material = device.RentMaterial();
                material.Technique = DrawTechnique.Solid;
                material.MainTexture = mainTexture;
                this.Blit(device, material, targetPingPongA[0]);
            }

            // Downsample to lowest target
            for (int i = 1; i < targetPingPongA.Length; i++) {
                BatchInfo material = device.RentMaterial();
                material.Technique = downsampleShader;
                material.MainTexture = targetPingPongA[i - 1].Targets[0];
                material.SetValue("pixelOffset", new Vector2(1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight));

                this.Blit(device, material, targetPingPongA[i]);
            }

            // Blur all targets, separating horizontal and vertical blur
            for (int i = 0; i < targetPingPongA.Length; i++) {
                BatchInfo material = device.RentMaterial();
                material.Technique = blurShader;
                material.MainTexture = targetPingPongA[i].Targets[0];
                material.SetValue("blurDirection", new Vector2(1f, 0f));
                material.SetValue("pixelOffset", new Vector2(1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight));

                this.Blit(device, material, targetPingPongB[i]);

                material.MainTexture = targetPingPongB[i].Targets[0];
                material.SetValue("blurDirection", new Vector2(0f, 1f));
                material.SetValue("pixelOffset", new Vector2(1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight));

                this.Blit(device, material, targetPingPongA[i]);
            }
#endif

            // Blit it into screen
            if (viewWaterLevel < viewSize.Y) {
                // Render lighting with water
                BatchInfo material = device.RentMaterial();
                material.Technique = combineSceneWaterShader;
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);
                material.SetTexture("displacementTex", noiseTexture); // Underwater displacement
#if !__ANDROID__
                material.SetTexture("blurHalfTex", targetPingPongA[1].Targets[0]);
                material.SetTexture("blurQuarterTex", targetPingPongA[2].Targets[0]);

                material.SetValue("ambientLight", ambientLight);
#endif
                material.SetValue("darknessColor", levelHandler.DarknessColor);

                material.SetValue("waterLevel", viewWaterLevel / viewSize.Y);

                this.Blit(device, material, finalTarget);
            } else {
                // Render lighting without water
                BatchInfo material = device.RentMaterial();
                material.Technique = combineSceneShader;
                material.SetTexture("mainTex", mainTexture);
                material.SetTexture("lightTex", lightingTexture);
#if !__ANDROID__
                material.SetTexture("blurHalfTex", targetPingPongA[1].Targets[0]);
                material.SetTexture("blurQuarterTex", targetPingPongA[2].Targets[0]);

                material.SetValue("ambientLight", ambientLight);
#endif
                material.SetValue("darknessColor", levelHandler.DarknessColor);

                this.Blit(device, material, finalTarget);
            }
        }

#if !__ANDROID__
        private void SetupTargets(Point2 size)
        {
            for (int i = 0; i < targetPingPongA.Length; i++) {
                SetupTarget(ref targetPingPongA[i], size);
                SetupTarget(ref targetPingPongB[i], size);
                size /= 2;
            }
        }

        private void SetupTarget(ref RenderTarget renderTarget, Point2 size)
        {
            // Create a new rendering target and backing texture, if not existing yet
            if (renderTarget == null) {
                Texture tex = new Texture(
                    size.X,
                    size.Y,
                    TextureSizeMode.NonPowerOfTwo,
                    TextureMagFilter.Linear,
                    TextureMinFilter.Linear);

                renderTarget = new RenderTarget(AAQuality.Off, false, tex);
            }

            // Resize the existing target to match the specified size
            if (renderTarget.Size != size) {
                Texture tex = renderTarget.Targets[0].Res;
                tex.Size = size;
                tex.ReloadData();
                renderTarget.SetupTarget();
            }
        }
#endif
    }
}