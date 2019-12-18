using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Duality;
using Jazz2.Compatibility;

namespace Import.Downloaders
{
    public static class JJ2PlusDownloader
    {
        private const string Url = "http://deat.tk/public/jazz2/jj2plus.zip";

        public static bool Run(string targetPath)
        {
            targetPath = Path.Combine(targetPath, "Content", "Animations");
            string zipFile = Path.GetFileName(Url);
            bool zipExists = File.Exists(zipFile);

            if (!zipExists) {
                Log.Write(LogType.Info, "Downloading JJ2+ (3 MB)...");
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

                string plusPath = Path.Combine(tempDir, "Plus.j2a");
                if (FileSystemUtils.FileResolveCaseInsensitive(ref plusPath)) {
                    JJ2Anims.Convert(plusPath, targetPath, true);
                }
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