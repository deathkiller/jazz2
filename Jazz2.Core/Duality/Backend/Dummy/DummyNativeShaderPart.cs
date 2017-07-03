using System;

namespace Duality.Backend.Dummy
{
    internal class DummyNativeShaderPart : INativeShaderPart
	{
		void INativeShaderPart.LoadSource(string sourceCode, Resources.ShaderType type) { }
		void IDisposable.Dispose() { }
	}
}
