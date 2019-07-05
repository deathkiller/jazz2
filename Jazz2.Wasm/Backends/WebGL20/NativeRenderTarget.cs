using System;
using System.Collections.Generic;
using System.Linq;

using Duality.Drawing;
using WebGLDotNET;

namespace Duality.Backend.Wasm
{
    public class NativeRenderTarget : INativeRenderTarget
    {
        public static int MaxRenderTargetSamples
        {
            get
            {
                return 0;
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
                GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, null);
                //GraphicsBackend.GL.ReadBuffer(WebGLRenderingContextBase.BACK);

                //GraphicsBackend.GL.DrawBuffers(new uint[] { WebGLRenderingContextBase.BACK });
            } else {
                GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, /*target.samples > 0 ? target.handleMsaaFBO :*/ target.handleMainFBO);
                /*uint[] buffers = new uint[target.targetInfos.Count];
                for (int i = 0; i < buffers.Length; i++) {
                    buffers[i] = WebGLRenderingContextBase.COLOR_ATTACHMENT0 + (uint)i;
                }
                GraphicsBackend.GL.DrawBuffers(buffers);*/
            }
        }


        private struct TargetInfo
        {
            public NativeTexture Target;
            public WebGLRenderbuffer HandleMsaaColorRBO;
        }


        private bool pendingPostRender;
        private WebGLFramebuffer handleMainFBO;
        private WebGLRenderbuffer handleDepthRBO;
        private WebGLFramebuffer handleMsaaFBO;
        private int samples;
        private bool depthBuffer;
        private RawList<TargetInfo> targetInfos = new RawList<TargetInfo>();


        public WebGLFramebuffer Handle
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
            /*if (this.samples > 0) {
                GraphicsBackend.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.handleMsaaFBO);
                GraphicsBackend.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, this.handleMainFBO);
                for (int i = 0; i < this.targetInfos.Count; i++) {
                    GraphicsBackend.GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + i));

                    DrawBufferMode mode = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
                    GraphicsBackend.GL.DrawBuffers(1, ref mode);

                    GraphicsBackend.GL.BlitFramebuffer(
                        0, 0, this.targetInfos.Data[i].Target.Width, this.targetInfos.Data[i].Target.Height,
                        0, 0, this.targetInfos.Data[i].Target.Width, this.targetInfos.Data[i].Target.Height,
                        ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                }
            }*/

            // Generate mipmaps for the target textures
            /*WebGLTexture lastTexId = null;
            for (int i = 0; i < this.targetInfos.Count; i++) {
                if (!this.targetInfos.Data[i].Target.HasMipmaps)
                    continue;

                if (lastTexId == null) {
                    GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, null);
                    lastTexId = (WebGLTexture)GraphicsBackend.GL.GetParameter(WebGLRenderingContextBase.TEXTURE_BINDING_2D);
                }

                WebGLTexture texId = this.targetInfos.Data[i].Target.Handle;
                GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, texId);
                GraphicsBackend.GL.GenerateMipmap(WebGLRenderingContextBase.TEXTURE_2D);
            }

            // Reset OpenGL state
            if (lastTexId != null) GraphicsBackend.GL.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, lastTexId);
            ApplyGLBind(curBound);*/

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

            // ToDo
            // Setup OpenGL resources
            //if (this.samples > 0)
            //	this.SetupMultisampled();
            //else
                this.SetupNonMultisampled();
        }
        void INativeRenderTarget.GetData<T>(T[] buffer, ColorDataLayout dataLayout, ColorDataElementType dataElementType, int targetIndex, int x, int y, int width, int height)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            // ToDo
            //this.ApplyPostRender();
            //if (curBound != this) ApplyGLBind(this);
            //{
            //	GraphicsBackend.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.handleMainFBO);
            //	GraphicsBackend.GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + targetIndex));
            //	GraphicsBackend.GL.ReadPixels(x, y, width, height, dataLayout.ToOpenTK(), dataElementType.ToOpenTK(), buffer);
            //}
            //ApplyGLBind(curBound);

            throw new NotSupportedException();
        }
        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) return;

            // If there are changes pending to be applied to the bound textures,
            // they should be executed before the render target is gone.
            this.ApplyPostRender();

            if (this.handleMainFBO != null) {
                //GraphicsBackend.GL.DeleteFramebuffer(this.handleMainFBO);
                try {
                    GraphicsBackend.GL.DeleteFramebuffer(this.handleMainFBO);
                } catch (Exception ex) {
                    Console.WriteLine("DeleteFramebuffer() failed: " + this.handleMainFBO + " | " + ex);
                }

                this.handleMainFBO = null;
            }
            if (this.handleDepthRBO != null) {
                //GraphicsBackend.GL.DeleteRenderbuffer(this.handleDepthRBO);
                try {
                    GraphicsBackend.GL.DeleteRenderbuffer(this.handleDepthRBO);
                } catch (Exception ex) {
                    Console.WriteLine("DeleteRenderbuffer() failed: " + this.handleDepthRBO + " | " + ex);
                }

                this.handleDepthRBO = null;
            }
            if (this.handleMsaaFBO != null) {
                //GraphicsBackend.GL.DeleteFramebuffer(this.handleMsaaFBO);
                try {
                    GraphicsBackend.GL.DeleteFramebuffer(this.handleMsaaFBO);
                } catch (Exception ex) {
                    Console.WriteLine("DeleteFramebuffer() failed: " + this.handleMsaaFBO + " | " + ex);
                }

                this.handleMsaaFBO = null;
            }
            for (int i = 0; i < this.targetInfos.Count; i++) {
                if (this.targetInfos.Data[i].HandleMsaaColorRBO != null) {
                    GraphicsBackend.GL.DeleteRenderbuffer(this.targetInfos.Data[i].HandleMsaaColorRBO);
                    this.targetInfos.Data[i].HandleMsaaColorRBO = null;
                }
            }
        }

        private void SetupNonMultisampled()
        {
            // Generate FBO
            if (this.handleMainFBO == null) this.handleMainFBO = GraphicsBackend.GL.CreateFramebuffer();
            GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, this.handleMainFBO);

            // Attach textures
            int oglWidth = 0;
            int oglHeight = 0;
            for (int i = 0; i < this.targetInfos.Count; i++) {
                NativeTexture tex = this.targetInfos[i].Target;

                uint attachment = (WebGLRenderingContextBase.COLOR_ATTACHMENT0 + (uint)i);
                GraphicsBackend.GL.FramebufferTexture2D(
                    WebGLRenderingContextBase.FRAMEBUFFER,
                    attachment,
                    WebGLRenderingContextBase.TEXTURE_2D,
                    tex.Handle,
                    0);
                oglWidth = tex.Width;
                oglHeight = tex.Height;
            }

            // Generate or delete depth renderbuffer
            /*if (this.depthBuffer) {
                if (this.handleDepthRBO == null) this.handleDepthRBO = GraphicsBackend.GL.CreateRenderbuffer();
                GraphicsBackend.GL.BindRenderbuffer(WebGLRenderingContextBase.RENDERBUFFER, this.handleDepthRBO);
                GraphicsBackend.GL.RenderbufferStorage(WebGLRenderingContextBase.RENDERBUFFER, WebGL2RenderingContextBase.DEPTH_COMPONENT24, oglWidth, oglHeight);
                GraphicsBackend.GL.FramebufferRenderbuffer(WebGLRenderingContextBase.FRAMEBUFFER, WebGLRenderingContextBase.DEPTH_ATTACHMENT, WebGLRenderingContextBase.RENDERBUFFER, this.handleDepthRBO);
            } else {
                GraphicsBackend.GL.FramebufferRenderbuffer(WebGLRenderingContextBase.FRAMEBUFFER, WebGLRenderingContextBase.DEPTH_ATTACHMENT, WebGLRenderingContextBase.RENDERBUFFER, null);
                if (this.handleDepthRBO != null) GraphicsBackend.GL.DeleteRenderbuffer(this.handleDepthRBO);
                this.handleDepthRBO = null;
            }*/

            // Check status
            int status = GraphicsBackend.GL.CheckFramebufferStatus(WebGLRenderingContextBase.FRAMEBUFFER);
            if (status != WebGLRenderingContextBase.FRAMEBUFFER_COMPLETE) {
                throw new BackendException(string.Format("Incomplete Framebuffer: {0}", status));
            }

            //GraphicsBackend.GL.BindRenderbuffer(WebGLRenderingContextBase.RENDERBUFFER, null);
            GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, null);
        }
        //private void SetupMultisampled()
        //{
        //	// Generate texture target FBO
        //	if (this.handleMainFBO == null) GraphicsBackend.GL.GenFramebuffers(1, out this.handleMainFBO);
        //	GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, this.handleMainFBO);
        //
        //	// Attach textures
        //	int oglWidth = 0;
        //	int oglHeight = 0;
        //	for (int i = 0; i < this.targetInfos.Count; i++) {
        //		NativeTexture tex = this.targetInfos[i].Target;
        //
        //		FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
        //		GraphicsBackend.GL.FramebufferTexture2D(
        //			WebGLRenderingContextBase.FRAMEBUFFER,
        //			attachment,
        //			WebGLRenderingContextBase.TEXTURE_2D,
        //			tex.Handle,
        //			0);
        //		oglWidth = tex.Width;
        //		oglHeight = tex.Height;
        //	}
        //
        //	// Check status
        //	FramebufferErrorCode status = GraphicsBackend.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        //	if (status != FramebufferErrorCode.FramebufferComplete) {
        //		throw new BackendException(string.Format("Incomplete Framebuffer: {0}", status));
        //	}
        //
        //	// Generate rendering FBO
        //	if (this.handleMsaaFBO == null) GraphicsBackend.GL.GenFramebuffers(1, out this.handleMsaaFBO);
        //	GraphicsBackend.GL.BindFramebuffer(WebGLRenderingContextBase.FRAMEBUFFER, this.handleMsaaFBO);
        //
        //	// Attach color renderbuffers
        //	for (int i = 0; i < this.targetInfos.Count; i++) {
        //		TargetInfo info = this.targetInfos.Data[i];
        //
        //		FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
        //		RenderbufferInternalFormat rbColorFormat = TexFormatToRboFormat(info.Target.Format);
        //
        //		if (info.HandleMsaaColorRBO == null) GraphicsBackend.GL.GenRenderbuffers(1, out info.HandleMsaaColorRBO);
        //		GraphicsBackend.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, info.HandleMsaaColorRBO);
        //		GraphicsBackend.GL.RenderbufferStorageMultisample(WebGLRenderingContextBase.RENDERBUFFER, this.samples, rbColorFormat, oglWidth, oglHeight);
        //		GraphicsBackend.GL.FramebufferRenderbuffer(WebGLRenderingContextBase.FRAMEBUFFER, attachment, RenderbufferTarget.Renderbuffer, info.HandleMsaaColorRBO);
        //
        //		this.targetInfos.Data[i] = info;
        //	}
        //	GraphicsBackend.GL.BindRenderbuffer(WebGLRenderingContextBase.RENDERBUFFER, null);
        //
        //	// Generate or delete depth renderbuffer
        //	if (this.depthBuffer) {
        //		if (this.handleDepthRBO == null) GraphicsBackend.GL.GenRenderbuffers(1, out this.handleDepthRBO);
        //		GraphicsBackend.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
        //		GraphicsBackend.GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, this.samples, RenderbufferInternalFormat.DepthComponent24, oglWidth, oglHeight);
        //		GraphicsBackend.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
        //		GraphicsBackend.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        //	} else {
        //		GraphicsBackend.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
        //		if (this.handleDepthRBO != null) GraphicsBackend.GL.DeleteRenderbuffers(1, ref this.handleDepthRBO);
        //		this.handleDepthRBO = 0;
        //	}
        //
        //	// Check status
        //	status = GraphicsBackend.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        //	if (status != FramebufferErrorCode.FramebufferComplete) {
        //		throw new BackendException(string.Format("Incomplete Multisample Framebuffer: {0}", status));
        //	}
        //
        //	GraphicsBackend.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        //}

        //private static uint TexFormatToRboFormat(TexturePixelFormat format)
        //{
        //	switch (format) {
        //		case TexturePixelFormat.Single: return WebGL2RenderingContextBase.R8;
        //		case TexturePixelFormat.Dual: return WebGL2RenderingContextBase.RG8;
        //		case TexturePixelFormat.Rgb: return WebGL2RenderingContextBase.RGB8;
        //		default:
        //		case TexturePixelFormat.Rgba: return WebGL2RenderingContextBase.RGBA8;
        //
        //		case TexturePixelFormat.FloatSingle: return WebGL2RenderingContextBase.R16F;
        //		case TexturePixelFormat.FloatDual: return WebGL2RenderingContextBase.RG16F;
        //		case TexturePixelFormat.FloatRgb: return WebGL2RenderingContextBase.RGB16F;
        //		case TexturePixelFormat.FloatRgba: return WebGL2RenderingContextBase.RGBA16F;
        //
        //		case TexturePixelFormat.CompressedSingle: return WebGL2RenderingContextBase.R8;
        //		case TexturePixelFormat.CompressedDual: return WebGL2RenderingContextBase.RG8;
        //		case TexturePixelFormat.CompressedRgb: return WebGL2RenderingContextBase.RGB8;
        //		case TexturePixelFormat.CompressedRgba: return WebGL2RenderingContextBase.RGBA8;
        //	}
        //}
    }
}