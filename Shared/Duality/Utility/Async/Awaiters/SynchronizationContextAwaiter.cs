using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Duality.Async.Awaiters
{
	public struct SynchronizationContextAwaiter : INotifyCompletion
	{
		private static readonly SendOrPostCallback postCallback = state => ((Action)state)();

		private SynchronizationContext context;

		public SynchronizationContextAwaiter(SynchronizationContext context)
		{
			this.context = context;
		}

		public bool IsCompleted => context == SynchronizationContext.Current;
		public void OnCompleted(Action continuation) => context.Post(postCallback, continuation);
		public void GetResult() { }
	}
}