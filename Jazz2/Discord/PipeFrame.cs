using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jazz2.Discord
{
    internal struct PipeFrame
    {
        public static readonly int MaxFrameSize = (16 * 1024);

        public uint Opcode;
        public byte[] Data;

        public uint Length => (uint)Data.Length;

        public string Message
        {
            get { return Encoding.UTF8.GetString(Data); }
            set { Data = Encoding.UTF8.GetBytes(value); }
        }

        public PipeFrame(uint opcode, string content)
        {
            Opcode = opcode;
            Data = null;

            Message = content;
        }
        
        public bool ReadStream(Stream stream)
        {
            uint opcode;
            if (!TryReadUInt32(stream, out opcode)) {
                return false;
            }

            uint length;
            if (!TryReadUInt32(stream, out length)) {
                return false;
            }

            uint readsRemaining = length;

            using (MemoryStream ms = new MemoryStream()) {
                byte[] buffer = new byte[Min(2048, length)];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, Min(buffer.Length, readsRemaining))) > 0) {
                    readsRemaining -= length;
                    ms.Write(buffer, 0, bytesRead);
                }

                byte[] result = ms.ToArray();
                if (result.LongLength != length) {
                    return false;
                }

                Opcode = opcode;
                Data = result;
                return true;
            }
        }

        public void WriteStream(Stream stream)
        {
            byte[] opcode = BitConverter.GetBytes((uint)Opcode);
            byte[] length = BitConverter.GetBytes(Length);

            byte[] buffer = new byte[opcode.Length + length.Length + Data.Length];
            opcode.CopyTo(buffer, 0);
            length.CopyTo(buffer, opcode.Length);
            Data.CopyTo(buffer, opcode.Length + length.Length);

            stream.Write(buffer, 0, buffer.Length);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int Min(int a, uint b)
        {
            if (b >= a) {
                return a;
            } else {
                return (int)b;
            }
        }

        private static bool TryReadUInt32(Stream stream, out uint value)
        {
            byte[] bytes = new byte[4];
            int count = stream.Read(bytes, 0, bytes.Length);

            if (count != 4) {
                value = default(uint);
                return false;
            }

            value = BitConverter.ToUInt32(bytes, 0);
            return true;
        }
    }
}