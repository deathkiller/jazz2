using System;
using System.Collections.Generic;

namespace Duality
{
    /// <summary>
    /// The abstract Resource class is inherited by any kind of Duality content. Instances of it or one of its subclasses
    /// are usually handled wrapped inside a <see cref="ContentRef{T}"/> and requested from the <see cref="DefaultContent"/>.
    /// </summary>
    /// <seealso cref="ContentRef{T}"/>
    /// <seealso cref="DefaultContent"/>
    public abstract class Resource : IManageableObject, IDisposable
	{
		private static List<Resource> finalizeSched = new List<Resource>();

		/// <summary>
		/// The initialization state of the Resource. Also specifies a disposed-state.
		/// </summary>
		private InitState initState = InitState.Initialized;

		/// <summary>
		/// [GET] Returns whether the Resource has been disposed. 
		/// Disposed Resources are not to be used and are treated the same as a null value by most methods.
		/// </summary>
		public bool Disposed
		{
			get { return this.initState == InitState.Disposed; }
		}

		bool IManageableObject.Active
		{
			get { return !this.Disposed; }
		}
		
		~Resource()
		{
			lock (finalizeSched) {
				finalizeSched.Add(this);
			}
		}

		/// <summary>
		/// Disposes the Resource. Please don't do something silly, like disposing a Scene while it is updated.. use <see cref="ExtMethodsIManageableObject.DisposeLater"/> instead!
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool manually)
		{
			if (this.initState == InitState.Initialized) {
				this.initState = InitState.Disposing;
				this.OnDisposing(manually);
				this.initState = InitState.Disposed;
			}
		}

		/// <summary>
		/// Called when beginning to dispose the Resource.
		/// </summary>
		/// <param name="manually"></param>
		protected virtual void OnDisposing(bool manually) { }


		internal static void RunCleanup()
		{
			Resource[] finalizeSchedArray;
			lock (finalizeSched) {
				if (finalizeSched.Count == 0) return;
				finalizeSchedArray = finalizeSched.ToArray();
				finalizeSched.Clear();
			}

			foreach (Resource res in finalizeSchedArray) {
				if (res == null) continue;
				res.Dispose(false);

#if DEBUG
                Log.Write(LogType.Info, "Disposing resource \"" + res + "\" (" + res.GetType().FullName + ")...");
#endif
            }
		}
	}
}