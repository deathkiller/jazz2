using System;
using System.Collections.Generic;
using System.Linq;
using Duality.Resources;

namespace Duality
{
    /// <summary>
    /// <para>
    /// The ContentProvider is Duality's main instance for content management. If you need any kind of <see cref="Resource"/>,
    /// simply request it from the ContentProvider. It keeps track of which Resources are loaded and valid and prevents
    /// Resources from being loaded more than once at a time, thus reducing loading times and redundancy.
    /// </para>
    /// <para>
    /// You can also manually <see cref="AddContent">register Resources</see> that have been created at runtime 
    /// using a string alias of your choice.
    /// </para>
    /// </summary>
    /// <seealso cref="Resource"/>
    /// <seealso cref="ContentRef{T}"/>
    /// <seealso cref="IContentRef"/>
    public static class ContentProvider
    {
        /// <summary>
        /// (Virtual) base path for Duality's embedded default content.
        /// </summary>
        public const string VirtualContentPath = "Default:";

        private static bool defaultContentInitialized = false;
        private static List<Resource> defaultContent = new List<Resource>();

        /// <summary>
        /// Initializes Dualitys embedded default content.
        /// </summary>
        public static void InitDefaultContent()
        {
            if (defaultContentInitialized) return;
            Console.WriteLine("Initializing default content...");
            //Log.Core.PushIndent();

            VertexShader.InitDefaultContent();
            FragmentShader.InitDefaultContent();
            ShaderProgram.InitDefaultContent();
            DrawTechnique.InitDefaultContent();
            Pixmap.InitDefaultContent();
            Texture.InitDefaultContent();
            Material.InitDefaultContent();
            RenderSetup.InitDefaultContent();
            AudioData.InitDefaultContent();
            Sound.InitDefaultContent();

            defaultContentInitialized = true;
        }
        /// <summary>
        /// Initializes Dualitys embedded default content.
        /// </summary>
        public static void DisposeDefaultContent()
        {
            if (!defaultContentInitialized) return;
            Console.WriteLine("Disposing default content..");
            //Log.Core.PushIndent();

            foreach (Resource r in defaultContent.ToArray()) {
                r.Dispose();
            }
            defaultContent.Clear();

            defaultContentInitialized = false;
        }
        
    }
}