using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Duality.Resources;
using WebGLDotNET;
using ShaderType = Duality.Resources.ShaderType;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeShaderPart : INativeShaderPart
    {
        private WebGLShader handle;
        private ShaderFieldInfo[] fields;

        public WebGLShader Handle
        {
            get { return this.handle; }
        }
        public ShaderFieldInfo[] Fields
        {
            get { return this.fields; }
        }

        void INativeShaderPart.LoadSource(string sourceCode, ShaderType type)
        {
            // Removed thread guards because of performance
            //DefaultOpenTKBackendPlugin.GuardSingleThreadState();

            if (this.handle == null) this.handle = GraphicsBackend.GL.CreateShader(GetOpenTKShaderType(type));
            GraphicsBackend.GL.ShaderSource(this.handle, sourceCode);
            GraphicsBackend.GL.CompileShader(this.handle);

            int result = (int)GraphicsBackend.GL.GetShaderParameter(this.handle, WebGLRenderingContextBase.COMPILE_STATUS);
            if (result == 0) {
                string infoLog = GraphicsBackend.GL.GetShaderInfoLog(this.handle);
                throw new BackendException(string.Format("{0} Compiler error:{2}{1}", type, infoLog, Environment.NewLine));
            }

            // Remove comments from source code before extracting variables
            string sourceWithoutComments;
            {
                const string blockComments = @"/\*(.*?)\*/";
                const string lineComments = @"//(.*?)\r?\n";
                const string strings = @"""((\\[^\n]|[^""\n])*)""";
                const string verbatimStrings = @"@(""[^""]*"")+";
                sourceWithoutComments = Regex.Replace(sourceCode,
                    blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                    match => {
                        if (match.Value.StartsWith("/*") || match.Value.StartsWith("//"))
                            return match.Value.StartsWith("//") ? Environment.NewLine : "";
                        else
                            return match.Value;
                    },
                    RegexOptions.Singleline);
            }

            // Scan remaining code chunk for variable declarations
            List<ShaderFieldInfo> varInfoList = new List<ShaderFieldInfo>();
            string[] lines = sourceWithoutComments.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in lines) {
                string curLine = t.TrimStart();

                ShaderFieldScope scope;
                int arrayLength;

                if (curLine.StartsWith("uniform"))
                    scope = ShaderFieldScope.Uniform;
                else if (curLine.StartsWith("attribute") || curLine.StartsWith("in"))
                    scope = ShaderFieldScope.Attribute;
                else continue;

                string[] curLineSplit = curLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                ShaderFieldType varType = ShaderFieldType.Unknown;
                switch (curLineSplit[1].ToUpper()) {
                    case "FLOAT": varType = ShaderFieldType.Float; break;
                    case "VEC2": varType = ShaderFieldType.Vec2; break;
                    case "VEC3": varType = ShaderFieldType.Vec3; break;
                    case "VEC4": varType = ShaderFieldType.Vec4; break;
                    case "MAT2": varType = ShaderFieldType.Mat2; break;
                    case "MAT3": varType = ShaderFieldType.Mat3; break;
                    case "MAT4": varType = ShaderFieldType.Mat4; break;
                    case "INT": varType = ShaderFieldType.Int; break;
                    case "BOOL": varType = ShaderFieldType.Bool; break;
                    case "SAMPLER2D": varType = ShaderFieldType.Sampler2D; break;
                }

                curLineSplit = curLineSplit[2].Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                arrayLength = (curLineSplit.Length > 1) ? int.Parse(curLineSplit[1]) : 1;

                varInfoList.Add(new ShaderFieldInfo(curLineSplit[0], varType, scope, arrayLength));
            }

            this.fields = varInfoList.ToArray();
        }
        void IDisposable.Dispose()
        {
            if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated &&
                this.handle != null) {
                // Removed thread guards because of performance
                //DefaultOpenTKBackendPlugin.GuardSingleThreadState();
                GraphicsBackend.GL.DeleteShader(this.handle);
                this.handle = null;
            }
        }

        private static uint GetOpenTKShaderType(ShaderType type)
        {
            switch (type) {
                default:
                case ShaderType.Vertex: return WebGLRenderingContextBase.VERTEX_SHADER;
                case ShaderType.Fragment: return WebGLRenderingContextBase.FRAGMENT_SHADER;
            }
        }
    }
}