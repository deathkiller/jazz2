using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Actors.Bosses;
using Jazz2.Actors.Solid;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Server;
using Jazz2.Server;
using Jazz2.Storage.Content;
using Lidgren.Network;
using MathF = Duality.MathF;

namespace Jazz2.Game
{
    public class LevelHandler : Scene, ILevelHandler
    {
        public const float NearZ = 10f;
        public const float FarZ = 1000f;
        public const float MainPlaneZ = (NearZ + FarZ) * 0.5f;
        public const float PlayerZ = MainPlaneZ - 10f;

        public const int LayerFormatVersion = 1;
        public const int EventSetVersion = 2;

        public const float DefaultGravity = 0.3f;

        private GameServer root;

        protected readonly GameObject rootObject;

        private TileMap tileMap;
        private EventMap eventMap;
        private EventSpawner eventSpawner;
        private DynamicTreeBroadPhase<ActorBase> collisions;
        private int collisionsCountA, collisionsCountB, collisionsCountC;

        protected List<Player> players = new List<Player>();
        protected List<GameObject> cameras = new List<GameObject>();
        private List<ActorBase> actors = new List<ActorBase>();

        private string levelFileName;
        private string levelFriendlyName;
        private string episodeName;
        private string defaultNextLevel;
        private string defaultSecretLevel;
        private GameDifficulty difficulty;
        private string musicPath;

        private InitState initState;
        private float gravity;
        private Rect levelBounds;

        private BossBase activeBoss;

        private IList<string> levelTexts;
        private Metadata commonResources;

        private LevelInitialization? nextLevelInit;
        private float levelChangeTimer;

        private WeatherType weatherType;
        private int weatherIntensity;
        private bool weatherOutdoors;

        private int waterLevel = int.MaxValue;

        //private Dictionary<int, RemotableActor> remotableActors;
        private List<ActorBase> spawnedActors = new List<ActorBase>();
        private Dictionary<ActorBase, string> spawnedActorsAnimation = new Dictionary<ActorBase, string>();
        private int lastSpawnedActorId;

        public GameServer Root => root;

        public TileMap TileMap => tileMap;
        public EventMap EventMap => eventMap;
        public EventSpawner EventSpawner => eventSpawner;

        public GameDifficulty Difficulty => difficulty;

        public float Gravity => gravity;

        public Rect LevelBounds => levelBounds;

        public string LevelFriendlyName => levelFriendlyName;

        // ToDo
        public float AmbientLightCurrent
        {
            get { return 100; }
            set { }
        }

        public int AmbientLightDefault
        {
            get { return 100; }
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

        public List<ActorBase> SpawnedActors => spawnedActors;

        public LevelHandler(GameServer root, string episodeName, string levelName)
        {
            this.root = root;

            this.levelFileName = levelName;
            this.episodeName = episodeName;
            difficulty = GameDifficulty.Multiplayer;

            gravity = DefaultGravity;

            collisions = new DynamicTreeBroadPhase<ActorBase>();

            eventSpawner = new EventSpawner(this);

            rootObject = new GameObject();
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Load level
            LoadLevel(levelFileName, episodeName);

            // Process carry overs
            /*if (data.PlayerCarryOvers != null)
            {
                for (int i = 0; i < data.PlayerCarryOvers.Length; i++)
                {
                    Vector2 spawnPosition = eventMap.GetSpawnPosition(data.PlayerCarryOvers[i].Type);
                    if (spawnPosition == new Vector2(-1, -1))
                    {
                        spawnPosition = eventMap.GetSpawnPosition(PlayerType.Jazz);
                        if (spawnPosition == new Vector2(-1, -1))
                        {
                            continue;
                        }
                    }

                    Player player = new Player();
                    player.OnActivated(new ActorActivationDetails
                    {
                        LevelHandler = this,
                        Pos = new Vector3(spawnPosition, PlayerZ),
                        Params = new[] { (ushort)data.PlayerCarryOvers[i].Type, (ushort)i }
                    });
                    AddPlayer(player);

                    player.ReceiveLevelCarryOver(data.ExitType, ref data.PlayerCarryOvers[i]);
                }
            }*/

            // Common sounds
            commonResources = ContentResolver.Current.RequestMetadata("Common/Scenery");
        }

        protected override void OnDisposing(bool manually)
        {
            if (eventMap != null) {
                eventMap.Dispose();
                eventMap = null;
            }

            if (tileMap != null) {
                tileMap.ReleaseResources();
                tileMap = null;
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
                public float XOffset { get; set; }
                public float YOffset { get; set; }

                public int Depth { get; set; }
                public bool InherentOffset { get; set; }
                public int BackgroundStyle { get; set; }
                public IList<int> BackgroundColor { get; set; }
                public bool ParallaxStarsEnabled { get; set; }
            }

            public VersionSection Version { get; set; }
            public DescriptionSection Description { get; set; }
            public IList<string> TextEvents { get; set; }
            public IList<TilesetSection> Tilesets { get; set; }
            public IDictionary<string, LayerSection> Layers { get; set; }

        }

        private void LoadLevel(string level, string episode)
        {
            string levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", episode, level + ".level");
            IFileSystem levelPackage = new CompressedContent(levelPath);

            LevelConfigJson json;
            using (Stream s = levelPackage.OpenFile(".res", FileAccessMode.Read)) {
                json = ContentResolver.Current.Json.Parse<LevelConfigJson>(s);
            }

            if (json.Version.LayerFormat > LayerFormatVersion || json.Version.EventSet > EventSetVersion) {
                throw new NotSupportedException("Level version not supported");
            }

            Log.Write(LogType.Info, "Loading level \"" + json.Description.Name + "\"...");

            levelFriendlyName = json.Description.Name;

            defaultNextLevel = json.Description.NextLevel;
            defaultSecretLevel = json.Description.SecretLevel;

            // Palette
            ColorRgba[] tileMapPalette;
            if (levelPackage.FileExists("Main.palette")) {
                using (Stream s = levelPackage.OpenFile("Main.palette", FileAccessMode.Read)) {
                    tileMapPalette = TileSet.LoadPalette(s);
                }
            } else {
                tileMapPalette = null;
            }

            // Tileset
            tileMap = new TileMap(this, json.Description.DefaultTileset, tileMapPalette, (json.Description.Flags & LevelFlags.HasPit) != 0);

            // Additional tilesets
            if (json.Tilesets != null) {
                for (int i = 0; i < json.Tilesets.Count; i++) {
                    LevelConfigJson.TilesetSection part = json.Tilesets[i];
                    tileMap.ReadTilesetPart(part.Name, part.Offset, part.Count);
                }
            }

            // Read all layers
            json.Layers.Add("Sprite", new LevelConfigJson.LayerSection {
                XSpeed = 1,
                YSpeed = 1
            });

            foreach (var layer in json.Layers.OrderBy(layer => layer.Value.Depth)) {
                LayerType type;
                if (layer.Key == "Sprite") {
                    type = LayerType.Sprite;
                } else if (layer.Key == "Sky") {
                    type = LayerType.Sky;

                    //if (layer.Value.BackgroundStyle != 0 /*Plain*/ && layer.Value.BackgroundColor != null && layer.Value.BackgroundColor.Count >= 3) {
                    //    camera.GetComponent<Camera>().ClearColor = new ColorRgba((byte)layer.Value.BackgroundColor[0], (byte)layer.Value.BackgroundColor[1], (byte)layer.Value.BackgroundColor[2]);
                    //}
                } else {
                    type = LayerType.Other;
                }

                using (Stream s = levelPackage.OpenFile(layer.Key + ".layer", FileAccessMode.Read)) {
                    tileMap.ReadLayerConfiguration(type, s, layer.Value);
                }
            }

            // Read animated tiles
            if (levelPackage.FileExists("Animated.tiles")) {
                using (Stream s = levelPackage.OpenFile("Animated.tiles", FileAccessMode.Read)) {
                    tileMap.ReadAnimatedTiles(s);
                }
            }

            levelBounds = new Rect(tileMap.Size * tileMap.Tileset.TileSize);

            // Read events
            eventMap = new EventMap(this, tileMap.Size);

            if (levelPackage.FileExists("Events.layer")) {
                using (Stream s2 = levelPackage.OpenFile("Events.layer", FileAccessMode.Read)) {
                    eventMap.ReadEvents(s2, json.Version.LayerFormat, difficulty);
                }
            }

            GameObject tilemapHandler = new GameObject();
            tilemapHandler.Parent = rootObject;
            tilemapHandler.AddComponent(tileMap);

            // Apply weather
            if (json.Description.DefaultWeather != WeatherType.None) {
                ApplyWeather(
                    json.Description.DefaultWeather,
                    json.Description.DefaultWeatherIntensity,
                    json.Description.DefaultWeatherOutdoors);
            }

            // Load level text events
            levelTexts = json.TextEvents ?? new List<string>();

            /*if (FileOp.Exists(levelPath + "." + i18n.Language)) {
                try {
                    using (Stream s = FileOp.Open(levelPath + "." + i18n.Language, FileAccessMode.Read)) {
                        json = ContentResolver.Current.Json.Parse<LevelConfigJson>(s);
                        if (json.TextEvents != null) {
                            for (int i = 0; i < json.TextEvents.Count && i < levelTexts.Count; i++) {
                                if (json.TextEvents[i] != null) {
                                    levelTexts[i] = json.TextEvents[i];
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log.Write(LogType.Warning, "Cannot load i18n for this level: " + ex);
                }
            }*/

            eventMap.ActivateEvents(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, false);
        }

        public virtual void AddActor(ActorBase actor)
        {
            /*actors.Add(actor);
            AddObject(actor);

            if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0)
            {
                actor.UpdateAABB();
                collisions.AddProxy(actor);
                actor.Transform.EventTransformChanged += OnActorTransformChanged;
            }*/

            if (string.IsNullOrEmpty(actor.LoadedMetadata)) {
                Log.Write(LogType.Warning, actor.ToString() + " has no metadata");
                return;
            }

            int index;

            lock (root.Synchronization) {
                lastSpawnedActorId++;
                index = (lastSpawnedActorId << 8) | 0xff; // Subindex 0xff means that the object is not owned by any player

                actor.Index = index;
                spawnedActors.Add(actor);
                AddObject(actor);

                if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                    actor.UpdateAABB();
                    collisions.AddProxy(actor);
                    actor.Transform.EventTransformChanged += OnActorTransformChanged;
                }
            }

            root.SendToActivePlayers(new CreateRemoteActor {
                Index = index,
                Pos = actor.Transform.Pos,
                MetadataPath = actor.LoadedMetadata,
                CollisionFlags = actor.CollisionFlags
            }, 64, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

            if (spawnedActorsAnimation.TryGetValue(actor, out string identifier)) {
                root.SendToActivePlayers(new RefreshActorAnimation {
                    Index = actor.Index,
                    Identifier = identifier,
                }, 32, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }
        }

        public virtual void RemoveActor(ActorBase actor)
        {
            /*if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0)
            {
                actor.Transform.EventTransformChanged -= OnActorTransformChanged;
                collisions.RemoveProxy(actor);
            }

            actors.Remove(actor);
            RemoveObject(actor);
            actor.OnDestroyed();
            actor.Dispose();*/

            int index = actor.Index;

            lock (root.Synchronization) {
                spawnedActors.Remove(actor);
                spawnedActorsAnimation.Remove(actor);
                RemoveObject(actor);
                actor.OnDestroyed();
                actor.Dispose();

                if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                    actor.Transform.EventTransformChanged -= OnActorTransformChanged;
                    collisions.RemoveProxy(actor);
                }
            }

            root.SendToActivePlayers(new DestroyRemoteActor {
                Index = index,
            }, 5, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        public void AddPlayer(Player actor)
        {
            players.Add(actor);

            //AddActor(actor);
            AddObject(actor);

            if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                actor.UpdateAABB();
                collisions.AddProxy(actor);
                actor.Transform.EventTransformChanged += OnActorTransformChanged;
            }
        }

        public void RemovePlayer(Player actor)
        {
            //RemoveActor(actor);

            if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                actor.Transform.EventTransformChanged -= OnActorTransformChanged;
                collisions.RemoveProxy(actor);
            }

            RemoveObject(actor);
            actor.OnDestroyed();
            actor.Dispose();

            players.Remove(actor);
        }

        public void FindCollisionActorsByAABB(ActorBase self, AABB aabb, Func<ActorBase, bool> callback)
        {
            collisions.Query(actor => {
                if (self == actor || (actor.CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    return true;
                }
                if (actor.IsCollidingWith(ref aabb)) {
                    return callback(actor);
                }
                return true;
            }, ref aabb);
        }

        public void FindCollisionActorsByRadius(float x, float y, float radius, Func<ActorBase, bool> callback)
        {
            AABB aabb = new AABB(x - radius, y - radius, x + radius, y + radius);
            collisions.Query((actor) => {
                if ((actor.CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
                    return true;
                }

                // Find the closest point to the circle within the rectangle
                float closestX = MathF.Clamp(x, actor.AABB.LowerBound.X, actor.AABB.UpperBound.X);
                float closestY = MathF.Clamp(y, actor.AABB.LowerBound.Y, actor.AABB.UpperBound.Y);

                // Calculate the distance between the circle's center and this closest point
                float distanceX = (x - closestX);
                float distanceY = (y - closestY);

                // If the distance is less than the circle's radius, an intersection occurs
                float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                if (distanceSquared < (radius * radius)) {
                    return callback(actor);
                }

                return true;
            }, ref aabb);
        }

        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards, out ActorBase collider)
        {
            collider = null;

            if ((self.CollisionFlags & CollisionFlags.CollideWithTileset) != 0) {
                if (!tileMap.IsTileEmpty(ref aabb, downwards)) {
                    return false;
                }
            }

            // Check for solid objects
            if ((self.CollisionFlags & CollisionFlags.CollideWithSolidObjects) != 0) {
                ActorBase colliderActor = null;

                FindCollisionActorsByAABB(self, aabb, (actor) => {
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

        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards)
        {
            return IsPositionEmpty(self, ref aabb, downwards, out _);
        }

        public IEnumerable<Player> GetCollidingPlayers(AABB aabb)
        {
            foreach (Player player in players) {
                if (AABB.TestOverlap(ref player.AABB, ref aabb)) {
                    yield return player;
                }
            }
        }

        public void InitLevelChange(ExitType exitType, string nextLevel)
        {
            if (initState == InitState.Disposing) {
                return;
            }

            initState = InitState.Disposing;

            foreach (Player player in players) {
                player.OnLevelChanging(exitType);
            }

            if (nextLevel == null) {
                nextLevel = (exitType == ExitType.Bonus ? defaultSecretLevel : defaultNextLevel);
            }

            LevelInitialization levelInit = default(LevelInitialization);

            if (nextLevel != null) {
                int i = nextLevel.IndexOf('/');
                if (i == -1) {
                    levelInit.EpisodeName = episodeName;
                    levelInit.LevelName = nextLevel;
                } else {
                    levelInit.EpisodeName = nextLevel.Substring(0, i);
                    levelInit.LevelName = nextLevel.Substring(i + 1);
                }
            }

            levelInit.Difficulty = difficulty;
            levelInit.ExitType = exitType;

            levelInit.PlayerCarryOvers = new PlayerCarryOver[players.Count];
            for (int i = 0; i < players.Count; i++) {
                levelInit.PlayerCarryOvers[i] = players[i].PrepareLevelCarryOver();
            }

            levelInit.LastEpisodeName = episodeName;

            nextLevelInit = levelInit;

            levelChangeTimer = 50f;
        }

        public void HandleGameOver()
        {
            // ToDo
        }

        public virtual bool HandlePlayerDied(Player player)
        {
            if (activeBoss != null) {
                if (activeBoss.HandlePlayerDied()) {
                    activeBoss = null;

                    // ToDo
                }
            }

            root.HandlePlayerDied(player.Index);
            return false;
        }

        public string GetLevelText(int textID)
        {
            if (textID < 0 || textID >= levelTexts.Count) {
                return null;
            }

            return levelTexts[textID];
        }

        public void WarpCameraToTarget(ActorBase target)
        {
            // ToDo
        }

        public void LimitCameraView(float left, float width)
        {
            levelBounds.X = left;

            if (width > 0f) {
                levelBounds.W = left;
            } else {
                levelBounds.W = (tileMap.Size.X * tileMap.Tileset.TileSize) - left;
            }

            // ToDo
        }

        public void ShakeCameraView(float duration)
        {
            // ToDo
        }

        public void PlayCommonSound(string name, ActorBase target, float gain = 1f, float pitch = 1f)
        {
            // ToDo
        }

        public void PlayCommonSound(string name, Vector3 pos, float gain = 1f, float pitch = 1f)
        {
            // ToDo
        }

        public void BroadcastLevelText(string text)
        {
            foreach (Player player in players) {
                player.ShowLevelText(text);
            }
        }

        public virtual void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            if (eventType == EventType.AreaActivateBoss) {
                ActivateBoss(eventParams[0]);
            }

            foreach (ActorBase actor in actors) {
                actor.OnTriggeredEvent(eventType, eventParams);
            }
        }

        public void BroadcastAnimationChanged(ActorBase actor, string identifier)
        {
            spawnedActorsAnimation[actor] = identifier;

            if (spawnedActors.Contains(actor)) {
                root.SendToActivePlayers(new RefreshActorAnimation {
                    Index = actor.Index,
                    Identifier = identifier,
                }, 32, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }
        }

        public bool OverridePlayerFireWeapon(Player player, WeaponType weaponType, out float weaponCooldown)
        {
            weaponCooldown = 0;
            return false;
        }

        internal void OnPlayerTakeDamage(Player player, int amount, float pushForce)
        {
            root.SendToPlayerByIndex(new PlayerTakeDamage {
                Index = (byte)player.Index,
                HealthBefore = (byte)player.Health,
                DamageAmount = (byte)amount,
                PushForce = pushForce
            }, 8, player.Index, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerAddHealth(Player player, int amount)
        {
            root.SendToPlayerByIndex(new PlayerAddHealth {
                Index = (byte)player.Index,
                Amount = (byte)amount
            }, 3, player.Index, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerSpringActivated(Player player, Vector2 force, bool keepSpeedX, bool keepSpeedY)
        {
            root.SendToPlayerByIndex(new PlayerActivateForce {
                Index = (byte)player.Index,
                ActivatedBy = PlayerActivateForce.ForceType.Spring,
                Force = force,
                KeepSpeedX = keepSpeedX,
                KeepSpeedY = keepSpeedY
            }, 12, player.Index, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerPinballBumperActivated(Player player, Vector2 force)
        {
            root.SendToPlayerByIndex(new PlayerActivateForce {
                Index = (byte)player.Index,
                ActivatedBy = PlayerActivateForce.ForceType.PinballBumper,
                Force = force
            }, 12, player.Index, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerPinballPaddleActivated(Player player, Vector2 force)
        {
            root.SendToPlayerByIndex(new PlayerActivateForce {
                Index = (byte)player.Index,
                ActivatedBy = PlayerActivateForce.ForceType.PinballPaddle,
                Force = force
            }, 12, player.Index, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerRefreshAmmo(Player player, WeaponType weaponType, short count, bool switchTo)
        {
            root.SendToPlayerByIndex(new PlayerRefreshAmmo {
                Index = (byte)player.Index,
                WeaponType = weaponType,
                Count = count,
                SwitchTo = switchTo
            }, 6, player.Index, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnPlayerRefreshWeaponUpgrades(Player player, WeaponType weaponType, byte upgrades)
        {
            root.SendToPlayerByIndex(new PlayerRefreshWeaponUpgrades {
                Index = (byte)player.Index,
                WeaponType = weaponType,
                Upgrades = upgrades
            }, 4, player.Index, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnPlayerSetModifier(Player player, Player.Modifier modifier)
        {
            root.SendToPlayerByIndex(new PlayerSetModifier {
                Index = (byte)player.Index,
                Modifier = modifier
            }, 3, player.Index, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnPlayerWarpToPosition(Player player, Vector2 pos, bool fast)
        {
            root.SendToPlayerByIndex(new PlayerWarpToPosition {
                Index = (byte)player.Index,
                Pos = pos,
                Fast = fast
            }, 7, player.Index, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnPlayerHit(Player player, Player attacker, bool isDead)
        {
            root.IncrementPlayerHits(attacker.Index, isDead);
        }

        internal void OnPlayerIncrementLap(Player player)
        {
            root.IncrementPlayerLap(player.Index, out int currentLap);
            if (currentLap == -1) {
                return;
            }

            root.SendToActivePlayers(new PlayerSetLap {
                Index = (byte)player.Index,
                Lap = currentLap
            }, 4, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        internal void OnPlayerSetInvulnerability(Player player, float time, bool withCircleEffect)
        {
            // ToDo: Not visible to other players

            root.SendToActivePlayers(new PlayerSetInvulnerability {
                Index = (byte)player.Index,
                Time = time,
                WithCircleEffect = withCircleEffect
            }, 7, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnAdvanceDestructibleTileAnimation(int tx, int ty, int amount)
        {
            root.SendToActivePlayers(new AdvanceTileAnimation {
                TileX = tx,
                TileY = ty,
                Amount = amount
            }, 6, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        internal void OnSetTrigger(ushort triggerID, bool newState)
        {
            root.SendToActivePlayers(new SetTrigger {
                TriggerID = triggerID,
                NewState = newState
            }, 4, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
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

        protected virtual void OnUpdate()
        {
            // ToDo

        }

        protected virtual void OnFixedUpdate(float timeMult)
        {

            eventMap.ProcessGenerators(timeMult);

            ResolveCollisions();

            // ToDo: Weather
            /*if (weatherType != WeatherType.None && commonResources.Graphics != null)
            {
                // ToDo: Apply weather effect to all other cameras too
                Vector3 viewPos = cameras[0].Transform.Pos;
                for (int i = 0; i < weatherIntensity; i++)
                {
                    TileMap.DebrisCollisionAction collisionAction;
                    if (weatherOutdoors)
                    {
                        collisionAction = TileMap.DebrisCollisionAction.Disappear;
                    }
                    else
                    {
                        collisionAction = (MathF.Rnd.NextFloat() > 0.7f
                            ? TileMap.DebrisCollisionAction.None
                            : TileMap.DebrisCollisionAction.Disappear);
                    }

                    Vector3 debrisPos = viewPos + MathF.Rnd.NextVector3((LevelRenderSetup.TargetSize.X / -2) - 40,
                                      (LevelRenderSetup.TargetSize.Y * -2 / 3), MainPlaneZ,
                                      LevelRenderSetup.TargetSize.X + 120, LevelRenderSetup.TargetSize.Y, 0);

                    if (weatherType == WeatherType.Rain)
                    {
                        GraphicResource res = commonResources.Graphics["Rain"];
                        Material material = res.Material.Res;
                        Texture texture = material.MainTexture.Res;

                        float scale = MathF.Rnd.NextFloat(0.4f, 1.1f);
                        float speedX = MathF.Rnd.NextFloat(2.2f, 2.7f) * scale;
                        float speedY = MathF.Rnd.NextFloat(7.6f, 8.6f) * scale;

                        debrisPos.Z = MainPlaneZ * scale;

                        tileMap.CreateDebris(new TileMap.DestructibleDebris
                        {
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
                    }
                    else
                    {
                        GraphicResource res = commonResources.Graphics["Snow"];
                        Material material = res.Material.Res;
                        Texture texture = material.MainTexture.Res;

                        float scale = MathF.Rnd.NextFloat(0.4f, 1.1f);
                        float speedX = MathF.Rnd.NextFloat(-1.6f, -1.2f) * scale;
                        float speedY = MathF.Rnd.NextFloat(3f, 4f) * scale;
                        float accel = MathF.Rnd.NextFloat(-0.008f, 0.008f) * scale;

                        debrisPos.Z = MainPlaneZ * scale;

                        tileMap.CreateDebris(new TileMap.DestructibleDebris
                        {
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
            }*/

            // Active Boss
            if (activeBoss != null && activeBoss.Scene == null) {
                activeBoss = null;

                // ToDo

                InitLevelChange(ExitType.Normal, null);
            }

            if (initState == InitState.Initializing) {
                initState = InitState.Initialized;
            }


            collisionsCountA = 0;
            collisionsCountB = 0;
            collisionsCountC = 0;
        }

        private void ActivateBoss(ushort musicFile)
        {
            if (activeBoss != null) {
                return;
            }

            foreach (GameObject obj in ActiveObjects) {
                activeBoss = obj as BossBase;
                if (activeBoss != null) {
                    break;
                }
            }

            if (activeBoss == null) {
                return;
            }

            if (!activeBoss.HandleBossActivated()) {
                return;
            }

            // ToDo
        }

        private void ResolveCollisions()
        {
            collisions.UpdatePairs((proxyA, proxyB) => {

                if (proxyA.Health <= 0 || proxyB.Health <= 0) {
                    return;
                }
                if ((proxyA.CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0 ||
                    (proxyB.CollisionFlags & CollisionFlags.CollideWithOtherActors) == 0) {
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

            actor.CollisionFlags |= CollisionFlags.TransformChanged;

            collisionsCountA++;
        }

        /*public void AddSpawnedActor(ActorBase actor)
        {
            if (string.IsNullOrEmpty(actor.LoadedMetadata)) {
                Log.Write(LogType.Warning, actor.ToString() + " has no metadata");
                return;
            }

            int index;

            lock (root.Synchronization) {
                index = (lastSpawnedActorId << 8) | 0xff; // Subindex 0xff means that the object is not owned by any player
                lastSpawnedActorId++;

                actor.Index = index;
                spawnedActors.Add(actor);

                if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                    actor.UpdateAABB();
                    collisions.AddProxy(actor);
                    actor.Transform.EventTransformChanged += OnActorTransformChanged;
                }
            }

            root.SendToActivePlayers(new CreateRemoteActor {
                Index = index,
                Pos = actor.Transform.Pos,
                MetadataPath = actor.LoadedMetadata
            }, 35, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

            if (spawnedActorsAnimation.TryGetValue(actor, out string identifier)) {
                root.SendToActivePlayers(new RefreshActorAnimation {
                    Index = actor.Index,
                    Identifier = identifier,
                }, 32, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }
        }

        public void DestroySpawnedActor(ActorBase actor)
        {
            int index = actor.Index;

            lock (root.Synchronization) {
                spawnedActors.Remove(actor);
                spawnedActorsAnimation.Remove(actor);

                if ((actor.CollisionFlags & CollisionFlags.ForceDisableCollisions) == 0) {
                    actor.Transform.EventTransformChanged -= OnActorTransformChanged;
                    collisions.RemoveProxy(actor);
                }
            }

            root.SendToActivePlayers(new DestroyRemoteActor {
                Index = index,
            }, 5, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }*/

        public void SendAllSpawnedActors(NetConnection connection)
        {
            // Send command to create all spawned actors
            foreach (ActorBase actor in spawnedActors) {
                root.Send(new CreateRemoteActor {
                    Index = actor.Index,
                    Pos = actor.Transform.Pos,
                    MetadataPath = actor.LoadedMetadata,
                    CollisionFlags = actor.CollisionFlags
                }, 64, connection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }

            foreach (var actor in spawnedActorsAnimation) {
                root.Send(new RefreshActorAnimation {
                    Index = actor.Key.Index,
                    Identifier = actor.Value,
                }, 32, connection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }
        }

        [ExecutionOrder(ExecutionRelation.After, typeof(Transform))]
        private class LocalController : Component, ICmpUpdatable, ICmpFixedUpdatable
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

            void ICmpFixedUpdatable.OnFixedUpdate(float timeMult)
            {
                levelHandler.OnFixedUpdate(timeMult);
            }
        }
    }
}