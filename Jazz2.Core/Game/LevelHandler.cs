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
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using Jazz2.Game.UI;
using Jazz2.Game.UI.Menu.InGame;
using MathF = Duality.MathF;

namespace Jazz2.Game
{
    public class LevelHandler : Scene, ILevelHandler
    {
        public const float NearZ = 10f;
        public const float FarZ = 1000f;
        public const float MainPlaneZ = (NearZ + FarZ) * 0.5f;
        public const float PlayerZ = MainPlaneZ - 10f;

        private const int LayerFormatVersion = 1;
        private const int EventSetVersion = 2;

        private const float DefaultGravity = 0.3f;

        private App root;
        private ActorApi api;

        protected readonly GameObject rootObject;
        private readonly GameObject camera;

        private TileMap tileMap;
        private EventMap eventMap;
        private EventSpawner eventSpawner;
        private DynamicTreeBroadPhase collisions;
        private int collisionsCountA, collisionsCountB, collisionsCountC;

        private List<Player> players = new List<Player>();
        private List<ActorBase> actors = new List<ActorBase>();

        private string levelFileName;
        private string episodeName;
        private string defaultNextLevel;
        private string defaultSecretLevel;
        private GameDifficulty difficulty;
        private string musicPath;

        private bool exiting;
        private float ambientLightCurrent;
        private float ambientLightTarget;
        private int ambientLightDefault;
        private Vector4 darknessColor;
        private float gravity;
        private Rect levelBounds;

        private BossBase activeBoss;

        private IDictionary<int, string> levelTexts;
        private Metadata commonResources;

        private OpenMptStream music;

        private LevelInitialization? currentCarryOver;
        private float levelChangeTimer;

        private WeatherType weatherType;
        private int weatherIntensity;
        private bool weatherOutdoors;

        private int waterLevel = int.MaxValue;

        public App Root => root;
        public ActorApi Api => api;

        public TileMap TileMap => tileMap;
        public EventMap EventMap => eventMap;
        public EventSpawner EventSpawner => eventSpawner;

        public GameDifficulty Difficulty => difficulty;

        public float Gravity => gravity;

        public Rect LevelBounds => levelBounds;

        public float AmbientLightCurrent
        {
            get { return ambientLightCurrent; }
            set { ambientLightTarget = value; }
        }

        public int AmbientLightDefault
        {
            get { return ambientLightDefault; }
        }

        public Vector4 DarknessColor
        {
            get { return darknessColor; }
            set { darknessColor = value; }
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

        public LevelHandler(App root, LevelInitialization data)
        {
            this.root = root;

            levelFileName = data.LevelName;
            episodeName = data.EpisodeName;
            difficulty = data.Difficulty;

            gravity = DefaultGravity;

            collisions = new DynamicTreeBroadPhase();

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
            cameraInner.Projection = ProjectionMode.Orthographic;

            CameraController cameraController = camera.AddComponent<CameraController>();

            // Load level
            LoadLevel(levelFileName, episodeName);

            // Create HUD
            Hud hud = rootObject.AddComponent<Hud>();

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
                        Params = new[] { (ushort)data.PlayerCarryOvers[i].Type, (ushort)i }
                    });
                    AddPlayer(player);

                    if (i == 0) {
                        player.AttachToHud(hud);
                    }

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
                    Params = new[] { (ushort)PlayerType.Jazz, (ushort)0 }
                });
                AddPlayer(targetPlayer);

                targetPlayer.AttachToHud(hud);
            }

            // Bind camera to player
            cameraInner.RenderingSetup = new LevelRenderSetup(this);

            cameraTransform.Pos = new Vector3(targetPlayerPosition.X, targetPlayerPosition.Y, 0);

            cameraController.TargetObject = targetPlayer;
            ((ICmpUpdatable)cameraController).OnUpdate();
            camera.Parent = rootObject;

            DualityApp.Sound.Listener = camera;

            // Common sounds
            commonResources = ContentResolver.Current.RequestMetadata("Common/Scenery");
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

            if (eventMap != null) {
                eventMap.Dispose();
                eventMap = null;
            }

            if (tileMap != null) {
                tileMap.ReleaseResources();
                tileMap = null;
            }

            api = null;

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
                public IList<int> DefaultDarkness { get; set; }

                public WeatherType DefaultWeather { get; set; }
                public int DefaultWeatherIntensity { get; set; }
                public bool DefaultWeatherOutdoors { get; set; }

                public LevelFlags Flags { get; set; }
            }

            public class TilesetSection
            {
                public string Name { get; set; }
                public int Offset { get; set; }
                public int Count { get; set; }
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
            public IList<TilesetSection> Tilesets { get; set; }
            public IDictionary<string, LayerSection> Layers { get; set; }

        }

        private void LoadLevel(string level, string episode)
        {
            string levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", episode, level);
            using (Stream s = FileOp.Open(PathOp.Combine(levelPath, ".res"), FileAccessMode.Read)) {
                // ToDo: Cache parser, move JSON parsing to ContentResolver
                JsonParser json = new JsonParser();
                LevelConfigJson config = json.Parse<LevelConfigJson>(s);

                if (config.Version.LayerFormat > LayerFormatVersion || config.Version.EventSet > EventSetVersion) {
                    throw new NotSupportedException("Version not supported");
                }

                App.Log("Loading level \"" + config.Description.Name + "\"...");

                root.Title = BitmapFont.StripFormatting(config.Description.Name);
                root.Immersive = false;

                defaultNextLevel = config.Description.NextLevel;
                defaultSecretLevel = config.Description.SecretLevel;
                ambientLightDefault = config.Description.DefaultLight;
                ambientLightCurrent = ambientLightTarget = ambientLightDefault * 0.01f;

                if (config.Description.DefaultDarkness != null && config.Description.DefaultDarkness.Count >= 4) {
                    darknessColor = new Vector4(config.Description.DefaultDarkness[0] / 255f, config.Description.DefaultDarkness[1] / 255f, config.Description.DefaultDarkness[2] / 255f, config.Description.DefaultDarkness[3] / 255f);
                } else {
                    darknessColor = new Vector4(0, 0, 0, 1);
                }

                // Palette
                {
                    ColorRgba[] tileMapPalette;

                    string levelPalette = PathOp.Combine(levelPath, ".palette");
                    if (FileOp.Exists(levelPalette)) {
                        tileMapPalette = TileSet.LoadPalette(levelPalette);
                    } else {
                        string tilesetPath = PathOp.Combine(DualityApp.DataDirectory, "Tilesets", config.Description.DefaultTileset);
                        tileMapPalette = TileSet.LoadPalette(PathOp.Combine(tilesetPath, ".palette"));
                    }

                    ContentResolver.Current.ApplyBasePalette(tileMapPalette);
                }

                // Tileset
                tileMap = new TileMap(this, config.Description.DefaultTileset, (config.Description.Flags & LevelFlags.HasPit) != 0);

                // Additional tilesets
                if (config.Tilesets != null) {
                    for (int i = 0; i < config.Tilesets.Count; i++) {
                        LevelConfigJson.TilesetSection part = config.Tilesets[i];
                        tileMap.ReadTilesetPart(part.Name, part.Offset, part.Count);
                    }
                }

                // Read all layers
                config.Layers.Add("Sprite", new LevelConfigJson.LayerSection {
                    XSpeed = 1,
                    YSpeed = 1
                });

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

                    tileMap.ReadLayerConfiguration(type, levelPath, layer.Key, layer.Value);
                }

                // Read animated tiles
                string animTilesPath = PathOp.Combine(levelPath, "Animated.tiles");
                if (FileOp.Exists(animTilesPath)) {
                    tileMap.ReadAnimatedTiles(animTilesPath);
                }

                levelBounds = new Rect(tileMap.Size * tileMap.Tileset.TileSize);

                CameraController controller = camera.GetComponent<CameraController>();
                controller.ViewBounds = levelBounds;

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

                // Load default music
                musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", config.Description.DefaultMusic);
                music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
                music.BeginFadeIn(0.5f);

                if (config.Description.DefaultWeather != WeatherType.None) {
                    ApplyWeather(
                        config.Description.DefaultWeather,
                        config.Description.DefaultWeatherIntensity,
                        config.Description.DefaultWeatherOutdoors);
                }
            }
        }

        public void AddActor(ActorBase actor)
        {
            actors.Add(actor);

            AddObject(actor);

            actor.UpdateAABB();

            collisions.AddProxy(actor);
            actor.Transform.EventTransformChanged += OnActorTransformChanged;
        }

        public void RemoveActor(ActorBase actor)
        {
            actor.Transform.EventTransformChanged -= OnActorTransformChanged;
            collisions.RemoveProxy(actor);

            actors.Remove(actor);

            RemoveObject(actor);
        }

        public void AddPlayer(Player actor)
        {
            players.Add(actor);

            AddActor(actor);
        }

        public void FindCollisionActorsFast(ActorBase self, Hitbox hitbox, Func<ActorBase, bool> callback)
        {
            AABB aabb = hitbox.ToAABB();

            collisions.Query((actor) => {
                if (self == actor || (actor.CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    return true;
                }
                if (actor.Hitbox.Intersects(ref hitbox)) {
                    return callback(actor);
                }
                return true;
            }, ref aabb);
        }

        public IEnumerable<ActorBase> FindCollisionActors(ActorBase self)
        {
            for (int i = 0; i < actors.Count; ++i) {
                if (self == actors[i] || (actors[i].CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    continue;
                }
                if (actors[i].IsCollidingWith(self)) {
                    yield return actors[i];
                }
            }
        }

        public IEnumerable<ActorBase> FindCollisionActorsRadius(float x, float y, float radius)
        {
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
                    yield return actors[i];
                }
            }
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
                ActorBase colliderActor = null;

                FindCollisionActorsFast(self, hitbox, (actor) => {
                    if ((actor.CollisionFlags & CollisionFlags.IsSolidObject) == 0) {
                        return true;
                    }

                    SolidObjectBase solidObject = actor as SolidObjectBase;
                    if (solidObject == null || !solidObject.IsOneWay || downwards) {
                        colliderActor = actor;
                        return false;
                    }

                    return true;
                });

                collider = colliderActor;
            }

            return (collider == null);
        }

        public bool IsPositionEmpty(ActorBase self, ref Hitbox hitbox, bool downwards)
        {
            ActorBase solidObject;
            return IsPositionEmpty(self, ref hitbox, downwards, out solidObject);
        }

        public IEnumerable<Player> GetCollidingPlayers(Hitbox hitbox)
        {
            foreach (Player player in players) {
                if (player.Hitbox.Intersects(ref hitbox)) {
                    yield return player;
                }
            }
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

            LevelInitialization data = default(LevelInitialization);

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

            data.LastEpisodeName = episodeName;

            currentCarryOver = data;

            levelChangeTimer = 50f;
        }

        public void HandleGameOver()
        {
            // ToDo: Implement Game Over screen
            root.ShowMainMenu();
        }

        public bool HandlePlayerDied(Player player)
        {
            if (activeBoss != null) {
                activeBoss.DeactivateBoss();
                activeBoss = null;

                Hud hud = rootObject.GetComponent<Hud>();
                if (hud != null) {
                    hud.ActiveBoss = null;
                }

                if (music != null) {
                    music.FadeOut(1.8f);
                }

                // Load default music again
                music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
                music.BeginFadeIn(0.4f);
            }

            // Single player can respawn immediately
            return true;
        }

        public string GetLevelText(int textID)
        {
            string text;
            levelTexts.TryGetValue(textID, out text);
            return text;
        }

        public void WarpCameraToTarget(ActorBase target)
        {
            CameraController controller = camera.GetComponent<CameraController>();
            if (controller != null && controller.TargetObject == target) {
                controller.TargetObject = target;
            }
        }

        public void LimitCameraView(float left, float width)
        {
            levelBounds.X = left;

            if (width > 0f) {
                levelBounds.W = left;
            } else {
                levelBounds.W = (tileMap.Size.X * tileMap.Tileset.TileSize) - left;
            }

            CameraController controller = camera.GetComponent<CameraController>();
            if (controller != null) {
                if (left == 0 && width == 0) {
                    controller.ViewBounds = levelBounds;
                } else {
                    controller.AnimateToBounds(levelBounds);
                }
            }
        }

        public void ShakeCameraView(float duration)
        {
            CameraController controller = camera.GetComponent<CameraController>();
            if (controller != null) {
                controller.Shake(duration);
            }
        }

        public void PlayCommonSound(string name, ActorBase target, float gain = 1f)
        {
            SoundResource resource;
            if (commonResources.Sounds.TryGetValue(name, out resource)) {
                SoundInstance instance = DualityApp.Sound.PlaySound3D(resource.Sound, target);
                instance.Volume = gain * SettingsCache.SfxVolume;

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
                instance.Volume = gain * SettingsCache.SfxVolume;

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

        public void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            foreach (ActorBase actor in actors) {
                actor.OnTriggeredEvent(eventType, eventParams);
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
                bool playersReady = true;
                foreach (Player player in players) {
                    // Exit type is already provided
                    playersReady &= player.OnLevelChanging(ExitType.None);
                }

                if (playersReady) {
                    if (levelChangeTimer > 0) {
                        levelChangeTimer -= Time.TimeMult;
                    } else {
                        root.ChangeLevel(currentCarryOver.Value);
                        currentCarryOver = null;
                        return;
                    }
                }
            }

            Vector3 pos = players[0].Transform.Pos;
            //int tx = (int)(pos.X / 32);
            //int ty = (int)(pos.Y / 32);
            int tx = (int)pos.X >> 5;
            int ty = (int)pos.Y >> 5;

            // ToDo: Remove this branching
#if __ANDROID__
            const int ActivateTileRange = 20;
#else
            const int ActivateTileRange = 26;
#endif

            for (int i = 0; i < actors.Count; i++) {
                if (actors[i].OnTileDeactivate(tx, ty, ActivateTileRange + 2)) {
                    i--;
                }
            }

            eventMap.ActivateEvents(tx, ty, ActivateTileRange);

            eventMap.ProcessGenerators();

            ResolveCollisions();

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
                            Size = res.Base.FrameDimensions,
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
                            Size = res.Base.FrameDimensions,
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
            if (activeBoss != null && activeBoss.Scene == null) {
                activeBoss = null;

                Hud hud = rootObject.GetComponent<Hud>();
                if (hud != null) {
                    hud.ActiveBoss = null;
                }

                InitLevelChange(ExitType.Normal, null);
                levelChangeTimer *= 2;
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                Scene.SwitchTo(new InGameMenu(root, this));
            }


            if (DualityApp.Keyboard.KeyPressed(Key.E)) {
                ambientLightTarget -= 0.05f;
            } else if (DualityApp.Keyboard.KeyPressed(Key.R)) {
                ambientLightTarget += 0.05f;
            }

            Hud.ShowDebugText("- FPS: " + Time.Fps.ToString("N0") + "  (" + Math.Round(Time.UnscaledDeltaTime * 1000, 1).ToString("N1") + " ms)");
            Hud.ShowDebugText("  Diff.: " + difficulty + " | Actors: " + actors.Count.ToString("N0"));
            Hud.ShowDebugText("  Ambient Light: " + ambientLightCurrent.ToString("0.00") + " / " + ambientLightTarget.ToString("0.00"));


            Hud.ShowDebugText("  Collisions: " + collisionsCountA + " > " + collisionsCountB + " > " + collisionsCountC);
            collisionsCountA = 0;
            collisionsCountB = 0;
            collisionsCountC = 0;
        }

        private void ResolveCollisions()
        {
            collisions.UpdatePairs((proxyA, proxyB) => {

                if (proxyA.Health <= 0 || proxyB.Health <= 0) {
                    return;
                }

                if (proxyA.IsCollidingWith(proxyB)) {
                    proxyA.OnHandleCollision(proxyB);
                    proxyB.OnHandleCollision(proxyA);

                    collisionsCountC++;
                }

                collisionsCountB++;

            });
        }

        private void OnActorTransformChanged(object sender, TransformChangedEventArgs e)
        {
            ActorBase actor = e.Component.GameObj as ActorBase;
            actor.UpdateAABB();
            collisions.MoveProxy(actor, ref actor.AABB, actor.Speed.Xy);

            collisionsCountA++;
        }

        [ExecutionOrder(ExecutionRelation.After, typeof(Transform))]
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