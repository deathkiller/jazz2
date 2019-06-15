using System.Threading;
using Duality.Async.Awaiters;

namespace Duality.Async
{
    public static class Extensions
	{
		/// <summary>
		/// Link the <see cref="Duality.Async.IAwaitInstruction"/>'s lifespan to an object and
		/// configure the type of update cycle it should be evaluated on.
		/// </summary>
		/// <returns>A continuation with updated params.</returns>
		public static Continuation<T> ConfigureAwait<T>(this T @this, IManageableObject owner, FrameScheduler scheduler) where T : IAwaitInstruction
			=> new Continuation<T>(@this).ConfigureAwait(owner, scheduler);

		/// <summary>
		/// Link the <see cref="Duality.Async.IAwaitInstruction"/>'s lifespan to an object.
		/// </summary>
		/// <returns>A continuation with updated params.</returns>
		public static Continuation<T> ConfigureAwait<T>(this T @this, IManageableObject owner) where T : IAwaitInstruction
			=> new Continuation<T>(@this).ConfigureAwait(owner);

		/// <summary>
		/// Configure the type of update cycle it should be evaluated on.
		/// </summary>
		/// <returns>A continuation with updated params.</returns>
		public static Continuation<T> ConfigureAwait<T>(this T @this, FrameScheduler scheduler) where T : IAwaitInstruction
			=> new Continuation<T>(@this).ConfigureAwait(scheduler);
		
		/// <summary>
		/// Link the <see cref="Duality.Async.IAwaitInstruction"/>'s lifespan to a
		/// <see cref="System.Threading.CancellationToken"/> and configure the type of update cycle it should be
		/// evaluated on.
		/// </summary>
		/// <returns>A continuation with updated params.</returns>
		public static Continuation<T> ConfigureAwait<T>(this T @this, CancellationToken cancellationToken, FrameScheduler scheduler) where T : IAwaitInstruction
			=> new Continuation<T>(@this).ConfigureAwait(cancellationToken, scheduler);

		/// <summary>
		/// Link the <see cref="Duality.Async.IAwaitInstruction"/>'s lifespan to a <see cref="System.Threading.CancellationToken"/>.
		/// </summary>
		/// <returns>A continuation with updated params.</returns>
		public static Continuation<T> ConfigureAwait<T>(this T @this, CancellationToken cancellationToken) where T : IAwaitInstruction
			=> new Continuation<T>(@this).ConfigureAwait(cancellationToken);
		
		public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext @this) => new SynchronizationContextAwaiter(@this);
	}
}