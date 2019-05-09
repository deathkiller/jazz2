using System;
using System.IO;
using System.Text;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2
{
    public static class ExtensionMethods
    {
        // Strings
        public static unsafe string SubstringByOffset(this string input, char delimiter, int offset)
        {
            if (string.IsNullOrEmpty(input)) {
                return null;
            }

            fixed (char* ptr = input) {
                int delimiterCount = 0;
                int start = 0;
                for (int i = 0; i < input.Length; i++) {
                    if (ptr[i] == delimiter) {
                        if (delimiterCount == offset - 1) {
                            start = i + 1;
                        } else if (delimiterCount == offset) {
                            return new string(ptr, start, i - start);
                        }
                        delimiterCount++;
                    }
                }

                if (offset == 0) {
                    return input;
                } else {
                    return (start > 0 ? new string(ptr, start, input.Length - start) : null);
                }
            }
        }

        // BinaryReader/Writer
        public static string ReadAsciiString(this BinaryReader r)
        {
            StringBuilder sb = new StringBuilder();

            byte value;
            while ((value = r.ReadByte()) != 0) {
                sb.Append((char)value);
            }

            return sb.ToString();
        }

        public static void WriteAsciiString(this BinaryWriter w, string value)
        {
            if (!string.IsNullOrEmpty(value)) {
                w.Write(Encoding.ASCII.GetBytes(value));
            }

            w.Write((byte)0);
        }

        // RenderSetups
        public static void Blit(this RenderSetup renderSetup, DrawDevice device, BatchInfo source, RenderTarget target)
        {
            device.Target = target;
            device.TargetSize = target.Size;
            device.ViewportRect = new Rect(target.Size);

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }


        public static void Blit(this RenderSetup renderSetup, DrawDevice device, BatchInfo source, Rect screenRect)
        {
            device.Target = null;
            device.TargetSize = screenRect.Size;
            device.ViewportRect = screenRect;

            device.PrepareForDrawcalls();
            device.AddFullscreenQuad(source, TargetResize.Stretch);
            device.Render();
        }

        // GraphicResources
        public static void Draw(this GraphicResource res, Canvas c, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            Texture texture = res.Material.Res.MainTexture.Res;
            if (texture == null) {
                return;
            }

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(texture.InternalWidth * scaleX, texture.InternalHeight * scaleY));

            c.State.SetMaterial(res.Material);
            c.State.ColorTint = color;
            c.FillRect((int)originPos.X, (int)originPos.Y, texture.InternalWidth * scaleX, texture.InternalHeight * scaleY);
        }

        public static void Draw(this GraphicResource res, Canvas c, float x, float y, Alignment alignment, ColorRgba color, float scaleX, float scaleY, Rect texRect)
        {
            Texture texture = res.Material.Res.MainTexture.Res;
            if (texture == null) {
                return;
            }

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(texture.InternalWidth * scaleX, texture.InternalHeight * scaleY));

            c.State.SetMaterial(res.Material);
            c.State.ColorTint = color;

            Rect tempRect = c.State.TextureCoordinateRect;
            c.State.TextureCoordinateRect = texRect;

            c.FillRect((int)originPos.X, (int)originPos.Y, texture.InternalWidth * scaleX, texture.InternalHeight * scaleY);

            c.State.TextureCoordinateRect = tempRect;
        }

        public static void Draw(this GraphicResource res, Canvas c, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            Texture texture = res.Material.Res.MainTexture.Res;
            if (texture == null) {
                return;
            }

            if (frame < 0) {
                // ToDo: HUD Animations are slowed down to 0.86f, adjust this in Metadata files
                frame = (int)(Time.GameTimer.TotalSeconds * 0.86f * res.FrameCount / res.FrameDuration) % res.FrameCount;
            }

            Rect uv = texture.LookupAtlas(frame);
            float w = texture.InternalWidth * scaleX * uv.W;
            float h = texture.InternalHeight * scaleY * uv.H;

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(w, h));

            c.State.SetMaterial(res.Material);
            c.State.ColorTint = color;
            c.State.TextureCoordinateRect = uv;
            c.FillRect((int)originPos.X, (int)originPos.Y, w, h);
        }

        // Stream
        public static void ReadBytes(this Stream s, byte[] buffer, int offset, int count)
        {
            while (count > 0) {
                int bytesRead;
                if ((bytesRead = s.Read(buffer, offset, count)) == 0) {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static int PeekByte(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 1) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 1) < 1) {
                return -1;
            }

            s.Seek(-1, SeekOrigin.Current);
            return internalBuffer[0];
        }

        public static byte ReadUInt8(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 1) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 1) < 1) {
                throw new EndOfStreamException();
            }

            return internalBuffer[0];
        }

        public static ushort ReadUInt16(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 2) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 2) < 2) {
                throw new EndOfStreamException();
            }

            return BitConverter.ToUInt16(internalBuffer, 0);
        }

        public static short ReadInt16(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 2) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 2) < 2) {
                throw new EndOfStreamException();
            }

            return BitConverter.ToInt16(internalBuffer, 0);
        }

        public static uint ReadUInt32(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 4) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 4) < 4) {
                throw new EndOfStreamException();
            }

            return BitConverter.ToUInt32(internalBuffer, 0);
        }

        public static int ReadInt32(this Stream s, ref byte[] internalBuffer)
        {
            if (internalBuffer == null || internalBuffer.Length < 4) {
                internalBuffer = new byte[4];
            }

            if (s.Read(internalBuffer, 0, 4) < 4) {
                throw new EndOfStreamException();
            }

            return BitConverter.ToInt32(internalBuffer, 0);
        }

        public static void Write(this Stream s, short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            s.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this Stream s, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            s.Write(bytes, 0, bytes.Length);
        }
    }
}