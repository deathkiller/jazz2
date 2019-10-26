using System;
using System.Collections.Generic;
using System.Threading;

namespace Jazz2
{
    [Serializable]
    public abstract class Disposable : IDisposable
    {
        public delegate void DisposeCallback(bool disposing);

        public static void Free<T>(ref T disposeMe, bool callerIsNotFinalizing = true) where T : class, IDisposable
        {
            if (disposeMe == null) {
                return;
            }

            if (callerIsNotFinalizing) {
                disposeMe.Dispose();
            }

            disposeMe = default(T);
        }

        public static void FreeContents<T>(T[] disposeUs) where T : class, IDisposable
        {
            if (disposeUs == null) {
                return;
            }

            for (int i = 0; i < disposeUs.Length; i++) {
                Free(ref disposeUs[i]);
            }
        }

        public static void FreeContentsAndClear<TKey, TValue>(IDictionary<TKey, TValue> disposeUs)
            where TValue : class, IDisposable
        {
            if (disposeUs == null) {
                return;
            }

            foreach (TValue disposable in disposeUs.Values) {
                if (disposable != null) {
                    disposable.Dispose();
                }
            }

            disposeUs.Clear();
        }

        public static void FreeContentsAndClear<T>(IList<T> disposeUs) where T : class, IDisposable
        {
            if (disposeUs == null) {
                return;
            }

            foreach (T disposeMe in disposeUs) {
                if (disposeMe != null)
                    disposeMe.Dispose();
            }

            disposeUs.Clear();
        }

        private int isDisposed;

        public bool IsDisposed
        {
            get { return (Thread.VolatileRead(ref isDisposed) == 1); }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 0) {
                try {
                    Dispose(true);
                } finally {
                    GC.SuppressFinalize(this);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~Disposable()
        {
            Dispose(false);
        }
    }
}