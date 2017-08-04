using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Import
{
    public static class DemoDownloader
    {
        private const string Url = "http://deat.tk/public/jazz2/demo.zip";

        public static void Start()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  If you don't have any suitable version, Shareware Demo can be automatically");
            Console.WriteLine("  imported, but functionality will be slightly degraded.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Press Enter to download and import Shareware Demo now. Otherwise, press");
            Console.WriteLine("  CTRL+C or close the window.");
            Console.WriteLine();

            Console.ReadLine();

            string targetPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (File.Exists(Path.Combine(targetPath, "Content", "Animations", "Jazz", "Idle.png"))) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  It seems that there is already other version imported. It's not");
                Console.WriteLine("  recommended to import Shareware Demo over full version of the game.");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("  Press Enter to continue. Otherwise, press CTRL+C or close the window.");
                Console.WriteLine();

                Console.ReadLine();
            }

            Console.WriteLine("  Downloading Shareware Demo...");

            string zipFile = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            WebClient client = new WebClient();
            client.DownloadFile(Url, zipFile);

            string tempDir = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            try {
                Directory.CreateDirectory(tempDir);

                Console.WriteLine("  Extracting Shareware Demo...");
                ZipFile.ExtractToDirectory(zipFile, tempDir);

                App.ConvertJJ2Anims(tempDir, targetPath);

                HashSet<string> usedMusic = new HashSet<string>();
                HashSet<string> usedTilesets = new HashSet<string>();

                App.ConvertJJ2Levels(tempDir, targetPath, usedTilesets, usedMusic);

                usedMusic.Add("boss1");
                usedMusic.Add("boss2");
                usedMusic.Add("bonus2");
                usedMusic.Add("bonus3");
                usedMusic.Add("menu");

                App.ConvertJJ2Music(tempDir, targetPath, usedMusic);

                App.ConvertJJ2Tilesets(tempDir, targetPath, usedTilesets);

                Console.WriteLine("  Done! (Press any key to exit)");
            } catch (Exception ex) {
                Console.WriteLine("  Shareware Demo can't be imported: " + ex);
            } finally {
                // Try to delete downloaded ZIP file
                for (int i = 0; i < 5; i++) {
                    try {
                        File.Delete(zipFile);
                        break;
                    } catch {
                        Thread.Sleep(100);
                    }
                }

                // Try to delete extracted files
                for (int i = 0; i < 5; i++) {
                    try {
                        Directory.Delete(tempDir, true);
                        break;
                    } catch {
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}