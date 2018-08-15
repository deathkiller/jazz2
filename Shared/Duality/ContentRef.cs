using System;
using System.Diagnostics;

namespace Duality
{
    /// <summary>
    /// This lightweight struct references <see cref="Resource">Resources</see> in an abstract way. It
    /// is tightly connected to the <see cref="DefaultContentProvider"/> and takes care of keeping or making 
    /// the referenced content available when needed. Never store actual Resource references permanently,
    /// instead use a ContentRef to it. However, you may retrieve and store a direct Resource reference
    /// temporarily, although this is only recommended at method-local scope.
    /// </summary>
    /// <seealso cref="Resource"/>
    /// <seealso cref="DefaultContentProvider"/>
    /// <seealso cref="IContentRef"/>
    [DebuggerTypeProxy(typeof(ContentRef<>.DebuggerTypeProxy))]
    public struct ContentRef<T> : IEquatable<ContentRef<T>>, IContentRef where T : Resource
    {
        private T contentInstance;

        /// <summary>
        /// [GET / SET] The actual <see cref="Resource"/>. If currently unavailable, it is loaded and then returned.
        /// Because of that, this Property is only null if the references Resource is missing, invalid, or
        /// this content reference has been explicitly set to null. Never returns disposed Resources.
        /// </summary>
        public T Res
        {
            get
            {
                return this.contentInstance;
            }
            set
            {
                this.contentInstance = value;
            }
        }
        /// <summary>
        /// [GET] Returns the current reference to the Resource that is stored locally. No attemp is made to load or reload
        /// the Resource if currently unavailable.
        /// </summary>
        public T ResWeak
        {
            get { return (this.contentInstance == null || this.contentInstance.Disposed) ? null : this.contentInstance; }
        }
        /// <summary>
        /// [GET] Returns whether this content reference has been explicitly set to null.
        /// </summary>
        public bool IsExplicitNull
        {
            get
            {
                return this.contentInstance == null;
            }
        }
        /// <summary>
        /// [GET] Returns whether this content reference is available in general. This may trigger loading it, if currently unavailable.
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                if (this.contentInstance != null && !this.contentInstance.Disposed) return true;
                return this.contentInstance != null;
            }
        }
        /// <summary>
        /// Creates a ContentRef pointing to the specified <see cref="Resource"/>.
        /// </summary>
        /// <param name="res">The Resource to reference.</param>
        public ContentRef(T res)
        {
            this.contentInstance = res;
        }
        
        /// <summary>
        /// Loads the associated content as if it was accessed now.
        /// You don't usually need to call this method. It is invoked implicitly by trying to access the ContentRef
        /// </summary>
        public void MakeAvailable()
        {
            //if (this.contentInstance == null || this.contentInstance.Disposed) this.RetrieveInstance();
        }
        /// <summary>
        /// Discards the resolved content reference cache to allow garbage-collecting the Resource
        /// without losing its reference. Accessing it will result in reloading the Resource.
        /// </summary>
        public void Detach()
        {
            this.contentInstance = null;
        }

        public override bool Equals(object obj)
        {
            if (obj is ContentRef<T>)
                return this == (ContentRef<T>)obj;
            else
                return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            if (this.contentInstance != null) return this.contentInstance.GetHashCode();
            else return 0;
        }
        public bool Equals(ContentRef<T> other)
        {
            return this == other;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Resource IContentRef.Res
        {
            get { return this.Res; }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Resource IContentRef.ResWeak
        {
            get { return this.ResWeak; }
        }

        public static implicit operator ContentRef<T>(T res)
        {
            return new ContentRef<T>(res);
        }
        public static explicit operator T(ContentRef<T> res)
        {
            return res.Res;
        }

        /// <summary>
        /// Compares two ContentRefs for equality.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is a two-step comparison. First, their actual Resources references are compared.
        /// If they're both not null and equal, true is returned. Otherwise, their Resource paths
        /// are compared for equality
        /// </remarks>
        public static bool operator ==(ContentRef<T> first, ContentRef<T> second)
        {
            // Old check, didn't work for XY == null when XY was a Resource created at runtime
            //if (first.contentInstance != null && second.contentInstance != null)
            //    return first.contentInstance == second.contentInstance;
            //else
            //    return first.contentPath == second.contentPath;

            // Completely identical
            if (first.contentInstance == second.contentInstance)
                return true;
            // Same instances
            else if (first.contentInstance != null && second.contentInstance != null)
                return first.contentInstance == second.contentInstance;
            // Null checks
            else if (first.IsExplicitNull) return second.IsExplicitNull;
            else if (second.IsExplicitNull) return first.IsExplicitNull;
            // Path comparison
            //else {
            //    string firstPath = first.contentInstance != null ? first.contentInstance.Path : first.contentPath;
            //    string secondPath = second.contentInstance != null ? second.contentInstance.Path : second.contentPath;
            //    return firstPath == secondPath;
            //}
            return false;
        }
        /// <summary>
        /// Compares two ContentRefs for inequality.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool operator !=(ContentRef<T> first, ContentRef<T> second)
        {
            return !(first == second);
        }

        internal class DebuggerTypeProxy
        {
            private ContentRef<T> cr;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T Res
            {
                get { return this.cr.Res; }
            }

            public DebuggerTypeProxy(ContentRef<T> cr)
            {
                this.cr = cr;
            }
        }
    }
}
