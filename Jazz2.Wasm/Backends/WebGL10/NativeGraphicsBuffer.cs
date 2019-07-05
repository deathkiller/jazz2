using System;
using Duality.Drawing;
using WebAssembly.Core;
using WebGLDotNET;

namespace Duality.Backend.Wasm.WebGL10
{
    public class NativeGraphicsBuffer : INativeGraphicsBuffer
    {
        private static NativeGraphicsBuffer boundVertex = null;
        private static NativeGraphicsBuffer boundIndex = null;
        
        public static void Bind(GraphicsBufferType type, NativeGraphicsBuffer buffer)
        {
            if (GetBound(type) == buffer) return;
            SetBound(type, buffer);

            uint target = ToOpenTKBufferType(type);
            GraphicsBackend.GL.BindBuffer(target, buffer != null ? buffer.Handle : null);
        }
        private static void SetBound(GraphicsBufferType type, NativeGraphicsBuffer buffer)
        {
            if (type == GraphicsBufferType.Vertex) boundVertex = buffer;
            else if (type == GraphicsBufferType.Index) boundIndex = buffer;
            else return;
        }
        private static NativeGraphicsBuffer GetBound(GraphicsBufferType type)
        {
            if (type == GraphicsBufferType.Vertex) return boundVertex;
            else if (type == GraphicsBufferType.Index) return boundIndex;
            else return null;
        }

        internal static void RebindIndexBuffer()
        {
            GraphicsBackend.GL.BindBuffer(WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER, boundIndex != null ? boundIndex.Handle : null);
        }


        private WebGLBuffer handle = null;
        private GraphicsBufferType type = GraphicsBufferType.Vertex;
        private int bufferSize = 0;

        public WebGLBuffer Handle
        {
            get { return this.handle; }
        }
        public GraphicsBufferType BufferType
        {
            get { return this.type; }
        }
        public int Length
        {
            get { return this.bufferSize; }
        }

        public NativeGraphicsBuffer(GraphicsBufferType type)
        {
            this.handle = GraphicsBackend.GL.CreateBuffer();
            this.type = type;
        }

        public void SetupEmpty(int size)
        {
            if (size < 0) throw new ArgumentException("Size cannot be less than zero.");

            NativeGraphicsBuffer prevBound = GetBound(this.type);
            Bind(this.type, this);

            uint target = ToOpenTKBufferType(this.type);
            GraphicsBackend.GL.BufferData(target, (ulong)size, WebGLRenderingContextBase.STREAM_DRAW);

            this.bufferSize = size;

            Bind(this.type, prevBound);
        }
        public unsafe void LoadData(IntPtr data, int size)
        {
            if (size < 0) throw new ArgumentException("Size cannot be less than zero.");

            NativeGraphicsBuffer prevBound = GetBound(this.type);
            Bind(this.type, this);

            uint target = ToOpenTKBufferType(this.type);
            if (data == IntPtr.Zero || size == 0) {
                // ToDo
                //GraphicsBackend.GL.BufferData(target, null, WebGLRenderingContextBase.STREAM_DRAW);
            } else {
                GraphicsBackend.GL.BufferData(target, TypedArray<Uint8ClampedArray, byte>.From(new Span<byte>(data.ToPointer(), size)), WebGLRenderingContextBase.STREAM_DRAW);
            }

            this.bufferSize = size;

            Bind(this.type, prevBound);
        }
        public unsafe void LoadSubData(IntPtr offset, IntPtr data, int size)
        {
            if (size < 0) throw new ArgumentException("Size cannot be less than zero.");
            if (this.bufferSize == 0) throw new InvalidOperationException(string.Format(
                "Cannot update {0}, because its storage was not initialized yet.",
                typeof(NativeGraphicsBuffer).Name));
            if ((uint)offset + size > this.bufferSize) throw new ArgumentException(string.Format(
                "Cannot update {0} with offset {1} and size {2}, as this exceeds the internal " +
                "storage size {3}.",
                typeof(NativeGraphicsBuffer).Name,
                offset,
                size,
                this.bufferSize));

            NativeGraphicsBuffer prevBound = GetBound(this.type);
            Bind(this.type, this);

            uint target = ToOpenTKBufferType(this.type);
            GraphicsBackend.GL.BufferSubData(target, (uint)offset, TypedArray<Uint8ClampedArray, byte>.From(new Span<byte>(data.ToPointer(), size)));

            Bind(this.type, prevBound);
        }
        public void Dispose()
        {
            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated &&
                this.handle != null)
            {
                //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
                GraphicsBackend.GL.DeleteBuffer(this.handle);
            }
            this.handle = null;
            this.bufferSize = 0;
        }

        private static uint ToOpenTKBufferType(GraphicsBufferType value)
        {
            switch (value)
            {
                case GraphicsBufferType.Vertex: return WebGLRenderingContextBase.ARRAY_BUFFER;
                case GraphicsBufferType.Index: return WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER;
            }

            return WebGLRenderingContextBase.ARRAY_BUFFER;
        }
    }
}