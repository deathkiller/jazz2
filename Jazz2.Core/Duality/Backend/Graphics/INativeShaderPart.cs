using System;

using Duality.Resources;

namespace Duality.Backend
{
    public interface INativeShaderPart : IDisposable
	{
		/// <summary>
		/// Loads the specified source code and prepares the shader part for being used.
		/// </summary>
		/// <param name="sourceCode"></param>
		/// <param name="type"></param>
		void LoadSource(string sourceCode, ShaderType type);
	}
}
