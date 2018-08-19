using System;
using Duality.Drawing;
using Duality.Resources;
using OpenTK.Graphics.ES20;
using GLPixelFormat = OpenTK.Graphics.ES20.PixelFormat;
using GLTexMagFilter = OpenTK.Graphics.ES20.TextureMagFilter;
using GLTexMinFilter = OpenTK.Graphics.ES20.TextureMinFilter;
using GLTexWrapMode = OpenTK.Graphics.ES20.TextureWrapMode;
using TextureMagFilter = Duality.Drawing.TextureMagFilter;
using TextureMinFilter = Duality.Drawing.TextureMinFilter;
using TextureWrapMode = Duality.Drawing.TextureWrapMode;

namespace Duality.Backend.Es20
{
    public class NativeTexture : INativeTexture
    {
        private static bool texInit;
        private static int activeTexUnit;
        private static TextureUnit[] texUnits;
        private static NativeTexture[] curBound;

        private static void InitTextureFields()
        {
            if (texInit) return;

            int numTexUnits;
            GL.GetInteger(GetPName.MaxTextureImageUnits, out numTexUnits);
            texUnits = new TextureUnit[numTexUnits];
            curBound = new NativeTexture[numTexUnits];

            for (int i = 0; i < numTexUnits; i++) {
                texUnits[i] = (TextureUnit)((int)TextureUnit.Texture0 + i);
            }

            texInit = true;
        }
        public static void Bind(ContentRef<Duality.Resources.Texture> target, int texUnit = 0)
        {
            Bind((target.Res != null ? target.Res : Texture.White.Res).Native as NativeTexture, texUnit);
        }
        public static void Bind(NativeTexture tex, int texUnit = 0)
        {
            if (!texInit) InitTextureFields();

            if (curBound[texUnit] == tex) return;
            if (activeTexUnit != texUnit) GL.ActiveTexture(texUnits[texUnit]);
            activeTexUnit = texUnit;

            if (tex == null) {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                curBound[texUnit] = null;
            } else {
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                curBound[texUnit] = tex;
            }
        }
        public static void ResetBinding(int beginAtIndex = 0)
        {
            if (!texInit) InitTextureFields();

            Bind(Texture.White.Res.Native as NativeTexture, beginAtIndex++);
            for (int i = beginAtIndex; i < texUnits.Length; i++) {
                Bind(null as NativeTexture, i);
            }
        }


        private int handle;
        private int width;
        private int height;
        private bool mipmaps;
        private TexturePixelFormat format = TexturePixelFormat.Rgba;

        public int Handle
        {
            get { return this.handle; }
        }
        public int Width
        {
            get { return this.width; }
        }
        public int Height
        {
            get { return this.height; }
        }
        public bool HasMipmaps
        {
            get { return this.mipmaps; }
        }
        public TexturePixelFormat Format
        {
            get { return this.format; }
        }

        public NativeTexture()
        {
            this.handle = GL.GenTexture();
        }

        void INativeTexture.SetupEmpty(TexturePixelFormat format, int width, int height, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapX, TextureWrapMode wrapY, int anisoLevel, bool mipmaps)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (width == 0 || height == 0)
            {
                return;
            }

            int lastTexId;
            GL.GetInteger(GetPName.TextureBinding2D, out lastTexId);
            if (lastTexId != this.handle) GL.BindTexture(TextureTarget.Texture2D, this.handle);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ToOpenTKTextureMinFilter(minFilter));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ToOpenTKTextureMagFilter(magFilter));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)ToOpenTKTextureWrapMode(wrapX));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)ToOpenTKTextureWrapMode(wrapY));

            // Anisotropic filtering
            // ToDo: Add similar code for OpenGL ES
            //if (anisoLevel > 0) {
            //    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, (float)anisoLevel);
            //}

            // If needed, care for Mipmaps
            // ToDo: Why are mipmaps disabled here?
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, mipmaps ? 1 : 0);

            // Setup pixel format
            GL.TexImage2D(TextureTarget2d.Texture2D, 0,
                ToOpenTKPixelInternalFormat(format), width, height, 0,
                ToOpenTKPixelFormat(format), PixelType.UnsignedByte, IntPtr.Zero);

            //GL.GenerateMipmap(TextureTarget.Texture2D);

            this.width = width;
            this.height = height;
            this.format = format;
            this.mipmaps = mipmaps;

            if (lastTexId != this.handle) GL.BindTexture(TextureTarget.Texture2D, lastTexId);
        }
        void INativeTexture.LoadData(TexturePixelFormat format, int width, int height, IntPtr data, ColorDataLayout dataLayout, ColorDataElementType dataElementType)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            int lastTexId;
            GL.GetInteger(GetPName.TextureBinding2D, out lastTexId);
            GL.BindTexture(TextureTarget.Texture2D, this.handle);

            // Load pixel data to video memory
            GL.TexImage2D(TextureTarget2d.Texture2D, 0,
                ToOpenTKPixelInternalFormat(format), width, height, 0,
                dataLayout.ToOpenTK(), dataElementType.ToOpenTK(),
                data);

            GL.GenerateMipmap(TextureTarget.Texture2D);

            this.width = width;
            this.height = height;
            this.format = format;

            GL.BindTexture(TextureTarget.Texture2D, lastTexId);
        }

        void INativeTexture.GetData(IntPtr target, ColorDataLayout dataLayout, ColorDataElementType dataElementType)
        {
            // ToDo: Add similar code for OpenGL ES
            throw new NotSupportedException();

            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
            //
            //int lastTexId;
            //GL.GetInteger(GetPName.TextureBinding2D, out lastTexId);
            //GL.BindTexture(TextureTarget.Texture2D, this.handle);
            //
            //
            //OpenTK.Graphics.OpenGL.GL.GetTexImage(TextureTarget.Texture2D, 0,
            //    dataLayout.ToOpenTK(), dataElementType.ToOpenTK(),
            //    target);
            //
            //GL.BindTexture(TextureTarget.Texture2D, lastTexId);
        }

        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated &&
                this.handle != 0) {
                // Removed thread guards because of performance
                //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
                GL.DeleteTexture(this.handle);
                this.handle = 0;
            }
        }

        private static TextureComponentCount ToOpenTKPixelInternalFormat(TexturePixelFormat format)
        {
            switch (format) {
                // ToDo
                //case TexturePixelFormat.Single: return TextureComponentCount./*R8Ext*/Rgb;
                //case TexturePixelFormat.Dual: return TextureComponentCount./*Rg8Ext*/Rgb;
                //case TexturePixelFormat.Rgb: return TextureComponentCount.Rgb;
                default:
                case TexturePixelFormat.Rgba: return TextureComponentCount.Rgba;

                //case TexturePixelFormat.FloatSingle: return TextureComponentCount.R16fExt;
                //case TexturePixelFormat.FloatDual: return TextureComponentCount.Rg16fExt;
                //case TexturePixelFormat.FloatRgb: return TextureComponentCount.Rgb16fExt;
                //case TexturePixelFormat.FloatRgba: return TextureComponentCount.Rgba16fExt;

                // Compressed formats are not supported in OpenGL ES
                //case TexturePixelFormat.CompressedSingle: return (PixelInternalFormat)TextureComponentCount.CompressedRed;
                //case TexturePixelFormat.CompressedDual: return (PixelInternalFormat)TextureComponentCount.CompressedRg;
                //case TexturePixelFormat.CompressedRgb: return (PixelInternalFormat)TextureComponentCount.CompressedRgb;
                //case TexturePixelFormat.CompressedRgba: return (PixelInternalFormat)TextureComponentCount.CompressedRgba;
            }
        }

        private static GLPixelFormat ToOpenTKPixelFormat(TexturePixelFormat format)
        {
            switch (format) {
                // ToDo
                //case TexturePixelFormat.Single: return GLPixelFormat.Red;
                //case TexturePixelFormat.Dual: return GLPixelFormat.Rgb;
                //case TexturePixelFormat.Rgb: return GLPixelFormat.Rgb;
                default:
                case TexturePixelFormat.Rgba: return GLPixelFormat.Rgba;

                //case TexturePixelFormat.FloatSingle: return GLPixelFormat.Red;
                //case TexturePixelFormat.FloatDual: return GLPixelFormat.Rgb;
                //case TexturePixelFormat.FloatRgb: return GLPixelFormat.Rgb;
                //case TexturePixelFormat.FloatRgba: return GLPixelFormat.Rgba;

                // Compressed formats are not supported in OpenGL ES
                //case TexturePixelFormat.CompressedSingle: return (PixelInternalFormat)TextureComponentCount.CompressedRed;
                //case TexturePixelFormat.CompressedDual: return (PixelInternalFormat)TextureComponentCount.CompressedRg;
                //case TexturePixelFormat.CompressedRgb: return (PixelInternalFormat)TextureComponentCount.CompressedRgb;
                //case TexturePixelFormat.CompressedRgba: return (PixelInternalFormat)TextureComponentCount.CompressedRgba;
            }
        }
        private static GLTexMagFilter ToOpenTKTextureMagFilter(TextureMagFilter value)
        {
            switch (value) {
                default:
                case TextureMagFilter.Nearest: return GLTexMagFilter.Nearest;
                case TextureMagFilter.Linear: return GLTexMagFilter.Linear;
            }
        }
        private static GLTexMinFilter ToOpenTKTextureMinFilter(TextureMinFilter value)
        {
            switch (value) {
                default:
                case TextureMinFilter.Nearest: return GLTexMinFilter.Nearest;
                case TextureMinFilter.Linear: return GLTexMinFilter.Linear;
                case TextureMinFilter.NearestMipmapNearest: return GLTexMinFilter.NearestMipmapNearest;
                case TextureMinFilter.LinearMipmapNearest: return GLTexMinFilter.LinearMipmapNearest;
                case TextureMinFilter.NearestMipmapLinear: return GLTexMinFilter.NearestMipmapLinear;
                case TextureMinFilter.LinearMipmapLinear: return GLTexMinFilter.LinearMipmapLinear;
            }
        }
        private static GLTexWrapMode ToOpenTKTextureWrapMode(TextureWrapMode value)
        {
            switch (value) {
                default:
                case TextureWrapMode.Clamp: return GLTexWrapMode.ClampToEdge;
                case TextureWrapMode.Repeat: return GLTexWrapMode.Repeat;
                // ToDo
                //case TextureWrapMode.MirroredRepeat: return GLTexWrapMode.MirroredRepeat;
            }
        }
    }
}