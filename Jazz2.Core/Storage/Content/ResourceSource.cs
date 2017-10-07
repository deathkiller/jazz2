using System.IO;
using System.IO.Compression;

namespace Jazz2.Storage.Content
{
    public abstract class ResourceSource
    {
        private readonly bool compressed;

        public bool Compressed => this.compressed;

        public abstract long Size { get; }

        protected ResourceSource(bool compressed)
        {
            this.compressed = compressed;
        }

        public abstract Stream GetStream();

        public Stream GetCompressedStream()
        {
            if (this.compressed) {
                return GetStream();
            } else {
                Stream result;
                using (MemoryStream dst = new MemoryStream()) {
                    using (DeflateStream gzip = new DeflateStream(dst, CompressionLevel.Optimal))
                    using (Stream src = GetStream()) {
                        src.CopyTo(gzip);
                    }
                    result = new MemoryStream(dst.ToArray());
                }
                return result;
            }
        }

        public Stream GetUncompressedStream()
        {
            if (this.compressed) {
                return new DeflateStream(GetStream(), CompressionMode.Decompress, false);
            } else {
                return GetStream();
            }
        }
    }
}