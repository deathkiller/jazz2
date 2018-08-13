using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Duality.IO;

namespace Duality
{
    /// <summary>
    /// The abstract Resource class is inherited by any kind of Duality content. Instances of it or one of its subclasses
    /// are usually handled wrapped inside a <see cref="ContentRef{T}"/> and requested from the <see cref="ContentProvider"/>.
    /// </summary>
    /// <seealso cref="ContentRef{T}"/>
    /// <seealso cref="ContentProvider"/>
    public abstract class Resource : IManageableObject, IDisposable
    {
        /// <summary>
        /// A Resource files extension.
        /// </summary>
        internal static readonly string FileExt = ".res";

        private static List<Resource> finalizeSched = new List<Resource>();

        /// <summary>
        /// Contains information on how this <see cref="Resource"/> should be treated during
        /// Asset import operations in the editor.
        /// </summary>
        //protected AssetInfo assetInfo = null;
        /// <summary>
        /// The path of this Resource.
        /// </summary>
        protected string path = null;
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
        /// <summary>
        /// [GET] The path where this Resource has been originally loaded from or was first saved to.
        /// It is also the path under which this Resource is registered at the ContentProvider.
        /// </summary>
        public string Path
        {
            get { return this.path; }
            internal set { this.path = value; }
        }

        /// <summary>
        /// [GET] Returns whether the Resource has been generated at runtime and  cannot be retrieved via content path.
        /// </summary>
        public bool IsRuntimeResource
        {
            get { return string.IsNullOrEmpty(this.path); }
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
            }
        }

        internal static void InitDefaultContent<T>(string nameExt, Func<Stream, T> resourceCreator) where T : Resource
        {
            InitDefaultContent<T>(name => {
                using (Stream stream = FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Internal", name + nameExt), FileAccessMode.Read)) {
                    return resourceCreator(stream);
                }
            });
        }
        internal static void InitDefaultContent<T>(IDictionary<string, T> dictionary) where T : Resource
        {
            InitDefaultContent<T>(name => {
                T res;
                return dictionary.TryGetValue(name, out res) ? res : null;
            });
        }
        internal static void InitDefaultContent<T>(Func<string, T> resourceCreator) where T : Resource
        {
            string contentPathBase = ContentProvider.VirtualContentPath + typeof(T).Name + ":";

            TypeInfo resourceType = typeof(T).GetTypeInfo();
            PropertyInfo[] defaultResProps = resourceType
                .DeclaredPropertiesDeep()
                .Where(p =>
                    p.IsPublic() &&
                    p.IsStatic() &&
                    typeof(IContentRef).GetTypeInfo().IsAssignableFrom(p.PropertyType.GetTypeInfo()))
                .ToArray();

            for (int i = 0; i < defaultResProps.Length; i++) {
                string name = defaultResProps[i].Name;
                string contentPath = contentPathBase + name.Replace('_', ':');

                T resource = resourceCreator(name);
                if (resource != null) {
                    defaultResProps[i].SetValue(null, new ContentRef<T>(resource));
                }
            }
        }
    }

    /// <summary>
    /// Allows to explicitly specify what kinds of Resources a certain Resource Type is able to reference.
    /// This is an optional attribute that is used for certain runtime optimizations. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExplicitResourceReferenceAttribute : Attribute
    {
        private Type[] referencedTypes = null;

        public IEnumerable<Type> ReferencedTypes
        {
            get { return this.referencedTypes; }
        }

        public ExplicitResourceReferenceAttribute(params Type[] referencedTypes)
        {
            if (referencedTypes == null) throw new ArgumentNullException(nameof(referencedTypes));
            TypeInfo resourceTypeInfo = typeof(Resource).GetTypeInfo();
            for (int i = 0; i < referencedTypes.Length; ++i) {
                if (referencedTypes[i] == null || !resourceTypeInfo.IsAssignableFrom(referencedTypes[i].GetTypeInfo()))
                    throw new ArgumentException("Only Resource Types are valied in this Attribute");
            }
            this.referencedTypes = referencedTypes;
        }
    }
}