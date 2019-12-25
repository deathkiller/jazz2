using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using Duality;

namespace Import.Downloaders
{
    public static class DemoDownloader
    {
        private const string Url = "http://deat.tk/jazz2/misc/shareware-demo.zip";

        public static bool Run(string targetPath, string exePath)
        {
            string zipFile = Path.GetFileName(Url);
            bool zipExists = File.Exists(zipFile);

            if (!zipExists) {
                Log.Write(LogType.Info, "Downloading Shareware Demo (7 MB)...");
                Log.PushIndent();

                zipFile = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

                try {
                    using (WebClient client = new WebClient()) {
                        client.DownloadFile(Url, zipFile);
                    }
                } catch (Exception ex) {
                    Log.Write(LogType.Error, "Failed to download required files: " + ex.ToString());
                    Log.PopIndent();
                    return false;
                }
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            try {
                Directory.CreateDirectory(tempDir);

                Log.Write(LogType.Info, "Extracting files...");
                ZipFile.ExtractToDirectory(zipFile, tempDir);

                HashSet<string> usedMusic = new HashSet<string>();
                HashSet<string> usedTilesets = new HashSet<string>();

                usedMusic.Add("boss1");
                usedMusic.Add("boss2");
                usedMusic.Add("bonus2");
                usedMusic.Add("bonus3");
                usedMusic.Add("menu");

                App.ConvertJJ2Anims(tempDir, targetPath, exePath);
                App.ConvertJJ2Levels(tempDir, targetPath, usedTilesets, usedMusic);
                App.ConvertJJ2Cinematics(tempDir, targetPath, usedMusic, false);
                App.ConvertJJ2Music(tempDir, targetPath, usedMusic, false);
                App.ConvertJJ2Tilesets(tempDir, targetPath, usedTilesets, false);
            } catch (Exception ex) {
                Log.Write(LogType.Error, ex.ToString());
                Log.PopIndent();
                return false;
            } finally {
                if (!zipExists) {
                    // Try to delete downloaded ZIP file
                    FileSystemUtils.FileTryDelete(zipFile);
                }

                // Try to delete extracted files
                FileSystemUtils.DirectoryTryDelete(tempDir, true);
            }

            Log.PopIndent();
            return true;
        }
    }
}