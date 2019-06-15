using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Duality.Async
{
	public interface IContinuation
	{
		bool Evaluate();
		FrameScheduler Scheduler { get; }
	}

	/// <summary>
	/// Encapsulates an <see cref="Duality.Async.IAwaitInstruction"/> with additional information about how the instruction
	/// will be queued and executed. Continuations are intended to be awaited after or shortly after instantiation.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="Duality.Async.IAwaitInstruction"/> to encapsulate</typeparam>
	public struct Continuation<T> : IContinuation, INotifyCompletion where T : IAwaitInstruction
	{
		private IManageableObject owner;
		private CancellationToken cancellationToken;
		private T instruction;
		private Action continuation;

		public FrameScheduler Scheduler { get; private set; }

		public Continuation(T inst)
		{
			instruction = inst;
			continuation = null;
			owner = null;
			Scheduler = FrameScheduler.BeforeUpdate;
		}

		public Continuation(T inst, FrameScheduler scheduler)
		{
			instruction = inst;
			continuation = null;
			owner = null;
			Scheduler = scheduler;
		}

		public Continuation(T inst, CancellationToken cancellationToken, FrameScheduler scheduler)
		{
			instruction = inst;
			continuation = null;
			owner = null;
			this.cancellationToken = cancellationToken;
			Scheduler = scheduler;
		}

		public Continuation(T inst, IManageableObject owner, FrameScheduler scheduler)
		{
			instruction = inst;
			continuation = null;
			this.owner = owner;
			Scheduler = scheduler;
		}

		/// <summary>
		/// Evaluate the encapsulated <see cref="Duality.Async.IAwaitInstruction"/> to determine whether the continuation
		/// is finished and can continue
		/// </summary>
		/// <returns>true if its owner is destroyed or its cancellation token has been cancelled</returns>
		public bool Evaluate()
		{
			if (owner != null && owner.Disposed) {
				return true;
			}

			if (cancellationToken.IsCancellationRequested || instruction.IsCompleted()) {
				continuation();
				return true;
			}

			return false;
		}

		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			this.continuation = continuation;
			AsyncManager.AddContinuation(this);
		}

		/// <summary>
		/// Link the continuation's lifespan to an object and configure the type of update
		/// cycle it should be evaluated on
		/// </summary>
		/// <returns>A new continuation with updated params</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> ConfigureAwait(IManageableObject owner, FrameScheduler scheduler)
		{
			this.owner = owner;
			Scheduler = scheduler;
			return this;
		}

		/// <summary>
		/// Link the continuation's lifespan to an object
		/// </summary>
		/// <returns>A new continuation with updated params</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> ConfigureAwait(IManageableObject owner)
		{
			this.owner = owner;
			return this;
		}

		/// <summary>
		/// Configure the type of update cycle it should be evaluated on
		/// </summary>
		/// <returns>A new continuation with updated params</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> ConfigureAwait(FrameScheduler scheduler)
		{
			Scheduler = scheduler;
			return this;
		}

		/// <summary>
		/// Link the continuation's lifespan to a <see cref="System.Threading.CancellationToken"/> and configure the
		/// type of update cycle it should be evaluated on
		/// </summary>
		/// <returns>A new continuation with updated params</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> ConfigureAwait(CancellationToken cancellationToken, FrameScheduler scheduler)
		{
			this.cancellationToken = cancellationToken;
			Scheduler = scheduler;
			return this;
		}

		/// <summary>
		/// Link the continuation's lifespan to a <see cref="System.Threading.CancellationToken"/>
		/// </summary>
		/// <returns>A new continuation with updated params</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> ConfigureAwait(CancellationToken cancellationToken)
		{
			this.cancellationToken = cancellationToken;
			return this;
		}

		public void GetResult() { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Continuation<T> GetAwaiter() => this;
	}
}