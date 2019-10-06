using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Jazz2.x86
{
    internal static class App
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (!Environment.Is64BitOperatingSystem) {
                // Load 'Any CPU' version on 32-bit system
                string path = Assembly.GetExecutingAssembly().Location;
                string ext = Path.GetExtension(path);
                path = path.Remove(path.Length - (ext.Length + 4), 4);

                string argsString = "";
                for (int i = 0; i < args.Length; i++) {
                    argsString = "\"" + args[i].Replace("\"", "\"\"") + "\" ";
                }

                Process p = Process.Start(path, argsString);
                p.Dispose();
                return 1;
            }

            return Jazz2.Game.App.Main(args);
        }
    }
}