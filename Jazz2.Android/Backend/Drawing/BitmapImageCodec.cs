using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Duality.Drawing;

namespace Duality.Backend.Android
{
    public class BitmapImageCodec : IImageCodec
    {
        public string CodecId
        {
            get { return "Android Image Codec"; }
        }

        public int Priority
        {
            get { return 0; }
        }

        public bool CanReadFormat(string formatId)
        {
            switch (formatId) {
                case ImageCodec.FormatBmp:
                case ImageCodec.FormatJpeg:
                case ImageCodec.FormatTiff:
                case ImageCodec.FormatPng:
                    return true;
                default:
                    return false;
            }
        }

        public unsafe PixelData Read(Stream stream)
        {
            ColorRgba[] rawColorData;
            int width, height;

            using (Bitmap bitmap = BitmapFactory.DecodeStream(stream, null, new BitmapFactory.Options {
                    InPremultiplied = false,
                })) {

                Bitmap.Config config = bitmap.GetConfig();
                if (config != Bitmap.Config.Argb8888) {
                    throw new NotSupportedException();
                }

                width = bitmap.Width;
                height = bitmap.Height;

                IntPtr ptr = bitmap.LockPixels();
                int stride = bitmap.RowBytes / sizeof(int);

                rawColorData = new ColorRgba[width * height];

                Parallel.ForEach(Partitioner.Create(0, height), range => {
                    for (int y = range.Item1; y < range.Item2; y++) {
                        for (int x = 0; x < width; x++) {
                            int argbValue = ((int*)ptr)[x + y * stride];

                            int i = x + y * width;
                            rawColorData[i].A = (byte)((argbValue & 0xFF000000) >> 24);
                            rawColorData[i].B = (byte)((argbValue & 0x00FF0000) >> 16);
                            rawColorData[i].G = (byte)((argbValue & 0x0000FF00) >> 8);
                            rawColorData[i].R = (byte)((argbValue & 0x000000FF) >> 0);
                        }
                    }
                });

                bitmap.UnlockPixels();
            }

            PixelData pixelData = new PixelData();
            pixelData.SetData(rawColorData, width, height);
            pixelData.ColorTransparentPixels();
            return pixelData;
        }

        public bool CanWriteFormat(string formatId)
        {
            return false;
        }

        public void Write(Stream stream, PixelData pixelData, string formatId)
        {
            // Not supported in Android Backend
        }
    }
}