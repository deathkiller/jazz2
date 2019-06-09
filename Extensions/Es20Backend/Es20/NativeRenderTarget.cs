using Duality.Drawing;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Duality.Backend.Es20
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
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                //GL.ReadBuffer(ReadBufferMode.Back);

                //DrawBufferMode mode = DrawBufferMode.Back;
                //GL.Ext.DrawBuffers(1, ref mode);
            } else {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.handleMainFBO);
                //DrawBufferMode[] buffers = new DrawBufferMode[target.targetInfos.Count];
                //for (int i = 0; i < buffers.Length; i++) {
                //    buffers[i] = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
                //}
                //GL.Ext.DrawBuffers(buffers.Length, buffers);
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


        public void ApplyPostRender()
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (!this.pendingPostRender) return;

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
            this.SetupNonMultisampled();
        }
        void INativeRenderTarget.GetData<T>(T[] buffer, ColorDataLayout dataLayout, ColorDataElementType dataElementType, int targetIndex, int x, int y, int width, int height)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            this.ApplyPostRender();
            if (curBound != this) ApplyGLBind(this);
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.handleMainFBO);
                //GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + targetIndex));
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
            for (int i = 0; i < Math.Min(1, this.targetInfos.Count); i++) {
                NativeTexture tex = this.targetInfos[i].Target;

                FramebufferSlot attachment = (FramebufferSlot)((int)FramebufferSlot.ColorAttachment0 + i);
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    (All)attachment,
                    TextureTarget2d.Texture2D,
                    tex.Handle,
                    0);
                oglWidth = tex.Width;
                oglHeight = tex.Height;
            }

            // Generate or delete depth renderbuffer
            if (this.depthBuffer) {
                if (this.handleDepthRBO == 0) GL.GenRenderbuffers(1, out this.handleDepthRBO);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, oglWidth, oglHeight);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, (All)FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, this.handleDepthRBO);
            } else {
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, (All)FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
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
    }
}