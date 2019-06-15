namespace Duality.Async
{
	public partial class AsyncManager
	{
		partial class ContinuationProcessorGroup
		{
			private const int MaxQueueSize = 5000;

			private class ContinuationProcessor<T> : IContinuationProcessor where T : IContinuation
			{
				public static ContinuationProcessor<T> instance;

				private T[] currentQueue;
				private T[] futureQueue;

				private int futureCount;

				public ContinuationProcessor()
				{
					currentQueue = new T[MaxQueueSize];
					futureQueue = new T[MaxQueueSize];
				}

				public void Process()
				{
					int count = futureCount;
					futureCount = 0;

					// Swap queues
					T[] tmp = currentQueue;
					currentQueue = futureQueue;
					futureQueue = tmp;

					// Process queue
					for (int i = 0; i < count; ++i) {
						var c = currentQueue[i];

						if (!c.Evaluate()) {
							futureQueue[futureCount] = c;
							++futureCount;
						}
					}
				}

				public void Add(T cont)
				{
					futureQueue[futureCount] = cont;
					++futureCount;
				}
			}
		}
	}
}