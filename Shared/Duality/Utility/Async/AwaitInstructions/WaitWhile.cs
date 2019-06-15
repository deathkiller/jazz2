using System;

namespace Duality.Async
{
	public struct WaitWhile : IAwaitInstruction
	{
		private Func<bool> condition;

		bool IAwaitInstruction.IsCompleted() => !condition();

		/// <summary>
		/// Waits until the condition returns false before continuing
		/// </summary>
		public WaitWhile(Func<bool> condition)
		{
			this.condition = condition;
		}
		
		public Continuation<WaitWhile> GetAwaiter() => new Continuation<WaitWhile>(this);
	}
}