using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Wasm;
using WebAssembly;
using WebAssembly.Core;
using WebGLDotNET;

namespace Duality.Backend.Wasm
{
    public class GraphicsBackend : IGraphicsBackend
    {
        private static readonly Version MinWebGLVersion = new Version(2, 0);

        private static GraphicsBackend activeInstance;
        public static GraphicsBackend ActiveInstance
        {
            get { return activeInstance; }
        }

        private static JSObject htmlCanvas;
        private static Point2 cachedCanvasSize;

        internal static WebGLRenderingContextBase GL;

        private IDrawDevice currentDevice;
        private RenderOptions renderOptions;
        private RenderStats renderStats;
        private RawList<WebGLBuffer> perVertexTypeVBO = new RawList<WebGLBuffer>();
        private Point2 externalBackbufferSize = Point2.Zero;
        private HashSet<NativeShaderProgram> activeShaders = new HashSet<NativeShaderProgram>();
        private HashSet<string> sharedShaderParameters = new HashSet<string>();
        private int sharedSamplerBindings = 0;

        private RawList<WebGLBuffer> perBatchEBO = new RawList<WebGLBuffer>();
        private int lastUsedEBO;
        private short[] indexCache;

        private float[] viewData = new float[16];
        private float[] projectionData = new float[16];

        public IEnumerable<ScreenResolution> AvailableScreenResolutions
        {
            get
            {
                // ToDo
                return null;
            }
        }
        public Point2 ExternalBackbufferSize
        {
            get { return this.externalBackbufferSize; }
            set { this.externalBackbufferSize = value; }
        }

        string IDualityBackend.Id
        {
            get { return "WebGLGraphicsBackend"; }
        }
        string IDualityBackend.Name
        {
            get { return "WebGL 2.0"; }
        }
        int IDualityBackend.Priority
        {
            get { return 1; }
        }

        bool IDualityBackend.CheckAvailable()
        {
            JSObject window = (JSObject)Runtime.GetGlobalObject();
            return window.GetObjectProperty("WebGL2RenderingContext") != null;
        }

        void IDualityBackend.Init()
        {
            activeInstance = this;

            // ToDo: Hardcoded size
            cachedCanvasSize = new Point2(720, 405);

            htmlCanvas = HtmlHelper.AddCanvas("game", cachedCanvasSize.X, cachedCanvasSize.Y);

            using (JSObject contextAttributes = new JSObject()) {
                contextAttributes.SetObjectProperty("premultipliedAlpha", false);
                GL = new WebGL2RenderingContext(htmlCanvas);
            }

            if (!GL.IsAvailable) {
                using (var app = (JSObject)Runtime.GetGlobalObject("App")) {
                    app.Invoke("webglNotSupported");
                }

                throw new NotSupportedException("This browser does not support WebGL 2");
            }

            GraphicsBackend.LogOpenGLSpecs();
        }
        void IDualityBackend.Shutdown()
        {
            if (activeInstance == this)
                activeInstance = null;

            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated) {
                for (int i = 0; i < this.perVertexTypeVBO.Count; i++) {
                    WebGLBuffer handle = this.perVertexTypeVBO[i];
                    if (handle != null) {
                        GL.DeleteBuffer(handle);
                    }
                }
                this.perVertexTypeVBO.Clear();

                for (int i = 0; i < this.perBatchEBO.Count; i++) {
                    WebGLBuffer handle = this.perBatchEBO[i];
                    if (handle != null) {
                        GL.DeleteBuffer(handle);
                    }
                }
                this.perBatchEBO.Clear();
            }
        }

        void IGraphicsBackend.BeginRendering(IDrawDevice device, RenderOptions options, RenderStats stats)
        {
            DebugCheckOpenGLErrors();

            // ToDo: AA is disabled for now
            //this.CheckContextCaps();

            this.currentDevice = device;
            this.renderOptions = options;
            this.renderStats = stats;

            // Prepare the target surface for rendering
            NativeRenderTarget.Bind(options.Target as NativeRenderTarget);

            // Determine the available size on the active rendering surface
            //Point2 availableSize;
            //if (NativeRenderTarget.BoundRT != null) {
            //	availableSize = new Point2(NativeRenderTarget.BoundRT.Width, NativeRenderTarget.BoundRT.Height);
            //} else {
            //	availableSize = this.externalBackbufferSize;
            //}

            Rect openGLViewport = options.Viewport;

            // Setup viewport and scissor rects
            GL.Viewport((int)openGLViewport.X, (int)openGLViewport.Y, (int)MathF.Ceiling(openGLViewport.W), (int)MathF.Ceiling(openGLViewport.H));
            GL.Scissor((int)openGLViewport.X, (int)openGLViewport.Y, (int)MathF.Ceiling(openGLViewport.W), (int)MathF.Ceiling(openGLViewport.H));

            // Clear buffers
            if (options.ClearFlags != ClearFlag.None) {
                uint glClearMask = 0;
                ColorRgba clearColor = options.ClearColor;
                if ((options.ClearFlags & ClearFlag.Color) != ClearFlag.None) glClearMask |= WebGLRenderingContextBase.COLOR_BUFFER_BIT;
                if ((options.ClearFlags & ClearFlag.Depth) != ClearFlag.None) glClearMask |= WebGLRenderingContextBase.DEPTH_BUFFER_BIT;
                GL.ClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255.0f);
                GL.ClearDepth(options.ClearDepth);
                GL.Clear(glClearMask);
            }

            // Configure Rendering params
            GL.Enable(WebGLRenderingContextBase.SCISSOR_TEST);
            GL.Enable(WebGLRenderingContextBase.DEPTH_TEST);
            if (options.DepthTest)
                GL.DepthFunc(WebGLRenderingContextBase.LEQUAL);
            else
                GL.DepthFunc(WebGLRenderingContextBase.ALWAYS);

            Matrix4 view = options.ViewMatrix;
            Matrix4 projection = options.ProjectionMatrix;
            if (NativeRenderTarget.BoundRT != null) {
                Matrix4 flipOutput = Matrix4.CreateScale(1.0f, -1.0f, 1.0f);
                projection = projection * flipOutput;
            }

            // Convert matrices to float arrays
            GetArrayMatrix(ref view, viewData);
            GetArrayMatrix(ref projection, projectionData);

            // All EBOs can be used again
            lastUsedEBO = 0;
        }
        void IGraphicsBackend.Render(IReadOnlyList<DrawBatch> batches)
        {
            if (batches.Count == 0) return;

            this.RetrieveActiveShaders(batches);
            this.SetupSharedParameters(this.renderOptions.ShaderParameters);

            int drawCalls = 0;
            DrawBatch lastRendered = null;
            for (int i = 0; i < batches.Count; i++) {
                DrawBatch batch = batches[i];
                VertexDeclaration vertexType = batch.VertexBuffer.VertexType;

                // Bind the vertex buffer we'll use. Note that this needs to be done
                // before setting up any vertex format state.
                NativeGraphicsBuffer vertexBuffer = batch.VertexBuffer.NativeVertex as NativeGraphicsBuffer;
                NativeGraphicsBuffer.Bind(GraphicsBufferType.Vertex, vertexBuffer);

                bool first = (i == 0);
                bool sameMaterial =
                    lastRendered != null &&
                    lastRendered.Material.Equals(batch.Material);

                // Setup vertex bindings. Note that the setup differs based on the 
                // materials shader, so material changes can be vertex binding changes.
                if (lastRendered != null) {
                    this.FinishVertexFormat(lastRendered.Material, lastRendered.VertexBuffer.VertexType);
                }
                this.SetupVertexFormat(batch.Material, vertexType);

                // Setup material when changed.
                if (!sameMaterial) {
                    this.SetupMaterial(
                        batch.Material,
                        lastRendered != null ? lastRendered.Material : null);
                }

                // Draw the current batch
                this.DrawVertexBatch(
                    batch.VertexBuffer,
                    batch.VertexRanges,
                    batch.VertexMode);

                drawCalls++;
                lastRendered = batch;
            }

            // Cleanup after rendering
            NativeGraphicsBuffer.Bind(GraphicsBufferType.Vertex, null);
            NativeGraphicsBuffer.Bind(GraphicsBufferType.Index, null);
            if (lastRendered != null) {
                this.FinishMaterial(lastRendered.Material);
                this.FinishVertexFormat(lastRendered.Material, lastRendered.VertexBuffer.VertexType);
            }

            if (this.renderStats != null) {
                this.renderStats.DrawCalls += drawCalls;
            }

            this.FinishSharedParameters();
        }
        void IGraphicsBackend.EndRendering()
        {
            GL.BindBuffer(WebGLRenderingContextBase.ARRAY_BUFFER, null);
            
            this.currentDevice = null;
            this.renderOptions = null;
            this.renderStats = null;
            
            DebugCheckOpenGLErrors();
        }

        INativeGraphicsBuffer IGraphicsBackend.CreateBuffer(GraphicsBufferType type)
        {
            return new NativeGraphicsBuffer(type);
        }
        INativeTexture IGraphicsBackend.CreateTexture()
        {
            return new NativeTexture();
        }
        INativeRenderTarget IGraphicsBackend.CreateRenderTarget()
        {
            return new NativeRenderTarget();
        }
        INativeShaderPart IGraphicsBackend.CreateShaderPart()
        {
            return new NativeShaderPart();
        }
        INativeShaderProgram IGraphicsBackend.CreateShaderProgram()
        {
            return new NativeShaderProgram();
        }
        INativeWindow IGraphicsBackend.CreateWindow(WindowOptions options)
        {
            return new NativeWindow(options);
        }

        void IGraphicsBackend.GetOutputPixelData(IntPtr buffer, ColorDataLayout dataLayout, ColorDataElementType dataElementType, int x, int y, int width, int height)
        {
            NativeRenderTarget lastRt = NativeRenderTarget.BoundRT;
            NativeRenderTarget.Bind(null);
            {
                // Use a temporary local buffer, since the image will be upside-down because
                // of OpenGL's coordinate system and we'll need to flip it before returning.
                byte[] byteData = new byte[width * height * 4];

                // Retrieve pixel data
                GL.ReadPixels(x, y, width, height, dataLayout.ToOpenTK(), dataElementType.ToOpenTK(), TypedArray<Uint8ClampedArray, byte>.From(new Span<byte>(byteData)));

                // Flip the retrieved image vertically
                int bytesPerLine = width * 4;
                byte[] switchLine = new byte[width * 4];
                for (int flipY = 0; flipY < height / 2; flipY++) {
                    int lineIndex = flipY * width * 4;
                    int lineIndex2 = (height - 1 - flipY) * width * 4;

                    // Copy the current line to the switch buffer
                    for (int lineX = 0; lineX < bytesPerLine; lineX++) {
                        switchLine[lineX] = byteData[lineIndex + lineX];
                    }

                    // Copy the opposite line to the current line
                    for (int lineX = 0; lineX < bytesPerLine; lineX++) {
                        byteData[lineIndex + lineX] = byteData[lineIndex2 + lineX];
                    }

                    // Copy the switch buffer to the opposite line
                    for (int lineX = 0; lineX < bytesPerLine; lineX++) {
                        byteData[lineIndex2 + lineX] = switchLine[lineX];
                    }
                }

                // Copy the flipped data to the output buffer
                Marshal.Copy(byteData, 0, buffer, width * height * 4);
            }
            NativeRenderTarget.Bind(lastRt);
        }

        // ToDo: AA is disabled for now
        /*private void CheckContextCaps()
        {
            if (this.contextCapsRetrieved) return;
            this.contextCapsRetrieved = true;

            // Make sure we're not on a render target, which may override
            // some settings that we'd like to get from the main contexts
            // backbuffer.
            NativeRenderTarget oldTarget = NativeRenderTarget.BoundRT;
            NativeRenderTarget.Bind(null);

            int targetSamples, actualSamples;
            DualityActivity activity = DualityActivity.Current;
            if (activity != null) {
                targetSamples = activity.InnerView.GraphicsMode.Samples;
            } else {
                targetSamples = 0;
            }

            // Retrieve how many MSAA samples are actually available, despite what 
            // was offered and requested vis graphics mode.
            CheckOpenGLErrors(true);
            gl.GetInteger(GetPName.Samples, out actualSamples);
            if (CheckOpenGLErrors()) actualSamples = targetSamples;

            NativeRenderTarget.Bind(oldTarget);
        }*/

        /// <summary>
        /// Updates the internal list of active shaders based on the specified rendering batches.
        /// </summary>
        /// <param name="batches"></param>
        private void RetrieveActiveShaders(IReadOnlyList<DrawBatch> batches)
        {
            this.activeShaders.Clear();
            for (int i = 0; i < batches.Count; i++)
            {
                DrawBatch batch = batches[i];
                BatchInfo material = batch.Material;
                DrawTechnique tech = material.Technique.Res ?? DrawTechnique.Solid.Res;
                this.activeShaders.Add(tech.NativeShader as NativeShaderProgram);
            }
        }
        /// <summary>
        /// Applies the specified parameter values to all currently active shaders.
        /// </summary>
        /// <param name="sharedParams"></param>
        /// <seealso cref="RetrieveActiveShaders"/>
        private void SetupSharedParameters(ShaderParameterCollection sharedParams)
        {
            this.sharedSamplerBindings = 0;
            this.sharedShaderParameters.Clear();
            if (sharedParams == null) return;

            foreach (NativeShaderProgram shader in this.activeShaders)
            {
                NativeShaderProgram.Bind(shader);

                ShaderFieldInfo[] varInfo = shader.Fields;
                NativeShaderProgram.FieldLocation[] locations = shader.FieldLocations;

                // Setup shared sampler bindings and uniform data
                for (int i = 0; i < varInfo.Length; i++) {
                    ref ShaderFieldInfo field = ref varInfo[i];

                    if (field.Scope == ShaderFieldScope.Attribute) continue;
                    if (field.Type == ShaderFieldType.Sampler2D) {
                        ContentRef<Texture> texRef;
                        if (!sharedParams.TryGetInternal(field.Name, out texRef)) continue;

                        NativeTexture.Bind(texRef, this.sharedSamplerBindings);
                        GL.Uniform1i(locations[i].Uniform, this.sharedSamplerBindings);

                        this.sharedSamplerBindings++;
                    } else {
                        float[] data;
                        if (!sharedParams.TryGetInternal(field.Name, out data)) continue;

                        NativeShaderProgram.SetUniform(ref field, locations[i].Uniform, data);
                    }

                    this.sharedShaderParameters.Add(field.Name);
                }
            }

            NativeShaderProgram.Bind(null);
        }

        private void SetupVertexFormat(BatchInfo material, VertexDeclaration vertexDeclaration)
        {
            DrawTechnique technique = material.Technique.Res ?? DrawTechnique.Solid.Res;
            NativeShaderProgram nativeProgram = (technique.NativeShader ?? DrawTechnique.Solid.Res.NativeShader) as NativeShaderProgram;

            VertexElement[] elements = vertexDeclaration.Elements;
            ShaderFieldInfo[] varInfo = nativeProgram.Fields;
            NativeShaderProgram.FieldLocation[] locations = nativeProgram.FieldLocations;

            bool[] varUsed = new bool[varInfo.Length];
            for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                int selectedVar = -1;
                for (int varIndex = 0; varIndex < varInfo.Length; varIndex++) {
                    if (varUsed[varIndex]) continue;

                    if (locations[varIndex].Attrib == -1 || varInfo[varIndex].Scope != ShaderFieldScope.Attribute) {
                        varUsed[varIndex] = true;
                        continue;
                    }

                    if (!ShaderVarMatches(
                        ref varInfo[varIndex],
                        elements[elementIndex].Type,
                        elements[elementIndex].Count))
                        continue;

                    //if (elements[elementIndex].Role != VertexElementRole.Unknown && varInfo[varIndex].Name != elements[elementIndex].Role.ToString()) {
                    //	continue;
                    //}

                    selectedVar = varIndex;
                    varUsed[varIndex] = true;
                    break;
                }
                if (selectedVar == -1) continue;

                uint attribType;
                switch (elements[elementIndex].Type) {
                    default:
                    case VertexElementType.Float: attribType = WebGLRenderingContextBase.FLOAT; break;
                    case VertexElementType.Byte: attribType = WebGLRenderingContextBase.UNSIGNED_BYTE; break;
                }

                GL.EnableVertexAttribArray((uint)locations[selectedVar].Attrib);
                GL.VertexAttribPointer(
                    (uint)locations[selectedVar].Attrib,
                    elements[elementIndex].Count,
                    attribType,
                    true,
                    vertexDeclaration.Size,
                    (uint)elements[elementIndex].Offset);
            }
        }

        private void SetupMaterial(BatchInfo material, BatchInfo lastMaterial)
        {
            DrawTechnique tech = material.Technique.Res ?? DrawTechnique.Solid.Res;
            DrawTechnique lastTech = lastMaterial != null ? lastMaterial.Technique.Res : null;

            // Setup BlendType
            if (lastTech == null || tech.Blending != lastTech.Blending) {
                this.SetupBlendType(tech.Blending);
            }

            // Bind Shader
            NativeShaderProgram nativeShader = tech.NativeShader as NativeShaderProgram;
            NativeShaderProgram.Bind(nativeShader);

            // Setup shader data
            ShaderFieldInfo[] varInfo = nativeShader.Fields;
            NativeShaderProgram.FieldLocation[] locations = nativeShader.FieldLocations;

            // Setup sampler bindings and uniform data
            int curSamplerIndex = this.sharedSamplerBindings;
            for (int i = 0; i < varInfo.Length; i++) {
                ref ShaderFieldInfo field = ref varInfo[i];

                if (field.Scope == ShaderFieldScope.Attribute) continue;
                if (this.sharedShaderParameters.Contains(field.Name)) continue;

                if (field.Type == ShaderFieldType.Sampler2D) {
                    ContentRef<Texture> texRef = material.GetInternalTexture(field.Name);
                    NativeTexture.Bind(texRef, curSamplerIndex);
                    GL.Uniform1i(locations[i].Uniform, curSamplerIndex);

                    curSamplerIndex++;
                } else {
                    float[] data;
                    if (varInfo[i].Name == "ModelView") {
                        data = viewData;
                    } else if (varInfo[i].Name == "Projection") {
                        data = projectionData;
                    } else {
                        data = material.GetInternalData(field.Name);
                        if (data == null) continue;
                    }

                    NativeShaderProgram.SetUniform(ref varInfo[i], locations[i].Uniform, data);
                }
            }
            NativeTexture.ResetBinding(curSamplerIndex);
        }

        private void SetupBlendType(BlendMode mode, bool depthWrite = true)
        {
            switch (mode) {
                default:
                case BlendMode.Reset:
                case BlendMode.Solid:
                    GL.DepthMask(depthWrite);
                    GL.Disable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    break;
                case BlendMode.Mask:
                    GL.DepthMask(depthWrite);
                    GL.Disable(WebGLRenderingContextBase.BLEND);
                    GL.Enable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    break;
                case BlendMode.Alpha:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFuncSeparate(WebGLRenderingContextBase.SRC_ALPHA, WebGLRenderingContextBase.ONE_MINUS_SRC_ALPHA, WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ONE_MINUS_SRC_ALPHA);
                    break;
                case BlendMode.AlphaPre:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFuncSeparate(WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ONE_MINUS_SRC_ALPHA, WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ONE_MINUS_SRC_ALPHA);
                    break;
                case BlendMode.Add:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFuncSeparate(WebGLRenderingContextBase.SRC_ALPHA, WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ONE);
                    break;
                case BlendMode.Light:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFuncSeparate(WebGLRenderingContextBase.DST_COLOR, WebGLRenderingContextBase.ONE, WebGLRenderingContextBase.ZERO, WebGLRenderingContextBase.ONE);
                    break;
                case BlendMode.Multiply:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFunc(WebGLRenderingContextBase.DST_COLOR, WebGLRenderingContextBase.ZERO);
                    break;
                case BlendMode.Invert:
                    GL.DepthMask(false);
                    GL.Enable(WebGLRenderingContextBase.BLEND);
                    GL.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
                    GL.BlendFunc(WebGLRenderingContextBase.ONE_MINUS_DST_COLOR, WebGLRenderingContextBase.ONE_MINUS_SRC_COLOR);
                    break;
            }
        }

        /// <summary>
        /// Draws the vertices of a single <see cref="DrawBatch"/>, after all other rendering state
        /// has been set up accordingly outside this method.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="ranges"></param>
        /// <param name="mode"></param>
        private unsafe void DrawVertexBatch(VertexBuffer buffer, RawList<VertexDrawRange> ranges, VertexMode mode)
        {
            NativeGraphicsBuffer indexBuffer = (buffer.IndexCount > 0 ? buffer.NativeIndex : null) as NativeGraphicsBuffer;
            IndexDataElementType indexType = buffer.IndexType;

            // Since the QUADS primitive is deprecated in OpenGL 3.0 and not available in OpenGL ES,
            // we'll emulate this with an ad-hoc index buffer object that we generate here.
            if (mode == VertexMode.Quads) {
                VertexDrawRange[] rangeData = ranges.Data;
                int rangeCount = ranges.Count;

                // Compute number of all vertices in batch and resize static cache if necessary
                int numberOfVertices = 0;
                for (int r = 0; r < rangeCount; r++) {
                    numberOfVertices += rangeData[r].Count;
                }

                int numberOfIndices = (numberOfVertices / 4) * 6;
                if (indexCache == null || indexCache.Length < numberOfIndices) {
                    indexCache = new short[numberOfIndices];
                }

                // Expand every 1 quad (4 vertices) to 2 triangles (2x3 vertices) using index buffer
                int destIndex = 0;
                for (int r = 0; r < rangeCount; r++) {
                    int srcIndex = rangeData[r].Index;
                    int count = rangeData[r].Count;

                    for (int offset = 0; offset < count; offset += 4, destIndex += 6) {
                        indexCache[destIndex] = (short)(srcIndex + offset);
                        indexCache[destIndex + 1] = (short)(srcIndex + offset + 1);
                        indexCache[destIndex + 2] = (short)(srcIndex + offset + 2);

                        indexCache[destIndex + 3] = (short)(srcIndex + offset + 2);
                        indexCache[destIndex + 4] = (short)(srcIndex + offset + 3);
                        indexCache[destIndex + 5] = (short)(srcIndex + offset);
                    }
                }

                // Find/allocate unused EBO and copy indices to it
                WebGLBuffer handle;
                if (lastUsedEBO < perBatchEBO.Count) {
                    handle = perBatchEBO[lastUsedEBO++];
                } else {
                    handle = GL.CreateBuffer();
                    perBatchEBO.Add(handle);
                }

                int bufferSize = numberOfIndices * sizeof(short);
                GL.BindBuffer(WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER, handle);
                fixed (short* ptr = indexCache) {
                    GL.BufferData(WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER, TypedArray<Uint8ClampedArray, byte>.From(new Span<byte>(ptr, bufferSize)), WebGLRenderingContextBase.STREAM_DRAW);
                }

                // Draw the current batch using indices
                GL.DrawElements(
                    WebGLRenderingContextBase.TRIANGLES,
                    numberOfIndices,
                    WebGLRenderingContextBase.UNSIGNED_SHORT,
                    0);

                NativeGraphicsBuffer.RebindIndexBuffer();
            } else {
                // Rendering using index buffer
                if (indexBuffer != null) {
                    if (ranges != null && ranges.Count > 0) {
                        App.Log(
                            "Rendering {0} instances that use index buffers do not support specifying vertex ranges, " +
                            "since the two features are mutually exclusive.",
                            typeof(DrawBatch).Name,
                            typeof(VertexMode).Name);
                    }

                    NativeGraphicsBuffer.Bind(GraphicsBufferType.Index, indexBuffer);

                    uint openTkMode = GetOpenTKVertexMode(mode);
                    uint openTkIndexType = GetOpenTKIndexType(indexType);
                    GL.DrawElements(
                        openTkMode,
                        buffer.IndexCount,
                        openTkIndexType,
                        0);
                }
                // Rendering using an array of vertex ranges
                else {
                    NativeGraphicsBuffer.Bind(GraphicsBufferType.Index, null);

                    uint openTkMode = GetOpenTKVertexMode(mode);
                    VertexDrawRange[] rangeData = ranges.Data;
                    int rangeCount = ranges.Count;
                    for (int r = 0; r < rangeCount; r++) {
                        GL.DrawArrays(
                            openTkMode,
                            rangeData[r].Index,
                            rangeData[r].Count);
                    }
                }
            }
        }

        private void FinishSharedParameters()
        {
            NativeTexture.ResetBinding();

            this.sharedSamplerBindings = 0;
            this.sharedShaderParameters.Clear();
            this.activeShaders.Clear();
        }
        private void FinishVertexFormat(BatchInfo material, VertexDeclaration vertexDeclaration)
        {
            DrawTechnique technique = material.Technique.Res ?? DrawTechnique.Solid.Res;
            NativeShaderProgram nativeProgram = (technique.NativeShader ?? DrawTechnique.Solid.Res.NativeShader) as NativeShaderProgram;

            //VertexDeclaration vertexDeclaration = renderBatch.VertexDeclaration;
            //VertexElement[] elements = vertexDeclaration.Elements;
            ShaderFieldInfo[] varInfo = nativeProgram.Fields;
            NativeShaderProgram.FieldLocation[] locations = nativeProgram.FieldLocations;

            for (int varIndex = 0; varIndex < varInfo.Length; varIndex++) {
                if (locations[varIndex].Attrib == -1) continue;

                GL.DisableVertexAttribArray(locations[varIndex].Attrib);
            }
        }
        private void FinishMaterial(BatchInfo material)
        {
            this.SetupBlendType(BlendMode.Reset);
            NativeShaderProgram.Bind(null);
            NativeTexture.ResetBinding(this.sharedSamplerBindings);
        }

        private static uint GetOpenTKVertexMode(VertexMode mode)
        {
            switch (mode) {
                default:
                case VertexMode.Points: return WebGLRenderingContextBase.POINTS;
                case VertexMode.Lines: return WebGLRenderingContextBase.LINES;
                case VertexMode.LineStrip: return WebGLRenderingContextBase.LINE_STRIP;
                case VertexMode.LineLoop: return WebGLRenderingContextBase.LINE_LOOP;

                case VertexMode.Quads:
                case VertexMode.Triangles: return WebGLRenderingContextBase.TRIANGLES;

                case VertexMode.TriangleStrip: return WebGLRenderingContextBase.TRIANGLE_STRIP;
                case VertexMode.TriangleFan: return WebGLRenderingContextBase.TRIANGLE_FAN;
            }
        }
        private static uint GetOpenTKIndexType(IndexDataElementType indexType)
        {
            switch (indexType) {
                default:
                case IndexDataElementType.UnsignedByte: return WebGLRenderingContextBase.UNSIGNED_BYTE;
                case IndexDataElementType.UnsignedShort: return WebGLRenderingContextBase.UNSIGNED_SHORT;
            }
        }
        private static void GetArrayMatrix(ref Matrix4 source, float[] target)
        {
            target[0] = source.M11;
            target[1] = source.M12;
            target[2] = source.M13;
            target[3] = source.M14;

            target[4] = source.M21;
            target[5] = source.M22;
            target[6] = source.M23;
            target[7] = source.M24;

            target[8] = source.M31;
            target[9] = source.M32;
            target[10] = source.M33;
            target[11] = source.M34;

            target[12] = source.M41;
            target[13] = source.M42;
            target[14] = source.M43;
            target[15] = source.M44;
        }

        private static bool ShaderVarMatches(ref ShaderFieldInfo varInfo, VertexElementType type, int count)
        {
            //if (varInfo.Scope != ShaderFieldScope.Attribute) return false;

            Type elementPrimitive = varInfo.Type.GetElementPrimitive();
            Type requiredPrimitive = null;
            switch (type) {
                case VertexElementType.Byte:
                    requiredPrimitive = typeof(byte);
                    break;
                case VertexElementType.Float:
                    requiredPrimitive = typeof(float);
                    break;
            }
            // ToDo: Bugfix to allow Color (byte to float) conversion
            if (elementPrimitive != requiredPrimitive && !(elementPrimitive == typeof(float) && requiredPrimitive == typeof(byte))) {
                return false;
            }

            int elementCount = varInfo.Type.GetElementCount();
            if (count != elementCount * varInfo.ArrayLength)
                return false;

            return true;
        }

        private static void LogOpenGLSpecs()
        {
            string versionString = null;
            try {
                CheckOpenGLErrors();
                versionString = (string)GL.GetParameter(WebGLRenderingContextBase.VERSION);
                App.Log(
                    "OpenGL Version: {0}" + Environment.NewLine +
                    "  Vendor: {1}" + Environment.NewLine +
                    "  Renderer: {2}" + Environment.NewLine +
                    "  Shader Version: {3}",
                    versionString,
                    (string)GL.GetParameter(WebGLRenderingContextBase.VENDOR),
                    (string)GL.GetParameter(WebGLRenderingContextBase.RENDERER),
                    (string)GL.GetParameter(WebGLRenderingContextBase.SHADING_LANGUAGE_VERSION));
                CheckOpenGLErrors();
            } catch (Exception e) {
                App.Log("Can't determine OpenGL specs, because an error occurred: {0}", e);
            }

            // Parse the OpenGL version string in order to determine if it's sufficient
            if (versionString != null) {
                string[] token = versionString.Split(' ');
                for (int i = 0; i < token.Length; i++) {
                    Version version;
                    if (Version.TryParse(token[i], out version)) {
                        if (version.Major < MinWebGLVersion.Major || (version.Major == MinWebGLVersion.Major && version.Minor < MinWebGLVersion.Minor)) {
                            App.Log(
                                "The detected OpenGL version {0} appears to be lower than the required minimum. Version {1} or higher is required to run Duality applications.",
                                version,
                                MinWebGLVersion);
                        }
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Checks for errors that might have occurred during video processing. You should avoid calling this method due to performance reasons.
        /// Only use it on suspect.
        /// </summary>
        /// <param name="silent">If true, errors aren't logged.</param>
        /// <returns>True, if an error occurred, false if not.</returns>
        public static bool CheckOpenGLErrors(bool silent = false, [CallerMemberName] string callerInfoMember = null, [CallerFilePath] string callerInfoFile = null, [CallerLineNumber] int callerInfoLine = -1)
        {
            int error;
            bool found = false;
            while ((error = GL.GetError()) != WebGLRenderingContextBase.NO_ERROR) {
                if (!silent) {
                    App.Log(
                        "Internal OpenGL error, code {0} at {1} in {2}, line {3}.",
                        error,
                        callerInfoMember,
                        callerInfoFile,
                        callerInfoLine);
                }
                found = true;
            }
            if (found && !silent && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            return found;
        }
        /// <summary>
        /// Checks for OpenGL errors using <see cref="CheckOpenGLErrors"/> when both compiled in debug mode and a with an attached debugger.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugCheckOpenGLErrors([CallerMemberName] string callerInfoMember = null, [CallerFilePath] string callerInfoFile = null, [CallerLineNumber] int callerInfoLine = -1)
        {
            if (!System.Diagnostics.Debugger.IsAttached) return;
            CheckOpenGLErrors(false, callerInfoMember, callerInfoFile, callerInfoLine);
        }

        internal static Point2 GetCanvasSize()
        {
            return cachedCanvasSize;
        }

        internal static void SetCanvasSize(Point2 size)
        {
            if (htmlCanvas == null) {
                return;
            }

            htmlCanvas.SetObjectProperty("width", size.X);
            htmlCanvas.SetObjectProperty("height", size.Y);

            cachedCanvasSize = size;
        }
    }
}