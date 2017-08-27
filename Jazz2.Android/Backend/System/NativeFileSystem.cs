using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Duality.IO;
using Environment = Android.OS.Environment;

namespace Duality.Backend.Android
{
    public class NativeFileSystem : IFileSystem
    {
        public readonly string RootPath;

        public NativeFileSystem()
        {
            string packageName = Application.Context.PackageName + "/";

            List<StorageInfo> storages = GetStorageList();
            for (int i = storages.Count - 1; i >= 0; i--) {
                string path = Path.Combine(storages[i].Path, "Android", "Data", packageName);
                if (Directory.Exists(path)) {
                    RootPath = path;
                    break;
                }

                path = Path.Combine(storages[i].Path, packageName);
                if (Directory.Exists(path)) {
                    RootPath = path;
                    break;
                }

                // ToDo: Remove this in future versions
                path = Path.Combine(storages[i].Path, "Download", packageName);
                if (Directory.Exists(path)) {
                    RootPath = path;
                    break;
                }
            }

            if (RootPath == null) {
                throw new DirectoryNotFoundException("Content directory was not found");
            }

            Console.WriteLine("Android Root Path: " + RootPath);
        }

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

            string nativePath = this.GetNativePathFormat(path);
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
            string dualityPath = nativePath.Substring(RootPath.Length);

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

            return RootPath + nativePath;
        }

        public struct StorageInfo
        {
            public readonly string Path;
            public readonly bool IsReadOnly;
            public readonly bool IsRemovable;

            public StorageInfo(string path, bool isReadOnly, bool isRemovable)
            {
                Path = path;
                IsReadOnly = isReadOnly;
                IsRemovable = isRemovable;
            }
        }

        public static List<StorageInfo> GetStorageList()
        {
            List<StorageInfo> storages = new List<StorageInfo>();
            HashSet<string> paths = new HashSet<string>();

            string defaultPathState = Environment.ExternalStorageState;
            bool defaultPathAvailable = (defaultPathState == Environment.MediaMounted || defaultPathState == Environment.MediaMountedReadOnly);
            if (defaultPathAvailable) {
                string defaultPath = Environment.ExternalStorageDirectory.Path;

                paths.Add(defaultPath);
                storages.Add(new StorageInfo(
                    defaultPath,
                    defaultPathState == Environment.MediaMountedReadOnly,
                    Environment.IsExternalStorageRemovable
                ));
            }

            try {
                using (StreamReader r = new StreamReader("/proc/mounts")) {
                    string line;
                    while ((line = r.ReadLine()) != null) {
                        //System.Diagnostics.Debug.WriteLine("Raw: " + line);
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        string mountPoint = parts[1];
                        if (paths.Contains(mountPoint)) {
                            continue;
                        }

                        string[] flags = parts[3].Split(',');
                        bool isReadOnly = flags.Contains("ro");

                        if (paths.Contains(parts[0])) { // Device
                            int idx = storages.IndexOfLast(storage => storage.Path == parts[0]);
                            if (idx != -1) {
                                // Apply Mount Point Redirection
                                paths.Add(mountPoint);
                                storages[idx] = new StorageInfo(
                                    mountPoint,
                                    isReadOnly,
                                    true
                                );
                            }
                        } else if (parts[0].StartsWith("/dev/block/vold", StringComparison.InvariantCulture) // Device (Volume Daemon)
                                    && !mountPoint.StartsWith("/mnt/secure", StringComparison.InvariantCulture)
                                    && !mountPoint.StartsWith("/mnt/asec", StringComparison.InvariantCulture)
                                    && !mountPoint.StartsWith("/mnt/obb", StringComparison.InvariantCulture)
                                    && !mountPoint.StartsWith("/dev/mapper", StringComparison.InvariantCulture)
                                    && parts[2] != "tmpfs") { // File System (RAM Disk)

                            paths.Add(mountPoint);
                            storages.Add(new StorageInfo(
                                mountPoint,
                                isReadOnly,
                                true
                            ));
                        }
                    }
                }
            } catch {
                Console.WriteLine("/proc/mounts failed!");
            }

            //foreach (var storage in storages) {
            //    System.Diagnostics.Debug.WriteLine("Parsed: " + storage.IsReadOnly + " | " + storage.IsRemovable + " | " + storage.Path);
            //}

            return storages;
        }
    }
}