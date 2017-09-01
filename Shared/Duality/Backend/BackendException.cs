using System;

namespace Duality.Backend
{
    public class BackendException : Exception
	{
		public BackendException(string message) : base(message) { }
		public BackendException(string message, Exception inner) : base(message, inner) { }
	}
}