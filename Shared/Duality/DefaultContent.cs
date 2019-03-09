using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Duality.IO;
using Duality.Resources;

namespace Duality
{
	/// <summary>
	/// Utility class for managing embedded default content in Duality.
	/// 
	/// There's usually no reason to use this class in game code, and it can in fact be somewhat dangerous
	/// when used the wrong way. If you consider invoking any of its API, be careful about it.
	/// </summary>
	internal static class DefaultContent
	{
		private static bool defaultContentInitialized = false;
		private static List<Resource> defaultContent = new List<Resource>();

		/// <summary>
		/// Initializes Duality's embedded default content.
		/// </summary>
		public static void Init()
		{
			if (defaultContentInitialized) return;

			VertexShader.InitDefaultContent();
			FragmentShader.InitDefaultContent();
			DrawTechnique.InitDefaultContent();
			Pixmap.InitDefaultContent();
			Texture.InitDefaultContent();
			Material.InitDefaultContent();
			RenderSetup.InitDefaultContent();

			defaultContentInitialized = true;
		}
		/// <summary>
		/// Initializes Duality's embedded default content.
		/// </summary>
		public static void Dispose()
		{
			if (!defaultContentInitialized) return;

			foreach (Resource r in defaultContent.ToArray()) {
				r.Dispose();
			}
			defaultContent.Clear();

			defaultContentInitialized = false;
		}

		public static void InitType<T>(string nameExt, Func<Stream, T> resourceCreator) where T : Resource
		{
			InitType<T>(name => {
#if UNCOMPRESSED_CONTENT
                string path = PathOp.Combine(DualityApp.DataDirectory, "Shaders", name + nameExt);
#elif __ANDROID__
                string path = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Shaders.ES30", name + nameExt);
#else
                string path = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Shaders", name + nameExt);
#endif
                using (Stream stream = FileOp.Open(path, FileAccessMode.Read)) {
					return resourceCreator(stream);
				}
			});
		}
		public static void InitType<T>(IDictionary<string, T> dictionary) where T : Resource
		{
			InitType<T>(name => {
				T res;
				return dictionary.TryGetValue(name, out res) ? res : null;
			});
		}
		public static void InitType<T>(Func<string, T> resourceCreator) where T : Resource
		{
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

				T resource = resourceCreator(name);
				if (resource != null) {
					defaultResProps[i].SetValue(null, new ContentRef<T>(resource));
				}
			}
		}
	}
}