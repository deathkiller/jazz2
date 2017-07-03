using System;
using Android.App;
using Android.Content;
using Android.Views;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeWindow : INativeWindow
    {
        private WeakReference<Activity> weakContext;
        private ScreenMode screenMode;

        public NativeWindow(WindowOptions options)
        {
            screenMode = options.ScreenMode;
        }

        void IDisposable.Dispose()
        {
            weakContext = null;
        }

        void INativeWindow.Run()
        {
        }

        string INativeWindow.Title
        {
            get
            {
                Activity context;
                weakContext.TryGetTarget(out context);
                if (context == null) {
                    return null;
                }

                return context.Title;
            }
            set
            {
                Activity context;
                weakContext.TryGetTarget(out context);
                if (context == null) {
                    return;
                }

                context.Title = value;
            }
        }

        ScreenMode INativeWindow.ScreenMode
        {
            get { return screenMode; }
            set
            {
                value &= (ScreenMode.Immersive);
                if (screenMode == value) {
                    return;
                }

                Activity context;
                weakContext.TryGetTarget(out context);
                if (context == null) {
                    return;
                }

                if ((value & ScreenMode.Immersive) != 0) {
                    context.Window.ClearFlags(WindowManagerFlags.Fullscreen);
                } else {
                    context.Window.AddFlags(WindowManagerFlags.Fullscreen);
                }

                screenMode = value;
            }
        }

        public void BindContext(Context context)
        {
            if (weakContext != null) {
                throw new InvalidOperationException("Already bound to different context");
            }

            weakContext = new WeakReference<Activity>(context as Activity);

            // Refresh ScreenMode after bind
            ScreenMode oldScreenMode = screenMode;
            screenMode = ScreenMode.Window;

            ((INativeWindow)this).ScreenMode = oldScreenMode;
        }
    }
}