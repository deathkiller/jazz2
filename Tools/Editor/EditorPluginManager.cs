using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Duality;
using Duality.Backend;
using Duality.IO;

namespace Editor
{
    /// <summary>
    /// Manages loading, initialization and life cycle of Duality editor plugins.
    /// 
    /// Since all assemblies are owned by the .Net runtime that only exposes a very limited
    /// degree of control, this class should only be used statically: Disposing it would
    /// only get rid of management data, not of the actual plugin assemblies, which would
    /// then cause problems.
    /// 
    /// A static instance of this class is available through <see cref="DualityEditorApp.PluginManager"/>.
    /// </summary>
    public class EditorPluginManager : PluginManager<EditorPlugin>
    {
        private Assembly[] editorAssemblies = new Assembly[] { typeof(App).GetTypeInfo().Assembly };

        /// <summary>
        /// <see cref="EditorPluginManager"/> should usually not be instantiated by users due to 
        /// its forced singleton-like usage. Use <see cref="DualityApp.PluginManager"/> instead.
        /// </summary>
        internal EditorPluginManager()
        {
        }

        /// <summary>
        /// Enumerates all currently loaded editor assemblies that are part of Duality, i.e. 
        /// the editor Assembly itsself and all loaded plugins.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Assembly> GetAssemblies()
        {
            return this.editorAssemblies.Concat(base.GetAssemblies());
        }

        /// <summary>
        /// Loads all available editor plugins, as well as auxilliary libraries.
        /// </summary>
        public override void LoadPlugins()
        {
            foreach (string dllPath in this.AssemblyLoader.AvailableAssemblyPaths) {
                if (!dllPath.EndsWith(".editor.dll", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                LoadPlugin(dllPath);
            }
        }
        /// <summary>
        /// Initializes all previously loaded plugins.
        /// </summary>
        public override void InitPlugins()
        {
            EditorPlugin[] initPlugins = this.LoadedPlugins.ToArray();
            foreach (EditorPlugin plugin in initPlugins) {
                this.InitPlugin(plugin);
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            this.AssemblyLoader.AssemblyResolve += this.assemblyLoader_AssemblyResolve;
        }
        protected override void OnTerminate()
        {
            base.OnTerminate();
            this.AssemblyLoader.AssemblyResolve -= this.assemblyLoader_AssemblyResolve;
        }
        protected override void OnInitPlugin(EditorPlugin plugin)
        {
            plugin.InitPlugin(App.MainWindow);
        }

        private void assemblyLoader_AssemblyResolve(object sender, AssemblyResolveEventArgs args)
        {
            // Early-out, if the Assembly has already been resolved
            if (args.IsResolved) return;

            // Search for editor plugins that haven't been loaded yet, and load them first.
            // This is required to satisfy dependencies while loading plugins, since
            // we can't know which one requires which beforehand.
            foreach (string libFile in this.AssemblyLoader.AvailableAssemblyPaths) {
                if (!libFile.EndsWith(".editor.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                string libName = PathOp.GetFileNameWithoutExtension(libFile);
                if (libName.Equals(args.AssemblyName, StringComparison.OrdinalIgnoreCase)) {
                    EditorPlugin plugin = this.LoadPlugin(libFile);
                    if (plugin != null) {
                        args.Resolve(plugin.PluginAssembly);
                        return;
                    }
                }
            }
        }
    }
}