using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duality.IO;
using Jazz2.Wasm;

namespace Duality.Backend.DotNetFramework
{
    public class NativeFileSystem : IFileSystem
    {
        string IFileSystem.GetFullPath(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            nativePath = Path.GetFullPath(nativePath);
            return this.GetDualityPathFormat(nativePath);
        }

        IEnumerable<string> IFileSystem.GetFiles(string path, bool recursive)
        {
            string nativePath = this.GetNativePathFormat(path);
            return Directory.EnumerateFiles(
                nativePath,
                "*",
                recursive ?
                    SearchOption.AllDirectories :
                    SearchOption.TopDirectoryOnly)
                .Select(this.GetDualityPathFormat);
        }
        IEnumerable<string> IFileSystem.GetDirectories(string path, bool recursive)
        {
            string nativePath = this.GetNativePathFormat(path);
            return Directory.EnumerateDirectories(
                nativePath,
                "*",
                recursive ?
                    SearchOption.AllDirectories :
                    SearchOption.TopDirectoryOnly)
                .Select(this.GetDualityPathFormat);
        }

        bool IFileSystem.FileExists(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            return File.Exists(nativePath);
        }
        bool IFileSystem.DirectoryExists(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            return Directory.Exists(nativePath);
        }

        Stream IFileSystem.CreateFile(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            return File.Open(nativePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
        Stream IFileSystem.OpenFile(string path, FileAccessMode mode)
        {
            if (mode == FileAccessMode.None) throw new ArgumentException("Can't open a file stream without any access capabilities.");

            string nativePath = this.GetNativePathFormat(path);

            FileAccess access;
            switch (mode) {
                default:
                case FileAccessMode.Read:
                    access = FileAccess.Read;
                    break;
                case FileAccessMode.Write:
                    access = FileAccess.Write;
                    break;
                case FileAccessMode.ReadWrite:
                    access = FileAccess.ReadWrite;
                    break;
            }

            return File.Open(nativePath, FileMode.Open, access, FileShare.ReadWrite);
        }
        void IFileSystem.DeleteFile(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            File.Delete(nativePath);
        }

        void IFileSystem.CreateDirectory(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            Directory.CreateDirectory(nativePath);
        }
        void IFileSystem.DeleteDirectory(string path)
        {
            string nativePath = this.GetNativePathFormat(path);
            Directory.Delete(nativePath, true);
        }

        public string GetDualityPathFormat(string nativePath)
        {
            string dualityPath = nativePath;

            if (Path.DirectorySeparatorChar != PathOp.DirectorySeparatorChar)
                dualityPath = dualityPath.Replace(
                    Path.DirectorySeparatorChar,
                    PathOp.DirectorySeparatorChar);

            if (Path.AltDirectorySeparatorChar != PathOp.DirectorySeparatorChar)
                dualityPath = dualityPath.Replace(
                    Path.AltDirectorySeparatorChar,
                    PathOp.DirectorySeparatorChar);

            return dualityPath;
        }
        public string GetNativePathFormat(string dualityPath)
        {
            string nativePath = dualityPath;

            if (PathOp.DirectorySeparatorChar != Path.DirectorySeparatorChar)
                nativePath = nativePath.Replace(
                    PathOp.DirectorySeparatorChar,
                    Path.DirectorySeparatorChar);

            if (PathOp.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
                nativePath = nativePath.Replace(
                    PathOp.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);

            return nativePath;
        }

        public static async Task<bool> DownloadToCache(string nativePath, Action<float> callback)
        {
            const int DefaultCopyBufferSize = 327680;

            if (File.Exists(nativePath)) {
                return true;
            }

            Console.WriteLine("Downloading \"" + nativePath + "\"...");

            string baseAddress = WasmResourceLoader.GetBaseAddress();

            (Stream httpStream, int size) = await WasmResourceLoader.LoadAsync(nativePath, baseAddress);
            if (httpStream == null) {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(nativePath));

            using (Stream s = File.Open(nativePath, FileMode.Create, FileAccess.Write)) {
                //httpStream.CopyTo(s);

                byte[] buffer = new byte[DefaultCopyBufferSize];
                int bytesRead;

                if (size > DefaultCopyBufferSize) {
                    int totalRead = 0;
                    while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false)) != 0) {
                        await s.WriteAsync(buffer, 0, bytesRead, CancellationToken.None).ConfigureAwait(false);

                        totalRead += bytesRead;
                        callback((float)totalRead / size);
                    }
                } else {
                    while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false)) != 0) {
                        await s.WriteAsync(buffer, 0, bytesRead, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }

            return true;
        }
    }
}
