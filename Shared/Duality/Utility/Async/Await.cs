using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Duality.Async
{
	public static partial class Await
	{
		private static readonly Continuation<WaitForRawFrames> nextBeforeUpdate = new Continuation<WaitForRawFrames>(new WaitForRawFrames(1));
		private static readonly Continuation<WaitForRawFrames> nextAfterUpdate = new Continuation<WaitForRawFrames>(new WaitForRawFrames(1), FrameScheduler.AfterUpdate);
		
		/// <summary>
		/// Quick access to main (rendering) <see cref="System.Threading.SynchronizationContext"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SynchronizationContext UiSyncContext() => AsyncManager.UiSyncContext;
		
		/// <summary>
		/// Quick access to the background <see cref="System.Threading.SynchronizationContext"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SynchronizationContext BackgroundSyncContext() => AsyncManager.BackgroundSyncContext;

		/// <summary>
		/// Convenience function to skip a single frame
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Continuation<WaitForRawFrames> NextUpdate() => nextBeforeUpdate;
		
		/// <summary>
		/// Convenience function to skip a number of frames
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Continuation<WaitForRawFrames> Updates(int count) => new Continuation<WaitForRawFrames>(new WaitForRawFrames(count));

		/// <summary>
		/// Convenience function to skip a single frame and continue after Update
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Continuation<WaitForRawFrames> NextAfterUpdate() => nextAfterUpdate;
		
		/// <summary>
		/// Convenience function to skip multiple frames and continue after Update
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Continuation<WaitForRawFrames> AfterUpdates(int count) => new Continuation<WaitForRawFrames>(new WaitForRawFrames(count), FrameScheduler.AfterUpdate);

		/// <summary>
		/// Convenience function to skip a number of scaled frames
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WaitForFrames ScaledUpdates(int count) => new WaitForFrames(count);

		/// <summary>
		/// Convenience function to wait for a condition to return true
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WaitUntil Until(Func<bool> condition) => new WaitUntil(condition);
		
		/// <summary>
		/// Convenience function to wait for a condition to return false
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WaitWhile While(Func<bool> condition) => new WaitWhile(condition);
	}
}