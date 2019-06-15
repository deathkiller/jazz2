using System.Collections.Generic;

namespace Duality.Async
{
	public partial class AsyncManager
	{
		partial class ContinuationProcessorGroup
		{
			private interface IContinuationProcessor
			{
				void Process();
			}

			private List<IContinuationProcessor> processors;

			public ContinuationProcessorGroup()
			{
				processors = new List<IContinuationProcessor>(16);
			}

			public void Add<T>(T cont) where T : IContinuation
			{
				ContinuationProcessor<T> p = ContinuationProcessor<T>.instance;

				if (p == null) {
					p = ContinuationProcessor<T>.instance = new ContinuationProcessor<T>();
					processors.Add(ContinuationProcessor<T>.instance);
				}

				p.Add(cont);
			}

			public void Process()
			{
				for (int i = 0; i < processors.Count; ++i) {
					processors[i].Process();
				}
			}
		}
	}
}