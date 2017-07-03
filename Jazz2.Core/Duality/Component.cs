namespace Duality
{
    /// <summary>
    /// Components are isolated logic units that can independently be added to and removed from <see cref="GameObject">GameObjects</see>.
    /// Each Component has a distinct purpose, thus it is not possible to add multiple Components of the same Type to one GameObject.
    /// Also, a Component may not belong to multiple GameObjects at once.
    /// </summary>
    public abstract class Component : IManageableObject
    {
        /// <summary>
        /// Describes the kind of initialization that can be performed on a Component
        /// </summary>
        public enum InitContext
        {
            /// <summary>
            /// A saving process has just finished.
            /// </summary>
            Saved,
            /// <summary>
            /// The Component has been fully loaded.
            /// </summary>
            Loaded,
            /// <summary>
            /// The Component is being activated. This can be the result of activating it,
            /// activating its GameObject, adding itsself or its GameObject to the current 
            /// Scene or entering a <see cref="Scene"/> in which this Component is registered.
            /// </summary>
            Activate,
            /// <summary>
            /// The Component has just been added to a GameObject
            /// </summary>
            AddToGameObject
        }
        /// <summary>
        /// Describes the kind of shutdown that can be performed on a Component
        /// </summary>
        public enum ShutdownContext
        {
            /// <summary>
            /// A saving process is about to start
            /// </summary>
            Saving,
            /// <summary>
            /// The Component has been deactivated. This can be the result of deactivating it,
            /// deactivating its GameObject, removing itsself or its GameObject from the 
            /// current Scene or leaving a <see cref="Scene"/> in which this Component is registered.
            /// </summary>
            Deactivate,
            /// <summary>
            /// The Component is being removed from its GameObject.
            /// </summary>
            RemovingFromGameObject
        }


        internal GameObject gameobj = null;
        private bool active = true;


        /// <summary>
        /// [GET / SET] Whether or not the Component is currently active. To return true,
        /// both the Component itsself and its parent GameObject need to be active.
        /// </summary>
        /// <seealso cref="ActiveSingle"/>
        public bool Active
        {
            get { return this.ActiveSingle && this.gameobj != null && this.gameobj.Active; }
            set { this.ActiveSingle = value; }
        }
        /// <summary>
        /// [GET / SET] Whether or not the Component is currently active. Unlike <see cref="Active"/>,
        /// this property ignores parent activation states and depends only on this single Component.
        /// The scene graph and other Duality instances usually check <see cref="Active"/>, not ActiveSingle.
        /// </summary>
        /// <seealso cref="Active"/>
        public bool ActiveSingle
        {
            get { return this.active; }
            set
            {
                if (this.active != value) {
                    if (this.gameobj != null && this.gameobj.ParentScene != null && this.gameobj.ParentScene.IsCurrent) {
                        if (value) {
                            ICmpInitializable cInit = this as ICmpInitializable;
                            if (cInit != null) cInit.OnInit(InitContext.Activate);
                        } else {
                            ICmpInitializable cInit = this as ICmpInitializable;
                            if (cInit != null) cInit.OnShutdown(ShutdownContext.Deactivate);
                        }
                    }

                    this.active = value;
                }
            }
        }
        /// <summary>
        /// [GET] Returns whether this Component has been disposed. Disposed Components are not to be used and should
        /// be treated specifically or as null references by your code.
        /// </summary>
        public bool Disposed
        {
            get { return this.gameobj != null && this.gameobj.Disposed; }
        }
        /// <summary>
        /// [GET / SET] The <see cref="GameObject"/> to which this Component belongs.
        /// </summary>
        public GameObject GameObj
        {
            get { return this.gameobj; }
            set
            {
                if (this.gameobj != null) this.gameobj.RemoveComponent(this);
                if (value != null) value.AddComponent(this);
            }
        }

        /*uint IUniqueIdentifyable.PreferredId
		{
			get 
			{
				unchecked
				{
					int idTemp = this.GetType().GetTypeId().GetHashCode();
					if (this.gameobj != null)
					{
						MathF.CombineHashCode(ref idTemp, this.gameobj.Id.GetHashCode());
					}
					return (uint)idTemp;
				}
			}
		}*/


        /// <summary>
        /// Disposes this Component. You usually don't need this - use <see cref="ExtMethodsIManageableObject.DisposeLater"/> instead.
        /// </summary>
        /// <seealso cref="ExtMethodsIManageableObject.DisposeLater"/>
        public void Dispose()
        {
            // Remove from GameObject
            if (this.gameobj != null)
                this.gameobj.RemoveComponent(this);
        }

        public override string ToString()
        {
            if (this.gameobj == null)
                return this.GetType().Name;
            else
                return string.Format("{0} in \"{1}\"", this.GetType().Name, this.gameobj.FullName);
        }

        private static ComponentRequirementMap requireMap = new ComponentRequirementMap();
        private static ComponentExecutionOrder execOrder = new ComponentExecutionOrder();

        /// <summary>
        /// [GET] Provides information about how different <see cref="Component"/> types are
        /// depending on each other, as well as functionality to automatically enforce the
        /// dependencies of a given <see cref="Component"/> type.
        /// </summary>
        public static ComponentRequirementMap RequireMap
        {
            get { return requireMap; }
        }
        /// <summary>
        /// [GET] Provides information about the order in which different <see cref="Component"/>
        /// types are updated, initialized and shut down.
        /// </summary>
        public static ComponentExecutionOrder ExecOrder
        {
            get { return execOrder; }
        }
    }
}
