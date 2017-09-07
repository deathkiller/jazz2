using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using Jazz2;

namespace Import.Downloaders
{
    public static class DemoDownloader
    {
        private const string Url = "http://deat.tk/public/jazz2/demo.zip";

        public static void Run(string targetPath)
        {
            Log.Write(LogType.Info, "Downloading Shareware Demo (7 MB)...");
            Log.PushIndent();

            string zipFile = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            WebClient client = new WebClient();
            client.DownloadFile(Url, zipFile);

            string tempDir = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            try {
                Directory.CreateDirectory(tempDir);

                Log.Write(LogType.Info, "Extracting files...");
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

                App.ConvertJJ2Music(tempDir, targetPath, usedMusic, false);

                App.ConvertJJ2Tilesets(tempDir, targetPath, usedTilesets, false);
            } catch (Exception ex) {
                Log.Write(LogType.Error, ex.ToString());
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

            Log.PopIndent();
        }
    }
}