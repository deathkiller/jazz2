using System;
using System.IO;

namespace Jazz2.Storage.Content
{
    public class FileResourceSource : ResourceSource
    {
        public class SourceStream : Stream
        {
            private FileStream stream;
            private readonly long offset, size;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => this.size;

            public override long Position
            {
                get
                {
                    return this.stream.Position - this.offset;
                }
                set
                {
                    if (value > this.size) {
                        throw new ArgumentException("Position is out of range for specified section");
                    }

                    this.stream.Position = value + this.offset;
                }
            }

            public SourceStream(FileResourceSource source)
            {
                this.offset = source.offset;
                this.size = source.size;
                this.stream = new FileStream(source.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (this.stream.Length < this.offset + this.size) {
                    this.stream.Dispose();
                    throw new FileLoadException("File is not large enough to contain specified section");
                }

                this.stream.Position = this.offset;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) {
                    if (this.stream != null) {
                        this.stream.Dispose();
                        this.stream = null;
                    }
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                count = (int)Math.Min(count, this.size - this.Position);
                if (count > 0) {
                    return this.stream.Read(buffer, offset, count);
                } else {
                    return 0;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long position;
                switch (origin) {
                    case SeekOrigin.Begin: position = offset; break;
                    case SeekOrigin.Current: position = this.Position + offset; break;
                    case SeekOrigin.End: position = this.size + offset; break;

                    default: throw new ArgumentException("Invalid seek origin value", nameof(origin));
                }

                if (position < 0 || position > this.size) {
                    throw new IOException("Seek position is out of range for specified section");
                }

                this.stream.Position = this.offset + position;
                return position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private readonly string path;
        private readonly long offset, size;

        public string Path => this.path;
        public long Offset => this.offset;
        public override long Size => this.size;

        public FileResourceSource(string path, long offset, long size, bool compressed) : base(compressed)
        {
            this.path = path;
            this.offset = offset;
            this.size = size;
        }

        public override Stream GetStream()
        {
            return new SourceStream(this);
        }
    }
}