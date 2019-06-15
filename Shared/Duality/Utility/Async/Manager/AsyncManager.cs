using System.Threading;

namespace Duality.Async
{
	public static partial class AsyncManager
	{
		private static int uiThreadId, updateCount, lateCount;
		private static ContinuationProcessorGroup beforeUpdates, afterUpdates;

		public static float CurrentRawFrameCount { get; private set; }

		public static float CurrentFrameCount { get; private set; }

		/// <summary>
		/// Main (rendering) <see cref="System.Threading.SynchronizationContext"/>
		/// </summary>
		public static SynchronizationContext UiSyncContext { get; private set; }

		/// <summary>
		/// Background (thread pool) <see cref="System.Threading.SynchronizationContext"/>
		/// </summary>
		public static SynchronizationContext BackgroundSyncContext { get; private set; }

		public static bool InUiContext => (Thread.CurrentThread.ManagedThreadId == uiThreadId);

		internal static void Init()
		{
			uiThreadId = Thread.CurrentThread.ManagedThreadId;
			UiSyncContext = SynchronizationContext.Current;

			BackgroundSyncContext = new SynchronizationContext();

			beforeUpdates = new ContinuationProcessorGroup();
			afterUpdates = new ContinuationProcessorGroup();
		}

		/// <summary>
		/// Queues a continuation
		/// Intended for internal use only - you shouldn't need to invoke this
		/// </summary>
		internal static void AddContinuation<T>(T cont) where T : IContinuation
		{
			switch (cont.Scheduler) {
				case FrameScheduler.BeforeUpdate:
					beforeUpdates.Add(cont);
					break;

				case FrameScheduler.AfterUpdate:
					afterUpdates.Add(cont);
					break;
			}
		}

		internal static void InvokeBeforeUpdate()
		{
			CurrentRawFrameCount = ++updateCount;
			CurrentFrameCount += Time.TimeMult;

			if (CurrentRawFrameCount > 1) {
				beforeUpdates.Process();
			}
		}

		internal static void InvokeAfterUpdate()
		{
			CurrentRawFrameCount = ++lateCount;

			if (CurrentRawFrameCount > 1) {
				afterUpdates.Process();
			}
		}
	}
}