using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + (v.Build != 0 || v.Revision != 0 ? ("." + v.Build.ToString(CultureInfo.InvariantCulture) + (v.Revision != 0 ? "." + v.Revision.ToString(CultureInfo.InvariantCulture) : "")) : "");
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

        public static void Log(string message, params object[] messageParams)
        {
            string line = (messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);

            Console.WriteLine(line);
        }

        [STAThread]
        private static void Main(string[] args)
        {
            // Override working directory
            try {
                Environment.CurrentDirectory = AssemblyPath;
            } catch (Exception ex) {
                Console.WriteLine("Cannot override current directory: " + ex);
            }

            DualityApp.Init(DualityApp.ExecutionContext.Game, new DefaultAssemblyLoader(), args);

            ScreenMode screenMode;
            switch (Preferences.Get<int>("Screen", 0)) {
                default:
                case 0: screenMode = ScreenMode.Window; break;
                case 1: screenMode = ScreenMode.FullWindow; break;
            }

            RefreshMode refreshMode = (RefreshMode)Preferences.Get<int>("RefreshMode", (int)RefreshMode.AdaptiveVSync);

            ContentResolver.Current.Init();

            using (INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                Title = AssemblyTitle,
                RefreshMode = refreshMode,
                Size = LevelRenderSetup.TargetSize,
                ScreenMode = screenMode
            })) {
                ContentResolver.Current.InitPostWindow();

                current = new App(window);
                current.ShowMainMenu();
                window.Run();
            }

            DualityApp.Terminate();

            // ToDo: Linux-specific workaround
            Environment.Exit(0);
        }
    }
}