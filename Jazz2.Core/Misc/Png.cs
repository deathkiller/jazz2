using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Duality;
using Duality.Drawing;

namespace Jazz2
{
    public class Png
    {
        [Flags]
        protected enum PngColorType { Indexed = 1, Color = 2, Alpha = 4 }

        protected enum PngFilter { None, Sub, Up, Average, Paeth }

        protected static readonly byte[] Signature = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };


        public int Width;
        public int Height;
        public ColorRgba[] Palette;
        public byte[] Data;
        public Dictionary<string, string> EmbeddedData;

        public Png(byte[] data, int width, int height, ColorRgba[] palette = null, Dictionary<string, string> embeddedData = null)
        {
            int expectLength = (width * height);
            if (palette == null) {
                expectLength *= 4;
            }
            if (data.Length != expectLength) {
                throw new InvalidDataException("Input data does not match expected length");
            }

            Width = width;
            Height = height;

            Palette = palette;
            Data = data;

            if (embeddedData != null) {
                EmbeddedData = embeddedData;
            } else {
                EmbeddedData = new Dictionary<string, string>();
            }
        }

        public Png(Stream s)
        {
            byte[] internalBuffer = new byte[8];

            // Check header signature
            s.Read(internalBuffer, 0, Signature.Length);

            for (int i = 0; i < Signature.Length; i++) {
                if (internalBuffer[i] != Signature[i]) {
                    throw new InvalidDataException("Invalid PNG file - header signature mismatch");
                }
            }

            // Load image
            bool headerParsed = false;
            bool isPaletted = false;
            bool is24Bit = false;
            RawList<byte> data = new RawList<byte>();

            EmbeddedData = new Dictionary<string, string>();

            while (true) {
                int length = IPAddress.NetworkToHostOrder(s.ReadInt32(ref internalBuffer));
                s.Read(internalBuffer, 0, 4);
                string type = Encoding.ASCII.GetString(internalBuffer, 0, 4);

                if (!headerParsed && type != "IHDR") {
                    throw new InvalidDataException("Invalid PNG file - header does not appear first");
                }

                int blockEndPosition = (int)s.Position + length;

                switch (type) {
                    case "IHDR": {
                        if (headerParsed) {
                            throw new InvalidDataException("Invalid PNG file - duplicate header");
                        }

                        Width = IPAddress.NetworkToHostOrder(s.ReadInt32(ref internalBuffer));
                        Height = IPAddress.NetworkToHostOrder(s.ReadInt32(ref internalBuffer));

                        byte bitDepth = s.ReadUInt8(ref internalBuffer);
                        PngColorType colorType = (PngColorType)s.ReadUInt8(ref internalBuffer);
                        isPaletted = IsPaletted(bitDepth, colorType);
                        is24Bit = (colorType == PngColorType.Color);

                        var dataLength = Width * Height;
                        if (!isPaletted) {
                            dataLength *= 4;
                        }

                        Data = new byte[dataLength];

                        byte compression = s.ReadUInt8(ref internalBuffer);
                        /*byte filter = */s.ReadUInt8(ref internalBuffer);
                        byte interlace = s.ReadUInt8(ref internalBuffer);

                        if (compression != 0) {
                            throw new InvalidDataException("Compression method (" + compression + ") not supported");
                        }
                        if (interlace != 0) {
                            throw new InvalidDataException("Interlacing (" + interlace + ") not supported");
                        }

                        headerParsed = true;
                        break;
                    }

                    case "PLTE": {
                        Palette = new ColorRgba[256];
                        for (int i = 0; i < length / 3; i++) {
                            byte r = s.ReadUInt8(ref internalBuffer);
                            byte g = s.ReadUInt8(ref internalBuffer);
                            byte b = s.ReadUInt8(ref internalBuffer);
                            Palette[i] = new ColorRgba(r, g, b);
                        }
                        break;
                    }

                    case "tRNS": {
                        if (Palette == null) {
                            throw new InvalidDataException("Non-palette indexed images are not supported");
                        }
                        for (var i = 0; i < length; i++) {
                            Palette[i].A = s.ReadUInt8(ref internalBuffer);
                        }
                        break;
                    }

                    case "IDAT": {
                        int newLength = data.Count + length;
                        data.Count = newLength;
                        s.Read(data.Data, newLength - length, length);
                        break;
                    }

                    case "tEXt": {
                        byte[] content = new byte[length];
                        s.Read(content, 0, length);

                        for (int i = 0; i < length; i++) {
                            if (content[i] == 0) {
                                string key = Encoding.ASCII.GetString(content, 0, i);
                                string value = Encoding.ASCII.GetString(content, i + 1, length - (i + 1));
                                EmbeddedData.Add(key, value);
                                break;
                            }
                        }
                        break;
                    }

                    case "IEND": {
                        using (var ms = new MemoryStream(data.Data)) {
                            ms.Position += 2;

                            using (var ds = new DeflateStream(ms, CompressionMode.Decompress, true)) {
                                int pxStride = (isPaletted ? 1 : (is24Bit ? 3 : 4));
                                int srcStride = Width * pxStride;
                                int dstStride = Width * (isPaletted ? 1 : 4);

                                byte[] buffer = new byte[srcStride];
                                byte[] bufferPrev = new byte[srcStride];

                                for (var y = 0; y < Height; y++) {
                                    // Read filter
                                    PngFilter filter = (PngFilter)ds.ReadUInt8(ref internalBuffer);

                                    // Read data
                                    ds.Read(buffer, 0, srcStride);

                                    for (var i = 0; i < srcStride; i++) {
                                        if (i < pxStride) {
                                            buffer[i] = UnapplyFilter(filter, buffer[i], 0, bufferPrev[i], 0);
                                        } else {
                                            buffer[i] = UnapplyFilter(filter, buffer[i], buffer[i - pxStride], bufferPrev[i], bufferPrev[i - pxStride]);
                                        }
                                    }

                                    if (is24Bit) {
                                        for (var i = 0; i < buffer.Length / 3; i++) {
                                            Buffer.BlockCopy(buffer, 3 * i, Data, y * dstStride + 4 * i, 3);
                                            Data[y * dstStride + 4 * i + 3] = 255;
                                        }
                                    } else {
                                        Buffer.BlockCopy(buffer, 0, Data, y * dstStride, srcStride);
                                    }

                                    bufferPrev = buffer;
                                }
                            }
                        }
                        return;
                    }

                    default: {
                        //Console.WriteLine("Unknown PNG section: " + type);
                        s.Position += length;
                        break;
                    }
                }

                if (s.Position != blockEndPosition) {
                    throw new InvalidDataException("Block " + type + " has incorrect length");
                }

                // Skip CRC
                s.Position += 4;
            }
        }

        public PixelData GetPixelData()
        {
            ColorRgba[] colors = new ColorRgba[Data.Length / 4];
            for (int i = 0, j = 0; i < Data.Length; i += 4, j += 1) {
                colors[j] = new ColorRgba(Data[i], Data[i + 1], Data[i + 2], Data[i + 3]);
            }

            PixelData pixelData = new PixelData();
            pixelData.SetData(colors, Width, Height);
            pixelData.ColorTransparentPixels();
            return pixelData;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static byte UnapplyFilter(PngFilter filter, byte x, byte a, byte b, byte c)
        {
            switch (filter) {
                case PngFilter.None: return x;
                case PngFilter.Sub: return (byte)(x + a);
                case PngFilter.Up: return (byte)(x + b);
                case PngFilter.Average: return (byte)(x + (a + b) / 2);
                case PngFilter.Paeth: return (byte)(x + UnapplyFilterPaeth(a, b, c));
                default: throw new InvalidOperationException("Unsupported filter (" + (int)filter + ") specified");
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static byte UnapplyFilterPaeth(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            return (pa <= pb && pa <= pc) ? a : (pb <= pc) ? b : c;
        }

        private static bool IsPaletted(byte bitDepth, PngColorType colorType)
        {
            if (bitDepth == 8) {
                if (colorType == (PngColorType.Indexed | PngColorType.Color)) {
                    return true;
                }
                if (colorType == (PngColorType.Color | PngColorType.Alpha) || colorType == PngColorType.Color) {
                    return false;
                }
            }

            throw new InvalidDataException("Unknown pixel format (" + bitDepth + "-bit, " + colorType + ") specified");
        }
    }
}