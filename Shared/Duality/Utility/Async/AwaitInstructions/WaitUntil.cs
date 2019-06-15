using System;

namespace Duality.Async
{
	public struct WaitUntil : IAwaitInstruction
	{
		private Func<bool> condition;

		bool IAwaitInstruction.IsCompleted() => condition();

		/// <summary>
		/// Waits until the condition returns true before continuing
		/// </summary>
		public WaitUntil(Func<bool> condition)
		{
			this.condition = condition;
		}
		
		public Continuation<WaitUntil> GetAwaiter() => new Continuation<WaitUntil>(this);
	}
}