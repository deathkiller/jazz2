using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.UI.Menu
{
    public class MainMenuRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

        private const int defaultWidth = 720, defaultHeight = 405;
        private Vector2 lastImageSize;

        private ContentRef<DrawTechnique> resizeShader;

        private Texture finalTexture;
        private RenderTarget finalTarget;

        private Settings.ResizeMode lastResizeMode;

        public MainMenuRenderSetup()
        {
            // Shaders
            lastResizeMode = Settings.Resize;

            switch (lastResizeMode) {
                default:
                case Settings.ResizeMode.None:
                    resizeShader = DrawTechnique.Solid;
                    break;
                case Settings.ResizeMode.HQ2x:
                    resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                    break;
                case Settings.ResizeMode.xBRZ3:
                    resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                    break;
                case Settings.ResizeMode.xBRZ4:
                    resizeShader = ContentResolver.Current.RequestShader("Resize4xBRZ");
                    break;
                case Settings.ResizeMode.CRT:
                    resizeShader = ContentResolver.Current.RequestShader("ResizeCRT");
                    break;
            }

            // Textures
            finalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            finalTarget = new RenderTarget(AAQuality.Off, false, finalTexture);

            // Render steps
            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.All,
                ClearFlags = ClearFlag.None,

                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Resize",

                MatrixMode = RenderMatrix.ScreenSpace,
                VisibilityMask = VisibilityFlag.None
            });
        }

        protected override void OnDisposing(bool manually)
        {
            base.OnDisposing(manually);

            Disposable.Free(ref finalTarget);
            Disposable.Free(ref finalTexture);
        }

        protected override void OnRenderPointOfView(Scene scene, DrawDevice drawDevice, Rect viewportRect, Vector2 imageSize)
        {
            // Switch between resize modes if necessary
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
                    case Settings.ResizeMode.xBRZ3:
                        resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                        break;
                    case Settings.ResizeMode.xBRZ4:
                        resizeShader = ContentResolver.Current.RequestShader("Resize4xBRZ");
                        break;
                    case Settings.ResizeMode.CRT:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeCRT");
                        break;
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

                ResizeRenderTarget(finalTarget, TargetSize);
            }

            base.OnRenderPointOfView(scene, drawDevice, viewportRect, imageSize);
        }

        protected override void OnRenderSingleStep(RenderStep step, Scene scene, DrawDevice drawDevice)
        {
            if (step.Id == "Resize") {
                ProcessResizeStep(drawDevice);
            } else {
                base.OnRenderSingleStep(step, scene, drawDevice);
            }
        }

        private void ProcessResizeStep(DrawDevice drawDevice)
        {
            BatchInfo material = new BatchInfo(resizeShader, ColorRgba.White, finalTexture);
            material.SetUniform("mainTexSize", (float)finalTexture.ContentWidth, (float)finalTexture.ContentHeight);
            this.Blit(drawDevice, material, drawDevice.ViewportRect);
        }
    }
}