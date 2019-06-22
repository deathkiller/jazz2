using System;
using System.Linq;
using Duality.Resources;
using WebGLDotNET;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeShaderProgram : INativeShaderProgram
    {
        public struct FieldLocation
        {
            public WebGLUniformLocation Uniform;
            public int Attrib;
        }

        private static NativeShaderProgram curBound;
        public static void Bind(NativeShaderProgram prog)
        {
            if (curBound == prog) return;

            if (prog == null) {
                GraphicsBackend.GL.UseProgram(null);
                curBound = null;
            } else {
                GraphicsBackend.GL.UseProgram(prog.Handle);
                curBound = prog;
            }
        }
        public static void SetUniform(ref ShaderFieldInfo field, WebGLUniformLocation location, params float[] data)
        {
            if (field.Scope != ShaderFieldScope.Uniform) return;
            if (location == null) return;
            switch (field.Type) {
                case ShaderFieldType.Bool:
                case ShaderFieldType.Int:
                    int[] arrI = new int[field.ArrayLength];
                    for (int j = 0; j < arrI.Length; j++) arrI[j] = (int)data[j];
                    GraphicsBackend.GL.Uniform1iv(location, new Span<int>(arrI));
                    break;
                case ShaderFieldType.Float:
                    GraphicsBackend.GL.Uniform1fv(location, new Span<float>(data));
                    break;
                case ShaderFieldType.Vec2:
                    GraphicsBackend.GL.Uniform2fv(location, new Span<float>(data));
                    break;
                case ShaderFieldType.Vec3:
                    GraphicsBackend.GL.Uniform3fv(location, new Span<float>(data));
                    break;
                case ShaderFieldType.Vec4:
                    GraphicsBackend.GL.Uniform4fv(location, new Span<float>(data));
                    break;
                case ShaderFieldType.Mat2:
                    GraphicsBackend.GL.UniformMatrix2fv(location, false, new Span<float>(data));
                    break;
                case ShaderFieldType.Mat3:
                    GraphicsBackend.GL.UniformMatrix3fv(location, false, new Span<float>(data));
                    break;
                case ShaderFieldType.Mat4:
                    GraphicsBackend.GL.UniformMatrix4fv(location, false, new Span<float>(data));
                    break;
            }
        }

        private WebGLProgram handle;
        private ShaderFieldInfo[] fields;
        private FieldLocation[] fieldLocations;

        public WebGLProgram Handle
        {
            get { return this.handle; }
        }
        public ShaderFieldInfo[] Fields
        {
            get { return this.fields; }
        }
        public FieldLocation[] FieldLocations
        {
            get { return this.fieldLocations; }
        }

        void INativeShaderProgram.LoadProgram(INativeShaderPart vertex, INativeShaderPart fragment)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (this.handle == null)
                this.handle = GraphicsBackend.GL.CreateProgram();
            else
                this.DetachShaders();

            if (vertex == null) {
                vertex = VertexShader.Minimal.Res.Native;
            }
            if (fragment == null) {
                fragment = FragmentShader.Minimal.Res.Native;
            }


            // Attach both shaders
            GraphicsBackend.GL.AttachShader(this.handle, (vertex as NativeShaderPart).Handle);
            GraphicsBackend.GL.AttachShader(this.handle, (fragment as NativeShaderPart).Handle);

            // Link the shader program
            GraphicsBackend.GL.LinkProgram(this.handle);

            int result = (int)GraphicsBackend.GL.GetProgramParameter(this.handle, WebGLRenderingContextBase.LINK_STATUS);
            if (result == 0) {
                string errorLog = GraphicsBackend.GL.GetProgramInfoLog(this.handle);
                this.RollbackAtFault();
                throw new BackendException(string.Format("Linker error:{1}{0}", errorLog, Environment.NewLine));
            }

            // Collect variable infos from sub programs
            {
                NativeShaderPart vert = vertex as NativeShaderPart;
                NativeShaderPart frag = fragment as NativeShaderPart;

                ShaderFieldInfo[] fragVarArray = frag != null ? frag.Fields : null;
                ShaderFieldInfo[] vertVarArray = vert != null ? vert.Fields : null;

                if (fragVarArray != null && vertVarArray != null)
                    this.fields = vertVarArray.Union(fragVarArray).ToArray();
                else if (vertVarArray != null)
                    this.fields = vertVarArray.ToArray();
                else
                    this.fields = fragVarArray.ToArray();

            }

            // Determine each variables location
            this.fieldLocations = new FieldLocation[this.fields.Length];
            for (int i = 0; i < this.fields.Length; i++) {
                if (this.fields[i].Scope == ShaderFieldScope.Uniform)
                    this.fieldLocations[i].Uniform = GraphicsBackend.GL.GetUniformLocation(this.handle, this.fields[i].Name);
                else
                    this.fieldLocations[i].Attrib = GraphicsBackend.GL.GetAttribLocation(this.handle, this.fields[i].Name);
            }
        }
        ShaderFieldInfo[] INativeShaderProgram.GetFields()
        {
            return this.fields.Clone() as ShaderFieldInfo[];
        }
        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated)
                return;

            this.DeleteProgram();
        }

        private void DeleteProgram()
        {
            if (this.handle == null) return;

            this.DetachShaders();
            GraphicsBackend.GL.DeleteProgram(this.handle);
            this.handle = null;
        }
        private void DetachShaders()
        {
            // Determine currently attached shaders
            WebGLShader[] attachedShaders = GraphicsBackend.GL.GetAttachedShaders(this.handle);

            // Detach all attached shaders
            for (int i = 0; i < attachedShaders.Length; i++) {
                GraphicsBackend.GL.DetachShader(this.handle, attachedShaders[i]);
            }
        }
        /// <summary>
        /// In case of errors loading the program, this methods rolls back the state of this
        /// shader program, so consistency can be assured.
        /// </summary>
        private void RollbackAtFault()
        {
            this.fields = new ShaderFieldInfo[0];
            this.fieldLocations = new FieldLocation[0];

            this.DeleteProgram();
        }
    }
}