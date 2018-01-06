using System.IO;
using System.IO.Compression;
using System.Text;

namespace Jazz2.Compatibility
{
    public class JJ2Block
    {
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        private readonly byte[] buffer;

        public JJ2Block(Stream s, int length, int uncompressedLength = 0)
        {
            if (uncompressedLength > 0) {
                length -= 2;
                s.Seek(2, SeekOrigin.Current);
            }

            buffer = new byte[length];

            s.Read(buffer, 0, length);

            MemoryStream ms = new MemoryStream(buffer, false);

            if (uncompressedLength > 0) {
                DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);

                stream = new MemoryStream(buffer = new byte[uncompressedLength], true);
                ds.CopyTo(stream);
                stream.Position = 0;
            } else {
                stream = ms;
            }

            reader = new BinaryReader(stream);
        }

        public void SeekTo(int offset)
        {
            stream.Position = offset;
        }

        public void DiscardBytes(int length)
        {
            if (stream.CanSeek) {
                stream.Seek(length, SeekOrigin.Current);
            } else {
                reader.ReadBytes(length);
            }
        }

        public byte[] AsByteArray()
        {
            return buffer;
        }

        public bool ReadBool()
        {
            return reader.ReadByte() != 0x00;
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public short ReadInt16()
        {
            return reader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return reader.ReadUInt16();
        }

        public int ReadInt32()
        {
            return reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }


        public int ReadUint7bitEncoded()
        {
            int result = 0;

            while (true) {
                byte current = reader.ReadByte();
                result |= (current & 0x7F);
                if (current >= 0x80) {
                    result <<= 7;
                } else {
                    break;
                }
            }

            return result;
        }

        public float ReadFloat()
        {
            return reader.ReadSingle();
        }

        public float ReadFloatEncoded()
        {
            return (reader.ReadInt32() * 1f / 65536f);
        }

        public byte[] ReadRawBytes(int length)
        {
            return reader.ReadBytes(length);
        }

        public byte[] ReadRawBytes(int length, uint offset)
        {
            long old = stream.Position;
            stream.Position = offset;
            byte[] result = reader.ReadBytes(length);
            stream.Position = old;
            return result;
        }

        public string ReadString(int length, bool trimToNull)
        {
            byte[] raw = reader.ReadBytes(length);
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

            // Use Windows-1252 (English & Western languages) encoding
            return Encoding.GetEncoding(1252).GetString(raw, 0, length);
        }
    }
}