using System;
using System.IO;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using Duality.Backend.Android;
using Jazz2.Game;
using Path = System.IO.Path;

namespace Jazz2.Android
{
    [Activity(
        MainLauncher = true,
        Icon = "@mipmap/ic_launcher",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize,
        ScreenOrientation = ScreenOrientation.UserLandscape,
        LaunchMode = LaunchMode.SingleInstance,
        ResizeableActivity = true,
        Immersive = true
    )]
    public class MainActivity : Activity
    {
        private const int StoragePermissionsRequest = 1;

        private static WeakReference<MainActivity> weakActivity;

        public static MainActivity Current
        {
            get
            {
                MainActivity activity;
                weakActivity.TryGetTarget(out activity);
                return activity;
            }
        }


        internal InnerView InnerView;

        private VideoView backgroundVideo;
        private Button retryButton;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            weakActivity = new WeakReference<MainActivity>(this);

            CrashHandlerActivity.Register(this);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            try {
                Window.DecorView.SystemUiVisibility |= (StatusBarVisibility)(SystemUiFlags.LayoutStable | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation);

                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.P) {
                    Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                }
            } catch /*(Exception ex)*/ {
#if !DEBUG
                throw;
#endif
            }

            TryInit();
        }

        protected override void OnDestroy()
        {
            weakActivity.SetTarget(null);

            base.OnDestroy();
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (InnerView != null) {
                InnerView.Pause();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (InnerView != null) {
                InnerView.Resume();
            }
            if (backgroundVideo != null) {
                backgroundVideo.Start();
            }
        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode) {
                case StoragePermissionsRequest: {
                    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted) {
                        TryInit();
                    }
                    break;
                }
            }
        }

        private void TryInit()
        {
            if (!CheckAppPermissions()) {
                ShowInfoScreen("Access denied", "You have to grant file access permissions to&nbsp;continue!", true);
                return;
            }

            int storagePathLength;
            string rootPath = NativeFileSystem.FindRootPath(out storagePathLength);
            if (rootPath == null) {
                var storageList = NativeFileSystem.GetStorageList();
                if (storageList.Count == 0) {
                    ShowInfoScreen("Content files not found", "No storage is accessible.", true);
                    return;
                }

                var found = storageList.Find(storage => storage.Path == "/storage/emulated/0");
                if (found.Path == null) {
                    found = storageList[0];
                }

                ShowInfoScreen("Content files not found", "Content should be placed in&nbsp;" + found.Path + "<b><u>/Android/Data/" + Application.Context.PackageName + "/Content/</u></b>… or&nbsp;in&nbsp;other compatible path.", true);
                return;
            }

            if (!File.Exists(Path.Combine(rootPath, "Content", "Main.dz"))) {
                ShowInfoScreen("Content files not found", "Content should be placed in&nbsp;" + rootPath.Substring(0, storagePathLength) + "<b><u>" + rootPath.Substring(storagePathLength) + "Content/</u></b>…<br>It includes <b>Main.dz</b> file and <b>Episodes</b>, <b>Music</b>, <b>Tilesets</b> directories.", true);
                return;
            }

            RunGame();
        }

        private bool CheckAppPermissions()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) {
                return true;
            }
            
            if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) != Permission.Granted
                && CheckSelfPermission(Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage },
                    StoragePermissionsRequest);
                return false;
            } else {
                return true;
            }
        }
        
        public void ShowInfoScreen(string header, string content, bool showRetry)
        {
            content += "<br><br><small>If you have any issues, report it to developers.<br><a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a></small>";

            try {
                Window.DecorView.SystemUiVisibility &= ~(StatusBarVisibility)SystemUiFlags.LayoutHideNavigation;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(new Color(0x30000000));
                    Window.SetNavigationBarColor(new Color(unchecked((int)0xff000000)));
                }
            } catch {
                // Nothing to do...
            }

            if (backgroundVideo == null || retryButton == null) {
                SetContentView(Resource.Layout.activity_info);

                backgroundVideo = FindViewById<VideoView>(Resource.Id.background_video);
                backgroundVideo.SetVideoURI(global::Android.Net.Uri.Parse("android.resource://" + PackageName + "/raw/logo"));
                backgroundVideo.Prepared += OnVideoViewPrepared;
                backgroundVideo.Start();

                retryButton = FindViewById<Button>(Resource.Id.retry_button);
                retryButton.Click += OnRetryButtonClick;

                TextView versionView = FindViewById<TextView>(Resource.Id.version);
                versionView.Text = "v" + App.AssemblyVersion;
            }

            TextView headerView = FindViewById<TextView>(Resource.Id.header);
            headerView.Text = header;

            TextView contentView = FindViewById<TextView>(Resource.Id.content);
            contentView.MovementMethod = LinkMovementMethod.Instance;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N) {
                contentView.TextFormatted = Html.FromHtml(content, FromHtmlOptions.ModeLegacy);
            } else {
                contentView.TextFormatted = Html.FromHtml(content);
            }

            retryButton.Visibility = (showRetry ? ViewStates.Visible : ViewStates.Gone);
        }

        private void OnRetryButtonClick(object sender, EventArgs e)
        {
            TryInit();
        }

        private void OnVideoViewPrepared(object sender, EventArgs e)
        {
            ((global::Android.Media.MediaPlayer)sender).Looping = true;
        }

        private void RunGame()
        {
            try {
                Window.DecorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutHideNavigation;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(new Color(0));
                    Window.SetNavigationBarColor(new Color(0));
                }
            } catch {
                // Nothing to do...
            }

            if (backgroundVideo != null) {
                backgroundVideo.Prepared -= OnVideoViewPrepared;
                backgroundVideo = null;
            }
            if (retryButton != null) {
                retryButton.Click -= OnRetryButtonClick;
                retryButton = null;
            }

            // Create our OpenGL view and show it
            InnerView = new InnerView(this);
            SetContentView(InnerView);
        }
    }
}