using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Duality;
using Duality.Backend;
using Duality.Backend.DotNetFramework;
using Jazz2.Storage;

namespace Jazz2.Game
{
    public partial class App
    {
        private static App current;
        private static string assemblyPath;

        public static string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0) {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (!string.IsNullOrEmpty(titleAttribute.Title)) {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + (v.Build != 0 || v.Revision != 0 ? ("." + v.Build.ToString(CultureInfo.InvariantCulture) + (v.Revision != 0 ? "." + v.Revision.ToString(CultureInfo.InvariantCulture) : "")) : "");
            }
        }

        public static string AssemblyPath
        {
            get
            {
                return "";
            }
        }

        public static void Log(string message, params object[] messageParams)
        {
            string line = (messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);

            Console.WriteLine(line);
        }

        public static void GetAssemblyVersionNumber(out byte major, out byte minor, out byte build)
        {
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            major = (byte)v.Major;
            minor = (byte)v.Minor;
            build = (byte)v.Build;
        }

        public static async void Main()
        {
            // ToDo
            await NativeFileSystem.DownloadToCache("Content/Cinematics/intro.j2v");

            await NativeFileSystem.DownloadToCache("Content/Main.dz");
            await NativeFileSystem.DownloadToCache("Content/i18n/en.res");

            await NativeFileSystem.DownloadToCache("Content/Tilesets/easter99.set");
            await NativeFileSystem.DownloadToCache("Content/Episodes/secretf/01_easter1.level");
            await NativeFileSystem.DownloadToCache("Content/Episodes/secretf/02_easter2.level");
            await NativeFileSystem.DownloadToCache("Content/Episodes/secretf/03_easter3.level");
            await NativeFileSystem.DownloadToCache("Content/Episodes/secretf/Episode.res");
            await NativeFileSystem.DownloadToCache("Content/Episodes/secretf/Logo.png");


            DualityApp.Init(DualityApp.ExecutionContext.Game, null, null);

            RefreshMode refreshMode = (RefreshMode)Preferences.Get<int>("RefreshMode", (int)RefreshMode.VSync);

            i18n.Language = Preferences.Get<string>("Language", "en");

            ContentResolver.Current.Init();

            INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                Title = AssemblyTitle,
                RefreshMode = refreshMode,
                Size = LevelRenderSetup.TargetSize,
                ScreenMode = ScreenMode.Window
            });

            ContentResolver.Current.InitPostWindow();

            current = new App(window);

            //current.PlayCinematics("intro", endOfStream => {
                current.ShowMainMenu(false);
            //});

            window.Run();

        }
    }
}