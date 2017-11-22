using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Editor
{
    public class EditorLevelHandler : Scene, ILevelHandler
    {
        private readonly GameObject rootObject;
        private readonly GameObject camera;

        private TileMap tileMap;

        private string levelFileName;
        private string episodeName;
        private string defaultNextLevel;
        private string defaultSecretLevel;

        private float ambientLightCurrent;
        private float ambientLightTarget;
        private int ambientLightDefault;

        private IDictionary<int, string> levelTexts;
        private EventMap eventMap;
        private EventSpawner eventSpawner;

        public ActorApi Api => null;

        public TileMap TileMap => tileMap;
        public EventMap EventMap => eventMap;
        public EventSpawner EventSpawner => eventSpawner;

        public EditorLevelHandler(string episodeName, string levelFileName)
        {
            this.levelFileName = levelFileName;
            this.episodeName = episodeName;

            // ...

            rootObject = new GameObject("LevelManager");
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Setup camera
            camera = new GameObject("MainCamera");
            Transform cameraTransform = camera.AddComponent<Transform>();

            Camera cameraInner = camera.AddComponent<Camera>();
            cameraInner.NearZ = LevelHandler.NearZ;
            cameraInner.FarZ = LevelHandler.FarZ;
            cameraInner.Perspective = PerspectiveMode.Flat;

            CameraController cameraController = camera.AddComponent<CameraController>();

            // Load level
            LoadLevel(levelFileName, episodeName);
        }


        private void LoadLevel(string level, string episode)
        {
            string levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", episode, level);
            using (Stream s = FileOp.Open(PathOp.Combine(levelPath, ".res"), FileAccessMode.Read)) {
                // ToDo: Cache parser, move JSON parsing to ContentResolver
                JsonParser json = new JsonParser();
                LevelHandler.LevelConfigJson config = json.Parse<LevelHandler.LevelConfigJson>(s);

                //if (config.Version.LayerFormat > LevelHandler.LayerFormatVersion || config.Version.EventSet > LevelHandler.EventSetVersion) {
                //    throw new NotSupportedException("Version not supported");
                //}

                Console.WriteLine("Loading level \"" + config.Description.Name + "\"...");

                defaultNextLevel = config.Description.NextLevel;
                defaultSecretLevel = config.Description.SecretLevel;
                ambientLightDefault = config.Description.DefaultLight;
                ambientLightCurrent = ambientLightTarget = ambientLightDefault * 0.01f;

                string tilesetPath = PathOp.Combine(DualityApp.DataDirectory, "Tilesets", config.Description.DefaultTileset);

                tileMap = new TileMap(this,
                    PathOp.Combine(tilesetPath, "tiles.png"),
                    PathOp.Combine(tilesetPath, "mask.png"),
                    PathOp.Combine(tilesetPath, "normal.png"),
                    (config.Description.Flags & LevelHandler.LevelFlags.HasPit) != 0);

                ColorRgba[] tileMapPalette = TileSet.LoadPalette(PathOp.Combine(tilesetPath, ".palette"));

                ContentResolver.Current.ApplyBasePalette(tileMapPalette);

                // Read all layers
                config.Layers.Add("Sprite", new LevelHandler.LevelConfigJson.LayerSection {
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

                CameraController controller = camera.GetComponent<CameraController>();
                controller.ViewRect = new Rect(tileMap.Size * tileMap.Tileset.TileSize);

                // Read events
                //eventMap = new EventMap(this, tileMap.Size);

                //string eventsPath = PathOp.Combine(levelPath, "Events.layer");
                //if (FileOp.Exists(animTilesPath)) {
                //    eventMap.ReadEvents(eventsPath, config.Version.LayerFormat, difficulty);
                //}

                levelTexts = config.TextEvents ?? new Dictionary<int, string>();

                GameObject tilemapHandler = new GameObject("TilemapHandler");
                tilemapHandler.Parent = rootObject;
                tilemapHandler.AddComponent(tileMap);

                // Load default music
                //musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", config.Description.DefaultMusic);
                //music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
                //music.BeginFadeIn(0.5f);
            }
        }

        public void AddActor(ActorBase actor)
        {
            // ToDo
        }

        public void RemoveActor(ActorBase actor)
        {
            // ToDo
        }

        public void PlayCommonSound(string name, Vector3 pos, float gain = 1f)
        {
            // ToDo
        }

        private void OnUpdate()
        {
            // ToDo
        }

        private class LocalController : Component, ICmpUpdatable
        {
            private readonly EditorLevelHandler levelHandler;

            public LocalController(EditorLevelHandler levelHandler)
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