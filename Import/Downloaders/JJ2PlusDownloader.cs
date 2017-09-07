using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using Jazz2;
using Jazz2.Compatibility;

namespace Import.Downloaders
{
    public static class JJ2PlusDownloader
    {
        // Latest version with installer, it can't be used
        //private const string Url = "https://get.jj2.plus/";

        // Older version without installer
        private const string Url = "https://www.jazz2online.com/jj2plus/old/plus-2016-12-06.zip";

        public static void Run(string targetPath)
        {
            targetPath = Path.Combine(targetPath, "Content", "Animations");

            Log.Write(LogType.Info, "Downloading JJ2+ (3 MB)...");
            Log.PushIndent();

            string zipFile = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            try {
                WebClient client = new WebClient();
                client.DownloadFile(Url, zipFile);
            } catch (Exception ex) {
                Log.Write(LogType.Error, ex.ToString());
                return;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "Jazz2-" + Guid.NewGuid());

            try {
                Directory.CreateDirectory(tempDir);

                Log.Write(LogType.Info, "Extracting files...");

                ZipFile.ExtractToDirectory(zipFile, tempDir);

                // ToDo: Extract plus_install.exe somehow to download latest version

                string plusPath = Path.Combine(tempDir, "Plus.j2a");
                if (Utils.FileResolveCaseInsensitive(ref plusPath)) {
                    JJ2Anims.Convert(plusPath, targetPath, true);
                }
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