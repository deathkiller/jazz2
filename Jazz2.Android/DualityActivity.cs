using System;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Jazz2.Android;

namespace Duality.Android
{
    [Activity(
        MainLauncher = true,
        Icon = "@mipmap/ic_launcher",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden,
        ScreenOrientation = ScreenOrientation.UserLandscape,
        LaunchMode = LaunchMode.SingleInstance
#if __ANDROID_11__
        , HardwareAccelerated = false
#endif
        )]
    public class DualityActivity : Activity
    {
        private static WeakReference<DualityActivity> weakActivity;

        public static DualityActivity Current
        {
            get
            {
                DualityActivity activity;
                weakActivity.TryGetTarget(out activity);
                return activity;
            }
        }


        internal InnerView InnerView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            weakActivity = new WeakReference<DualityActivity>(this);

            CrashHandlerActivity.Register();

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            try {
                View decorView = Window.DecorView;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutStable;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutFullscreen;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.Immersive;

                // Minimal supported SDK is already 18
                //if ((int)Build.VERSION.SdkInt < 18)
                //    RequestedOrientation = ScreenOrientation.SensorLandscape;

                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(Color.Argb(0x22, 0x00, 0x00, 0x00));
                }
            } catch /*(Exception ex)*/ {
#if DEBUG
                throw;
#endif
            }

            // Create our OpenGL view and show it
            InnerView = new InnerView(this);
            SetContentView(InnerView);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPause()
        {
            base.OnPause();
            InnerView.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            InnerView.Resume();
        }
    }
}