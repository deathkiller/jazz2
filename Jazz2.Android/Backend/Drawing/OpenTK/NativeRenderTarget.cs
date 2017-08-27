using System;
using System.Collections.Generic;
using System.Linq;

using Duality.Drawing;

using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeRenderTarget : INativeRenderTarget
    {
        private static int maxFboSamples = -1;
        public static int MaxRenderTargetSamples
        {
            get
            {
               if (maxFboSamples == -1) GL.GetInteger(GetPName.MaxSamples, out maxFboSamples);
                return maxFboSamples;
            }
        }

        private static NativeRenderTarget curBound;
        public static NativeRenderTarget BoundRT
        {
            get { return curBound; }
        }
        public static void Bind(NativeRenderTarget nextBound)
        {
            if (curBound == nextBound) return;

            // When binding a different target, execute pending post-render steps for the previous one
            if (curBound != null && curBound != nextBound)
                curBound.ApplyPostRender();

            // Bind new RenderTarget
            ApplyGLBind(nextBound);

            // Update binding info
            curBound = nextBound;
            if (curBound != null)
                curBound.pendingPostRender = true;
        }
        private static void ApplyGLBind(NativeRenderTarget target)
        {
            if (target == null) {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ReadBuffer(ReadBufferMode.Back);

                DrawBufferMode mode = DrawBufferMode.Back;
                GL.DrawBuffers(1, ref mode);
            } else {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.samples > 0 ? target.handleMsaaFBO : target.handleMainFBO);
                DrawBufferMode[] buffers = new DrawBufferMode[target.targetInfos.Count];
                for (int i = 0; i < buffers.Length; i++) {
                    buffers[i] = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
                }
                GL.DrawBuffers(buffers.Length, buffers);
            }
        }


        private struct TargetInfo
        {
            public NativeTexture Target;
            public int HandleMsaaColorRBO;
        }


        private bool pendingPostRender;
        private int handleMainFBO;
        private int handleDepthRBO;
        private int handleMsaaFBO;
        private int samples;
        private bool depthBuffer;
        private RawList<TargetInfo> targetInfos = new RawList<TargetInfo>();


        public int Handle
        {
            get { return this.handleMainFBO; }
        }
        public int Width
        {
            get { return this.targetInfos.FirstOrDefault().Target != null ? this.targetInfos.FirstOrDefault().Target.Width : 0; }
        }
        public int Height
        {
            get { return this.targetInfos.FirstOrDefault().Target != null ? this.targetInfos.FirstOrDefault().Target.Height : 0; }
        }
        public int Samples
        {
            get { return this.samples; }
        }


        public void ApplyPostRender()
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (!this.pendingPostRender) return;

            // Resolve multisampling to the main FBO
            if (this.samples > 0) {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.handleMsaaFBO);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, this.handleMainFBO);
                for (int i = 0; i < this.targetInfos.Count; i++) {
                    GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + i));

                    DrawBufferMode mode = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
                    GL.DrawBuffers(1, ref mode);

                    GL.BlitFramebuffer(
                        0, 0, this.targetInfos.Data[i].Target.Width, this.targetInfos.Data[i].Target.Height,
                        0, 0, this.targetInfos.Data[i].Target.Width, this.targetInfos.Data[i].Target.Height,
                        ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                }
            }

            // Generate mipmaps for the target textures
            int lastTexId = -1;
            for (int i = 0; i < this.targetInfos.Count; i++) {
                if (!this.targetInfos.Data[i].Target.HasMipmaps)
                    continue;

                if (lastTexId == -1) {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.GetInteger(GetPName.TextureBinding2D, out lastTexId);
                }

                int texId = this.targetInfos.Data[i].Target.Handle;
                GL.BindTexture(TextureTarget.Texture2D, texId);
                GL.GenerateMipmap(TextureTarget.Texture2D);
            }

            // Reset OpenGL state
            if (lastTexId != -1) GL.BindTexture(TextureTarget.Texture2D, lastTexId);
            ApplyGLBind(curBound);

            this.pendingPostRender = false;
        }

        void INativeRenderTarget.Setup(IReadOnlyList<INativeTexture> targets, AAQuality multisample, bool depthBuffer)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (targets == null) return;
            if (targets.Count == 0) return;
            if (targets.All(i => i == null)) return;

            // ToDo: AA is disabled for now
            /*int highestAALevel = MathF.RoundToInt(MathF.Log(MathF.Max(MaxRenderTargetSamples, 1.0f), 2.0f));
            int targetAALevel = highestAALevel;
            switch (multisample) {
                case AAQuality.High: targetAALevel = highestAALevel; break;
                case AAQuality.Medium: targetAALevel = highestAALevel / 2; break;
                case AAQuality.Low: targetAALevel = highestAALevel / 4; break;
                case AAQuality.Off: targetAALevel = 0; break;
            }
            int targetSampleCount = MathF.RoundToInt(MathF.Pow(2.0f, targetAALevel));
            GraphicsMode sampleMode =
                GraphicsBackend.ActiveInstance.AvailableGraphicsModes.LastOrDefault(m => m.Samples <= targetSampleCount) ??
                GraphicsBackend.ActiveInstance.AvailableGraphicsModes.Last();
            this.samples = sampleMode.Samples;*/
            this.samples = 0;

            this.depthBuffer = depthBuffer;

            // Synchronize target information
            {
                this.targetInfos.Reserve(targets.Count);
                int localIndex = 0;
                for (int i = 0; i < targets.Count; i++) {
                    if (targets[i] == null) continue;

                    this.targetInfos.Count = Math.Max(this.targetInfos.Count, localIndex + 1);
                    this.targetInfos.Data[localIndex].Target = targets[i] as NativeTexture;

                    localIndex++;
                }
            }

            // Setup OpenGL resources
            if (this.samples > 0)
                this.SetupMultisampled();
            else
                this.SetupNonMultisampled();
        }
        void INativeRenderTarget.GetData<T>(T[] buffer, ColorDataLayout dataLayout, ColorDataElementType dataElementType, int targetIndex, int x, int y, int width, int height)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            this.ApplyPostRender();
            if (curBound != this) ApplyGLBind(this);
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.handleMainFBO);
                GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + targetIndex));
                GL.ReadPixels(x, y, width, height, dataLayout.ToOpenTK(), dataElementType.ToOpenTK(), buffer);
            }
            ApplyGLBind(curBound);
        }
        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) return;
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            // If there are changes pending to be applied to the bound textures,
            // they should be executed before the render target is gone.
            this.ApplyPostRender();

            if (this.handleMainFBO != 0) {
                GL.DeleteFramebuffers(1, ref this.handleMainFBO);
                this.handleMainFBO = 0;
            }
            if (this.handleDepthRBO != 0) {
                GL.DeleteRenderbuffers(1, ref this.handleDepthRBO);
                this.handleDepthRBO = 0;
            }
            if (this.handleMsaaFBO != 0) {
                GL.DeleteFramebuffers(1, ref this.handleMsaaFBO);
                this.handleMsaaFBO = 0;
            }
            for (int i = 0; i < this.targetInfos.Count; i++) {
                if (this.targetInfos.Data[i].HandleMsaaColorRBO != 0) {
                    GL.DeleteRenderbuffers(1, ref this.targetInfos.Data[i].HandleMsaaColorRBO);
                    this.targetInfos.Data[i].HandleMsaaColorRBO = 0;
                }
            }
        }

        private void SetupNonMultisampled()
        {
            // Generate FBO
            if (this.handleMainFBO == 0) GL.GenFramebuffers(1, out this.handleMainFBO);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.handleMainFBO);

            // Attach textures
            int oglWidth = 0;
            int oglHeight = 0;
            for (int i = 0; i < this.targetInfos.Count; i++) {
                NativeTexture tex = this.targetInfos[i].Target;

                FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    TextureTarget.Texture2D,
                    tex.Handle,
                    0);
                oglWidth = tex.Width;
                oglHeight = tex.Height;
            }

            // Generate or delete depth renderbuffer
            if (this.depthBuffer) {
                if (this.handleDepthRBO == 0) GL.GenRenderbuffers(1, out this.handleDepthRBO);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent24, oglWidth, oglHeight);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
            } else {
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                if (this.handleDepthRBO != 0) GL.DeleteRenderbuffers(1, ref this.handleDepthRBO);
                this.handleDepthRBO = 0;
            }

            // Check status
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete) {
                throw new BackendException(string.Format("Incomplete Framebuffer: {0}", status));
            }

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        private void SetupMultisampled()
        {
            // Generate texture target FBO
            if (this.handleMainFBO == 0) GL.GenFramebuffers(1, out this.handleMainFBO);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.handleMainFBO);

            // Attach textures
            int oglWidth = 0;
            int oglHeight = 0;
            for (int i = 0; i < this.targetInfos.Count; i++) {
                NativeTexture tex = this.targetInfos[i].Target;

                FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    TextureTarget.Texture2D,
                    tex.Handle,
                    0);
                oglWidth = tex.Width;
                oglHeight = tex.Height;
            }

            // Check status
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete) {
                throw new BackendException(string.Format("Incomplete Framebuffer: {0}", status));
            }

            // Generate rendering FBO
            if (this.handleMsaaFBO == 0) GL.GenFramebuffers(1, out this.handleMsaaFBO);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.handleMsaaFBO);

            // Attach color renderbuffers
            for (int i = 0; i < this.targetInfos.Count; i++) {
                TargetInfo info = this.targetInfos.Data[i];

                FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
                RenderbufferInternalFormat rbColorFormat = TexFormatToRboFormat(info.Target.Format);

                if (info.HandleMsaaColorRBO == 0) GL.GenRenderbuffers(1, out info.HandleMsaaColorRBO);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, info.HandleMsaaColorRBO);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, this.samples, rbColorFormat, oglWidth, oglHeight);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, info.HandleMsaaColorRBO);

                this.targetInfos.Data[i] = info;
            }
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            // Generate or delete depth renderbuffer
            if (this.depthBuffer) {
                if (this.handleDepthRBO == 0) GL.GenRenderbuffers(1, out this.handleDepthRBO);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, this.samples, RenderbufferInternalFormat.DepthComponent24, oglWidth, oglHeight);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            } else {
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                if (this.handleDepthRBO != 0) GL.DeleteRenderbuffers(1, ref this.handleDepthRBO);
                this.handleDepthRBO = 0;
            }

            // Check status
            status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete) {
                throw new BackendException(string.Format("Incomplete Multisample Framebuffer: {0}", status));
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private static RenderbufferInternalFormat TexFormatToRboFormat(TexturePixelFormat format)
        {
            switch (format) {
                case TexturePixelFormat.Single: return RenderbufferInternalFormat.R8;
                case TexturePixelFormat.Dual: return RenderbufferInternalFormat.Rg8;
                case TexturePixelFormat.Rgb: return RenderbufferInternalFormat.Rgb8;
                default:
                case TexturePixelFormat.Rgba: return RenderbufferInternalFormat.Rgba8;

                case TexturePixelFormat.FloatSingle: return /*RenderbufferInternalFormat.R16f*/(RenderbufferInternalFormat)33325;
                case TexturePixelFormat.FloatDual: return /*RenderbufferInternalFormat.Rg16f*/(RenderbufferInternalFormat)33327;
                case TexturePixelFormat.FloatRgb: return /*RenderbufferInternalFormat.Rgb16f*/(RenderbufferInternalFormat)34843;
                case TexturePixelFormat.FloatRgba: return /*RenderbufferInternalFormat.Rgba16f*/(RenderbufferInternalFormat)34842;

                case TexturePixelFormat.CompressedSingle: return RenderbufferInternalFormat.R8;
                case TexturePixelFormat.CompressedDual: return RenderbufferInternalFormat.Rg8;
                case TexturePixelFormat.CompressedRgb: return RenderbufferInternalFormat.Rgb8;
                case TexturePixelFormat.CompressedRgba: return RenderbufferInternalFormat.Rgba8;
            }
        }
    }
}