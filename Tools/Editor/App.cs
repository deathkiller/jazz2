using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Duality;
using Duality.Backend;
using Duality.Editor.Backend;
using Duality.Resources;

namespace Editor
{
    internal static class App
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Override working directory
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Init();
        }


        private static EditorPluginManager pluginManager = new EditorPluginManager();
        private static MainWindow mainWindow;
        private static IEditorGraphicsBackend graphicsBack;
        private static INativeEditorGraphicsContext mainGraphicsContext;
        private static GameObjectManager editorObjects = new GameObjectManager();
	    private static HashSet<GameObject> updateObjects = new HashSet<GameObject>();
		private static bool isSuspended;

        public static event EventHandler Terminating;
        public static event EventHandler EventLoopIdling;
        public static event EventHandler EditorIdling;
        public static event EventHandler UpdatingEngine;

        private static bool AppStillIdle
        {
            get
            {
                NativeMethods.Message msg;
                return !NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        public static MainWindow MainWindow => mainWindow;
        public static GameObjectManager EditorObjects => editorObjects;

        private static void Init()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //DualityApp.PluginManager.PluginsReady += DualityApp_PluginsReady;

            DualityApp.Init(
                DualityApp.ExecutionContext.Game,
                new DefaultAssemblyLoader(),
                null);

            // Initialize the plugin manager for the editor. We'll use the same loader as the core.
            pluginManager.Init(DualityApp.PluginManager.AssemblyLoader);

            // Need to load editor plugins before initializing the graphics context, so the backend is available
            pluginManager.LoadPlugins();

            // Need to initialize graphics context and default content before instantiating anything that could require any of them
            InitMainGraphicsContext();
            DualityApp.InitPostWindow();

            //LoadUserData();
            pluginManager.InitPlugins();

            // Set up core plugin reloader
            //corePluginReloader = new ReloadCorePluginDialog(mainForm);

	        mainWindow = new MainWindow();

            // Register events
            mainWindow.Activated += mainWindow_Activated;
            mainWindow.Deactivate += mainWindow_Deactivate;
            Scene.Leaving += Scene_Leaving;
            Scene.Entered += Scene_Entered;
            Application.Idle += Application_Idle;

            // Enter a new, empty Scene, which will trigger the usual updates
            //Scene.SwitchTo(null, true);
            Scene.SwitchTo(new EditorLevelHandler("prince", "04_carrot1n"), true);

            mainWindow.Show();

            Application.Run();
        }

        public static bool Terminate(bool byUser)
        {
            bool cancel = false;

            // ...

            // Did we cancel it? Return false.
            if (cancel)
                return false;

            // Otherwise, actually start terminating.
            // From this point on, there's no return - need to re-init the editor afterwards.
            if (Terminating != null)
                Terminating(null, EventArgs.Empty);

            // Unregister events
            mainWindow.Activated -= mainWindow_Activated;
            mainWindow.Deactivate -= mainWindow_Deactivate;
            Scene.Leaving -= Scene_Leaving;
            Scene.Entered -= Scene_Entered;
            Application.Idle -= Application_Idle;

            // Shut down the editor backend
            DualityApp.ShutdownBackend(ref graphicsBack);

            // Shut down the plugin manager
            pluginManager.Terminate();

            // Terminate Duality
            DualityApp.Terminate();

            return true;
        }

        private static void InitMainGraphicsContext()
        {
            if (mainGraphicsContext != null) return;

            if (graphicsBack == null)
                DualityApp.InitBackend(out graphicsBack, GetAvailDualityEditorTypes);

            try {
                // Currently bound to game-specific settings. Should be decoupled
                // from them at some point, so the editor can use independent settings.
                mainGraphicsContext = graphicsBack.CreateContext(AAQuality.Off);
            } catch (Exception e) {
                mainGraphicsContext = null;
                Console.WriteLine("Can't create editor graphics context, because an error occurred: {0}", /*LogFormat.Exception(*/e/*)*/);
            }
        }
        public static void PerformBufferSwap()
        {
            if (mainGraphicsContext == null) return;
            mainGraphicsContext.PerformBufferSwap();
        }
        public static INativeRenderableSite CreateRenderableSite()
        {
            if (mainGraphicsContext == null) return null;
            return mainGraphicsContext.CreateRenderableSite();
        }

        public static IEnumerable<Assembly> GetDualityEditorAssemblies()
        {
            return pluginManager.GetAssemblies();
        }
        public static IEnumerable<TypeInfo> GetAvailDualityEditorTypes(Type baseType)
        {
            return pluginManager.GetTypes(baseType);
        }

        private static void OnEventLoopIdling()
        {
            if (EventLoopIdling != null)
                EventLoopIdling(null, EventArgs.Empty);
        }
        private static void OnEditorIdling()
        {
            if (EditorIdling != null)
                EditorIdling(null, EventArgs.Empty);
        }
        private static void OnUpdatingEngine()
        {
            if (UpdatingEngine != null)
                UpdatingEngine(null, EventArgs.Empty);
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;

            // Trigger global event loop idle event.
            OnEventLoopIdling();

            // Perform some global operations, if no modal dialog is open
            if (mainWindow.Visible && mainWindow.CanFocus) {
                // Trigger global editor idle event.
                OnEditorIdling();

                // Trigger autosave after a while
                /*if (autosaveFrequency != AutosaveFrequency.Disabled) {
                    TimeSpan timeSinceLastAutosave = DateTime.Now - autosaveLast;
                    if ((autosaveFrequency == AutosaveFrequency.OneHour && timeSinceLastAutosave.TotalMinutes > 60) ||
                        (autosaveFrequency == AutosaveFrequency.ThirtyMinutes && timeSinceLastAutosave.TotalMinutes > 30) ||
                        (autosaveFrequency == AutosaveFrequency.TenMinutes && timeSinceLastAutosave.TotalMinutes > 10)) {
                        SaveAllProjectData();
                        autosaveLast = DateTime.Now;
                    }
                }*/
            }

            // Update Duality engine
            var watch = new System.Diagnostics.Stopwatch();
            while (AppStillIdle) {
                watch.Restart();
                if (!isSuspended) {
                    bool fixedSingleStep = /*Sandbox.TakeSingleStep()*/true;
                    try {
                        DualityApp.EditorUpdate(
                            editorObjects.ActiveObjects.Concat(updateObjects),
                            fixedSingleStep /*|| (Sandbox.State == SandboxState.Playing && !Sandbox.IsFreezed)*/,
                            fixedSingleStep);
                        updateObjects.Clear();
                    } catch (Exception exception) {
                        Console.WriteLine("An error occurred during a core update: {0}", /*LogFormat.Exception(*/exception/*)*/);
                    }
                    OnUpdatingEngine();
                }

                // Perform a buffer swap
                PerformBufferSwap();

                // Give the processor a rest if we have the time, don't use 100% CPU
                while (watch.Elapsed.TotalSeconds < 0.01d) {
                    // Sleep a little
                    System.Threading.Thread.Sleep(1);
                    // App wants to do something? Stop waiting.
                    if (!AppStillIdle) break;
                }
            }

            Application.Idle += Application_Idle;
        }

        private static void Scene_Leaving(object sender, EventArgs e)
        {

        }

        private static void Scene_Entered(object sender, EventArgs e)
        {

        }

        private static void mainWindow_Activated(object sender, EventArgs e)
        {
            // Core plugin reload
            /*if (needsRecovery) {
                needsRecovery = false;
                Logs.Editor.Write("Recovering from full plugin reload restart...");
                Logs.Editor.PushIndent();
                corePluginReloader.State = ReloadCorePluginDialog.ReloaderState.RecoverFromRestart;
            } else if (corePluginReloader.ReloadSchedule.Count > 0) {
                corePluginReloader.State = ReloadCorePluginDialog.ReloaderState.ReloadPlugins;
            }*/
        }

        private static void mainWindow_Deactivate(object sender, EventArgs e)
        {

        }
    }
}