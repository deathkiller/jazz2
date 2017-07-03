using System.IO;
using System.IO.Compression;
using System.Text;

namespace Jazz2.Compatibility
{
    public class JJ2Block
    {
        private readonly MemoryStream s;
        private readonly BinaryReader r;
        private readonly byte[] b;

        public JJ2Block(Stream s, int length, int uncompressedLength = 0)
        {
            if (uncompressedLength > 0) {
                length -= 2;
                s.Seek(2, SeekOrigin.Current);
            }

            byte[] buffer = b = new byte[length];

            s.Read(buffer, 0, length);

            MemoryStream ms = new MemoryStream(buffer, false);

            if (uncompressedLength > 0) {
                DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);

                this.s = new MemoryStream(b = new byte[uncompressedLength], true);
                ds.CopyTo(this.s);
                this.s.Position = 0;
            } else {
                this.s = ms;
            }

            r = new BinaryReader(this.s);
        }

        public void SeekTo(int offset)
        {
            s.Position = offset;
        }

        public void DiscardBytes(int length)
        {
            if (s.CanSeek) {
                s.Seek(length, SeekOrigin.Current);
            } else {
                r.ReadBytes(length);
            }
        }

        public byte[] AsByteArray()
        {
            return b;
        }

        public bool ReadBool()
        {
            return r.ReadByte() != 0x00;
        }

        public byte ReadByte()
        {
            return r.ReadByte();
        }

        public short ReadInt16()
        {
            return r.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return r.ReadUInt16();
        }

        public int ReadInt32()
        {
            return r.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return r.ReadUInt32();
        }

        public float ReadFloat()
        {
            return (r.ReadInt32() * 1f / 65536f);
        }

        public byte[] ReadRawBytes(int length)
        {
            return r.ReadBytes(length);
        }

        public byte[] ReadRawBytes(int length, uint offset)
        {
            long old = s.Position;
            s.Position = offset;
            byte[] result = r.ReadBytes(length);
            s.Position = old;
            return result;
        }

        public string ReadString(int length, bool trimToNull)
        {
            byte[] raw = r.ReadBytes(length);
            if (raw.Length == 0) {
                throw new EndOfStreamException();
            }

            if (trimToNull) {
                for (int i = 0; i < length; i++) {
                    if (raw[i] == '\0') {
                        length = i;
                    }
                }
            } else {
                while (length > 0 && (raw[length - 1] == '\0' || raw[length - 1] == ' ')) {
                    length--;
                }
            }

            return Encoding.GetEncoding(1252).GetString(raw, 0, length);
        }
    }
}