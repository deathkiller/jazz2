using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Resources;

namespace Editor.CamView
{
    public abstract class CamViewState : CamViewClient//, IHelpProvider
	{
		[Flags]
		public enum UserGuideType
		{
			None		= 0x0,

			Position	= 0x1,
			Scale		= 0x2,

			All			= Position | Scale
		}
		public enum CameraAction
		{
			None,
			Move,

			// Alternate movement (Spacebar pressed)
			DragScene,
		}

		private Vector3       camVel                 = Vector3.Zero;
		private Point         camActionBeginLoc      = Point.Empty;
		private Vector3       camActionBeginLocSpace = Vector3.Zero;
		private CameraAction  camAction              = CameraAction.None;
		private bool          camActionAllowed       = true;
		private bool          camTransformChanged    = false;
		private bool          camBeginDragScene      = false;
		private RenderStep    camPassBg              = null;
		private RenderStep    camPassEdWorld         = null;
		private RenderStep    camPassEdWorldNoDepth  = null;
		private RenderStep    camPassEdScreen        = null;
		private bool          engineUserInput        = false;
		private UserGuideType snapToUserGuides       = UserGuideType.All;
		private bool          mouseover              = false;
		private CameraAction  drawCamGizmoState      = CameraAction.None;
		private List<Type>    lastActiveLayers       = new List<Type>();
		private List<string>  lastObjVisibility      = new List<string>();
		private int           renderFrameLast        = -1;
		private bool          renderFrameScheduled   = false;
		private Canvas        overlayCanvas          = new Canvas();


		public abstract string StateName { get; }

		protected virtual bool IsActionInProgress
		{
			get { return false; }
		}
		protected virtual bool HasCameraFocusPosition
		{
			get { return false; }
		}
		protected virtual Vector3 CameraFocusPosition
		{
			get { return Vector3.Zero; }
		}

		public bool IsActive
		{
			get { return this.View != null && this.View.ActiveState == this; }
		}
		public bool EngineUserInput
		{
			get { return this.engineUserInput; }
			protected set { this.engineUserInput = value; }
		}
		public virtual Rect RenderedViewport
		{
			get { return new Rect(this.RenderedImageSize); }
		}
		public virtual Point2 RenderedImageSize
		{
			get
			{
				return new Point2(
					this.RenderableControl.ClientSize.Width, 
					this.RenderableControl.ClientSize.Height);
			}
		}
		public bool CameraActionAllowed
		{
			get { return this.camActionAllowed; }
			protected set
			{ 
				this.camActionAllowed = value;
				if (!this.camActionAllowed && this.camAction != CameraAction.None)
				{
					this.camAction = CameraAction.None;
					this.Invalidate();
				}
			}
		}
		public bool Mouseover
		{
			get { return this.mouseover; }
		}
		public bool CamActionRequiresCursor
		{
			get { return this.camBeginDragScene; }
		}
		public CameraAction CamAction
		{
			get { return this.camAction; }
		}

		/// <summary>
		/// Called when the <see cref="CamViewState"/> is entered.
		/// Use this for overall initialization of the state.
		/// </summary>
		protected internal virtual void OnEnterState()
		{
			this.RestoreActiveLayers();

			// Create re-usable render passes for editor gizmos
			{
				// A screen overlay that is rendered behind all following gizmos.
				// This is used for the "background plate" to grey out or darken
				// the actual rendered world in order to make custom gizmos more visible.
				this.camPassBg = new RenderStep
				{
					Id = "EditorGizmoBackground",
					MatrixMode = RenderMatrix.ScreenSpace,
					ClearFlags = ClearFlag.None,
					VisibilityMask = VisibilityFlag.ScreenOverlay
				};

				// An in-world rendering step that can make use of the existing depth
				// buffer values, so gizmos can interact with actual world geometry.
				this.camPassEdWorld = new RenderStep
				{
					Id = "EditorGizmoWorld",
					ClearFlags = ClearFlag.None,
					VisibilityMask = VisibilityFlag.None
				};

				// An in-world rendering step where the depth buffer has been cleared.
				// This allows to render gizmos in world coordinates that can occlude
				// each other, while not interacting with world geometry or previously
				// rendered gizmos.
				this.camPassEdWorldNoDepth = new RenderStep
				{
					Id = "EditorGizmoWorldOverlay",
					ClearFlags = ClearFlag.Depth,
					VisibilityMask = VisibilityFlag.None
				};

				// The final screen overlay rendering step after all gizmos have been 
				// rendered. This is ideal for most text / status overlays, as well as
				// direct cursor feedback.
				this.camPassEdScreen = new RenderStep
				{
					Id = "EditorGizmoScreenOverlay",
					MatrixMode = RenderMatrix.ScreenSpace,
					ClearFlags = ClearFlag.None,
					VisibilityMask = VisibilityFlag.ScreenOverlay
				};
			}

			Control control = this.RenderableSite.Control;
			control.Paint		+= this.RenderableControl_Paint;
			control.MouseDown	+= this.RenderableControl_MouseDown;
			control.MouseUp		+= this.RenderableControl_MouseUp;
			control.MouseMove	+= this.RenderableControl_MouseMove;
			control.MouseWheel	+= this.RenderableControl_MouseWheel;
			control.MouseLeave	+= this.RenderableControl_MouseLeave;
			control.KeyDown		+= this.RenderableControl_KeyDown;
			control.KeyUp		+= this.RenderableControl_KeyUp;
			control.GotFocus	+= this.RenderableControl_GotFocus;
			control.LostFocus	+= this.RenderableControl_LostFocus;
			control.DragDrop	+= this.RenderableControl_DragDrop;
			control.DragEnter	+= this.RenderableControl_DragEnter;
			control.DragLeave	+= this.RenderableControl_DragLeave;
			control.DragOver	+= this.RenderableControl_DragOver;
			control.Resize		+= this.RenderableControl_Resize;
			//this.View.CurrentCameraChanged	+= this.View_CurrentCameraChanged;
			App.UpdatingEngine += this.DualityEditorApp_UpdatingEngine;
			//App.ObjectPropertyChanged += this.DualityEditorApp_ObjectPropertyChanged;

			Scene.Leaving += this.Scene_Changed;
			Scene.Entered += this.Scene_Changed;
			Scene.GameObjectParentChanged += this.Scene_Changed;
			Scene.GameObjectsAdded += this.Scene_Changed;
			Scene.GameObjectsRemoved += this.Scene_Changed;
			Scene.ComponentAdded += this.Scene_Changed;
			Scene.ComponentRemoving += this.Scene_Changed;

			if (Scene.Current != null) this.Scene_Changed(this, EventArgs.Empty);

			//if (this.IsViewVisible)
				this.OnShown();
		}
		/// <summary>
		/// Called when the <see cref="CamViewState"/> is left.
		/// Use this for overall termination of the state.
		/// </summary>
		protected internal virtual void OnLeaveState()
		{
			//if (this.IsViewVisible)
				this.OnHidden();

			//this.Cursor = CursorHelper.Arrow;

			Control control = this.RenderableSite.Control;
			control.Paint		-= this.RenderableControl_Paint;
			control.MouseDown	-= this.RenderableControl_MouseDown;
			control.MouseUp		-= this.RenderableControl_MouseUp;
			control.MouseMove	-= this.RenderableControl_MouseMove;
			control.MouseWheel	-= this.RenderableControl_MouseWheel;
			control.MouseLeave	-= this.RenderableControl_MouseLeave;
			control.KeyDown		-= this.RenderableControl_KeyDown;
			control.KeyUp		-= this.RenderableControl_KeyUp;
			control.GotFocus	-= this.RenderableControl_GotFocus;
			control.LostFocus	-= this.RenderableControl_LostFocus;
			control.DragDrop	-= this.RenderableControl_DragDrop;
			control.DragEnter	-= this.RenderableControl_DragEnter;
			control.DragLeave	-= this.RenderableControl_DragLeave;
			control.DragOver	-= this.RenderableControl_DragOver;
			control.Resize		-= this.RenderableControl_Resize;
			//this.View.CurrentCameraChanged			-= this.View_CurrentCameraChanged;
			App.UpdatingEngine			-= this.DualityEditorApp_UpdatingEngine;
			//DualityEditorApp.ObjectPropertyChanged	-= this.DualityEditorApp_ObjectPropertyChanged;

			Scene.Leaving -= this.Scene_Changed;
			Scene.Entered -= this.Scene_Changed;
			Scene.GameObjectParentChanged -= this.Scene_Changed;
			Scene.GameObjectsAdded -= this.Scene_Changed;
			Scene.GameObjectsRemoved -= this.Scene_Changed;
			Scene.ComponentAdded -= this.Scene_Changed;
			Scene.ComponentRemoving -= this.Scene_Changed;

			this.SaveActiveLayers();
		}
		/// <summary>
		/// Called when the <see cref="CamViewState"/> becomes visible to the user, e.g.
		/// by being entered, selecting the multi-document tab that contains its parent <see cref="CamView"/>
		/// or similar.
		/// </summary>
		protected internal virtual void OnShown() { }
		/// <summary>
		/// Called when the <see cref="CamViewState"/> becomes hidden from the user, e.g.
		/// by being left, deselecting the multi-document tab that contains its parent <see cref="CamView"/>
		/// or similar.
		/// </summary>
		protected internal virtual void OnHidden() { }


		protected virtual void OnCollectStateDrawcalls(Canvas canvas)
		{
			// Collect the views layer drawcalls
			this.CollectLayerDrawcalls(canvas);
		}
		protected virtual void OnCollectStateWorldOverlayDrawcalls(Canvas canvas)
		{
			// Collect the views layer drawcalls
			this.CollectLayerWorldOverlayDrawcalls(canvas);
		}
		protected virtual void OnCollectStateOverlayDrawcalls(Canvas canvas)
		{
			// Gather general data
			Point cursorPos = this.PointToClient(Cursor.Position);

			// Collect the views overlay layer drawcalls
			this.CollectLayerOverlayDrawcalls(canvas);

			// Collect the states overlay drawcalls
			canvas.PushState();
			{
				// Draw camera movement indicators
				if (this.camAction != CameraAction.None)
				{
					canvas.PushState();
					canvas.State.ColorTint *= ColorRgba.White.WithAlpha(0.5f);
					if (this.camAction == CameraAction.DragScene)
					{
						// Don't draw anything.
					}
					else if (this.camAction == CameraAction.Move)
					{
						canvas.FillCircle(this.camActionBeginLoc.X, this.camActionBeginLoc.Y, 3);
						canvas.DrawLine(this.camActionBeginLoc.X, this.camActionBeginLoc.Y, cursorPos.X, cursorPos.Y);
					}
					canvas.PopState();
				}
			}
			canvas.PopState();

			// Draw a focus indicator at the view border
			canvas.PushState();
			{
				ColorRgba focusColor = ColorRgba.Lerp(this.FgColor, this.BgColor, 0.25f).WithAlpha(255);
				ColorRgba noFocusColor = ColorRgba.Lerp(this.FgColor, this.BgColor, 0.75f).WithAlpha(255);
				canvas.State.ColorTint *= this.Focused ? focusColor : noFocusColor;
				canvas.DrawRect(0, 0, canvas.DrawDevice.TargetSize.X, canvas.DrawDevice.TargetSize.Y);
			}
			canvas.PopState();
		}
		protected virtual void OnCollectStateBackgroundDrawcalls(Canvas canvas)
		{
			// Collect the views overlay layer drawcalls
			this.CollectLayerBackgroundDrawcalls(canvas);
		}
		protected virtual string UpdateActionText(ref Vector2 actionTextPos)
		{
			return null;
		}
		protected virtual void OnRenderState()
		{
			RenderSetup renderSetup = this.CameraComponent.ActiveRenderSetup;

			renderSetup.EventCollectDrawcalls += this.CameraComponent_EventCollectDrawcalls;
			renderSetup.AddRendererFilter(this.RendererFilter);
			renderSetup.AddRenderStep(RenderStepPosition.Last, this.camPassBg);
			renderSetup.AddRenderStep(this.camPassBg.Id, RenderStepPosition.After, this.camPassEdWorld);
			renderSetup.AddRenderStep(this.camPassEdWorld.Id, RenderStepPosition.After, this.camPassEdWorldNoDepth);
			renderSetup.AddRenderStep(this.camPassEdWorldNoDepth.Id, RenderStepPosition.After, this.camPassEdScreen);

			// Render CamView
			Point2 clientSize = new Point2(this.ClientSize.Width, this.ClientSize.Height);
			this.CameraComponent.Render(new Rect(clientSize), clientSize);

			renderSetup.Steps.Remove(this.camPassBg);
			renderSetup.Steps.Remove(this.camPassEdWorld);
			renderSetup.Steps.Remove(this.camPassEdWorldNoDepth);
			renderSetup.Steps.Remove(this.camPassEdScreen);
			renderSetup.RemoveRendererFilter(this.RendererFilter);
			renderSetup.EventCollectDrawcalls -= this.CameraComponent_EventCollectDrawcalls;
		}
		protected virtual void OnUpdateState()
		{
			Camera cam = this.CameraComponent;
			GameObject camObj = this.CameraObj;
			Point cursorPos = this.PointToClient(Cursor.Position);

			float unscaledTimeMult = Time.TimeMult / Time.TimeScale;

			this.camTransformChanged = false;

			if (this.camAction == CameraAction.DragScene)
			{
				Vector2 curPos = new Vector2(cursorPos.X, cursorPos.Y);
				Vector2 lastPos = new Vector2(this.camActionBeginLoc.X, this.camActionBeginLoc.Y);
				this.camActionBeginLoc = new Point((int)curPos.X, (int)curPos.Y);

				float refZ = (this.HasCameraFocusPosition && camObj.Transform.Pos.Z < this.CameraFocusPosition.Z - cam.NearZ) ? this.CameraFocusPosition.Z : 0.0f;
				if (camObj.Transform.Pos.Z >= refZ - cam.NearZ)
					refZ = camObj.Transform.Pos.Z + MathF.Abs(cam.FocusDist);

				Vector2 targetOff = (-(curPos - lastPos) / this.GetScaleAtZ(refZ));
				Vector2 targetVel = targetOff / unscaledTimeMult;
				MathF.TransformCoord(ref targetVel.X, ref targetVel.Y, camObj.Transform.Angle);
				this.camVel.Z *= MathF.Pow(0.9f, unscaledTimeMult);
				this.camVel += (new Vector3(targetVel, this.camVel.Z) - this.camVel) * unscaledTimeMult;
				this.camTransformChanged = true;
			}
			else if (this.camAction == CameraAction.Move)
			{
				Vector3 moveVec = new Vector3(
					cursorPos.X - this.camActionBeginLoc.X,
					cursorPos.Y - this.camActionBeginLoc.Y,
					this.camVel.Z);

				const float BaseSpeedCursorLen = 25.0f;
				const float BaseSpeed = 3.0f;
				moveVec.X = BaseSpeed * MathF.Sign(moveVec.X) * MathF.Pow(MathF.Abs(moveVec.X) / BaseSpeedCursorLen, 1.5f);
				moveVec.Y = BaseSpeed * MathF.Sign(moveVec.Y) * MathF.Pow(MathF.Abs(moveVec.Y) / BaseSpeedCursorLen, 1.5f);

				MathF.TransformCoord(ref moveVec.X, ref moveVec.Y, camObj.Transform.Angle);

				if (this.camBeginDragScene)
				{
					float refZ = (this.HasCameraFocusPosition && camObj.Transform.Pos.Z < this.CameraFocusPosition.Z - cam.NearZ) ? this.CameraFocusPosition.Z : 0.0f;
					if (camObj.Transform.Pos.Z >= refZ - cam.NearZ)
						refZ = camObj.Transform.Pos.Z + MathF.Abs(cam.FocusDist);
					moveVec = new Vector3(moveVec.Xy * 0.5f / this.GetScaleAtZ(refZ), moveVec.Z);
				}

				this.camVel = moveVec;
				this.camTransformChanged = true;
			}
			else if (this.camVel.Length > 0.01f)
			{
				this.camVel *= MathF.Pow(0.9f, unscaledTimeMult);
				this.camTransformChanged = true;
			}
			else
			{
				this.camTransformChanged = this.camTransformChanged || (this.camVel != Vector3.Zero);
				this.camVel = Vector3.Zero;
			}
			if (this.camTransformChanged)
			{
				camObj.Transform.MoveBy(this.camVel * unscaledTimeMult);
				//this.View.OnCamTransformChanged();
				this.Invalidate();
			}

			// If we're currently executing the game, invalidate every frame
			//if (Sandbox.State == SandboxState.Playing)
			//	this.Invalidate();

			// If we previously skipped a repaint event because we already rendered
			// a frame with that number, perform another repaint once we've entered
			// the next frame. This will make sure we won't forget about previous
			// one-shot invalidate calls just because we were already done rendering that
			// frame.
			if (this.renderFrameScheduled && this.renderFrameLast != Time.FrameCount)
				this.Invalidate();
		}

		protected virtual void OnSceneChanged()
		{
			this.Invalidate();
		}

		protected virtual void OnGotFocus() {}
		protected virtual void OnLostFocus() {}
		protected virtual void OnResize() {}

		protected virtual void OnDragEnter(DragEventArgs e) {}
		protected virtual void OnDragOver(DragEventArgs e) {}
		protected virtual void OnDragDrop(DragEventArgs e) {}
		protected virtual void OnDragLeave(EventArgs e) {}

		protected virtual void OnKeyDown(KeyEventArgs e) {}
		protected virtual void OnKeyUp(KeyEventArgs e) {}
		protected virtual void OnMouseDown(MouseEventArgs e) {}
		protected virtual void OnMouseUp(MouseEventArgs e) {}
		protected virtual void OnMouseMove(MouseEventArgs e) {}
		protected virtual void OnMouseWheel(MouseEventArgs e) {}
		protected virtual void OnMouseLeave(EventArgs e) {}
		protected virtual void OnCamActionRequiresCursorChanged(EventArgs e) {}

		protected void OnMouseMove()
		{
			Point mousePos = this.PointToClient(Cursor.Position);
			this.OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, mousePos.X, mousePos.Y, 0));
		}

		protected void StopCameraMovement()
		{
			this.camVel = Vector3.Zero;
		}

		protected void SetDefaultActiveLayers(params Type[] activeLayers)
		{
			this.lastActiveLayers = activeLayers.ToList();
		}
		protected void SaveActiveLayers()
		{
			this.lastActiveLayers = this.View.ActiveLayers.Select(l => l.GetType()).ToList();
		}
		protected void RestoreActiveLayers()
		{
			this.View.SetActiveLayers(this.lastActiveLayers);
		}
		protected void SetDefaultObjectVisibility(params Type[] visibleObjectTypes)
		{
			this.lastObjVisibility.Clear();
			foreach (Type type in visibleObjectTypes)
			{
				this.lastObjVisibility.Add(type.GetTypeId());
			}
		}

		protected void CollectLayerDrawcalls(Canvas canvas)
		{
			var layers = this.View.ActiveLayers.ToArray();
			layers.StableSort((a, b) => a.Priority - b.Priority);
			foreach (var layer in layers)
			{
				canvas.PushState();
				layer.OnCollectDrawcalls(canvas);
				canvas.PopState();
			}
		}
		protected void CollectLayerWorldOverlayDrawcalls(Canvas canvas)
		{
			var layers = this.View.ActiveLayers.ToArray();
			layers.StableSort((a, b) => a.Priority - b.Priority);
			foreach (var layer in layers)
			{
				canvas.PushState();
				layer.OnCollectWorldOverlayDrawcalls(canvas);
				canvas.PopState();
			}
		}
		protected void CollectLayerOverlayDrawcalls(Canvas canvas)
		{
			var layers = this.View.ActiveLayers.ToArray();
			layers.StableSort((a, b) => a.Priority - b.Priority);
			foreach (var layer in layers)
			{
				canvas.PushState();
				layer.OnCollectOverlayDrawcalls(canvas);
				canvas.PopState();
			}
		}
		protected void CollectLayerBackgroundDrawcalls(Canvas canvas)
		{
			var layers = this.View.ActiveLayers.ToArray();
			layers.StableSort((a, b) => a.Priority - b.Priority);
			foreach (var layer in layers)
			{
				canvas.PushState();
				layer.OnCollectBackgroundDrawcalls(canvas);
				canvas.PopState();
			}
		}

		private void ForceDragDropRenderUpdate()
		{
			// Force immediate buffer swap and continuous repaint, because there is no event loop while dragging.
			this.renderFrameLast = 0;
			App.PerformBufferSwap();
		}

		private bool RendererFilter(ICmpRenderer r)
		{
            //GameObject obj = (r as Component).GameObj;
            //
			//if (!this.View.ObjectVisibility.Matches(obj))
			//	return false;
            //
			//DesignTimeObjectData data = DesignTimeObjectData.Get(obj);
			//return !data.IsHidden;

            return true;
		}

		private void RenderableControl_Paint(object sender, PaintEventArgs e)
		{
			if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) return;
			if (DualityApp.GraphicsBackend == null) return;

			// Only allow one rendered frame per simulated frame to avoid spamming repaints
			// based on user input like OnMouseMove or similar. Remember that all buffer swaps
			// and various core updates are only performed when the WinForms app reports to
			// be idle. This certainly doesn't happen if the event queue fills up with repaint
			// events faster than can be processed.
			if (this.renderFrameLast == Time.FrameCount)
			{
				// If we skipped this repaint, schedule one once we're ready for the next
				// per-frame rendering. Otherwise we will lose one-off repaint events if
				// they happen to fall in the same frame slot.
				this.renderFrameScheduled = true;
				return;
			}
			this.renderFrameScheduled = false;
			this.renderFrameLast = Time.FrameCount;

			// Retrieve OpenGL context
 			try { this.RenderableSite.MakeCurrent(); } catch (Exception) { return; }

			// Perform rendering
			try
			{
				this.OnRenderState();
			}
			catch (Exception exception)
			{
				Console.WriteLine("An error occurred during CamView {1} rendering. The current DrawDevice state may be compromised. Exception: {0}", /*LogFormat.Exception(*/exception/*)*/, this.CameraComponent.ToString());
			}

			// Make sure the rendered result ends up on screen
			this.RenderableSite.SwapBuffers();
		}
		private void RenderableControl_MouseMove(object sender, MouseEventArgs e)
		{
			this.mouseover = true;
			if (!this.camBeginDragScene) this.OnMouseMove(e);
		}
		private void RenderableControl_MouseUp(object sender, MouseEventArgs e)
		{
			this.drawCamGizmoState = CameraAction.None;

			if (this.camBeginDragScene)
			{
				this.camAction = CameraAction.None;
				//this.Cursor = CursorHelper.HandGrab;
			}
			else
			{
				if (this.camAction == CameraAction.Move && e.Button == MouseButtons.Middle)
					this.camAction = CameraAction.None;

				this.OnMouseUp(e);
			}

			this.Invalidate();
		}
		private void RenderableControl_MouseDown(object sender, MouseEventArgs e)
		{
			bool alt = (Control.ModifierKeys & Keys.Alt) != Keys.None;

			this.drawCamGizmoState = CameraAction.None;

			if (this.camBeginDragScene)
			{
				this.camActionBeginLoc = e.Location;
				if (e.Button == MouseButtons.Left)
				{
					this.camAction = CameraAction.DragScene;
					this.camActionBeginLocSpace = this.CameraObj.Transform.RelativePos;
					//this.Cursor = CursorHelper.HandGrabbing;
				}
				else if (e.Button == MouseButtons.Middle)
				{
					this.camAction = CameraAction.Move;
					this.camActionBeginLocSpace = this.CameraObj.Transform.RelativePos;
				}
			}
			else
			{
				if (this.camActionAllowed && this.camAction == CameraAction.None)
				{
					this.camActionBeginLoc = e.Location;
					if (e.Button == MouseButtons.Middle)
					{
						this.camAction = CameraAction.Move;
						this.camActionBeginLocSpace = this.CameraObj.Transform.RelativePos;
					}
				}

				this.OnMouseDown(e);
			}
		}
		private void RenderableControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (!this.mouseover) return;

			this.drawCamGizmoState = CameraAction.None;

			/*if (e.Delta != 0)
			{
				if (this.View.PerspectiveMode == PerspectiveMode.Parallax)
				{
					GameObject camObj = this.CameraObj;
					float curVel = this.camVel.Length * MathF.Sign(this.camVel.Z);
					Vector2 curTemp = new Vector2(
						(e.X * 2.0f / this.ClientSize.Width) - 1.0f,
						(e.Y * 2.0f / this.ClientSize.Height) - 1.0f);
					MathF.TransformCoord(ref curTemp.X, ref curTemp.Y, camObj.Transform.RelativeAngle);

					if (MathF.Sign(e.Delta) != MathF.Sign(curVel))
						curVel = 0.0f;
					else
						curVel *= 1.5f;
					curVel += 0.015f * e.Delta;
					curVel = MathF.Sign(curVel) * MathF.Min(MathF.Abs(curVel), 500.0f);

					Vector3 movVec = new Vector3(
						MathF.Sign(e.Delta) * MathF.Sign(curTemp.X) * MathF.Pow(curTemp.X, 2.0f), 
						MathF.Sign(e.Delta) * MathF.Sign(curTemp.Y) * MathF.Pow(curTemp.Y, 2.0f), 
						1.0f);
					movVec.Normalize();
					this.camVel = movVec * curVel;
				}
				else
				{
					this.View.FocusDist = this.View.FocusDist + this.View.FocusDistIncrement * e.Delta / 40;
				}
			}*/

			this.OnMouseWheel(e);
		}
		private void RenderableControl_MouseLeave(object sender, EventArgs e)
		{
			if (!this.camBeginDragScene) this.OnMouseMove();
			this.OnMouseLeave(e);
			this.mouseover = false;

			this.Invalidate();
		}
		private void RenderableControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (this.camActionAllowed)
			{
				if (e.KeyCode == Keys.Space && !this.IsActionInProgress && !this.camBeginDragScene)
				{
					this.camBeginDragScene = true;
					this.OnCamActionRequiresCursorChanged(EventArgs.Empty);
					//this.Cursor = CursorHelper.HandGrab;
				}
				else if (e.KeyCode == Keys.F)
				{
					//if (DualityEditorApp.Selection.MainGameObject != null)
					//	this.View.FocusOnObject(DualityEditorApp.Selection.MainGameObject);
					//else
						this.View.ResetCamera();
				}
				else if (e.Control && e.KeyCode == Keys.Left)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.X = MathF.Round(pos.X - 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}
				else if (e.Control && e.KeyCode == Keys.Right)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.X = MathF.Round(pos.X + 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}
				else if (e.Control && e.KeyCode == Keys.Up)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.Y = MathF.Round(pos.Y - 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}
				else if (e.Control && e.KeyCode == Keys.Down)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.Y = MathF.Round(pos.Y + 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}
				/*else if (e.Control && e.KeyCode == Keys.Add)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.Z = MathF.Round(pos.Z + 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}
				else if (e.Control && e.KeyCode == Keys.Subtract)
				{
					this.drawCamGizmoState = CameraAction.Move;
					Vector3 pos = this.CameraObj.Transform.Pos;
					pos.Z = MathF.Round(pos.Z - 1.0f);
					this.CameraObj.Transform.Pos = pos;
					this.Invalidate();
				}*/
			}

			this.OnKeyDown(e);
		}
		private void RenderableControl_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space && this.camBeginDragScene)
			{
				this.camBeginDragScene = false;
				this.camAction = CameraAction.None;
				//this.Cursor = CursorHelper.Arrow;
				this.OnCamActionRequiresCursorChanged(EventArgs.Empty);
			}

			this.OnKeyUp(e);
		}
		private void RenderableControl_GotFocus(object sender, EventArgs e)
		{
			this.MakeDualityTarget();
			this.OnGotFocus();
		}
		private void RenderableControl_LostFocus(object sender, EventArgs e)
		{
			if (App.MainWindow == null) return;

			this.camAction = CameraAction.None;
			this.OnLostFocus();
			this.Invalidate();
		}
		private void RenderableControl_DragOver(object sender, DragEventArgs e)
		{
			this.OnDragOver(e);
			this.ForceDragDropRenderUpdate();
		}
		private void RenderableControl_DragLeave(object sender, EventArgs e)
		{
			this.OnDragLeave(e);
			this.ForceDragDropRenderUpdate();
		}
		private void RenderableControl_DragEnter(object sender, DragEventArgs e)
		{
			this.OnDragEnter(e);
			this.ForceDragDropRenderUpdate();
		}
		private void RenderableControl_DragDrop(object sender, DragEventArgs e)
		{
			this.OnDragDrop(e);
			this.ForceDragDropRenderUpdate();
		}
		private void RenderableControl_Resize(object sender, EventArgs e)
		{
			if (this.ClientSize == Size.Empty) return;

			this.OnResize();
		}
		private void DualityEditorApp_UpdatingEngine(object sender, EventArgs e)
		{
			this.OnUpdateState();
		}
		private void Scene_Changed(object sender, EventArgs e)
		{
			this.OnSceneChanged();
		}
		private void CameraComponent_EventCollectDrawcalls(object sender, CollectDrawcallEventArgs e)
		{
			if (e.RenderStepId == this.camPassBg.Id)
			{
				this.overlayCanvas.Begin(e.Device);
				this.overlayCanvas.State.ColorTint = this.FgColor;

				this.OnCollectStateBackgroundDrawcalls(this.overlayCanvas);

				this.overlayCanvas.End();
			}
			else if (e.RenderStepId == this.camPassEdWorld.Id)
			{
				this.overlayCanvas.Begin(e.Device);
				this.overlayCanvas.State.ColorTint = this.FgColor;

				this.OnCollectStateDrawcalls(this.overlayCanvas);

				this.overlayCanvas.End();
			}
			else if (e.RenderStepId == this.camPassEdWorldNoDepth.Id)
			{
				this.overlayCanvas.Begin(e.Device);
				this.overlayCanvas.State.ColorTint = this.FgColor;

				this.OnCollectStateWorldOverlayDrawcalls(this.overlayCanvas);

				this.overlayCanvas.End();
			}
			else if (e.RenderStepId == this.camPassEdScreen.Id)
			{
				this.overlayCanvas.Begin(e.Device);
				this.overlayCanvas.State.ColorTint = this.FgColor;

				this.OnCollectStateOverlayDrawcalls(this.overlayCanvas);

				this.overlayCanvas.End();
			}
		}
	}
}