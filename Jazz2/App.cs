using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Duality;
using Duality.Backend;

namespace Jazz2.Game
{
    public partial class App
    {
        private static App current;

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
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + (v.Build != 0 ? "." + v.Build.ToString(CultureInfo.InvariantCulture) : "");
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
        private static void Main(string[] args)
        {
            // Override working directory
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            DualityApp.Init(DualityApp.ExecutionContext.Game, new DefaultAssemblyLoader(), args);

            using (INativeWindow window = DualityApp.OpenWindow(new WindowOptions {
                Title = AssemblyTitle,
                RefreshMode = (args.Contains("/nv") ? RefreshMode.NoSync : (args.Contains("/mv") ? RefreshMode.ManualSync : RefreshMode.VSync)),
                Size = LevelRenderSetup.TargetSize
            })) {
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