using System;
using Duality.Backend;


namespace Duality.Resources
{
    /// <summary>
    /// Represents an OpenGL Shader in an abstract form.
    /// </summary>
    [ExplicitResourceReference]
    public abstract class AbstractShader : Resource
    {
        private string source = null;
        private INativeShaderPart native = null;
        private bool compiled = false;

        /// <summary>
        /// [GET] The shaders native backend. Don't use this unless you know exactly what you're doing.
        /// </summary>
        public INativeShaderPart Native
        {
            get { return this.native; }
        }
        /// <summary>
        /// The type of OpenGL shader that is represented.
        /// </summary>
        protected abstract ShaderType Type { get; }
        /// <summary>
        /// [GET] Whether this shader has been compiled yet or not.
        /// </summary>
        public bool Compiled
        {
            get { return this.compiled; }
        }
        /// <summary>
        /// [GET] The shaders source code.
        /// </summary>
        public string Source
        {
            get { return this.source; }
            set
            {
                this.compiled = false;
                this.source = value;
            }
        }


        protected AbstractShader() { }
        protected AbstractShader(string sourceCode)
        {
            this.Source = sourceCode;
        }


        /// <summary>
        /// Compiles the shader. This is done automatically when loading the shader
        /// or attaching it to a <see cref="Duality.Resources.ShaderProgram"/>.
        /// </summary>
        public void Compile()
        {
            if (string.IsNullOrEmpty(this.source)) throw new InvalidOperationException("Can't compile a shader without any source code specified.");

            if (this.native == null)
                this.native = DualityApp.GraphicsBackend.CreateShaderPart();

            try {
                this.native.LoadSource(this.source, this.Type);
            } catch (Exception e) {
                Console.WriteLine("Error loading Shader {0}:{2}{1}", this.FullName, /*Log.Exception(*/e/*)*/, Environment.NewLine);
            }

            this.compiled = true;
        }

        protected override void OnLoaded()
        {
            this.Compile();
            base.OnLoaded();
        }
        protected override void OnDisposing(bool manually)
        {
            base.OnDisposing(manually);
            if (this.native != null) {
                this.native.Dispose();
                this.native = null;
            }
        }
    }
}
