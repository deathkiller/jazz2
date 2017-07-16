using System;
using Android.App;
using Android.Content;
using Android.OS;
using Duality;
using Duality.Backend;
using Jazz2.Game;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform.Android;
using Vector2 = Duality.Vector2;
using Debug = System.Diagnostics.Debug;
using NativeWindow = Duality.Backend.Android.OpenTK.NativeWindow;
using INativeWindow = Duality.Backend.INativeWindow;

namespace Jazz2.Android
{
    public partial class GLView : AndroidGameView
    {
        private Controller controller;
        private int viewportWidth, viewportHeight;

        private readonly Vibrator vibrator;

        public GLView(Context context) : base(context)
        {
            vibrator = (Vibrator)context.GetSystemService(Context.VibratorService);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            viewportWidth = Width;
            viewportHeight = Height;

            DualityApp.Init(DualityApp.ExecutionContext.Game, /*new DefaultAssemblyLoader()*/null, null);
            DualityApp.WindowSize = new Point2(viewportWidth, viewportHeight);
            INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                ScreenMode = ScreenMode.Window
            });

            if (window is NativeWindow) {
                // Backend was initialized successfully
                (window as NativeWindow).BindContext(Context);
            }

            InitInput();

            controller = new Controller(window);
            controller.ShowMainMenu();

            // Run the render loop
            Run();
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);

            DualityApp.Terminate();
        }

        protected override void CreateFrameBuffer()
        {
            ContextRenderingApi = GLVersion.ES3;

            // The default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
            try {
                Debug.WriteLine("GLView :: Loading with default settings");

                base.CreateFrameBuffer();
                return;
            } catch (Exception ex) {
                Debug.WriteLine("GLView :: CreateFrameBuffer() threw an exception: " + ex);
            }

            // This is a graphics setting that sets everything to the lowest mode possible so
            // the device returns a reliable graphics setting.
            try {
                Debug.WriteLine("GLView :: Loading with custom Android settings (low mode)");
                GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);

                base.CreateFrameBuffer();
                return;
            } catch (Exception ex) {
                Debug.WriteLine("GLView :: CreateFrameBuffer() threw an exception: " + ex);
            }

            throw new BackendException("Cannot initialize OpenGL ES 3.0 FrameBuffer");
        }

        protected override void OnResize(EventArgs e)
        {
            viewportHeight = Height;
            viewportWidth = Width;

            DualityApp.WindowSize = new Point2(viewportWidth, viewportHeight);

            MakeCurrent();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            //base.OnUpdateFrame(e);

            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    (Context as Activity).FinishAndRemoveTask();
                } else {
                    (Context as Activity).Finish();
                }

                return;
            }

            DualityApp.Update();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //base.OnRenderFrame(e);

            //try {
                DualityApp.Render(null, new Rect(viewportWidth, viewportHeight), new Vector2(viewportWidth, viewportHeight));
            //} catch (Exception ex) {
            //    Console.WriteLine(ex.ToString());
            //}

            SwapBuffers();
        }
    }
}