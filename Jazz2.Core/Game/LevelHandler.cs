using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Duality;
using Duality.Audio;
using Duality.Components;
using Duality.Drawing;
using Duality.Input;
using Duality.IO;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Actors.Bosses;
using Jazz2.Actors.Solid;
using Jazz2.Game.Events;
using Jazz2.Game.Menu;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game
{
    public class LevelHandler : Scene
    {
        public const float NearZ = 0f;
        public const float FarZ = 1000f;
        public const float MainPlaneZ = (NearZ + FarZ) * 0.5f;
        public const float PlayerZ = MainPlaneZ - 10f;

        private const int LayerFormatVersion = 1;
        private const int EventSetVersion = 2;

        private const float DefaultGravity = 0.3f;

        private readonly Controller root;
        private readonly ActorApi api;

        private readonly GameObject rootObject;
        private readonly GameObject camera;

        private TileMap tileMap;
        private ColorRgba[] tileMapPalette;

        private List<Player> players = new List<Player>();
        private List<ActorBase> actors = new List<ActorBase>();

        private EventMap eventMap;
        private EventSpawner eventSpawner;
        //private string levelName;
        private string levelFileName;
        private string episodeName;
        private string defaultNextLevel;
        private string defaultSecretLevel;
        private GameDifficulty difficulty;

        private bool exiting;
        private float ambientLightCurrent;
        private float ambientLightTarget;
        private int ambientLightDefault;
        private float gravity;

        private BossBase activeBoss;

        private IDictionary<int, string> levelTexts;
        private Metadata commonResources;

        private OpenMptStream music;

        private InitLevelData? currentCarryOver;
        private float levelChangeTimer;

        private WeatherType weatherType;
        private int weatherIntensity;
        private bool weatherOutdoors;

        private int waterLevel = int.MaxValue;

        public Controller Root => root;
        public ActorApi Api => api;

        public TileMap TileMap => tileMap;
        public EventMap EventMap => eventMap;
        public EventSpawner EventSpawner => eventSpawner;

        public GameDifficulty Difficulty => difficulty;

        public float Gravity => gravity;

        public float AmbientLightCurrent
        {
            get { return ambientLightCurrent; }
            set { ambientLightTarget = value; }
        }

        public int AmbientLightDefault
        {
            get { return ambientLightDefault; }
        }

        public int WaterLevel
        {
            get { return waterLevel; }
            set { waterLevel = value; }
        }

        public List<Player> Players
        {
            get { return players; }
        }

        public LevelHandler(Controller root, InitLevelData data)
        {
            this.root = root;

            //levelName = data.LevelName;
            levelFileName = data.LevelName;
            episodeName = data.EpisodeName;
            difficulty = data.Difficulty;

            gravity = DefaultGravity;

            api = new ActorApi(this);
            eventSpawner = new EventSpawner(api);

            rootObject = new GameObject("LevelManager");
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Setup camera
            camera = new GameObject("MainCamera");
            Transform cameraTransform = camera.AddComponent<Transform>();

            Camera cameraInner = camera.AddComponent<Camera>();
            cameraInner.NearZ = NearZ;
            cameraInner.FarZ = FarZ;
            cameraInner.Perspective = PerspectiveMode.Flat;

            CameraController cameraController = camera.AddComponent<CameraController>();

            // Load level
            LoadLevel(levelFileName, episodeName);

            // Process carry overs
            if (data.PlayerCarryOvers != null) {
                for (int i = 0; i < data.PlayerCarryOvers.Length; i++) {
                    Vector2 spawnPosition = eventMap.GetSpawnPosition(data.PlayerCarryOvers[i].Type);
                    if (spawnPosition == new Vector2(-1, -1)) {
                        spawnPosition = eventMap.GetSpawnPosition(PlayerType.Jazz);
                        if (spawnPosition == new Vector2(-1, -1)) {
                            continue;
                        }
                    }

                    Player player = new Player();
                    player.OnAttach(new ActorInstantiationDetails {
                        Api = api,
                        Pos = new Vector3(spawnPosition, PlayerZ),
                        Params = new[] { (ushort)data.PlayerCarryOvers[i].Type }
                    });
                    AddPlayer(player);

                    player.ReceiveLevelCarryOver(data.ExitType, ref data.PlayerCarryOvers[i]);
                }
            }

            Player targetPlayer;
            Vector3 targetPlayerPosition;
            if (players.Count > 0) {
                targetPlayer = players[0];
                targetPlayerPosition = targetPlayer.Transform.Pos;
            } else {
                Debug.WriteLine("No spawn point found, used default location instead!");

                targetPlayerPosition = new Vector3(120, 160, PlayerZ);

                targetPlayer = new Player();
                targetPlayer.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = targetPlayerPosition,
                    Params = new[] { (ushort)PlayerType.Jazz }
                });
                AddPlayer(targetPlayer);
            }

            // Bind camera to player
            cameraInner.RenderingSetup = new LevelRenderSetup(this);

            cameraTransform.Pos = new Vector3(targetPlayerPosition.X, targetPlayerPosition.Y, 0);

            cameraController.TargetObject = targetPlayer;
            ((ICmpUpdatable)cameraController).OnUpdate();
            camera.Parent = rootObject;

            DualityApp.Sound.Listener = camera;

            // Attach player to UI
            targetPlayer.AttachToHud(rootObject.AddComponent<Hud>());

            // Common sounds
            commonResources = ContentResolver.Current.RequestMetadata("Common/Scenery", tileMapPalette);
        }

        protected override void OnDisposing(bool manually)
        {
            if (music != null) {
                music.FadeOut(1f);
                music = null;
            }

            foreach (Player player in players) {
                player.AttachToHud(null);
            }

            base.OnDisposing(manually);
        }

        // ToDo: Move this somewhere
        [Flags]
        public enum LevelFlags
        {
            FastCamera = 1 << 0,
            HasPit = 1 << 1,

            Multiplayer = 1 << 10,
            MultiplayerRace = 1 << 11,
            MultiplayerFlags = 1 << 12,

        }

        public class LevelConfigJson
        {
            public class VersionSection
            {
                //public string Target { get; set; }
                public uint LayerFormat { get; set; }
                public uint EventSet { get; set; }
            }

            public class DescriptionSection
            {
                //public string LevelToken { get; set; }
                public string Name { get; set; }
                public string NextLevel { get; set; }
                public string SecretLevel { get; set; }
                public string DefaultTileset { get; set; }
                public string DefaultMusic { get; set; }
                public int DefaultLight { get; set; }

                public LevelFlags Flags { get; set; }
            }

            public class LayerSection
            {
                public float XSpeed { get; set; }
                public float YSpeed { get; set; }
                public float XAutoSpeed { get; set; }
                public float YAutoSpeed { get; set; }
                public bool XRepeat { get; set; }
                public bool YRepeat { get; set; }

                public int Depth { get; set; }
                public bool InherentOffset { get; set; }
                public int BackgroundStyle { get; set; }
                public IList<int> BackgroundColor { get; set; }
                public bool ParallaxStarsEnabled { get; set; }
            }

            public VersionSection Version { get; set; }
            public DescriptionSection Description { get; set; }
            public IDictionary<int, string> TextEvents { get; set; }
            public IDictionary<string, LayerSection> Layers { get; set; }

        }

        private void LoadLevel(string level, string episode)
        {
            string levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", episode, level);
            using (Stream s = DualityApp.SystemBackend.FileSystem.OpenFile(PathOp.Combine(levelPath, ".res"), FileAccessMode.Read)) {
                // ToDo: Cache parser, move JSON parsing to ContentResolver
                JsonParser json = new JsonParser();
                LevelConfigJson config = json.Parse<LevelConfigJson>(s);

                if (config.Version.LayerFormat > LayerFormatVersion || config.Version.EventSet > EventSetVersion) {
                    throw new NotSupportedException("Version not supported");
                }

                Console.WriteLine("Loading level \"" + config.Description.Name + "\"...");

                root.Title = BitmapFont.StripFormatting(config.Description.Name);
                root.Immersive = false;

                defaultNextLevel = config.Description.NextLevel;
                defaultSecretLevel = config.Description.SecretLevel;
                ambientLightDefault = config.Description.DefaultLight;
                ambientLightCurrent = ambientLightTarget = ambientLightDefault * 0.01f;

                string tilesetPath = PathOp.Combine(DualityApp.DataDirectory, "Tilesets", config.Description.DefaultTileset);

                tileMap = new TileMap(this,
                    PathOp.Combine(tilesetPath, "tiles.png"),
                    PathOp.Combine(tilesetPath, "mask.png"),
                    PathOp.Combine(tilesetPath, "normal.png"),
                    (config.Description.Flags & LevelFlags.HasPit) != 0);

                tileMapPalette = TileSet.LoadPalette(PathOp.Combine(tilesetPath, ".palette"));

                // Read all layers
                config.Layers.Add("Sprite", new LevelConfigJson.LayerSection {
                    XSpeed = 1,
                    YSpeed = 1
                });

                int i = 0;
                foreach (var layer in config.Layers.OrderBy(layer => layer.Value.Depth)) {
                    LayerType type;
                    if (layer.Key == "Sprite") {
                        type = LayerType.Sprite;
                    } else if (layer.Key == "Sky") {
                        type = LayerType.Sky;

                        if (layer.Value.BackgroundStyle != 0 /*Plain*/ && layer.Value.BackgroundColor != null && layer.Value.BackgroundColor.Count >= 3) {
                            camera.GetComponent<Camera>().ClearColor = new ColorRgba((byte)layer.Value.BackgroundColor[0], (byte)layer.Value.BackgroundColor[1], (byte)layer.Value.BackgroundColor[2]);
                        }
                    } else {
                        type = LayerType.Other;
                    }

                    tileMap.ReadLayerConfiguration(type, i, levelPath, layer.Key, layer.Value);
                    i++;
                }

                // Read animated tiles
                string animTilesPath = PathOp.Combine(levelPath, "Animated.tiles");
                if (FileOp.Exists(animTilesPath)) {
                    tileMap.ReadAnimatedTiles(animTilesPath);
                }

                CameraController controller = camera.GetComponent<CameraController>();
                controller.ViewRect = new Rect(tileMap.Size * tileMap.Tileset.TileSize);
                //controller.Smoothness = ((config.Description.Flags & LevelFlags.FastCamera) != 0 ? -2.6f : 1f);

                // Read events
                eventMap = new EventMap(this, tileMap.Size);

                string eventsPath = PathOp.Combine(levelPath, "Events.layer");
                if (FileOp.Exists(animTilesPath)) {
                    eventMap.ReadEvents(eventsPath, config.Version.LayerFormat, difficulty);
                }

                levelTexts = config.TextEvents ?? new Dictionary<int, string>();

                GameObject tilemapHandler = new GameObject("TilemapHandler");
                tilemapHandler.Parent = rootObject;
                tilemapHandler.AddComponent(tileMap);

                // Load extensions
                // ToDo: Implement level extensions (.dll) for scripting
                /*if (config.Extensions != null) {
                    for (int j = 0; j < config.Extensions.Count; j++) {
                        string path = PathOp.Combine(levelPath, config.Extensions[j]);
                        System.Reflection.Assembly a = DualityApp.PluginLoader.LoadAssembly(path);
                    }
                }*/

                // Load default music
                string musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", config.Description.DefaultMusic);
                music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
                music.BeginFadeIn(0.5f);
            }
        }

        public void AddActor(ActorBase actor)
        {
            actors.Add(actor);

            //actor.Parent = rootObject;
            AddObject(actor);
        }

        public void RemoveActor(ActorBase actor)
        {
            actors.Remove(actor);

            RemoveObject(actor);
        }

        public void AddPlayer(Player actor)
        {
            players.Add(actor);
            actors.Add(actor);

            //actor.Parent = rootObject;
            AddObject(actor);
        }

        public List<ActorBase> FindCollisionActorsFast(ActorBase self, ref Hitbox hitbox)
        {
            List<ActorBase> res = new List<ActorBase>();
            for (int i = 0; i < actors.Count; ++i) {
                if (self == actors[i] || (actors[i].CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    continue;
                }
                if (actors[i].Hitbox.Overlaps(ref hitbox)) {
                    res.Add(actors[i]);
                }
            }
            return res;
        }

        public List<ActorBase> FindCollisionActors(ActorBase self)
        {
            List<ActorBase> res = new List<ActorBase>();
            for (int i = 0; i < actors.Count; ++i) {
                if (self == actors[i] || (actors[i].CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    continue;
                }
                if (actors[i].IsCollidingWith(self)) {
                    res.Add(actors[i]);
                }
            }
            return res;
        }

        public List<ActorBase> FindCollisionActorsRadius(float x, float y, float radius)
        {
            List<ActorBase> res = new List<ActorBase>();
            for (int i = 0; i < actors.Count; ++i) {
                if ((actors[i].CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    continue;
                }

                Hitbox hitbox = actors[i].Hitbox;

                // Find the closest point to the circle within the rectangle
                float closestX = MathF.Clamp(x, hitbox.Left, hitbox.Right);
                float closestY = MathF.Clamp(y, hitbox.Top, hitbox.Bottom);

                // Calculate the distance between the circle's center and this closest point
                float distanceX = x - closestX;
                float distanceY = y - closestY;

                // If the distance is less than the circle's radius, an intersection occurs
                float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                if (distanceSquared < (radius * radius)) {
                    res.Add(actors[i]);
                }
            }
            return res;
        }

        public bool IsPositionEmpty(ActorBase self, ref Hitbox hitbox, bool downwards, out ActorBase collider)
        {
            collider = null;

            if ((self.CollisionFlags & CollisionFlags.CollideWithTileset) != 0) {
                if (!tileMap.IsTileEmpty(ref hitbox, downwards)) {
                    return false;
                }
            }

            // Check for solid objects
            if ((self.CollisionFlags & CollisionFlags.CollideWithSolidObjects) != 0) {
                List<ActorBase> collision = FindCollisionActorsFast(self, ref hitbox);
                for (int i = 0; i < collision.Count; i++) {
                    if ((collision[i].CollisionFlags & CollisionFlags.IsSolidObject) == 0) {
                        continue;
                    }

                    SolidObjectBase solidObject = collision[i] as SolidObjectBase;
                    if (solidObject == null || (solidObject != null && (!solidObject.IsOneWay || downwards))) {
                        collider = collision[i];
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsPositionEmpty(ActorBase self, ref Hitbox hitbox, bool downwards)
        {
            ActorBase solidObject;
            return IsPositionEmpty(self, ref hitbox, downwards, out solidObject);
        }

        public List<Player> GetCollidingPlayers(ref Hitbox hitbox)
        {
            List<Player> result = new List<Player>();

            foreach (Player p in players) {
                if (p.Hitbox.Overlaps(ref hitbox)) {
                    result.Add(p);
                }
            }

            return result;
        }

        public void InitLevelChange(ExitType exitType, string nextLevel)
        {
            if (exiting) {
                return;
            }

            exiting = true;

            foreach (Player player in players) {
                player.OnLevelChanging(exitType);
            }

            if (nextLevel == null) {
                nextLevel = (exitType == ExitType.Bonus ? defaultSecretLevel : defaultNextLevel);
            }

            InitLevelData data = default(InitLevelData);

            if (nextLevel != null) {
                int i = nextLevel.IndexOf('/');
                if (i == -1) {
                    data.EpisodeName = episodeName;
                    data.LevelName = nextLevel;
                } else {
                    data.EpisodeName = nextLevel.Substring(0, i);
                    data.LevelName = nextLevel.Substring(i + 1);
                }
            }

            data.Difficulty = difficulty;
            data.ExitType = exitType;

            data.PlayerCarryOvers = new PlayerCarryOver[players.Count];
            for (int i = 0; i < players.Count; i++) {
                data.PlayerCarryOvers[i] = players[i].PrepareLevelCarryOver();
            }

            currentCarryOver = data;

            levelChangeTimer = (exitType == ExitType.Warp || exitType == ExitType.Bonus ? /*27f*/50f : /*98f*/130f);
        }

        public void HandleGameOver()
        {
            // ToDo: Implement Game Over screen
            root.ShowMainMenu();
        }

        public string GetLevelText(int textID)
        {
            string text;
            levelTexts.TryGetValue(textID, out text);
            return text;
        }

        public void WarpCameraToTarget(GameObject target)
        {
            if (camera.GetComponent<CameraController>().TargetObject == target) {
                Vector3 pos = target.Transform.Pos;
                camera.Transform.Pos = new Vector3(pos.X, pos.Y, 0);
            }
        }

        public Metadata RequestMetadata(string path)
        {
            return ContentResolver.Current.RequestMetadata(path, tileMapPalette);
        }

        public void PlayCommonSound(string name, ActorBase target, float gain = 1f)
        {
            SoundResource resource;
            if (commonResources.Sounds.TryGetValue(name, out resource)) {
                SoundInstance instance = DualityApp.Sound.PlaySound3D(resource.Sound, target);
                // ToDo: Hardcoded volume
                instance.Volume = gain * Settings.SfxVolume;

                if (target.Transform.Pos.Y >= api.WaterLevel) {
                    instance.Lowpass = 0.2f;
                    instance.Pitch = 0.7f;
                }
            }
        }

        public void PlayCommonSound(string name, Vector3 pos, float gain = 1f)
        {
            SoundResource resource;
            if (commonResources.Sounds.TryGetValue(name, out resource)) {
                SoundInstance instance = DualityApp.Sound.PlaySound3D(resource.Sound, pos);
                // ToDo: Hardcoded volume
                instance.Volume = gain * Settings.SfxVolume;

                if (pos.Y >= api.WaterLevel) {
                    instance.Lowpass = 0.2f;
                    instance.Pitch = 0.7f;
                }
            }
        }

        public bool ActivateBoss(ushort musicFile)
        {
            if (activeBoss != null) {
                return false;
            }

            foreach (GameObject obj in ActiveObjects) {
                activeBoss = obj as BossBase;
                if (activeBoss != null) {
                    break;
                }
            }

            if (activeBoss == null) {
                return false;
            }

            activeBoss.OnBossActivated();

            Hud hud = rootObject.GetComponent<Hud>();
            if (hud != null) {
                hud.ActiveBoss = activeBoss;
            }

            if (music != null) {
                music.FadeOut(3f);
            }

            // ToDo: Hardcoded music file
            string musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", "Boss" + (musicFile + 1).ToString(CultureInfo.InvariantCulture) + ".j2b");

            music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
            music.BeginFadeIn(1f);

            return true;
        }

        public void BroadcastLevelText(string text)
        {
            foreach (Player player in players) {
                player.ShowLevelText(text);
            }
        }

        // ToDo: Move this somewhere
        public enum WeatherType
        {
            None,
            Snow,
            Flowers,    // ToDo: Implement this
            Rain,
            Leaf        // ToDo: Implement this
        }

        public void ApplyWeather(WeatherType type, int intensity, bool outdoors)
        {
            weatherType = type;
            weatherIntensity = intensity;
            weatherOutdoors = outdoors;
        }

        private void OnUpdate()
        {
            if (currentCarryOver.HasValue) {
                if (levelChangeTimer > 0) {
                    levelChangeTimer -= Time.TimeMult;
                } else {
                    root.ChangeLevel(currentCarryOver.Value);
                    currentCarryOver = null;
                    return;
                }
            }

            Vector3 pos = players[0].Transform.Pos;
            //int tx = (int)(pos.X / 32);
            //int ty = (int)(pos.Y / 32);
            int tx = (int)pos.X >> 5;
            int ty = (int)pos.Y >> 5;

            // ToDo: Remove this branching
#if __ANDROID__
            const int ActivateTileRange = 17;
#else
            const int ActivateTileRange = 26;
#endif

            for (int i = 0; i < actors.Count; i++) {
                if (actors[i].Deactivate(tx, ty, ActivateTileRange + 2)) {
                    i--;
                }
            }

            eventMap.ActivateEvents(tx, ty, ActivateTileRange);

            eventMap.ProcessGenerators();

            // Ambient Light Transition
            if (ambientLightCurrent != ambientLightTarget) {
                float step = Time.TimeMult * 0.012f;
                if (MathF.Abs(ambientLightCurrent - ambientLightTarget) < step) {
                    ambientLightCurrent = ambientLightTarget;
                } else {
                    ambientLightCurrent += step * ((ambientLightTarget < ambientLightCurrent) ? -1 : 1);
                }
            }

            // Weather
            if (weatherType != WeatherType.None) {
                Vector3 viewPos = camera.Transform.Pos;
                for (int i = 0; i < weatherIntensity; i++) {
                    TileMap.DebrisCollisionAction collisionAction;
                    if (weatherOutdoors) {
                        collisionAction = TileMap.DebrisCollisionAction.Disappear;
                    } else {
                        collisionAction = (MathF.Rnd.NextFloat() > 0.7f
                            ? TileMap.DebrisCollisionAction.None
                            : TileMap.DebrisCollisionAction.Disappear);
                    }

                    Vector3 debrisPos = viewPos + MathF.Rnd.NextVector3((LevelRenderSetup.TargetSize.X / -2) - 40,
                                      (LevelRenderSetup.TargetSize.Y * -2 / 3), MainPlaneZ,
                                      LevelRenderSetup.TargetSize.X + 120, LevelRenderSetup.TargetSize.Y, 0);

                    if (weatherType == WeatherType.Rain) {
                        GraphicResource res = commonResources.Graphics["Rain"];
                        Material material = res.Material.Res;
                        Texture texture = material.MainTexture.Res;

                        float scale = MathF.Rnd.NextFloat(0.4f, 1.1f);
                        float speedX = MathF.Rnd.NextFloat(2.2f, 2.7f) * scale;
                        float speedY = MathF.Rnd.NextFloat(7.6f, 8.6f) * scale;

                        debrisPos.Z = MainPlaneZ * scale;

                        tileMap.CreateDebris(new TileMap.DestructibleDebris {
                            Pos = debrisPos,
                            Size = res.FrameDimensions,
                            Speed = new Vector2(speedX, speedY),

                            Scale = scale,
                            Angle = MathF.Atan2(speedY, speedX),
                            Alpha = 1f,

                            Time = 180f,

                            Material = material,
                            MaterialOffset = texture.LookupAtlas(res.FrameOffset + MathF.Rnd.Next(res.FrameCount)),

                            CollisionAction = collisionAction
                        });
                    } else {
                        GraphicResource res = commonResources.Graphics["Snow"];
                        Material material = res.Material.Res;
                        Texture texture = material.MainTexture.Res;

                        float scale = MathF.Rnd.NextFloat(0.4f, 1.1f);
                        float speedX = MathF.Rnd.NextFloat(-1.6f, -1.2f) * scale;
                        float speedY = MathF.Rnd.NextFloat(3f, 4f) * scale;
                        float accel = MathF.Rnd.NextFloat(-0.008f, 0.008f) * scale;

                        debrisPos.Z = MainPlaneZ * scale;

                        tileMap.CreateDebris(new TileMap.DestructibleDebris {
                            Pos = debrisPos,
                            Size = res.FrameDimensions,
                            Speed = new Vector2(speedX, speedY),
                            Acceleration = new Vector2(accel, -MathF.Abs(accel)),

                            Scale = scale,
                            Angle = MathF.Rnd.NextFloat(MathF.TwoPi),
                            AngleSpeed = speedX * 0.02f,
                            Alpha = 1f,

                            Time = 180f,

                            Material = material,
                            MaterialOffset = texture.LookupAtlas(res.FrameOffset + MathF.Rnd.Next(res.FrameCount)),

                            CollisionAction = collisionAction
                        });
                    }
                }
            }

            // Active Boss
            if (activeBoss != null && activeBoss.ParentScene == null) {
                activeBoss = null;

                Hud hud = rootObject.GetComponent<Hud>();
                if (hud != null) {
                    hud.ActiveBoss = null;
                }

                InitLevelChange(ExitType.Normal, null);
                levelChangeTimer *= 2;
            }

            if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                Scene.SwitchTo(new InGameMenu(root, this));
            }

            Hud.ShowDebugText("- FPS: " + Time.Fps.ToString("N0") + "  (" + Math.Round(Time.UnscaledDeltaTime * 1000, 1).ToString("N1") + " ms)");
            Hud.ShowDebugText("  Diff.: " + difficulty + " | Actors: " + actors.Count.ToString("N0"));
            Hud.ShowDebugText("  Ambient Light: " + ambientLightCurrent.ToString("0.00") + " / " + ambientLightTarget.ToString("0.00"));
        }

        private class LocalController : Component, ICmpUpdatable
        {
            private readonly LevelHandler levelHandler;

            public LocalController(LevelHandler levelHandler)
            {
                this.levelHandler = levelHandler;
            }

            void ICmpUpdatable.OnUpdate()
            {
                levelHandler.OnUpdate();
            }
        }
    }
}