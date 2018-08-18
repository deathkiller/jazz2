using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.UI.Menu.InGame
{
    public class InGameMenuRenderSetup : RenderSetup
    {
        public static Point2 TargetSize = new Point2(defaultWidth, defaultHeight);

        private const int defaultWidth = 720, defaultHeight = 405;
        private Vector2 lastImageSize;

        private readonly ContentRef<DrawTechnique> resizeShader;

        private Texture finalTexture;
        private RenderTarget finalTarget;

        public InGameMenuRenderSetup()
        {
            // Shaders
            try {
                switch (Jazz2.Settings.Resize) {
                    default:
                    case Jazz2.Settings.ResizeMode.None:
                        resizeShader = DrawTechnique.Solid;
                        break;
                    case Jazz2.Settings.ResizeMode.HQ2x:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeHQ2x");
                        break;
                    case Jazz2.Settings.ResizeMode.xBRZ3:
                        resizeShader = ContentResolver.Current.RequestShader("Resize3xBRZ");
                        break;
                    case Jazz2.Settings.ResizeMode.xBRZ4:
                        resizeShader = ContentResolver.Current.RequestShader("Resize4xBRZ");
                        break;
                    case Jazz2.Settings.ResizeMode.CRT:
                        resizeShader = ContentResolver.Current.RequestShader("ResizeCRT");
                        break;
                }
            } catch {
                resizeShader = DrawTechnique.Solid;
            }

            // Textures
            finalTexture = new Texture(null, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest);

            finalTarget = new RenderTarget(AAQuality.Off, false, finalTexture);

            // Render steps
            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Projection = ProjectionMode.Screen,
                VisibilityMask = VisibilityFlag.All,
                ClearFlags = ClearFlag.None,

                Output = finalTarget
            });

            AddRenderStep(RenderStepPosition.Last, new RenderStep {
                Id = "Resize",

                Projection = ProjectionMode.Screen,
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
            BatchInfo material = drawDevice.RentMaterial();
            material.Technique = resizeShader;
            material.MainTexture = finalTexture;
            material.SetValue("mainTexSize", new Vector2(finalTexture.ContentWidth, finalTexture.ContentHeight));
            this.Blit(drawDevice, material, drawDevice.ViewportRect);
        }
    }
}