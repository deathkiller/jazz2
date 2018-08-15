using System;
using System.Collections.Generic;
using Duality.Resources;

namespace Duality
{
    internal static class DefaultContentProvider
    {
        private static bool defaultContentInitialized = false;
        private static List<Resource> defaultContent = new List<Resource>();

        /// <summary>
        /// Initializes Duality's embedded default content.
        /// </summary>
        public static void InitDefaultContent()
        {
            if (defaultContentInitialized) return;

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
        /// Initializes Duality's embedded default content.
        /// </summary>
        public static void DisposeDefaultContent()
        {
            if (!defaultContentInitialized) return;

            foreach (Resource r in defaultContent.ToArray()) {
                r.Dispose();
            }
            defaultContent.Clear();

            defaultContentInitialized = false;
        }
    }
}