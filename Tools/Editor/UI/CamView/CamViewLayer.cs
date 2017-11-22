using System.Xml.Linq;
using Duality.Drawing;

namespace Editor.UI.CamView
{
    public abstract class CamViewLayer : CamViewClient
    {
        public virtual int Priority
        {
            get { return 0; }
        }
        public virtual bool MouseTracking
        {
            get { return false; }
        }

        public abstract string LayerName { get; }
        public abstract string LayerDesc { get; }

        protected internal virtual void SaveUserData(XElement node) { }
        protected internal virtual void LoadUserData(XElement node) { }
        protected internal virtual void OnActivateLayer() { }
        protected internal virtual void OnDeactivateLayer() { }
        protected internal virtual void OnCollectDrawcalls(Canvas canvas) { }
        protected internal virtual void OnCollectWorldOverlayDrawcalls(Canvas canvas) { }
        protected internal virtual void OnCollectOverlayDrawcalls(Canvas canvas) { }
        protected internal virtual void OnCollectBackgroundDrawcalls(Canvas canvas) { }
    }
}