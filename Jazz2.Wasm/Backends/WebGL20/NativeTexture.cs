using System;
using Duality.Drawing;
using Duality.Resources;
using WebAssembly.Core;
using WebGLDotNET;
using TextureMagFilter = Duality.Drawing.TextureMagFilter;
using TextureMinFilter = Duality.Drawing.TextureMinFilter;
using TextureWrapMode = Duality.Drawing.TextureWrapMode;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeTexture : INativeTexture
    {
        private static bool texInit;
        private static int activeTexUnit;
        private static uint[] texUnits;
        private static NativeTexture[] curBound;

        private static void InitTextureFields()
        {
            if (texInit) return;

            int numTexUnits = (int)GraphicsBackend.GL.GetParameter(WebGLRenderingContextBase.MAX_TEXTURE_IMAGE_UNITS);
            texUnits = new uint[numTexUnits];
            curBound = new NativeTexture[numTexUnits];

            for (int i = 0; i < numTexUnits; i++) {
                texUnits[i] = (WebGLRenderingContextBase.TEXTURE0 + (uint)i);
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
            if (activeTexUnit != texUnit) GraphicsBackend.GL.ActiveTexture(texUnits[texUnit]);
            activeTexUnit = texUnit;

            if (tex == null) {
                GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, null);
                curBound[texUnit] = null;
            } else {
                GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, tex.Handle);
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


        private WebGLTexture handle;
        private int width;
        private int height;
        private bool mipmaps;
        private TexturePixelFormat format = TexturePixelFormat.Rgba;

        public WebGLTexture Handle
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
            this.handle = GraphicsBackend.GL.CreateTexture();
        }

        void INativeTexture.SetupEmpty(TexturePixelFormat format, int width, int height, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapX, TextureWrapMode wrapY, int anisoLevel, bool mipmaps)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            WebGLTexture lastTexId = (WebGLTexture)GraphicsBackend.GL.GetParameter(WebGLRenderingContextBase.TEXTURE_BINDING_2D);
            if (lastTexId != this.handle) GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, this.handle);

            // Set texture parameters
            GraphicsBackend.GL.TexParameteri(WebGLRenderingContextBase.TEXTURE_2D, WebGLRenderingContextBase.TEXTURE_MIN_FILTER, (int)ToOpenTKTextureMinFilter(minFilter));
            GraphicsBackend.GL.TexParameteri(WebGLRenderingContextBase.TEXTURE_2D, WebGLRenderingContextBase.TEXTURE_MAG_FILTER, (int)ToOpenTKTextureMagFilter(magFilter));
            GraphicsBackend.GL.TexParameteri(WebGLRenderingContextBase.TEXTURE_2D, WebGLRenderingContextBase.TEXTURE_WRAP_S, (int)ToOpenTKTextureWrapMode(wrapX));
            GraphicsBackend.GL.TexParameteri(WebGLRenderingContextBase.TEXTURE_2D, WebGLRenderingContextBase.TEXTURE_WRAP_T, (int)ToOpenTKTextureWrapMode(wrapY));

            // Anisotropic filtering
            // ToDo: Add similar code for OpenGL ES
            //if (anisoLevel > 0) {
            //    GraphicsBackend.GL.TexParameter(WebGLRenderingContextBase.TEXTURE_2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, (float)anisoLevel);
            //}

            // If needed, care for Mipmaps
            // ToDo: Why are mipmaps disabled here?
            //GraphicsBackend.GL.TexParameter(WebGLRenderingContextBase.TEXTURE_2D, TextureParameterName.GenerateMipmap, mipmaps ? 1 : 0);

            // Setup pixel format
            GraphicsBackend.GL.TexImage2D(WebGLRenderingContextBase.TEXTURE_2D, 0,
                ToOpenTKPixelInternalFormat(format), width, height, 0,
                ToOpenTKPixelFormat(format), WebGLRenderingContextBase.UNSIGNED_BYTE, null);

            //GraphicsBackend.GL.GenerateMipmap(WebGLRenderingContextBase.TEXTURE_2D);

            this.width = width;
            this.height = height;
            this.format = format;
            this.mipmaps = mipmaps;

            if (lastTexId != this.handle) GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, lastTexId);
        }
        unsafe void INativeTexture.LoadData(TexturePixelFormat format, int width, int height, IntPtr data, ColorDataLayout dataLayout, ColorDataElementType dataElementType)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            WebGLTexture lastTexId = (WebGLTexture)GraphicsBackend.GL.GetParameter(WebGLRenderingContextBase.TEXTURE_BINDING_2D);
            GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, this.handle);

            // Load pixel data to video memory
            GraphicsBackend.GL.TexImage2D(WebGLRenderingContextBase.TEXTURE_2D, 0,
                ToOpenTKPixelInternalFormat(format), width, height, 0,
                dataLayout.ToOpenTK(), dataElementType.ToOpenTK(),
                TypedArray<Uint8ClampedArray, byte>.From(new Span<byte>(data.ToPointer(), /*ToDo*/(width * height * 4))));

            GraphicsBackend.GL.GenerateMipmap(WebGLRenderingContextBase.TEXTURE_2D);

            this.width = width;
            this.height = height;
            this.format = format;

            GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, lastTexId);
        }

        void INativeTexture.GetData(IntPtr target, ColorDataLayout dataLayout, ColorDataElementType dataElementType)
        {
            // ToDo: Add similar code for OpenGL ES
            throw new NotSupportedException();

            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
            //
            //int lastTexId;
            //GraphicsBackend.GL.GetInteger(GetPName.TextureBinding2D, out lastTexId);
            //GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, this.handle);
            //
            //
            //OpenTK.Graphics.OpenGraphicsBackend.GL.GraphicsBackend.GL.GetTexImage(WebGLRenderingContextBase.TEXTURE_2D, 0,
            //    dataLayout.ToOpenTK(), dataElementType.ToOpenTK(),
            //    target);
            //
            //GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, lastTexId);
        }

        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated &&
                this.handle != null) {
                // Removed thread guards because of performance
                //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
                GraphicsBackend.GL.DeleteTexture(this.handle);
                this.handle = null;
            }
        }

        private static uint ToOpenTKPixelInternalFormat(TexturePixelFormat format)
        {
            switch (format) {
                case TexturePixelFormat.Single: return WebGL2RenderingContextBase.R8;
                case TexturePixelFormat.Dual: return WebGL2RenderingContextBase.RG8;
                case TexturePixelFormat.Rgb: return WebGL2RenderingContextBase.RGB;
                default:
                case TexturePixelFormat.Rgba: return WebGL2RenderingContextBase.RGBA;

                case TexturePixelFormat.FloatSingle: return WebGL2RenderingContextBase.R16F;
                case TexturePixelFormat.FloatDual: return WebGL2RenderingContextBase.RG16F;
                case TexturePixelFormat.FloatRgb: return WebGL2RenderingContextBase.RGB16F;
                case TexturePixelFormat.FloatRgba: return WebGL2RenderingContextBase.RGBA16F;

                // Compressed formats are not supported in OpenGL ES
                //case TexturePixelFormat.CompressedSingle: return (PixelInternalFormat)TextureComponentCount.CompressedRed;
                //case TexturePixelFormat.CompressedDual: return (PixelInternalFormat)TextureComponentCount.CompressedRg;
                //case TexturePixelFormat.CompressedRgb: return (PixelInternalFormat)TextureComponentCount.CompressedRgb;
                //case TexturePixelFormat.CompressedRgba: return (PixelInternalFormat)TextureComponentCount.CompressedRgba;
            }
        }

        private static uint ToOpenTKPixelFormat(TexturePixelFormat format)
        {
            switch (format) {
                case TexturePixelFormat.Single: return WebGL2RenderingContextBase.RED;
                case TexturePixelFormat.Dual: return WebGL2RenderingContextBase.RG;
                case TexturePixelFormat.Rgb: return WebGL2RenderingContextBase.RGB;
                default:
                case TexturePixelFormat.Rgba: return WebGL2RenderingContextBase.RGBA;

                case TexturePixelFormat.FloatSingle: return WebGL2RenderingContextBase.RED;
                case TexturePixelFormat.FloatDual: return WebGL2RenderingContextBase.RG;
                case TexturePixelFormat.FloatRgb: return WebGL2RenderingContextBase.RGB;
                case TexturePixelFormat.FloatRgba: return WebGL2RenderingContextBase.RGBA;

                // Compressed formats are not supported in OpenGL ES
                //case TexturePixelFormat.CompressedSingle: return (PixelInternalFormat)TextureComponentCount.CompressedRed;
                //case TexturePixelFormat.CompressedDual: return (PixelInternalFormat)TextureComponentCount.CompressedRg;
                //case TexturePixelFormat.CompressedRgb: return (PixelInternalFormat)TextureComponentCount.CompressedRgb;
                //case TexturePixelFormat.CompressedRgba: return (PixelInternalFormat)TextureComponentCount.CompressedRgba;
            }
        }
        private static uint ToOpenTKTextureMagFilter(TextureMagFilter value)
        {
            switch (value) {
                default:
                case TextureMagFilter.Nearest: return WebGLRenderingContextBase.NEAREST;
                case TextureMagFilter.Linear: return WebGLRenderingContextBase.LINEAR;
            }
        }
        private static uint ToOpenTKTextureMinFilter(TextureMinFilter value)
        {
            switch (value) {
                default:
                case TextureMinFilter.Nearest: return WebGLRenderingContextBase.NEAREST;
                case TextureMinFilter.Linear: return WebGLRenderingContextBase.LINEAR;
                case TextureMinFilter.NearestMipmapNearest: return WebGLRenderingContextBase.NEAREST_MIPMAP_NEAREST;
                case TextureMinFilter.LinearMipmapNearest: return WebGLRenderingContextBase.LINEAR_MIPMAP_NEAREST;
                case TextureMinFilter.NearestMipmapLinear: return WebGLRenderingContextBase.NEAREST_MIPMAP_LINEAR;
                case TextureMinFilter.LinearMipmapLinear: return WebGLRenderingContextBase.LINEAR_MIPMAP_LINEAR;
            }
        }
        private static uint ToOpenTKTextureWrapMode(TextureWrapMode value)
        {
            switch (value) {
                default:
                case TextureWrapMode.Clamp: return WebGLRenderingContextBase.CLAMP_TO_EDGE;
                case TextureWrapMode.Repeat: return WebGLRenderingContextBase.REPEAT;
                case TextureWrapMode.MirroredRepeat: return WebGLRenderingContextBase.MIRRORED_REPEAT;
            }
        }
    }
}