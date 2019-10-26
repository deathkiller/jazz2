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
                Stream result = new MemoryStream();
                using (DeflateStream ds = new DeflateStream(result, CompressionLevel.Optimal, true))
                using (Stream stream = GetStream()) {
                    stream.CopyTo(ds);
                }
                result.Position = 0;
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