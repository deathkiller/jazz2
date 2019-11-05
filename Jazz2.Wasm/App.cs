using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Duality;
using Duality.Backend;
using Duality.Backend.DotNetFramework;
using Jazz2.Storage;
using WebAssembly;

namespace Jazz2.Game
{
    public partial class App
    {
        private static App current;

        public static string AssemblyTitle
        {
            get
            {
                return "Jazz² Resurrection";
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + "." + v.Build.ToString(CultureInfo.InvariantCulture) + (v.Revision != 0 ? "." + v.Revision.ToString(CultureInfo.InvariantCulture) : "");
            }
        }

        public static string AssemblyPath
        {
            get
            {
                return "";
            }
        }

        public static void GetAssemblyVersionNumber(out byte major, out byte minor, out byte build)
        {
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            major = (byte)v.Major;
            minor = (byte)v.Minor;
            build = (byte)v.Build;
        }

        private static async Task<bool> DownloadFilesToCache(string[] files)
        {
            using (var app = (JSObject)Runtime.GetGlobalObject("App")) {
                for (int i = 0; i < files.Length; i++) {
                    app.Invoke("loadingProgress", i * 100 / (files.Length + 1));

                    bool success = await NativeFileSystem.DownloadToCache(files[i], progress => {
                        app.Invoke("loadingProgress", (i + progress) * 100 / (files.Length + 1));
                    });
                    if (!success) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static async void Main()
        {
            DualityApp.Init(DualityApp.ExecutionContext.Game, null, null);

            // Download all needed files
            bool assetsLoaded = await DownloadFilesToCache(new[] {
                "Content/Main.dz",
                "Content/i18n/en.res",

                "Content/Tilesets/labrat1n.set",
                "Content/Tilesets/psych2.set",
                "Content/Tilesets/diam2.set",

                "Content/Episodes/share/01_share1.level",
                "Content/Episodes/share/02_share2.level",
                "Content/Episodes/share/03_share3.level",
                "Content/Episodes/share/Episode.res",
                "Content/Episodes/share/Logo.png"
            });
            if (!assetsLoaded) {
                using (var app = (JSObject)Runtime.GetGlobalObject("App")) {
                    app.Invoke("loadingFailed");
                }
                return;
            }

            i18n.Language = Preferences.Get<string>("Language", "en");

            ContentResolver.Current.Init();

            INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                Title = AssemblyTitle,
                RefreshMode = RefreshMode.VSync,
                Size = LevelRenderSetup.TargetSize,
                ScreenMode = ScreenMode.Window
            });

            ContentResolver.Current.InitPostWindow();

            current = new App(window);

            current.ShowMainMenu(false);

            using (var app = (JSObject)Runtime.GetGlobalObject("App")) {
                app.Invoke("ready");
            }

            window.Run();
        }
    }
}