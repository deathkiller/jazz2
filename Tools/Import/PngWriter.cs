using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using Duality.Drawing;
using Jazz2;

namespace Import
{
    public class PngWriter
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        private int width;
        private int height;
        private ColorRgba[] data;

        public int Width => width;
        public int Height => height;

        public PngWriter(int width, int height)
        {
            this.width = width;
            this.height = height;
            data = new ColorRgba[width * height];
        }

        public PngWriter(Bitmap bitmap)
        {
            width = bitmap.Width;
            height = bitmap.Height;
            data = new ColorRgba[width * height];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Color color = bitmap.GetPixel(x, y);
                    data[x + y * width] = new ColorRgba(color.R, color.G, color.B, color.A);
                }
            }
        }

        public void SetPixel(int x, int y, ColorRgba color)
        {
            data[x + y * width] = color;
        }

        public ColorRgba GetPixel(int x, int y)
        {
            return data[x + y * width];
        }

        public unsafe void Save(string path)
        {
            using (Stream s = File.Open(path, FileMode.Create, FileAccess.Write)) {
                s.Write(Signature, 0, Signature.Length);

                using (MemoryStream ms = new MemoryStream()) {
                    ms.Write(IPAddress.HostToNetworkOrder(width));
                    ms.Write(IPAddress.HostToNetworkOrder(height));
                    ms.WriteByte(8); // Bit Depth

                    ms.WriteByte(2 | 4); // Color Type

                    ms.WriteByte(0); // Compression
                    ms.WriteByte(0); // Filter
                    ms.WriteByte(0); // Interlacing

                    WriteChunk(s, "IHDR", ms);
                }

                using (MemoryStream ms = new MemoryStream()) {
                    ms.WriteByte(0x78); // Compression Method and Flags
                    ms.WriteByte(0x9c); // Flags

                    using (Stream compressed = new DeflateStream(ms, CompressionLevel.Optimal, true)) {
                        fixed (ColorRgba* ptr = data) {
                            int stride = width * 4;
                            byte[] buffer = new byte[stride];

                            for (var y = 0; y < Height; y++) {
                                Marshal.Copy(new IntPtr((byte*)ptr + y * stride), buffer, 0, stride);

                                compressed.WriteByte(0); // Filter - None
                                compressed.Write(buffer, 0, stride); // Row
                            }
                        }
                    }

                    WriteChunk(s, "IDAT", ms);
                }

                WriteChunk(s, "IEND", null);
            }
        }

        private static void WriteChunk(Stream s, string name, MemoryStream input)
        {
            if (name.Length != 4) {
                throw new InvalidDataException();
            }

            byte[] bytes;
            if (input == null) {
                bytes = null;
                s.Write(IPAddress.HostToNetworkOrder((int)0));
            } else {
                bytes = input.ToArray();
                s.Write(IPAddress.HostToNetworkOrder((int)bytes.Length));
            }

            s.WriteByte((byte)name[0]);
            s.WriteByte((byte)name[1]);
            s.WriteByte((byte)name[2]);
            s.WriteByte((byte)name[3]);

            if (bytes != null) {
                s.Write(bytes, 0, bytes.Length);
            }

            s.Write((int)0); // CRC - Not needed by the game
        }
    }
}