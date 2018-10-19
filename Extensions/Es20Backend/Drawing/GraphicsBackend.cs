using Duality.Drawing;
using Duality.Resources;
using OpenTK;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Duality.Backend.Es20
{
    public class GraphicsBackend : IGraphicsBackend
    {
        private static readonly Version MinOpenGLVersion = new Version(2, 0);

        private static GraphicsBackend activeInstance;
        public static GraphicsBackend ActiveInstance
        {
            get { return activeInstance; }
        }

        private IDrawDevice currentDevice;
        private RenderOptions renderOptions;
        private RenderStats renderStats;
        private OpenTK.Graphics.GraphicsMode defaultGraphicsMode;
        private RawList<uint> perVertexTypeVBO = new RawList<uint>();
        private NativeWindow activeWindow;
        private Point2 externalBackbufferSize = Point2.Zero;
        private HashSet<NativeShaderProgram> activeShaders = new HashSet<NativeShaderProgram>();
        private HashSet<string> sharedShaderParameters = new HashSet<string>();
        private int sharedSamplerBindings = 0;

        private RawList<uint> perBatchEBO = new RawList<uint>();
        private int lastUsedEBO;
        private short[] indexCache;

        private float[] viewData = new float[16];
        private float[] projectionData = new float[16];

        public IEnumerable<ScreenResolution> AvailableScreenResolutions
        {
            get
            {
                return DisplayDevice.Default.AvailableResolutions
                    .Select(resolution => new ScreenResolution(resolution.Width, resolution.Height, resolution.RefreshRate))
                    .Distinct();
            }
        }
        public Point2 ExternalBackbufferSize
        {
            get { return this.externalBackbufferSize; }
            set { this.externalBackbufferSize = value; }
        }

        string IDualityBackend.Id
        {
            get { return "Es20Backend"; }
        }
        string IDualityBackend.Name
        {
            get { return "OpenGL ES 2.0"; }
        }
        int IDualityBackend.Priority
        {
            get { return -1; }
        }

        bool IDualityBackend.CheckAvailable()
        {
            // AccessViolation is thrown because of this...
            /*string versionString;
            try {
                versionString = GL.GetString(StringName.Version);
                CheckOpenGLErrors();
            } catch {
                return false;
            }

            // Parse the OpenGL ES version string in order to determine if it's sufficient
            if (versionString != null) {
                string[] token = versionString.Split(' ');
                for (int i = 0; i < token.Length; i++) {
                    Version version;
                    if (Version.TryParse(token[i], out version)) {
                        return !(version.Major < MinOpenGLVersion.Major ||
                                 (version.Major == MinOpenGLVersion.Major && version.Minor < MinOpenGLVersion.Minor));
                    }
                }
            }
            return false;*/

            return true;
        }

        void IDualityBackend.Init()
        {
            activeInstance = this;

            Console.WriteLine("Active graphics backend: OpenGL ES 2.0");
        }
        void IDualityBackend.Shutdown()
        {
            if (activeInstance == this)
                activeInstance = null;

            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated) {
                //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
                for (int i = 0; i < this.perVertexTypeVBO.Count; i++) {
                    uint handle = this.perVertexTypeVBO[i];
                    if (handle != 0) {
                        GL.DeleteBuffers(1, ref handle);
                    }
                }
                this.perVertexTypeVBO.Clear();

                for (int i = 0; i < this.perBatchEBO.Count; i++) {
                    uint handle = this.perBatchEBO[i];
                    if (handle != 0) {
                        GL.DeleteBuffers(1, ref handle);
                    }
                }
                this.perBatchEBO.Clear();
            }
        }

        void IGraphicsBackend.BeginRendering(IDrawDevice device, RenderOptions options, RenderStats stats)
        {
            DebugCheckOpenGLErrors();

            this.currentDevice = device;
            this.renderOptions = options;
            this.renderStats = stats;

            // Prepare the target surface for rendering
            NativeRenderTarget.Bind(options.Target as NativeRenderTarget);

            // Determine the available size on the active rendering surface
            //Point2 availableSize;
            //if (NativeRenderTarget.BoundRT != null) {
            //    availableSize = new Point2(NativeRenderTarget.BoundRT.Width, NativeRenderTarget.BoundRT.Height);
            //} else {
            //    availableSize = this.externalBackbufferSize;
            //}

            Rect openGLViewport = options.Viewport;

            // Setup viewport and scissor rects
            GL.Viewport((int)openGLViewport.X, (int)openGLViewport.Y, (int)MathF.Ceiling(openGLViewport.W), (int)MathF.Ceiling(openGLViewport.H));
            GL.Scissor((int)openGLViewport.X, (int)openGLViewport.Y, (int)MathF.Ceiling(openGLViewport.W), (int)MathF.Ceiling(openGLViewport.H));

            // Clear buffers
            ClearBufferMask glClearMask = 0;
            ColorRgba clearColor = options.ClearColor;
            if ((options.ClearFlags & ClearFlag.Color) != ClearFlag.None) glClearMask |= ClearBufferMask.ColorBufferBit;
            if ((options.ClearFlags & ClearFlag.Depth) != ClearFlag.None) glClearMask |= ClearBufferMask.DepthBufferBit;
            GL.ClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255.0f);
            GL.ClearDepth(options.ClearDepth);
            GL.Clear(glClearMask);

            // Configure Rendering params
            GL.Enable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.DepthTest);
            if (options.DepthTest)
                GL.DepthFunc(DepthFunction.Lequal);
            else
                GL.DepthFunc(DepthFunction.Always);

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
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            
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
            // Only one game window allowed at a time
            if (this.activeWindow != null)
            {
                (this.activeWindow as INativeWindow).Dispose();
                this.activeWindow = null;
            }

            // Create a window and keep track of it
            this.activeWindow = new NativeWindow(defaultGraphicsMode, options);
            return this.activeWindow;
        }

        void IGraphicsBackend.GetOutputPixelData(IntPtr buffer, ColorDataLayout dataLayout, ColorDataElementType dataElementType, int x, int y, int width, int height)
        {
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            NativeRenderTarget lastRt = NativeRenderTarget.BoundRT;
            NativeRenderTarget.Bind(null);
            {
                // Use a temporary local buffer, since the image will be upside-down because
                // of OpenGL's coordinate system and we'll need to flip it before returning.
                byte[] byteData = new byte[width * height * 4];

                // Retrieve pixel data
                GL.ReadPixels(x, y, width, height, dataLayout.ToOpenTK(), dataElementType.ToOpenTK(), byteData);

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
                int[] locations = shader.FieldLocations;

                // Setup shared sampler bindings and uniform data
                for (int i = 0; i < varInfo.Length; i++) {
                    ref ShaderFieldInfo field = ref varInfo[i];

                    if (field.Scope == ShaderFieldScope.Attribute) continue;
                    if (field.Type == ShaderFieldType.Sampler2D) {
                        ContentRef<Texture> texRef;
                        if (!sharedParams.TryGetInternal(field.Name, out texRef)) continue;

                        NativeTexture.Bind(texRef, this.sharedSamplerBindings);
                        GL.Uniform1(locations[i], this.sharedSamplerBindings);

                        this.sharedSamplerBindings++;
                    } else {
                        float[] data;
                        if (!sharedParams.TryGetInternal(field.Name, out data)) continue;

                        NativeShaderProgram.SetUniform(ref field, locations[i], data);
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
            int[] locations = nativeProgram.FieldLocations;

            bool[] varUsed = new bool[varInfo.Length];
            for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                int selectedVar = -1;
                for (int varIndex = 0; varIndex < varInfo.Length; varIndex++) {
                    if (varUsed[varIndex]) continue;

                    if (locations[varIndex] == -1 || varInfo[varIndex].Scope != ShaderFieldScope.Attribute) {
                        varUsed[varIndex] = true;
                        continue;
                    }

                    if (!ShaderVarMatches(
                        ref varInfo[varIndex],
                        elements[elementIndex].Type,
                        elements[elementIndex].Count))
                        continue;

                    //if (elements[elementIndex].Role != VertexElementRole.Unknown && varInfo[varIndex].Name != elements[elementIndex].Role.ToString()) {
                    //    continue;
                    //}

                    selectedVar = varIndex;
                    varUsed[varIndex] = true;
                    break;
                }
                if (selectedVar == -1) continue;

                VertexAttribPointerType attribType;
                switch (elements[elementIndex].Type) {
                    default:
                    case VertexElementType.Float: attribType = VertexAttribPointerType.Float; break;
                    case VertexElementType.Byte: attribType = VertexAttribPointerType.UnsignedByte; break;
                }

                GL.EnableVertexAttribArray(locations[selectedVar]);
                GL.VertexAttribPointer(
                    locations[selectedVar],
                    elements[elementIndex].Count,
                    attribType,
                    true,
                    vertexDeclaration.Size,
                    elements[elementIndex].Offset);
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
            int[] locations = nativeShader.FieldLocations;

            // Setup sampler bindings and uniform data
            int curSamplerIndex = this.sharedSamplerBindings;
            for (int i = 0; i < varInfo.Length; i++) {
                ref ShaderFieldInfo field = ref varInfo[i];

                if (field.Scope == ShaderFieldScope.Attribute) continue;
                if (this.sharedShaderParameters.Contains(field.Name)) continue;

                if (field.Type == ShaderFieldType.Sampler2D) {
                    ContentRef<Texture> texRef = material.GetInternalTexture(field.Name);
                    NativeTexture.Bind(texRef, curSamplerIndex);
                    GL.Uniform1(locations[i], curSamplerIndex);

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

                    NativeShaderProgram.SetUniform(ref varInfo[i], locations[i], data);
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
                    GL.Disable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    break;
                case BlendMode.Mask:
                    GL.DepthMask(depthWrite);
                    GL.Disable(EnableCap.Blend);
                    GL.Enable(EnableCap.SampleAlphaToCoverage);
                    break;
                case BlendMode.Alpha:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                    break;
                case BlendMode.AlphaPre:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                    break;
                case BlendMode.Add:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One, BlendingFactorSrc.One, BlendingFactorDest.One);
                    break;
                case BlendMode.Light:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.One, BlendingFactorSrc.Zero, BlendingFactorDest.One);
                    break;
                case BlendMode.Multiply:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.Zero);
                    break;
                case BlendMode.Invert:
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Disable(EnableCap.SampleAlphaToCoverage);
                    GL.BlendFunc(BlendingFactorSrc.OneMinusDstColor, BlendingFactorDest.OneMinusSrcColor);
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
                uint handle;
                if (lastUsedEBO < perBatchEBO.Count) {
                    handle = perBatchEBO[lastUsedEBO++];
                } else {
                    GL.GenBuffers(1, out handle);
                    perBatchEBO.Add(handle);
                }

                int bufferSize = numberOfIndices * sizeof(short);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, handle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
                fixed (short* ptr = indexCache) {
                    GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)bufferSize, (IntPtr)ptr, BufferUsageHint.StreamDraw);
                }

                // Draw the current batch using indices
                GL.DrawElements(
                    PrimitiveType.Triangles,
                    numberOfIndices,
                    DrawElementsType.UnsignedShort,
                    IntPtr.Zero);

                NativeGraphicsBuffer.RebindIndexBuffer();
            } else {
                // Rendering using index buffer
                if (indexBuffer != null) {
                    if (ranges != null && ranges.Count > 0) {
                        Console.WriteLine(
                            "Rendering {0} instances that use index buffers do not support specifying vertex ranges, " +
                            "since the two features are mutually exclusive.",
                            typeof(DrawBatch).Name,
                            typeof(VertexMode).Name);
                    }

                    NativeGraphicsBuffer.Bind(GraphicsBufferType.Index, indexBuffer);

                    PrimitiveType openTkMode = GetOpenTKVertexMode(mode);
                    DrawElementsType openTkIndexType = GetOpenTKIndexType(indexType);
                    GL.DrawElements(
                        openTkMode,
                        buffer.IndexCount,
                        openTkIndexType,
                        IntPtr.Zero);
                }
                // Rendering using an array of vertex ranges
                else {
                    NativeGraphicsBuffer.Bind(GraphicsBufferType.Index, null);

                    PrimitiveType openTkMode = GetOpenTKVertexMode(mode);
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
            int[] locations = nativeProgram.FieldLocations;

            for (int varIndex = 0; varIndex < varInfo.Length; varIndex++) {
                if (locations[varIndex] == -1) continue;

                GL.DisableVertexAttribArray(locations[varIndex]);
            }
        }
        private void FinishMaterial(BatchInfo material)
        {
            //DrawTechnique tech = material.Technique.Res;
            this.SetupBlendType(BlendMode.Reset);
            NativeShaderProgram.Bind(null);
            NativeTexture.ResetBinding(this.sharedSamplerBindings);
        }

        private static PrimitiveType GetOpenTKVertexMode(VertexMode mode)
        {
            switch (mode) {
                default:
                case VertexMode.Points: return PrimitiveType.Points;
                case VertexMode.Lines: return PrimitiveType.Lines;
                case VertexMode.LineStrip: return PrimitiveType.LineStrip;
                case VertexMode.LineLoop: return PrimitiveType.LineLoop;

                case VertexMode.Quads:
                case VertexMode.Triangles: return PrimitiveType.Triangles;

                case VertexMode.TriangleStrip: return PrimitiveType.TriangleStrip;
                case VertexMode.TriangleFan: return PrimitiveType.TriangleFan;
            }
        }
        private static DrawElementsType GetOpenTKIndexType(IndexDataElementType indexType)
        {
            switch (indexType) {
                default:
                case IndexDataElementType.UnsignedByte: return DrawElementsType.UnsignedByte;
                case IndexDataElementType.UnsignedShort: return DrawElementsType.UnsignedShort;
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

        public static void LogOpenGLSpecs()
        {
            string versionString = null;
            try {
                CheckOpenGLErrors();
                versionString = GL.GetString(StringName.Version);
                Console.WriteLine(
                    "OpenGL Version: {0}" + Environment.NewLine +
                    "  Vendor: {1}" + Environment.NewLine +
                    "  Renderer: {2}" + Environment.NewLine +
                    "  Shader Version: {3}",
                    versionString,
                    GL.GetString(StringName.Vendor),
                    GL.GetString(StringName.Renderer),
                    GL.GetString(StringName.ShadingLanguageVersion));
                CheckOpenGLErrors();
            } catch (Exception e) {
                Console.WriteLine("Can't determine OpenGL specs, because an error occurred: {0}", e);
            }

            // Parse the OpenGL version string in order to determine if it's sufficient
            if (versionString != null) {
                string[] token = versionString.Split(' ');
                for (int i = 0; i < token.Length; i++) {
                    Version version;
                    if (Version.TryParse(token[i], out version)) {
                        if (version.Major < MinOpenGLVersion.Major || (version.Major == MinOpenGLVersion.Major && version.Minor < MinOpenGLVersion.Minor)) {
                            Console.WriteLine(
                                "The detected OpenGL version {0} appears to be lower than the required minimum. Version {1} or higher is required to run Duality applications.",
                                version,
                                MinOpenGLVersion);
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
            ErrorCode error;
            bool found = false;
            while ((error = GL.GetError()) != ErrorCode.NoError) {
                if (!silent) {
                    Console.WriteLine(
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
    }
}