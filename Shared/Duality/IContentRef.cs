using System;

namespace Duality
{
    /// <summary>
    /// IContentRef is a general interface for <see cref="ContentRef{T}">content references</see> of any <see cref="Resource"/> type.
    /// </summary>
    /// <seealso cref="Resource"/>
    /// <seealso cref="ContentProvider"/>
    /// <seealso cref="ContentRef{T}"/>
    public interface IContentRef
    {
        /// <summary>
        /// [GET] Returns the actual <see cref="Resource"/>. If currently unavailable, it is loaded and then returned.
        /// Because of that, this Property is only null if the references Resource is missing, invalid, or
        /// this content reference has been explicitly set to null. Never returns disposed Resources.
        /// </summary>
        Resource Res { get; }
        /// <summary>
        /// [GET] Returns the current reference to the Resource that is stored locally. No attemp is made to load or reload
        /// the Resource if currently unavailable.
        /// </summary>
        Resource ResWeak { get; }
        /// <summary>
        /// [GET] Returns whether this content reference has been explicitly set to null.
        /// </summary>
        bool IsExplicitNull { get; }
        /// <summary>
        /// [GET] Returns whether this content reference is available in general. This may trigger loading it, if currently unavailable.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Loads the associated content as if it was accessed now.
        /// You don't usually need to call this method. It is invoked implicitly by trying to access the ContentRef.
        /// </summary>
        void MakeAvailable();
        /// <summary>
        /// Discards the resolved content reference cache to allow garbage-collecting the Resource
        /// without losing its reference. Accessing it will result in reloading the Resource.
        /// </summary>
        void Detach();
    }
}
