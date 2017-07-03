using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.Menu
{
    public class MainMenuRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

        private const int defaultWidth = 720, defaultHeight = 405;
        private Vector2 lastImageSize;

        //private readonly ContentRef<DrawTechnique> postprocessingShader;
        private ContentRef<DrawTechnique> resizeShader;

        //private readonly ContentRef<DrawTechnique> testShader;

        private Texture /*mainTexture,*/ finalTexture;
        private RenderTarget /*mainTarget,*/ finalTarget;

        //private Texture pageTexture;
        //private RenderTarget pageTarget;

        //private int currentTarget;
        //private float sectionTransition;
        //private Texture pageSnapshot;
        //private RenderTarget pageSnapshotTarget;
        //private Texture[] mainTextures;
        //private RenderTarget[] mainTargets;

        private Settings.ResizeMode lastResizeMode;

        public MainMenuRenderSetup()
        {
            // Shaders
            //postprocessingShader = ContentManager.Current.RequestShaderRaw("MainMenuPostprocessing", BlendMode.Solid);

            lastResizeMode = Settings.Resize;

            switch (lastResizeMode) {
                default:
                case Settings.ResizeMode.None:
                    resizeShader = DrawTechnique.Solid;
                    break;
                case Settings.ResizeMode.HQ2x:
                    resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                    break;
                case Settings.ResizeMode.xBRZ:
                    resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                    break;
            }

            //testShader = ContentManager.Current.RequestShaderRaw("PageCurl", BlendMode.Solid);

            // Main texture
            /*mainTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            mainTarget = new RenderTarget(AAQuality.Off, true, mainTexture);*/

            // Lighting texture
            finalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            finalTarget = new RenderTarget(AAQuality.Off, false, finalTexture);

            //
            /*mainTextures = new Texture[2];
            mainTargets = new RenderTarget[2];

            mainTextures[0] = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            mainTextures[0].Size = TargetSize;
            mainTextures[0].ReloadData();

            mainTargets[0] = new RenderTarget(AAQuality.Off, false, mainTextures[0]);

            //
            mainTextures[1] = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            mainTextures[1].Size = TargetSize;
            mainTextures[1].ReloadData();

            mainTargets[1] = new RenderTarget(AAQuality.Off, false, mainTextures[1]);*/

            // Render steps
            /*AddRenderStep(RenderStepPosition.Last, new RenderStep {
                MatrixMode = RenderMatrix.WorldSpace,
                VisibilityMask = VisibilityFlag.AllGroups,
                ClearFlags = ClearFlag.All,
                DefaultClearColor = true,

                Output = mainTarget
            });*/

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.All,
                ClearFlags = ClearFlag.None,

                Output = finalTarget
            });

            /*AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Postprocess",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None,
                ClearFlags = ClearFlag.None,

                Input = new BatchInfo(DrawTechnique.Solid, ColorRgba.White, mainTexture),
                Output = finalTarget
            });*/

            /*AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "PageTransition",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None,

                Output = finalTarget
            });*/

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Final",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None
            });
        }

        protected override void OnDisposing(bool manually)
        {
            base.OnDisposing(manually);

            //Disposable.Free(ref mainTarget);
            //Disposable.Free(ref mainTexture);

            Disposable.Free(ref finalTarget);
            Disposable.Free(ref finalTexture);
        }

        protected override void OnRenderPointOfView(Scene scene, DrawDevice drawDevice, Rect viewportRect, Vector2 imageSize)
        {
            if (lastResizeMode != Settings.Resize) {
                lastResizeMode = Settings.Resize;

                switch (lastResizeMode) {
                    default:
                    case Settings.ResizeMode.None:
                        resizeShader = DrawTechnique.Solid;
                        break;
                    case Settings.ResizeMode.HQ2x:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                        break;
                    case Settings.ResizeMode.xBRZ:
                        resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                        break;
                }
            }

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

                ResizeRenderTarget(finalTarget, TargetSize);
            }

            base.OnRenderPointOfView(scene, drawDevice, viewportRect, imageSize);
        }

        protected override void OnRenderSingleStep(RenderStep step, Scene scene, DrawDevice drawDevice)
        {
            /*if (step.Id == "PageTransition") {
                ProcessPageTransitionStep(drawDevice);
            } else*/ if (step.Id == "Final") {
                ProcessFinalStep(drawDevice);
            } /*else if (step.Id == "Postprocess") {
                ProcessPostprocessStep(drawDevice);
            }*/ else {
                //step.Output = mainTargets[currentTarget];
                base.OnRenderSingleStep(step, scene, drawDevice);
            }
        }

        /*private void ProcessPageTransitionStep(DrawDevice drawDevice)
        {
            if (sectionTransition <= 0f) {
                BatchInfo material = new BatchInfo(DrawTechnique.Solid, ColorRgba.White, mainTextures[currentTarget]);
                Blit(drawDevice, material, finalTarget);

                currentTarget = 1 - currentTarget;
            } else {
                Vector2 originalSize = drawDevice.ViewportRect.Size;

                BatchInfo material = new BatchInfo(testShader, ColorRgba.White);
                material.SetUniform("resolution", originalSize.X, originalSize.Y);
                material.SetUniform("progress", 1f - sectionTransition);
                material.SetTexture("from", mainTextures[1 - currentTarget]);
                material.SetTexture("to", mainTextures[currentTarget]);
                Blit(drawDevice, material, finalTarget);

                sectionTransition -= Time.TimeMult * 0.02f;
            }
        }*/

        private void ProcessFinalStep(DrawDevice drawDevice)
        {
            BatchInfo material = new BatchInfo(resizeShader, ColorRgba.White, finalTexture);
            material.SetUniform("mainTexSize", (float)finalTexture.ContentWidth, (float)finalTexture.ContentHeight);
            Blit(drawDevice, material, drawDevice.ViewportRect);


            //BatchInfo material = new BatchInfo(testShader, ColorRgba.White);
            /*BatchInfo material = new BatchInfo(testShader, ColorRgba.White);
            material.SetTexture("fromTex", finalTexture);
            material.SetTexture("toTex", finalTexture);
            //material.SetUniform("resolution", (float)finalTexture.ContentWidth, (float)finalTexture.ContentHeight);
            material.SetUniform("resolution", originalSize.X, originalSize.Y);
            material.SetUniform("progress", DualityApp.Mouse.Pos.Y / originalSize.Y);
            Blit(drawDevice, material, new Rect((originalSize.X - size.X) * 0.5f, (originalSize.Y - size.Y) * 0.5f, size.X, size.Y));*/

            /*
            BatchInfo material = new BatchInfo(testShader, ColorRgba.White, new[] {
                new KeyValuePair<string, ContentRef<Texture>>("fromTex", finalTexture),
                new KeyValuePair<string, ContentRef<Texture>>("toTex", finalTexture)
            });
            material.SetUniform("resolution", (float)finalTexture.ContentWidth, (float)finalTexture.ContentHeight);
            material.SetUniform("progress", DualityApp.Mouse.Pos.Y / originalSize.Y);
            Blit(drawDevice, material, drawDevice.ViewportRect);*/
        }

        /*private void ProcessPostprocessStep(DrawDevice drawDevice)
        {
            BatchInfo material = new BatchInfo(postprocessingShader, ColorRgba.White, mainTexture);

            material.SetUniform("mainTexSize", 1f / material.MainTexture.Res.ContentWidth, 1f / material.MainTexture.Res.ContentHeight);

            Blit(drawDevice, material, finalTarget);
        }*/

        private static void Blit(DrawDevice device, BatchInfo source, RenderTarget target)
        {
            device.Target = target;
            device.TargetSize = target.Size;
            device.ViewportRect = new Rect(target.Size);

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }

        private static void Blit(DrawDevice device, BatchInfo source, Rect screenRect)
        {
            device.Target = null;
            device.TargetSize = screenRect.Size;
            device.ViewportRect = screenRect;

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }

        /*public void BeginPageTransition()
        {
            sectionTransition = 1f;
        }*/
    }
}