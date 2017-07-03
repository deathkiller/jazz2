using System;
using System.IO;

namespace Jazz2.Compatibility
{
    public class JJ2DataFile // .j2d
    {
        public static JJ2DataFile Open(string path, bool strictParser)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader r = new BinaryReader(s)) {
                JJ2DataFile j2d = new JJ2DataFile();

                uint id = r.ReadUInt32();
                if (id != 0x42494C50) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                uint magic = r.ReadUInt32();
                if (magic != 0xbebaadde) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                uint version = r.ReadUInt32();

                uint recordedSize = r.ReadUInt32();
                if (strictParser && s.Length != recordedSize) {
                    throw new InvalidOperationException("Unexpected file size");
                }

                uint recordedCRC = r.ReadUInt32();
                int headerBlockPackedSize = r.ReadInt32();
                int headerBlockUnpackedSize = r.ReadInt32();

                JJ2Block headerBlock = new JJ2Block(s, headerBlockPackedSize, headerBlockUnpackedSize);

                try {
                    while (true) {
                        string name = headerBlock.ReadString(32, true);

                        uint type = headerBlock.ReadUInt32();
                        uint offset = headerBlock.ReadUInt32();
                        uint fileCRC = headerBlock.ReadUInt32();
                        int filePackedSize = headerBlock.ReadInt32();
                        int fileUnpackedSize = headerBlock.ReadInt32();

                        //Console.WriteLine(name + " | " + type.ToString("X") + " | " + fileUnpackedSize + " | " + offset);
                    }
                } catch (EndOfStreamException) {
                    // End of file list
                }

                // ToDo: Extract files

                return j2d;
            }
        }
    }
}