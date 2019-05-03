using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Jazz2;
using Jazz2.Compatibility;

namespace Import.Downloaders
{
    public static class JJ2PlusDownloader
    {
        // Latest version with installer, it can't be used
        //private const string Url = "https://get.jj2.plus/";

        // Older version without installer
        private const string Url = "http://deat.tk/public/jazz2/jj2plus.zip";

        public static bool Run(string targetPath)
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
                Log.PopIndent();
                return false;
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
                Log.PopIndent();
                return false;
            } finally {
                // Try to delete downloaded ZIP file
                Utils.FileTryDelete(zipFile);

                // Try to delete extracted files
                Utils.DirectoryTryDelete(tempDir, true);
            }

            Log.PopIndent();
            return true;
        }
    }
}