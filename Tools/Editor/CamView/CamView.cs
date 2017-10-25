using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Editor.Backend;
using Duality.Input;
using Duality.Resources;
using Editor.CamView.States;
using BitArray = System.Collections.BitArray;
using Key = Duality.Input.Key;
using MouseButton = Duality.Input.MouseButton;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace Editor.CamView
{
    public partial class CamView : Control, IMouseInputSource, IKeyboardInputSource
	{
		public const float DefaultDisplayBoundRadius = 25.0f;

		private static CamView	activeCamView	= null;

		private	int						runtimeId					= 0;
		private	bool					isHiddenDocument			= false;
		private	INativeRenderableSite	graphicsControl				= null;
		private	GameObject				camObj						= null;
		private	Camera					camComp						= null;
		private	CamViewState			activeState					= null;
		private	List<CamViewLayer>		activeLayers				= null;
		private	HashSet<Type>			lockedLayers				= new HashSet<Type>();
		private	GameObject				nativeCamObj				= null;
		private	string					loadTempState				= null;
		private	string					loadTempPerspective			= null;
		private	string					loadTempRenderSetup			= null;
		//private	InputEventMessageRedirector	globalInputFilter		= null;
		private DateTime					globalInputLastOtherKey	= DateTime.Now;
		private DateTime					lastLocalMouseMove		= DateTime.Now;
		private Color					oldColorDialogColor;
		private Color					selectedColorDialogColor;

		private	Dictionary<Type,CamViewLayer>	availLayers	= new Dictionary<Type,CamViewLayer>();
		private	Dictionary<Type,CamViewState>	availStates	= new Dictionary<Type,CamViewState>();

		private	int				inputLastUpdateFrame	= -1;
		private	bool			inputMouseCapture		= false;
		private	int				inputMouseX				= 0;
		private	int				inputMouseY				= 0;
		private	float			inputMouseWheel			= 0.0f;
		private	int				inputMouseButtons		= 0;
		private	bool			inputMouseInView		= false;
		private	bool			inputKeyFocus			= false;
		private	BitArray		inputKeyPressed			= new BitArray((int)Key.Last + 1, false);
		private	string			inputCharInput			= null;
		private	StringBuilder	inputCharInputBuffer	= new StringBuilder();

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		public ColorRgba BgColor
		{
			get { return this.camComp.ClearColor; }
			set { this.camComp.ClearColor = value; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public ColorRgba FgColor
		{
			get { return this.BgColor.GetLuminance() < 0.5f ? ColorRgba.White : ColorRgba.Black; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public float NearZ
		{
			get { return this.camComp.NearZ; }
			set { this.camComp.NearZ = value; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public float FarZ
		{
			get { return this.camComp.FarZ; }
			set { this.camComp.FarZ = value; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public PerspectiveMode PerspectiveMode
		{
			get { return this.camComp.Perspective; }
			set { this.camComp.Perspective = value; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public Camera CameraComponent
		{
			get { return this.camComp; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public GameObject CameraObj
		{
			get { return this.camObj; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public INativeRenderableSite RenderableSite
		{
			get { return this.graphicsControl; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public Control RenderableControl
		{
			get { return this.graphicsControl != null ? this.graphicsControl.Control : null; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public CamViewState ActiveState
		{
			get { return this.activeState; }
		}
	    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<CamViewLayer> ActiveLayers
		{
			get { return this.activeLayers; }
		}

		public CamView()
		{
		    try {
		        var camViewStateTypeQuery =
		            from t in App.GetAvailDualityEditorTypes(typeof(CamViewState))
		            where !t.IsAbstract
		            select t;
		        foreach (TypeInfo t in camViewStateTypeQuery) {
		            CamViewState state = t.CreateInstanceOf() as CamViewState;
		            state.View = this;
		            this.availStates.Add(t, state);
		        }

		        var camViewLayerTypeQuery =
		            from t in App.GetAvailDualityEditorTypes(typeof(CamViewLayer))
		            where !t.IsAbstract
		            select t;
		        foreach (TypeInfo t in camViewLayerTypeQuery) {
		            CamViewLayer layer = t.CreateInstanceOf() as CamViewLayer;
		            layer.View = this;
		            this.availLayers.Add(t, layer);
		        }
            } catch {
		        // ToDo: Designer
		    }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            try {
                this.InitGLControl();
                this.InitNativeCamera();
                this.SetCurrentCamera(null);

                // Initialize from loaded state id, if not done yet manually
                if (this.activeState == null) {
                    Type stateType = ReflectionHelper.ResolveType(this.loadTempState) ?? typeof(/*SceneEditorCamViewState*/TileMapEditorState);
                    this.SetCurrentState(stateType);
                    this.loadTempState = null;
                }
                // If we set the state explicitly before, we'll still need to fire its enter event. See SetCurrentState.
                else {
                    this.activeState.OnEnterState();
                }

                // Register DualityApp updater for camera steering behaviour
                this.RegisterEditorEvents();

                // Initially assume ownership of Duality rendering and audio
                this.MakeDualityTarget();
            } catch {
                // ToDo: Designer
            }
		}

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

			// If this was the active Camera View, stop assuming this.
			if (activeCamView == this)
				activeCamView = null;

			if (this.nativeCamObj != null)
				this.nativeCamObj.Dispose();

			this.UnregisterEditorEvents();

			this.SetCurrentState((CamViewState)null);
		}

	    private void RegisterEditorEvents()
	    {
	        DualityApp.PluginManager.PluginsRemoving += this.PluginManager_PluginsRemoving;
	        //FileEventManager.ResourceModified += this.FileEventManager_ResourceModified;
	        App.Terminating += this.DualityEditorApp_Terminating;
	        //App.HighlightObject += this.DualityEditorApp_HighlightObject;
	        //App.ObjectPropertyChanged += this.DualityEditorApp_ObjectPropertyChanged;
	        App.UpdatingEngine += this.DualityEditorApp_UpdatingEngine;
	        Scene.Entered += this.Scene_Entered;
	        Scene.Leaving += this.Scene_Leaving;
	        Scene.GameObjectsRemoved += this.Scene_GameObjectsUnregistered;
	        Scene.ComponentRemoving += this.Scene_ComponentRemoving;
	    }
	    private void UnregisterEditorEvents()
	    {
	        DualityApp.PluginManager.PluginsRemoving -= this.PluginManager_PluginsRemoving;
	        //FileEventManager.ResourceModified -= this.FileEventManager_ResourceModified;
	        App.Terminating -= this.DualityEditorApp_Terminating;
	        //App.HighlightObject -= this.DualityEditorApp_HighlightObject;
	        //App.ObjectPropertyChanged -= this.DualityEditorApp_ObjectPropertyChanged;
	        App.UpdatingEngine -= this.DualityEditorApp_UpdatingEngine;
	        Scene.Entered -= this.Scene_Entered;
	        Scene.Leaving -= this.Scene_Leaving;
	        Scene.GameObjectsRemoved -= this.Scene_GameObjectsUnregistered;
	        Scene.ComponentRemoving -= this.Scene_ComponentRemoving;
	    }

        private void InitGLControl()
		{
			this.SuspendLayout();

			// Get rid of a possibly existing old glControl
			if (this.graphicsControl != null)
			{
				Control oldControl = this.graphicsControl.Control;

				oldControl.MouseEnter		-= this.graphicsControl_MouseEnter;
				oldControl.MouseLeave		-= this.graphicsControl_MouseLeave;
				oldControl.MouseDown		-= this.graphicsControl_MouseDown;
				oldControl.MouseUp			-= this.graphicsControl_MouseUp;
				oldControl.MouseWheel		-= this.graphicsControl_MouseWheel;
				oldControl.MouseMove		-= this.graphicsControl_MouseMove;
				oldControl.GotFocus			-= this.graphicsControl_GotFocus;
				oldControl.LostFocus		-= this.graphicsControl_LostFocus;
				oldControl.PreviewKeyDown	-= this.graphicsControl_PreviewKeyDown;
				oldControl.KeyDown			-= this.graphicsControl_KeyDown;
				oldControl.KeyUp			-= this.graphicsControl_KeyUp;
				oldControl.KeyPress			-= this.graphicsControl_KeyPress;
				oldControl.Resize			-= this.graphicsControl_Resize;

				this.graphicsControl.Dispose();
				this.Controls.Remove(oldControl);
			}

			// Create a new glControl
			this.graphicsControl = App.CreateRenderableSite();
            if (this.graphicsControl == null) {
                return;
            }

			Control control = this.graphicsControl.Control;

			control.BackColor = Color.Black;
			control.Dock = DockStyle.Fill;
			control.Name = "graphicsControl";
			control.AllowDrop = true;
			control.MouseEnter		+= this.graphicsControl_MouseEnter;
			control.MouseLeave		+= this.graphicsControl_MouseLeave;
			control.MouseDown		+= this.graphicsControl_MouseDown;
			control.MouseUp			+= this.graphicsControl_MouseUp;
			control.MouseWheel		+= this.graphicsControl_MouseWheel;
			control.MouseMove		+= this.graphicsControl_MouseMove;
			control.GotFocus		+= this.graphicsControl_GotFocus;
			control.LostFocus		+= this.graphicsControl_LostFocus;
			control.PreviewKeyDown	+= this.graphicsControl_PreviewKeyDown;
			control.KeyDown			+= this.graphicsControl_KeyDown;
			control.KeyUp			+= this.graphicsControl_KeyUp;
			control.KeyPress		+= this.graphicsControl_KeyPress;
			control.Resize			+= this.graphicsControl_Resize;
			this.Controls.Add(control);
			this.Controls.SetChildIndex(control, 0);

			this.ResumeLayout(true);
		}

		private void InitNativeCamera()
		{
			// Create internal Camera object
			this.nativeCamObj = new GameObject();
			this.nativeCamObj.Name = "CamView Camera " + this.runtimeId;
			this.nativeCamObj.AddComponent<Transform>();
			this.nativeCamObj.AddComponent<SoundListener>().MakeCurrent();

			Camera c = this.nativeCamObj.AddComponent<Camera>();
		    c.Perspective = PerspectiveMode.Flat;
			c.ClearColor = ColorRgba.DarkGrey;
			c.FarZ = 1000.0f;
			c.RenderingSetup = RenderSetup.Default;

			this.nativeCamObj.Transform.Pos = new Vector3(0f, 0f, /*-c.FocusDist*/0f);
			App.EditorObjects.AddObject(this.nativeCamObj);
		}

		public void SetCurrentCamera(Camera c)
		{
			if (c == null) c = this.nativeCamObj.GetComponent<Camera>();
			if (c == this.camComp) return;

			Camera prev = this.camComp;

			if (c.GameObj == this.nativeCamObj)
			{
				this.camObj = this.nativeCamObj;
				this.camComp = this.camObj.GetComponent<Camera>();
			}
			else
			{
				this.camObj = c.GameObj;
				this.camComp = c;
			}

			this.OnCurrentCameraChanged(prev, this.camComp);
			this.RenderableControl.Invalidate();
		}
		public void SetCurrentState(Type stateType)
		{
			if (!typeof(CamViewState).IsAssignableFrom(stateType)) return;
			if (this.activeState != null && this.activeState.GetType() == stateType) return;

			this.SetCurrentState(this.availStates[stateType]);
		}
		public void SetCurrentState(CamViewState state)
		{
			if (this.activeState == state) return;
			if (this.activeState != null) this.activeState.OnLeaveState();

			this.activeState = state;

			// If we have a graphics control, we have initialized properly and can enter the state right away.
			// Otherwise, this is the initial state and we'll need to wait until initialization. Enter the state later.
			if (this.graphicsControl != null)
			{
				if (this.activeState != null) this.activeState.OnEnterState();
				this.RenderableControl.Invalidate();
			}
		}

		public void SetActiveLayers(IEnumerable<Type> layerTypes)
		{
			if (this.activeLayers == null) this.activeLayers = new List<CamViewLayer>();

			// Deactivate unused layers
			for (int i = this.activeLayers.Count - 1; i >= 0; i--)
			{
				Type layerType = this.activeLayers[i].GetType();
				if (!layerTypes.Contains(layerType)) this.DeactivateLayer(this.activeLayers[i]);
			}

			// Activate not-yet-active layers
			foreach (Type layerType in layerTypes)
				this.ActivateLayer(layerType);
		}
		public void ActivateLayer(CamViewLayer layer)
		{
			if (this.activeLayers == null) this.activeLayers = new List<CamViewLayer>();
			if (this.activeLayers.Contains(layer)) return;
			if (this.activeLayers.Any(l => l.GetType() == layer.GetType())) return;
			if (this.lockedLayers.Contains(layer.GetType())) return;

			this.activeLayers.Add(layer);
			layer.View = this;
			// No glControl yet? We're not initialized properly and this is the initial state. Enter the state later.
			if (this.graphicsControl != null)
			{
				layer.OnActivateLayer();
				this.RenderableControl.Invalidate();
			}
		}
		public void ActivateLayer(Type layerType)
		{
			this.ActivateLayer(this.availLayers[layerType]);
		}
		public void DeactivateLayer(CamViewLayer layer)
		{
			if (this.activeLayers == null) this.activeLayers = new List<CamViewLayer>();
			if (!this.activeLayers.Contains(layer)) return;
			if (this.lockedLayers.Contains(layer.GetType())) return;

			layer.OnDeactivateLayer();
			layer.View = null;
			this.activeLayers.Remove(layer);
			this.RenderableControl.Invalidate();
		}
		public void DeactivateLayer(Type layerType)
		{
			this.DeactivateLayer(this.activeLayers.FirstOrDefault(l => l.GetType() == layerType));
		}
		public void LockLayer(CamViewLayer layer)
		{
			this.LockLayer(layer.GetType());
		}
		public void LockLayer(Type layerType)
		{
			if (this.lockedLayers.Contains(layerType)) return;
			this.lockedLayers.Add(layerType);
		}
		public void UnlockLayer(CamViewLayer layer)
		{
			this.UnlockLayer(layer.GetType());
		}
		public void UnlockLayer(Type layerType)
		{
			this.lockedLayers.Remove(layerType);
		}

		public void MakeDualityTarget()
		{
			if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) return;

			activeCamView = this;

			DualityApp.WindowSize = this.activeState.RenderedImageSize;
			DualityApp.Mouse.Source = this;
			DualityApp.Keyboard.Source = this;

			// If we have a CamView-local listener that is active, use that one.
			// Note: The GameViewCamViewState deactivates its own camera, since it uses the
			// regular Scene rendering setup, rather than providing its own.
			SoundListener localListener = this.CameraObj.GetComponent<SoundListener>();
			if (localListener != null && localListener.Active)
			{
				localListener.MakeCurrent();
			}
			// If we don't have a local listener, use the regular one from the current scene
			else
			{
				SoundListener sceneListener = Scene.Current.FindComponent<SoundListener>();
				if (sceneListener != null)
					sceneListener.MakeCurrent();
			}
		}

		public void ResetCamera()
		{
			this.FocusOnPos(Vector3.Zero);
			this.camObj.Transform.Angle = 0.0f;
		}
		public void FocusOnPos(Vector3 targetPos)
		{
			if (!this.activeState.CameraActionAllowed) return;
			//targetPos -= Vector3.UnitZ * MathF.Abs(this.camComp.FocusDist);
			//targetPos.Z = MathF.Min(this.camObj.Transform.Pos.Z, targetPos.Z);
			this.camObj.Transform.Pos = targetPos;
			//this.OnCamTransformChanged();
			this.RenderableControl.Invalidate();
		}
		public void FocusOnObject(GameObject obj)
		{
			this.FocusOnPos((obj == null || obj.Transform == null) ? Vector3.Zero : obj.Transform.Pos);
		}

		private void OnCurrentCameraChanged(Camera prev, Camera next)
		{
			//if (this.CurrentCameraChanged != null)
			//	this.CurrentCameraChanged(this, new CameraChangedEventArgs(prev, next));
		}

		/*private void InstallFocusHook()
		{
			if (this.graphicsControl.Control.Focused) return;

			// Hook global message filter
			if (this.globalInputFilter == null)
			{
				this.globalInputFilter = new InputEventMessageRedirector(
					this.graphicsControl.Control, 
					this.FocusHookFilter, 
					InputEventMessageRedirector.MessageType.MouseWheel,
					InputEventMessageRedirector.MessageType.KeyDown);
				Application.AddMessageFilter(this.globalInputFilter);
			}
		}
		private void RemoveFocusHook()
		{
			// Remove global message filter
			if (this.globalInputFilter != null)
			{
				Application.RemoveMessageFilter(this.globalInputFilter);
				this.globalInputFilter = null;
			}
		}
		private bool FocusHookFilter(InputEventMessageRedirector.MessageType type, EventArgs e)
		{
			// Don't capture when the sandbox is active. Input is likely to be needed in the current Game View.
			if (Sandbox.State == SandboxState.Playing) return false;

			// Capture mouse wheel for camera navigation
			if (type == InputEventMessageRedirector.MessageType.MouseWheel)
			{
				return true;
			}
			// Capture space key for alternative camera navigation
			else if (type == InputEventMessageRedirector.MessageType.KeyDown)
			{
				KeyEventArgs keyArgs = e as KeyEventArgs;
				if (keyArgs == null) return false;
				if (keyArgs.KeyCode == Keys.Space)
				{
					// Only capture the space key when we had recent movement and no other input keys.
					// The user might be typing something with the mouse cursor accidentally hovering here.
					if ((DateTime.Now - this.globalInputLastOtherKey).TotalMilliseconds > 1000 &&
						(DateTime.Now - this.lastLocalMouseMove).TotalMilliseconds < 1000)
					{
						return true;
					}
				}
				else
				{
					this.globalInputLastOtherKey = DateTime.Now;
				}
			}
			
			return false;
		}*/
		private void graphicsControl_MouseLeave(object sender, EventArgs e)
		{
			if (this.activeState.EngineUserInput)
			{
				this.inputMouseInView = false;
			}

			//this.RemoveFocusHook();

			if (this.activeLayers.Any(l => l.MouseTracking))
				this.RenderableControl.Invalidate();
		}
		private void graphicsControl_MouseEnter(object sender, EventArgs e)
		{
			//this.InstallFocusHook();

			if (this.activeLayers.Any(l => l.MouseTracking))
				this.RenderableControl.Invalidate();
		}
		private void graphicsControl_MouseDown(object sender, MouseEventArgs e)
		{
			this.inputMouseCapture = true;
			if (this.activeState.EngineUserInput)
			{
				MouseButton inputButton = e.Button.ToDualitySingle();
				this.inputMouseButtons |= e.Button.ToDuality();
			}
		}
		private void graphicsControl_MouseUp(object sender, MouseEventArgs e)
		{
			if (this.activeState.EngineUserInput)
			{
				MouseButton inputButton = e.Button.ToDualitySingle();
				this.inputMouseButtons &= ~e.Button.ToDuality();
			}
		}
		private void graphicsControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (!this.RenderableControl.Focused) this.RenderableControl.Focus();

			if (this.activeState.EngineUserInput)
			{
				this.inputMouseWheel += e.Delta / 120.0f;
			}
		}
		private void graphicsControl_MouseMove(object sender, MouseEventArgs e)
		{
			this.lastLocalMouseMove = DateTime.Now;

			if (this.activeState.EngineUserInput)
			{
				Vector2 gameSize = this.activeState.RenderedImageSize;
				Rect inputArea = this.activeState.RenderedViewport;

				this.inputMouseX = MathF.RoundToInt(gameSize.X * (e.X - inputArea.X) / inputArea.W);
				this.inputMouseY = MathF.RoundToInt(gameSize.Y * (e.Y - inputArea.Y) / inputArea.H);
				this.inputMouseInView = 
					this.inputMouseX >= 0.0f &&
					this.inputMouseX <= gameSize.X &&
					this.inputMouseY >= 0.0f &&
					this.inputMouseY <= gameSize.Y;
			}

			if (this.activeLayers.Any(l => l.MouseTracking))
				this.RenderableControl.Invalidate();
		}
		private void graphicsControl_GotFocus(object sender, EventArgs e)
		{
			//this.RemoveFocusHook();
			this.inputMouseCapture = true;

			if (this.activeState != null)
			{
				if (this.activeState.EngineUserInput)
				{
					this.inputKeyFocus = true;
				}
			}
		}
		private void graphicsControl_LostFocus(object sender, EventArgs e)
		{
			if (this.activeState != null && this.activeState.EngineUserInput)
			{
				this.inputKeyFocus = false;
			}
		}
		private void graphicsControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (this.activeState.EngineUserInput) 
				e.IsInputKey = // Special key blacklist: Do not forward to game
					e.KeyCode != Keys.F1 &&
					e.KeyCode != Keys.F2 &&
					e.KeyCode != Keys.F3 &&
					e.KeyCode != Keys.F4 &&
					e.KeyCode != Keys.F5 &&
					e.KeyCode != Keys.F6 &&
					e.KeyCode != Keys.F7 &&
					e.KeyCode != Keys.F8 &&
					e.KeyCode != Keys.F9 &&
					e.KeyCode != Keys.F10 &&
					e.KeyCode != Keys.F11 &&
					e.KeyCode != Keys.F12;
			else
				e.IsInputKey = // Special key whitelist: Do forward to CamViewState
					e.KeyCode == Keys.Left || 
					e.KeyCode == Keys.Right || 
					e.KeyCode == Keys.Up || 
					e.KeyCode == Keys.Down;
		}
		private void graphicsControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (!this.RenderableControl.Focused) this.RenderableControl.Focus();

			if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Alt)
			{
				this.inputMouseCapture = false;
			}

			if (this.activeState.EngineUserInput)
			{
				Key inputKey = e.KeyCode.ToDualityKey();
				this.inputKeyPressed[(int)inputKey] = true;
			}
		}
		private void graphicsControl_KeyUp(object sender, KeyEventArgs e)
		{
			if (this.activeState.EngineUserInput)
			{
				Key inputKey = e.KeyCode.ToDualityKey();
				this.inputKeyPressed[(int)inputKey] = false;
			}
		}
		private void graphicsControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (this.activeState.EngineUserInput)
			{
				if (e.KeyChar == '\b') return; // Filter out backspace control character, so we don't get more chars than the regular launcher backend.
				this.inputCharInputBuffer.Append(e.KeyChar);
			}
		}
		private void graphicsControl_Resize(object sender, EventArgs e)
		{
			if (activeCamView == this)
			{
				DualityApp.WindowSize = this.activeState.RenderedImageSize;
			}
			this.RenderableControl.Invalidate();
		}

		/*private void FileEventManager_ResourceModified(object sender, ResourceEventArgs e)
		{
			if (!e.IsResource) return;
			this.RenderableControl.Invalidate();
		}*/
		private void PluginManager_PluginsRemoving(object sender, DualityPluginEventArgs e)
		{
			//this.objectVisibility.ClearTypeCache();
		}
		private void DualityEditorApp_Terminating(object sender, EventArgs e)
		{
			this.UnregisterEditorEvents();
		}
		private void DualityEditorApp_UpdatingEngine(object sender, EventArgs e)
		{
			//if (this.camObj != null && this.camObj != this.nativeCamObj)
			//	App.UpdateGameObject(this.camObj);
		}

		private void Scene_Entered(object sender, EventArgs e)
		{
			//if (!Sandbox.IsActive && !Sandbox.IsChangingState) this.ResetCamera();
			this.RenderableControl.Invalidate();
		}
		private void Scene_Leaving(object sender, EventArgs e)
		{
			if (this.camObj != this.nativeCamObj) this.SetCurrentCamera(null);
			this.RenderableControl.Invalidate();
		}
		private void Scene_ComponentRemoving(object sender, ComponentEventArgs e)
		{
			if (this.camComp == e.Component) this.SetCurrentCamera(null);
		}
		private void Scene_GameObjectsUnregistered(object sender, GameObjectGroupEventArgs e)
		{
			if (e.Objects.Contains(this.camObj))
				this.SetCurrentCamera(null);
		}


		Point2 IMouseInputSource.Pos
		{
			get { return new Point2(this.inputMouseX, this.inputMouseY); }
			set
			{
				if (!this.activeState.EngineUserInput) return;
				if (!this.RenderableControl.Focused) return;
				if (!this.inputMouseCapture) return;

				Vector2 gameSize = this.activeState.RenderedImageSize;
				Rect inputArea = this.activeState.RenderedViewport;

				Point targetLocalPoint = new Point(
					MathF.RoundToInt(inputArea.X + inputArea.W * value.X / gameSize.X),
					MathF.RoundToInt(inputArea.Y + inputArea.H * value.Y / gameSize.Y));
				Point targetScreenPoint = this.RenderableControl.PointToScreen(targetLocalPoint);

				Cursor.Position = targetScreenPoint;
			}
		}
		float IMouseInputSource.Wheel
		{
			get { return this.inputMouseWheel; }
		}
		bool IMouseInputSource.this[MouseButton btn]
		{
			get { return (this.inputMouseButtons & (1 << (int)btn)) != 0; }
		}

		string IKeyboardInputSource.CharInput
		{
			get { return this.inputCharInput ?? string.Empty; }
		}
		bool IKeyboardInputSource.this[Key key]
		{
			get { return this.inputKeyPressed[(int)key]; }
		}

		string IUserInputSource.Description
		{
			get { return "Camera View"; }
		}
		bool IUserInputSource.IsAvailable
		{
			// These should be separated.. but C# doesn't allow to implement IsAvailable for both sources separately.
			get { return this.inputKeyFocus && this.inputMouseInView; }
		}
		void IUserInputSource.UpdateState()
		{
			if (this.inputLastUpdateFrame == Time.FrameCount) return;
			this.inputLastUpdateFrame = Time.FrameCount;

			this.inputCharInput = this.inputCharInputBuffer.ToString();
			this.inputCharInputBuffer.Clear();
		}
	}
}