using Duality;

namespace Editor
{
    public abstract class EditorPlugin : DualityPlugin
    {
        /// <summary>
        /// The Plugins ID. This should be unique.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// This method is called when all plugins and the editors user data and layout are loaded. May initialize GUI.
        /// </summary>
        /// <param name="main"></param>
        protected internal virtual void InitPlugin(MainWindow main) { }
    }
}