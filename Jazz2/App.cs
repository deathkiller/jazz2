using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Duality;
using Duality.Backend;
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
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
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
                Version v = Assembly.GetEntryAssembly().GetName().Version;
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + "." + v.Build.ToString(CultureInfo.InvariantCulture) + (v.Revision != 0 ? "." + v.Revision.ToString(CultureInfo.InvariantCulture) : "");
            }
        }

        public static string AssemblyPath
        {
            get
            {
                if (assemblyPath == null) {
#if LINUX_BUNDLE
                    try {
                        assemblyPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    } catch {
                        assemblyPath = "";
                    }
#else
                    assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
#endif
                }

                return assemblyPath;
            }
        }

        public static void GetAssemblyVersionNumber(out byte major, out byte minor, out byte build)
        {
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            major = (byte)v.Major;
            minor = (byte)v.Minor;
            build = (byte)v.Build;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

            // Override working directory
            try {
                Environment.CurrentDirectory = AssemblyPath;
            } catch (Exception ex) {
                Log.Write(LogType.Warning, "Cannot override working directory: " + ex);
            }

            DualityApp.Init(DualityApp.ExecutionContext.Game, new DefaultAssemblyLoader(AssemblyPath), args);

            ScreenMode screenMode;
            switch (Preferences.Get<int>("Screen", 0)) {
                default:
                case 0: screenMode = ScreenMode.Window; break;
                case 1: screenMode = ScreenMode.FullWindow; break;
            }

            RefreshMode refreshMode = (RefreshMode)Preferences.Get<int>("RefreshMode", (int)RefreshMode.VSync);

            i18n.Language = Preferences.Get<string>("Language", "en");

            ContentResolver.Current.Init();

            using (INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                Title = AssemblyTitle,
                RefreshMode = refreshMode,
                Size = LevelRenderSetup.TargetSize,
                ScreenMode = screenMode
            })) {
                ContentResolver.Current.InitPostWindow();

                current = new App(window);

                bool suppressMainMenu = false;
#if MULTIPLAYER
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].StartsWith("/connect:", StringComparison.InvariantCulture)) {
                        int idx = args[i].LastIndexOf(':', 10);
                        if (idx == -1) {
                            continue;
                        }

                        int port;
                        if (!int.TryParse(args[i].Substring(idx + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out port)) {
                            continue;
                        }

                        try {
                            System.Net.IPAddress ip = Lidgren.Network.NetUtility.Resolve(args[i].Substring(9, idx - 9));
                            current.ConnectToServer(new System.Net.IPEndPoint(ip, port));
                            suppressMainMenu = true;
                        } catch {
                            // Nothing to do...
                        }
                    }
                }
#endif
                if (!suppressMainMenu) {
                    current.PlayCinematics("intro", endOfStream => {
                        current.ShowMainMenu(endOfStream);
                    });
                }

                window.Run();
            }

            DualityApp.Terminate();

            // ToDo: Linux-specific workaround
            Environment.Exit(0);

            return 0;
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try {
                Log.Write(LogType.Error, "Unhandled exception: " + e.ExceptionObject);
            } catch {
                // Nothing to do...
            }
        }
    }
}