using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Duality;
using Duality.IO;

namespace Jazz2.Storage.Content
{
    public class CompressedContent : IFileSystem
    {
        [Flags]
        public enum ResourceFlags : byte
        {
            None = 0,

            HasChildren = 1 << 0,
            HasResource = 1 << 1,
            Compressed = 1 << 2,
            External = 1 << 3
        }

        public delegate void PostprocessFileCallback(ContentTree.Node node, ref ResourceFlags flags, ref Stream overrideStream);

        private readonly string path;
        private readonly ContentTree tree;

        public ContentTree Tree => this.tree;

        public CompressedContent(string path)
        {
            this.path = path;

            this.tree = ReadContentTree();
        }

        private ContentTree ReadContentTree()
        {
            Stream stream;
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Game ||
                DualityApp.ExecContext == DualityApp.ExecutionContext.Server) {
                stream = DualityApp.SystemBackend.FileSystem.OpenFile(path, FileAccessMode.Read);
            } else {
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            using (stream)
            using (BinaryReader r = new BinaryReader(stream, Encoding.UTF8, true)) {
                uint signature = r.ReadUInt32();
                if (signature != 0x5A616544u) {
                    throw new FileLoadException("Invalid CompressedContent file signature");
                }
                byte version = r.ReadByte();
                if (version > 1) {
                    throw new FileLoadException("Unknown CompressedContent version");
                }

                long dataOffset = r.ReadUInt32() + 4 + 1 + 4;

                ContentTree tree = new ContentTree();
                ReadContentTreeSection(tree.Root, r, dataOffset);
                return tree;
            }
        }

        private void ReadContentTreeSection(ContentTree.Node node, BinaryReader r, long dataOffset, string fullPath = null)
        {
            ResourceFlags flags = (ResourceFlags)r.ReadByte();

            string name = r.ReadAsciiString();
            if (!string.IsNullOrEmpty(fullPath)) {
                fullPath += PathOp.DirectorySeparatorChar;
            }
            fullPath += name;

            ushort childrenCount;
            if ((flags & ResourceFlags.HasChildren) != 0) {
                childrenCount = r.ReadUInt16();
            } else {
                childrenCount = 0;
            }

            FileResourceSource source = null;
            if ((flags & ResourceFlags.HasResource) != 0) {
                string path;
                long offset, size;

                if ((flags & ResourceFlags.External) == 0) {
                    path = this.path;
                    offset = r.ReadUInt32();
                    size = r.ReadUInt32();

                    offset += dataOffset;
                } else {
                    path = r.ReadAsciiString();
                    offset = r.ReadUInt32();
                    size = r.ReadUInt32();
                }

                source = new FileResourceSource(path, offset, size, (flags & ResourceFlags.Compressed) != 0);
            }

            if (!string.IsNullOrEmpty(name)) {
                node = node.TryAdd(name);
                node.Source = source;
            }

            for (uint i = 0; i < childrenCount; i++) {
                ReadContentTreeSection(node, r, dataOffset, fullPath);
            }
        }

        public static void Create(string path, ContentTree tree, PostprocessFileCallback postprocessFileCallback = null)
        {
            using (MemoryStream tableStream = new MemoryStream())
            using (MemoryStream dataStream = new MemoryStream()) {
                WriteContentTreeSection(tree.Root, tableStream, dataStream, postprocessFileCallback);

                Stream stream;
                if (DualityApp.ExecContext == DualityApp.ExecutionContext.Game ||
                    DualityApp.ExecContext == DualityApp.ExecutionContext.Server) {
                    stream = DualityApp.SystemBackend.FileSystem.CreateFile(path);
                } else {
                    stream = File.Create(path);
                }

                using (stream) {
                    using (BinaryWriter w = new BinaryWriter(stream, Encoding.UTF8, true)) {
                        w.Write((uint)0x5A616544u); // Signature
                        w.Write((byte)1);           // Version
                        w.Write((uint)tableStream.Position);
                    }

                    tableStream.Position = 0;
                    tableStream.CopyTo(stream);

                    dataStream.Position = 0;
                    dataStream.CopyTo(stream);
                }
            }
        }

        private static void WriteContentTreeSection(ContentTree.Node node, Stream tableStream, Stream dataStream, PostprocessFileCallback postprocessFileCallback)
        {
            using (BinaryWriter w = new BinaryWriter(tableStream, Encoding.UTF8, true)) {
                ResourceFlags flags = 0;

                if (node.Children.Count > 0) {
                    flags |= ResourceFlags.HasChildren;
                }

                if (node.Source != null) {
                    flags |= ResourceFlags.HasResource;
                    flags &= ~ResourceFlags.External;
                }

                Stream overrideStream = null;
                if (postprocessFileCallback != null) {
                    postprocessFileCallback(node, ref flags, ref overrideStream);
                }

                try {
                    w.Write((byte)flags);
                    w.WriteAsciiString(node.Name);

                    if ((flags & ResourceFlags.HasChildren) != 0) {
                        w.Write((ushort)node.Children.Count);
                    }

                    if ((flags & ResourceFlags.HasResource) != 0) {
                        if ((flags & ResourceFlags.External) != 0) {
                            if (overrideStream != null) {
                                throw new InvalidOperationException("Cannot use overrideStream with ResourceFlags.External");
                            }

                            FileResourceSource source = node.Source as FileResourceSource;
                            if (source == null) {
                                throw new InvalidOperationException("Specified resource must be saved as file");
                            }

                            w.WriteAsciiString(source.Path);
                            w.Write((uint)source.Offset);
                            w.Write((uint)source.Size);
                        } else {
                            long offset = dataStream.Position;

                            if (overrideStream != null) {
                                if ((flags & ResourceFlags.Compressed) != 0) {
                                    using (DeflateStream ds = new DeflateStream(dataStream, CompressionLevel.Optimal, true)) {
                                        overrideStream.CopyTo(ds);
                                    }
                                } else {
                                    overrideStream.CopyTo(dataStream);
                                }
                            } else {
                                using (Stream stream = (flags & ResourceFlags.Compressed) != 0
                                        ? node.Source.GetCompressedStream()
                                        : node.Source.GetUncompressedStream()) {
                                    stream.CopyTo(dataStream);
                                }
                            }

                            long size = (dataStream.Position - offset);

                            w.Write((uint)offset);
                            w.Write((uint)size);
                        }
                    }
                } finally {
                    if (overrideStream != null) {
                        overrideStream.Dispose();
                    }
                }
            }

            foreach (ContentTree.Node children in node.Children) {
                WriteContentTreeSection(children, tableStream, dataStream, postprocessFileCallback);
            }
        }

        string IFileSystem.GetFullPath(string path)
        {
            // ToDo
            return path;
        }

        IEnumerable<string> IFileSystem.GetFiles(string path, bool recursive)
        {
            path = path.Replace(PathOp.AltDirectorySeparatorChar, PathOp.DirectorySeparatorChar);

            if (recursive) {
                string parentPath;
                int idx = path.LastIndexOf(PathOp.DirectorySeparatorChar);
                if (idx == -1) {
                    parentPath = string.Empty;
                } else {
                    parentPath = path.Substring(0, idx);
                }

                Queue<Tuple<ContentTree.Node, string>> stack = new Queue<Tuple<ContentTree.Node, string>>();
                stack.Enqueue(Tuple.Create(this.tree[path], parentPath));

                do {
                    Tuple<ContentTree.Node, string> parent = stack.Dequeue();
                    string fullPath = PathOp.Combine(parent.Item2, parent.Item1.Name);

                    if (parent.Item1.Source != null) {
                        yield return fullPath;
                    }

                    foreach (ContentTree.Node node in parent.Item1) {
                        stack.Enqueue(Tuple.Create(node, fullPath));
                    }
                } while (stack.Count > 0);
            } else {
                foreach (ContentTree.Node child in this.tree[path]) {
                    if (child.Source != null) {
                        yield return PathOp.Combine(path, child.Name);
                    }
                }
            }
        }

        IEnumerable<string> IFileSystem.GetDirectories(string path, bool recursive)
        {
            path = path.Replace(PathOp.AltDirectorySeparatorChar, PathOp.DirectorySeparatorChar);

            if (recursive) {
                string parentPath;
                int idx = path.LastIndexOf(PathOp.DirectorySeparatorChar);
                if (idx == -1) {
                    parentPath = string.Empty;
                } else {
                    parentPath = path.Substring(0, idx);
                }

                Queue<Tuple<ContentTree.Node, string>> stack = new Queue<Tuple<ContentTree.Node, string>>();
                stack.Enqueue(Tuple.Create(this.tree[path], parentPath));

                do {
                    Tuple<ContentTree.Node, string> parent = stack.Dequeue();
                    string fullPath = PathOp.Combine(parent.Item2, parent.Item1.Name);

                    if (parent.Item1.Source == null) {
                        yield return fullPath;
                    }

                    foreach (ContentTree.Node node in parent.Item1) {
                        stack.Enqueue(Tuple.Create(node, fullPath));
                    }
                } while (stack.Count > 0);
            } else {
                foreach (ContentTree.Node child in this.tree[path]) {
                    if (child.Source == null) {
                        yield return PathOp.Combine(path, child.Name);
                    }
                }
            }
        }

        bool IFileSystem.FileExists(string path)
        {
            path = path.Replace(PathOp.AltDirectorySeparatorChar, PathOp.DirectorySeparatorChar);

            ContentTree.Node node = tree[path];
            return (node != null && node.Source != null);
        }

        bool IFileSystem.DirectoryExists(string path)
        {
            path = path.Replace(PathOp.AltDirectorySeparatorChar, PathOp.DirectorySeparatorChar);

            ContentTree.Node node = tree[path];
            return (node != null && node.Source == null);
        }

        Stream IFileSystem.CreateFile(string path)
        {
            throw new NotSupportedException();
        }

        Stream IFileSystem.OpenFile(string path, FileAccessMode mode)
        {
            path = path.Replace(PathOp.AltDirectorySeparatorChar, PathOp.DirectorySeparatorChar);

            ContentTree.Node node = tree[path];
            if (node == null) {
                throw new FileNotFoundException("File \"" + path + "\" was not found in CompressedContent");
            }
            if (node.Source == null) {
                throw new NotSupportedException();
            }
            return node.Source.GetUncompressedStream();
        }

        void IFileSystem.DeleteFile(string path)
        {
            throw new NotSupportedException();
        }

        void IFileSystem.CreateDirectory(string path)
        {
            throw new NotSupportedException();
        }

        void IFileSystem.DeleteDirectory(string path)
        {
            throw new NotSupportedException();
        }
    }
}