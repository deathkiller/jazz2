using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Duality;
using Import;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Compatibility.EventConverter;

namespace Jazz2.Compatibility
{
    public class JJ2Level // .j2l
    {
        private struct LayerSection
        {
            public uint Flags;              // ToDo: All except Parallax Stars supported
            public byte Type;               // Ignored
            public bool Used;
            public int Width;
            public int InternalWidth;
            public int Height;
            public int Depth;
            public byte DetailLevel;        // Ignored
            public double WaveX;            // ToDo: Not supported
            public double WaveY;            // ToDo: Not supported
            public double SpeedX;
            public double SpeedY;
            public double AutoSpeedX;
            public double AutoSpeedY;
            public byte TexturedBackgroundType;
            public byte TexturedParams1;
            public byte TexturedParams2;
            public byte TexturedParams3;
            public ushort[] Tiles;
        }

        private struct TileEventSection
        {
            public JJ2Event EventType;
            public byte Difficulty;
            public bool Illuminate;
            public uint TileParams;         // Partially supported
        }

        private struct TilePropertiesSection
        {
            public TileEventSection Event;
            public bool Flipped;
            public byte Type;               // Partially supported (Translucent: supported, Caption: ignored)
        }

        private unsafe struct AnimatedTileSection
        {
            public ushort Delay;
            public ushort DelayJitter;
            public ushort ReverseDelay;
            public bool IsReverse;
            public byte Speed;
            public byte FrameCount;
            public fixed ushort Frames[64];
        }

        private unsafe struct DictionaryEntry
        {
            public fixed ushort Tiles[4];
        }

        public struct ExtraTilesetEntry
        {
            public string Name;
            public ushort Offset;
            public ushort Count;
        }

        public struct LevelToken
        {
            public string Episode;
            public string Level;
        }

        public enum WeatherType
        {
            None,
            Snow,
            Flowers,
            Rain,
            Leaf
        }

        private const int JJ2LayerCount = 8;

        private string path, levelToken, name;
        private string tileset, music;
        private string nextLevel, bonusLevel, secretLevel;

        private LayerSection[] layers;
        private TilePropertiesSection[] staticTiles;
        private AnimatedTileSection[] animatedTiles;
        private TileEventSection[] events;

        private RawList<string> textEventStrings;
        private HashSet<int> levelTokenTextIDs;

        private JJ2Version version;
        private ushort lightingMin, lightingStart;
        private ushort animCount;
        private bool verticalMPSplitscreen;
        private bool isMpLevel;
        private bool hasPit, hasCTF, hasLaps;

        private Color[] palette; // JJ2+
        private ExtraTilesetEntry[] extraTilesets; // JJ2+

        private WeatherType weatherType; // JJ2+
        private byte weatherIntensity; // JJ2+
        private bool weatherOutdoors; // JJ2+
        private Color darknessColor = Color.FromArgb(255, 0, 0, 0); // JJ2+

        private Dictionary<JJ2Event, int> unsupportedEvents;

        public int MaxSupportedTiles => (version == JJ2Version.BaseGame ? 1024 : 4096);
        public int MaxSupportedAnims => (version == JJ2Version.BaseGame ? 128 : 256);

        public string CurrentLevelToken => levelToken;

        public string Tileset => tileset;
        public ExtraTilesetEntry[] ExtraTilesets => extraTilesets;
        public string Music => music;
        public JJ2Version Version => version;
        public bool VerticalMpSplitscreen => verticalMPSplitscreen;
        public bool IsMpLevel => isMpLevel;

        public Dictionary<JJ2Event, int> UnsupportedEvents => unsupportedEvents;

        public unsafe static JJ2Level Open(string path, bool strictParser)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                // Skip copyright notice
                s.Seek(180, SeekOrigin.Current);

                JJ2Level level = new JJ2Level();
                level.path = path;
                level.levelToken = Path.GetFileNameWithoutExtension(path);

                JJ2Block headerBlock = new JJ2Block(s, 262 - 180);

                uint magic = headerBlock.ReadUInt32();
                if (magic != 0x4C56454C /*LEVL*/) {
                    throw new InvalidOperationException("Invalid magic string");
                }

                uint passwordHash = headerBlock.ReadUInt32();

                level.name = headerBlock.ReadString(32, true);

                ushort version = headerBlock.ReadUInt16();
                level.version = (version <= 514 ? JJ2Version.BaseGame : JJ2Version.TSF);

                int recordedSize = headerBlock.ReadInt32();
                if (strictParser && s.Length != recordedSize) {
                    throw new InvalidOperationException("Unexpected file size");
                }

                // Get the CRC; would check here if it matches if we knew what variant it is AND what it applies to
                // Test file across all CRC32 variants + Adler had no matches to the value obtained from the file
                // so either the variant is something else or the CRC is not applied to the whole file but on a part
                int recordedCRC = headerBlock.ReadInt32();

                // Read the lengths, uncompress the blocks and bail if any block could not be uncompressed
                // This could look better without all the copy-paste, but meh.
                int infoBlockPackedSize = headerBlock.ReadInt32();
                int infoBlockUnpackedSize = headerBlock.ReadInt32();
                int eventBlockPackedSize = headerBlock.ReadInt32();
                int eventBlockUnpackedSize = headerBlock.ReadInt32();
                int dictBlockPackedSize = headerBlock.ReadInt32();
                int dictBlockUnpackedSize = headerBlock.ReadInt32();
                int layoutBlockPackedSize = headerBlock.ReadInt32();
                int layoutBlockUnpackedSize = headerBlock.ReadInt32();

                JJ2Block infoBlock = new JJ2Block(s, infoBlockPackedSize, infoBlockUnpackedSize);
                JJ2Block eventBlock = new JJ2Block(s, eventBlockPackedSize, eventBlockUnpackedSize);
                JJ2Block dictBlock = new JJ2Block(s, dictBlockPackedSize, dictBlockUnpackedSize);
                JJ2Block layoutBlock = new JJ2Block(s, layoutBlockPackedSize, layoutBlockUnpackedSize);

                level.LoadMetadata(infoBlock, strictParser);
                level.LoadEvents(eventBlock, strictParser);
                level.LoadLayers(dictBlock, dictBlockUnpackedSize / 8, layoutBlock, strictParser);

                // Try to read MLLE data stream
                const int mlleHeaderSize = 4 + 4 + 4 + 4;
                byte[] mlleHeader = new byte[mlleHeaderSize];

                if (s.Read(mlleHeader, 0, mlleHeaderSize) == mlleHeaderSize) {
                    fixed (byte* ptr = mlleHeader) {
                        uint mlleMagic = *(uint*)ptr;
                        if (mlleMagic == 0x454C4C4D /*MLLE*/) {
                            uint mlleVersion = *(uint*)(ptr + 4);
                            int mlleBlockPackedSize = *(int*)(ptr + 4 + 4);
                            int mlleBlockUnpackedSize = *(int*)(ptr + 4 + 4 + 4);

                            JJ2Block mlleBlock = new JJ2Block(s, mlleBlockPackedSize, mlleBlockUnpackedSize);
                            level.LoadMlleData(mlleBlock, mlleVersion, strictParser);
                        }
                    }
                }

                return level;
            }
        }

        private JJ2Level()
        {
        }

        private void LoadMetadata(JJ2Block block, bool strictParser)
        {
            // First 9 bytes are JCS coordinates on last save.
            block.DiscardBytes(9);

            lightingMin = block.ReadByte();
            lightingStart = block.ReadByte();

            animCount = block.ReadUInt16();

            verticalMPSplitscreen = block.ReadBool();
            isMpLevel = block.ReadBool();

            // This should be the same as size of block in the start?
            int headerSize = block.ReadInt32();

            string secondLevelName = block.ReadString(32, true);
            if (strictParser && name != secondLevelName) {
                throw new InvalidOperationException("Level name mismatch");
            }

            tileset = block.ReadString(32, true);
            bonusLevel = block.ReadString(32, true);
            nextLevel = block.ReadString(32, true);
            secretLevel = block.ReadString(32, true);
            music = block.ReadString(32, true);

            textEventStrings = new RawList<string>();
            for (int i = 0; i < 16; ++i) {
                textEventStrings.Add(block.ReadString(512, true));
            }

            levelTokenTextIDs = new HashSet<int>();

            LoadLayerMetadata(block, strictParser);

            ushort staticTilesCount = block.ReadUInt16();
            if (strictParser && MaxSupportedTiles - animCount != staticTilesCount) {
                throw new InvalidOperationException("Tile count mismatch");
            }

            LoadStaticTileData(block, strictParser);

            // The unused XMask field
            block.DiscardBytes(MaxSupportedTiles);

            LoadAnimatedTiles(block, strictParser);
        }

        private void LoadStaticTileData(JJ2Block block, bool strictParser)
        {
            int tileCount = MaxSupportedTiles;
            staticTiles = new TilePropertiesSection[tileCount];

            for (int i = 0; i < tileCount; ++i) {
                int tileEvent = block.ReadInt32();

                ref TilePropertiesSection tile = ref staticTiles[i];
                tile.Event.EventType = (JJ2Event)(byte)(tileEvent & 0x000000FF);
                tile.Event.Difficulty = (byte)((tileEvent & 0x0000C000) >> 14);
                tile.Event.Illuminate = ((tileEvent & 0x00002000) >> 13 == 1);
                tile.Event.TileParams = (uint)(((tileEvent >> 12) & 0x000FFFF0) | ((tileEvent >> 8) & 0x0000000F));
            }
            for (int i = 0; i < tileCount; ++i) {
                staticTiles[i].Flipped = block.ReadBool();
            }

            for (int i = 0; i < tileCount; ++i) {
                staticTiles[i].Type = block.ReadByte();
            }
        }

        private unsafe void LoadAnimatedTiles(JJ2Block block, bool strictParser)
        {
            animatedTiles = new AnimatedTileSection[animCount];

            for (int i = 0; i < animCount; i++) {
                ref AnimatedTileSection tile = ref animatedTiles[i];
                tile.Delay = block.ReadUInt16();
                tile.DelayJitter = block.ReadUInt16();
                tile.ReverseDelay = block.ReadUInt16();
                tile.IsReverse = block.ReadBool();
                tile.Speed = block.ReadByte(); // 0-70
                tile.FrameCount = block.ReadByte();

                fixed (ushort* frames = tile.Frames) {
                    for (int j = 0; j < 64; j++) {
                        frames[j] = block.ReadUInt16();
                    }
                }
            }
        }

        private void LoadLayerMetadata(JJ2Block block, bool strictParser)
        {
            layers = new LayerSection[JJ2LayerCount];

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Flags = block.ReadUInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Type = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Used = block.ReadBool();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Width = block.ReadInt32();
            }

            // This is related to how data is presented in the file; the above is a WYSIWYG version, solely shown on the UI
            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].InternalWidth = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Height = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].Depth = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].DetailLevel = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].WaveX = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].WaveY = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].SpeedX = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].SpeedY = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].AutoSpeedX = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].AutoSpeedY = block.ReadFloatEncoded();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].TexturedBackgroundType = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].TexturedParams1 = block.ReadByte();
                layers[i].TexturedParams2 = block.ReadByte();
                layers[i].TexturedParams3 = block.ReadByte();
            }
        }

        private void LoadEvents(JJ2Block block, bool strictParser)
        {
            int width = layers[3].Width;
            int height = layers[3].Height;
            events = new TileEventSection[width * height];
            if (width <= 0 && height <= 0) {
                return;
            }

            try {
                for (int y = 0; y < layers[3].Height; y++) {
                    for (int x = 0; x < width; x++) {
                        uint eventData = block.ReadUInt32();

                        ref TileEventSection tileEvent = ref events[x + y * width];
                        tileEvent.EventType = (JJ2Event)(byte)(eventData & 0x000000FF);
                        tileEvent.Difficulty = (byte)((eventData & 0x00000300) >> 8);
                        tileEvent.Illuminate = ((eventData & 0x00000400) >> 10 == 1);
                        tileEvent.TileParams = ((eventData & 0xFFFFF000) >> 12);
                    }
                }
            } catch (Exception) {
                throw new InvalidOperationException("Event block length mismatch");
            }

            ref TileEventSection lastTileEvent = ref events[events.Length - 1];
            if (lastTileEvent.EventType == JJ2Event.EMPTY_255 /*MCE Event*/) {
                hasPit = true;
            }

            for (int i = 0; i < events.Length; i++) {
                if (events[i].EventType == JJ2Event.CTF_BASE) {
                    hasCTF = true;
                } else if (events[i].EventType == JJ2Event.WARP_ORIGIN) {
                    if (((events[i].TileParams >> 16) & 1) == 1) {
                        hasLaps = true;
                    }
                }
            }
        }

        private unsafe void LoadLayers(JJ2Block dictBlock, int dictLength, JJ2Block layoutBlock, bool strictParser)
        {
            DictionaryEntry[] dictionary = new DictionaryEntry[dictLength];
            for (int i = 0; i < dictLength; i++) {
                ref DictionaryEntry entry = ref dictionary[i];

                fixed (ushort* tiles = entry.Tiles) {
                    for (int j = 0; j < 4; j++) {
                        tiles[j] = dictBlock.ReadUInt16();
                    }
                }
            }

            for (int i = 0; i < layers.Length; ++i) {
                ref LayerSection layer = ref layers[i];

                if (layers[i].Used) {
                    layer.Tiles = new ushort[layer.InternalWidth * layer.Height];

                    for (int y = 0; y < layer.Height; y++) {
                        for (int x = 0; x < layer.InternalWidth; x += 4) {
                            ushort dictIdx = layoutBlock.ReadUInt16();

                            fixed (ushort* tiles = dictionary[dictIdx].Tiles) {
                                for (int j = 0; j < 4; j++) {
                                    if (j + x >= layer.Width) {
                                        break;
                                    }

                                    layer.Tiles[j + x + y * layer.InternalWidth] = tiles[j];
                                }
                            }
                        }
                    }
                } else {
                    // Array will be initialized with zeros
                    layer.Tiles = new ushort[layer.Width * layer.Height];
                }
            }
        }

        private void LoadMlleData(JJ2Block block, uint version, bool strictParser)
        {
            if (version != 0x104) {
                Log.Write(LogType.Warning, "Unsupported version (0x" + version.ToString("X") + ") of MLLE stream found in level \"" + levelToken + "\".");
                return;
            }

            bool isSnowing = block.ReadBool();
            bool isSnowingOutdoorsOnly = block.ReadBool();
            byte snowIntensity = block.ReadByte();
            byte snowType = block.ReadByte();

            if (isSnowing) {
                weatherType = (WeatherType)(snowType + 1);
                weatherIntensity = snowIntensity;
                weatherOutdoors = isSnowingOutdoorsOnly;
            }

            bool warpsTransmuteCoins = block.ReadBool(); // ToDo
            bool delayGeneratedCrateOrigins = block.ReadBool(); // ToDo
            int echo = block.ReadInt32(); // ToDo
            int darknessColorRaw = block.ReadInt32();
            float waterChangeSpeed = block.ReadFloat(); // ToDo
            byte waterInteraction = block.ReadByte(); // ToDo
            int waterLayer = block.ReadInt32(); // ToDo
            byte waterLighting = block.ReadByte(); // ToDo
            float waterLevel = block.ReadFloat(); // ToDo
            uint waterGradient1 = block.ReadUInt32(); // ToDo
            uint waterGradient2 = block.ReadUInt32(); // ToDo

            darknessColor = Color.FromArgb(darknessColorRaw);

            bool hasPalette = block.ReadBool();
            if (hasPalette) {
                palette = new Color[256];

                for (int i = 0; i < 256; i++) {
                    byte r = block.ReadByte();
                    byte g = block.ReadByte();
                    byte b = block.ReadByte();
                    palette[i] = Color.FromArgb(0xFF, r, g, b);
                }
            }

            // ToDo
            LoadMlleRecoloring(block); // PINBALL, 0, 4
            LoadMlleRecoloring(block); // PINBALL, 2, 4
            LoadMlleRecoloring(block); // CARROTPOLE, 0, 1
            LoadMlleRecoloring(block); // DIAMPOLE, 0, 1
            LoadMlleRecoloring(block); // PINBALL, 4, 8
            LoadMlleRecoloring(block); // JUNGLEPOLE, 0, 1
            LoadMlleRecoloring(block); // PLUS_SCENERY, 0, 17
            LoadMlleRecoloring(block); // PSYCHPOLE, 0, 1
            LoadMlleRecoloring(block); // SMALTREE, 0, 1
            LoadMlleRecoloring(block); // SNOW, 0, 8
            LoadMlleRecoloring(block); // COMMON, 2, 18

            byte tilesetCount = block.ReadByte();
            if (tilesetCount > 0) {
                extraTilesets = new ExtraTilesetEntry[tilesetCount];

                for (int i = 0; i < tilesetCount; i++) {
                    int tilesetNameLength = block.ReadUint7bitEncoded();
                    extraTilesets[i].Name = block.ReadString(tilesetNameLength, false);

                    extraTilesets[i].Offset = block.ReadUInt16();
                    extraTilesets[i].Count = block.ReadUInt16();

                    // ToDo
                    bool tilesetHasColors = block.ReadBool();
                    if (tilesetHasColors) {
                        for (int j = 0; j < 256; j++) {
                            byte idx = block.ReadByte();
                        }
                    }
                }
            }

            int layerCount = block.ReadInt32();

            Array.Resize(ref layers, layerCount);

            try {
                for (int i = 8; i < layerCount; i += 8) {
                    int index = path.LastIndexOf('.');
                    string extraLayersPath = path.Substring(0, index) + "-MLLE-Data-" + (i / 8) + ".j2l";

                    JJ2Level extraLayersFile = JJ2Level.Open(extraLayersPath, strictParser);

                    for (int j = 0; j < 8 && (i + j) < layerCount; j++) {
                        layers[i + j] = extraLayersFile.layers[j];
                    }
                }
            } catch (Exception ex) {
                Log.Write(LogType.Error, "Cannot load extra layers for level \"" + levelToken + "\". " + ex.Message);
            }

            int nextExtraLayerIdx = 8;
            int[] layerOrder = new int[layerCount];
            for (int i = 0; i < layerCount; i++) {
                sbyte id = unchecked((sbyte)block.ReadByte());
                if (id >= 0) {
                    layerOrder[id] = i;
                } else {
                    layerOrder[nextExtraLayerIdx++] = i;
                }

                int layerNameLength = block.ReadUint7bitEncoded();
                string layerName = block.ReadString(layerNameLength, false);

                bool hideTiles = block.ReadBool();
                byte spriteMode = block.ReadByte();
                byte spriteParam = block.ReadByte();
                int rotationAngle = block.ReadInt32();
                int rotationRadiusMult = block.ReadInt32();
            }

            // Sprite layer has zero depth
            int zeroDepthIdx = layerOrder[3];

            // Adjust depth of all layers
            for (int i = 0; i < layers.Length; i++) {
                int newIdx = layerOrder[i];
                layers[i].Depth = (newIdx - zeroDepthIdx) * 100;

                if (layers[i].Depth < -200) {
                    layers[i].Depth = -200 - (200 - layers[i].Depth) / 20;
                } else if (layers[i].Depth > 300) {
                    layers[i].Depth = 300 + (layers[i].Depth - 300) / 20;
                }
            }

            // ToDo
            ushort imageCount = block.ReadUInt16();
            for (int i = 0; i < imageCount; i++) {
                ushort id = block.ReadUInt16();
                for (int j = 0; j < 32 * 32; j++) {
                    byte idx = block.ReadByte();
                }
            }

            // ToDo
            ushort maskCount = block.ReadUInt16();
            for (int i = 0; i < imageCount; i++) {
                ushort id = block.ReadUInt16();
                for (int j = 0; j < (32 * 32) / 8; j++) {
                    byte idx = block.ReadByte();
                }
            }

            if (warpsTransmuteCoins || delayGeneratedCrateOrigins || imageCount > 0 || maskCount > 0) {
                Log.Write(LogType.Warning, "Unsupported MLLE property found in level \"" + levelToken + "\".");
            }
        }

        private void LoadMlleRecoloring(JJ2Block block)
        {
            bool exists = block.ReadBool();
            if (exists) {
                for (int i = 0; i < 256; i++) {
                    byte idx = block.ReadByte();
                }
            }
        }

        public void Convert(string path, Func<string, LevelToken> levelTokenConversion = null)
        {
            WriteLayer(Path.Combine(path, "Sprite.layer"), layers[3]);
            WriteLayer(Path.Combine(path, "Sky.layer"), layers[7]);

            for (int i = 0; i < layers.Length; i++) {
                if (i != 3 && i != 7) {
                    WriteLayer(Path.Combine(path, (i + 1).ToString(CultureInfo.InvariantCulture) + ".layer"), layers[i]);
                }
            }

            WriteEvents(Path.Combine(path, "Events.layer"), layers[3].Width, layers[3].Height);

            WriteAnimatedTiles(Path.Combine(path, "Animated.tiles"));

            WriteResFile(Path.Combine(path, ".res"), levelTokenConversion);

            WritePalette(Path.Combine(path, "Main.palette"));
        }

        public void AddLevelTokenTextID(ushort textID)
        {
            levelTokenTextIDs.Add(textID);
        }

        private void WriteResFile(string path, Func<string, LevelToken> levelTokenConversion = null)
        {
            const int LayerFormatVersion = 1;
            const int EventSetVersion = 2;

            using (Stream s = File.Create(path))
            using (StreamWriter w = new StreamWriter(s, new UTF8Encoding(false))) {
                w.WriteLine("{");
                w.WriteLine("    \"Version\": {");
                w.WriteLine("        \"Target\": \"Jazz² Resurrection\",");
                w.WriteLine("        \"LayerFormat\": " + LayerFormatVersion.ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("        \"EventSet\": " + EventSetVersion.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("    },");

                w.WriteLine("    \"Description\": {");
                w.WriteLine("        \"Name\": \"" + JJ2Text.ConvertFormattedString(name ?? "", true) + "\",");

                if (!string.IsNullOrEmpty(nextLevel)) {
                    if (nextLevel.EndsWith(".j2l", StringComparison.InvariantCultureIgnoreCase) ||
                        nextLevel.EndsWith(".lev", StringComparison.InvariantCultureIgnoreCase)) {
                        nextLevel = nextLevel.Substring(0, nextLevel.Length - 4);
                    }

                    if (levelTokenConversion != null) {
                        LevelToken token = levelTokenConversion(nextLevel);
                        nextLevel = (token.Episode == null ? "" : token.Episode + "/") + token.Level;
                    }

                    w.WriteLine("        \"NextLevel\": \"" + nextLevel + "\",");
                }

                if (!string.IsNullOrEmpty(secretLevel)) {
                    if (secretLevel.EndsWith(".j2l", StringComparison.InvariantCultureIgnoreCase) ||
                        secretLevel.EndsWith(".lev", StringComparison.InvariantCultureIgnoreCase)) {
                        secretLevel = secretLevel.Substring(0, secretLevel.Length - 4);
                    }

                    if (levelTokenConversion != null) {
                        LevelToken token = levelTokenConversion(secretLevel);
                        secretLevel = (token.Episode == null ? "" : token.Episode + "/") + token.Level;
                    }

                    w.WriteLine("        \"SecretLevel\": \"" + secretLevel + "\",");
                }

                if (!string.IsNullOrEmpty(bonusLevel)) {
                    if (bonusLevel.EndsWith(".j2l", StringComparison.InvariantCultureIgnoreCase) ||
                        bonusLevel.EndsWith(".lev", StringComparison.InvariantCultureIgnoreCase)) {
                        bonusLevel = bonusLevel.Substring(0, bonusLevel.Length - 4);
                    }

                    if (levelTokenConversion != null) {
                        LevelToken token = levelTokenConversion(bonusLevel);
                        bonusLevel = (token.Episode == null ? "" : token.Episode + "/") + token.Level;
                    }

                    if (bonusLevel != secretLevel) {
                        w.WriteLine("        \"BonusLevel\": \"" + bonusLevel + "\",");
                    }
                }

                w.WriteLine();
                if (tileset.EndsWith(".j2t", StringComparison.InvariantCultureIgnoreCase)) {
                    tileset = tileset.Substring(0, tileset.Length - 4);
                }
                w.WriteLine("        \"DefaultTileset\": \"" + tileset.ToLowerInvariant() + "\",");

                if (!string.IsNullOrWhiteSpace(music)) {
                    if (!music.Contains('.')) {
                        music = music + ".j2b";
                    }
                    w.WriteLine("        \"DefaultMusic\": \"" + music.ToLowerInvariant() + "\",");
                }

                w.Write("        \"DefaultLight\": " + (lightingStart * 100 / 64).ToString(CultureInfo.InvariantCulture));

                if (darknessColor.R != 0 || darknessColor.G != 0 || darknessColor.B != 0 || darknessColor.A != 255) {
                    w.WriteLine(",");
                    w.Write("        \"DefaultDarkness\": [ " + darknessColor.R.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                darknessColor.G.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                darknessColor.B.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                darknessColor.A.ToString(CultureInfo.InvariantCulture) + " ]");
                }

                if (weatherType != WeatherType.None) {
                    w.WriteLine(",");
                    w.WriteLine("        \"DefaultWeather\": " + weatherType.ToString("D") + ",");
                    w.Write("        \"DefaultWeatherIntensity\": " + weatherIntensity.ToString(CultureInfo.InvariantCulture));

                    if (weatherOutdoors) {
                        w.WriteLine(",");
                        w.Write("        \"DefaultWeatherOutdoors\": true");
                    }
                }

                LevelHandler.LevelFlags flags = 0;

                if (hasPit) {
                    flags |= LevelHandler.LevelFlags.HasPit;
                }
                if (verticalMPSplitscreen) {
                    flags |= LevelHandler.LevelFlags.FastCamera;
                }
                if (isMpLevel) {
                    flags |= LevelHandler.LevelFlags.Multiplayer;
                    if (hasLaps) {
                        flags |= LevelHandler.LevelFlags.MultiplayerRace;
                    }
                    if (hasCTF) {
                        flags |= LevelHandler.LevelFlags.MultiplayerFlags;
                    }
                }

                if (flags != 0) {
                    w.WriteLine(",");
                    w.WriteLine();
                    w.Write("        \"Flags\": " + flags.ToString("D"));
                }

                w.WriteLine();
                w.WriteLine("    },");

                bool textFound = false;
                for (int i = 0; i < textEventStrings.Count; ++i) {
                    if (!string.IsNullOrEmpty(textEventStrings[i])) {
                        if (textFound) {
                            w.WriteLine(",");
                        } else {
                            textFound = true;
                            w.WriteLine("    \"TextEvents\": {");
                        }

                        string current = textEventStrings[i];

                        if (levelTokenTextIDs.Contains(i)) {
                            string[] tokens = current.Split(new[] { '|' }, StringSplitOptions.None);

                            for (int j = 0; j < tokens.Length; j++) {
                                LevelToken token = levelTokenConversion(tokens[j]);
                                tokens[j] = (token.Episode == null ? "" : token.Episode + ":") + token.Level;
                            }

                            current = string.Join("|", tokens);
                        } else {
                            current = JJ2Text.ConvertFormattedString(current);
                        }

                        w.Write("        \"" + i.ToString(CultureInfo.InvariantCulture) + "\": \"" + current + "\"");
                    }
                }
                if (textFound) {
                    w.WriteLine();
                    w.WriteLine("    },");
                }

                if (extraTilesets != null) {
                    w.WriteLine("    \"Tilesets\": [");

                    for (int i = 0; i < extraTilesets.Length; i++) {
                        if (i > 0) {
                            w.WriteLine(",");
                        }

                        if (extraTilesets[i].Name.EndsWith(".j2t", StringComparison.InvariantCultureIgnoreCase)) {
                            extraTilesets[i].Name = extraTilesets[i].Name.Substring(0, extraTilesets[i].Name.Length - 4);
                        }

                        w.Write("        { \"Name\": \"" + extraTilesets[i].Name.ToLowerInvariant() + "\", \"Offset\": " +
                            extraTilesets[i].Offset.ToString(CultureInfo.InvariantCulture) + ", \"Count\": " +
                            extraTilesets[i].Count.ToString(CultureInfo.InvariantCulture) + " }");
                    }

                    w.WriteLine();
                    w.WriteLine("    ],");
                }

                w.WriteLine("    \"Layers\": {");

                if (layers[7].Used) {
                    WriteResFileLayerSection(w, "Sky", layers[7], true);
                }

                for (int i = 0; i < layers.Length; i++) {
                    if (i != 3 && i != 7 && layers[i].Used) {
                        w.WriteLine(",");
                        WriteResFileLayerSection(w, (i + 1).ToString(CultureInfo.InvariantCulture), layers[i], false);
                    }
                }
                w.WriteLine();

                w.WriteLine("    }");
                w.Write("}");
            }
        }

        private void WriteResFileLayerSection(StreamWriter w, string sectionName, LayerSection layer, bool addBackgroundFields)
        {
            w.WriteLine("        \"" + sectionName + "\": {");

            if (layer.SpeedX != 0 || layer.SpeedY != 0) {
                w.WriteLine("            \"XSpeed\": " + layer.SpeedX.ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("            \"YSpeed\": " + layer.SpeedY.ToString(CultureInfo.InvariantCulture) + ",");
            }

            if (layer.AutoSpeedX != 0 || layer.AutoSpeedY != 0) {
                w.WriteLine("            \"XAutoSpeed\": " + layer.AutoSpeedX.ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("            \"YAutoSpeed\": " + layer.AutoSpeedY.ToString(CultureInfo.InvariantCulture) + ",");
            }

            bool xRepeat = (layer.Flags & 0x00000001) != 0;
            bool yRepeat = (layer.Flags & 0x00000002) != 0;
            bool inherentOffset = (layer.Flags & 0x00000004) != 0;

            if (xRepeat || yRepeat) {
                w.WriteLine("            \"XRepeat\": " + (xRepeat ? "true" : "false") + ",");
                w.WriteLine("            \"YRepeat\": " + (yRepeat ? "true" : "false") + ",");
            }

            w.WriteLine("            \"Depth\": " + layer.Depth.ToString(CultureInfo.InvariantCulture) + ",");
            w.Write("            \"InherentOffset\": " + (inherentOffset ? "true" : "false"));

            if (addBackgroundFields) {
                w.WriteLine(",");

                w.WriteLine("            \"BackgroundStyle\": " + ((layer.Flags & 0x00000008) > 0 ? (layer.TexturedBackgroundType + 1) : 0).ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("            \"BackgroundColor\": [ " + layer.TexturedParams1.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                    layer.TexturedParams2.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                    layer.TexturedParams3.ToString(CultureInfo.InvariantCulture) + " ],");
                w.WriteLine("            \"ParallaxStarsEnabled\": " + ((layer.Flags & 0x00000010) > 0 ? "true" : "false"));
            } else {
                w.WriteLine();
            }

            w.Write("        }");
        }

        private void WriteLayer(string path, LayerSection layer)
        {
            if (!layer.Used) {
                return;
            }

            using (Stream s = File.Create(path))
            using (BinaryWriter w = new BinaryWriter(s)) {

                ushort maxTiles = (ushort)MaxSupportedTiles;
                ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

                w.Write(layer.Width);
                w.Write(layer.Height);

                for (int y = 0; y < layer.Height; ++y) {
                    for (int x = 0; x < layer.Width; ++x) {
                        ushort tileIdx = layer.Tiles[x + y * layer.InternalWidth];

                        bool flipX = false, flipY = false;
                        if ((tileIdx & 0x2000) != 0) {
                            flipY = true;
                            tileIdx -= 0x2000;
                        }

                        if ((tileIdx & ~(maxTiles | (maxTiles - 1))) != 0) {
                            // Fix of bug in updated Psych2.j2l
                            tileIdx = (ushort)((tileIdx & (maxTiles | (maxTiles - 1))) | maxTiles);
                        }

                        // Max. tiles is either 0x0400 or 0x1000 and doubles as a mask to separate flipped tiles.
                        // In J2L, each flipped tile had a separate entry in the tile list, probably to make
                        // the dictionary concept easier to handle.
                        
                        if ((tileIdx & maxTiles) > 0) {
                            flipX = true;
                            tileIdx -= maxTiles;
                        }

                        bool animated = false;
                        if (tileIdx >= lastTilesetTileIndex) {
                            animated = true;
                            tileIdx -= lastTilesetTileIndex;
                        }

                        bool legacyTranslucent = false;
                        bool invisible = false;
                        if (!animated && tileIdx < lastTilesetTileIndex) {
                            legacyTranslucent = (staticTiles[tileIdx].Type == 1);
                            invisible = (staticTiles[tileIdx].Type == 3);
                        }

                        byte tileFlags = 0;
                        if (flipX) {
                            tileFlags |= 0x01;
                        }
                        if (flipY) {
                            tileFlags |= 0x02;
                        }
                        if (animated) {
                            tileFlags |= 0x04;
                        }

                        if (legacyTranslucent) {
                            tileFlags |= 0x10;
                        } else if (invisible) {
                            tileFlags |= 0x20;
                        }

                        w.Write(tileIdx);
                        w.Write(tileFlags);
                    }
                }
            }
        }

        private void WriteEvents(string path, int width, int height)
        {
            unsupportedEvents = new Dictionary<JJ2Event, int>();

            using (Stream s = File.Create(path))
            using (BinaryWriter w = new BinaryWriter(s)) {
                w.Write(width);
                w.Write(height);

                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        ref TileEventSection tileEvent = ref events[x + y * width];

                        int flags = 0;
                        if (tileEvent.Illuminate) flags |= 0x04; // Illuminated
                        if (tileEvent.Difficulty != 2 /*DIFFICULTY_HARD*/) flags |= 0x10; // Difficulty: Easy
                        if (tileEvent.Difficulty == 0 /*DIFFICULTY_ALL*/) flags |= 0x20; // Difficulty: Normal
                        if (tileEvent.Difficulty != 1 /*DIFFICULTY_EASY*/) flags |= 0x40; // Difficulty: Hard
                        if (tileEvent.Difficulty == 3 /*DIFFICULTY_MULTIPLAYER*/) flags |= 0x80; // Multiplayer Only

                        // ToDo: Flag 0x08 not used

                        JJ2Event eventType;
                        int generatorDelay;
                        byte generatorFlags;
                        if (tileEvent.EventType == JJ2Event.MODIFIER_GENERATOR) {
                            // Generators are converted differently
                            ushort[] eventParams = ConvertParamInt(tileEvent.TileParams,
                                Pair.Create(JJ2EventParamType.UInt, 8),  // Event
                                Pair.Create(JJ2EventParamType.UInt, 8),  // Delay
                                Pair.Create(JJ2EventParamType.Bool, 1)); // Initial Delay

                            eventType = (JJ2Event)eventParams[0];
                            generatorDelay = eventParams[1];
                            generatorFlags = (byte)eventParams[2];
                        } else {
                            eventType = tileEvent.EventType;
                            generatorDelay = -1;
                            generatorFlags = 0;
                        }

                        ConversionResult converted = EventConverter.TryConvert(this, eventType, tileEvent.TileParams);

                        // If the event is unsupported or can't be converted, add it to warning list
                        if (eventType != JJ2Event.EMPTY && converted.eventType == EventType.Empty) {
                            int count;
                            unsupportedEvents.TryGetValue(eventType, out count);
                            unsupportedEvents[eventType] = (count + 1);
                        }

                        w.Write((ushort)converted.eventType);
                        if (converted.eventParams == null || converted.eventParams.All(p => p == 0)) {
                            if (generatorDelay == -1) {
                                w.Write((byte)(flags | 0x01 /*NoParams*/));
                            } else {
                                w.Write((byte)(flags | 0x01 /*NoParams*/ | 0x02 /*Generator*/));
                                w.Write((byte)generatorFlags);
                                w.Write((byte)generatorDelay);
                            }
                        } else {
                            if (generatorDelay == -1) {
                                w.Write((byte)flags);
                            } else {
                                w.Write((byte)(flags | 0x02 /*Generator*/));
                                w.Write((byte)generatorFlags);
                                w.Write((byte)generatorDelay);
                            }

                            if (converted.eventParams.Length > 8) {
                                throw new NotSupportedException("Event parameter count must be at most 8");
                            }

                            int i = 0;
                            for (; i < Math.Min(converted.eventParams.Length, 8); i++) {
                                w.Write(converted.eventParams[i]);
                            }
                            for (; i < 8; i++) {
                                w.Write((ushort)0);
                            }
                        }
                    }
                }
            }
        }

        private unsafe void WriteAnimatedTiles(string path)
        {
            ushort maxTiles = (ushort)MaxSupportedTiles;
            ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

            using (Stream s = File.Create(path))
            using (BinaryWriter w = new BinaryWriter(s)) {
                w.Write(animatedTiles.Length);

                for (int i = 0; i < animatedTiles.Length; i++) {
                    ref AnimatedTileSection tile = ref animatedTiles[i];
                    //if (tile.frameCount <= 0) {
                    //    continue;
                    //}

                    w.Write((ushort)tile.FrameCount);

                    fixed (ushort* frames = tile.Frames) {
                        for (int j = 0; j < tile.FrameCount; j++) {
                            // Max. tiles is either 0x0400 or 0x1000 and doubles as a mask to separate flipped tiles.
                            // In J2L, each flipped tile had a separate entry in the tile list, probably to make
                            // the dictionary concept easier to handle.
                            bool flipX = false, flipY = false;
                            ushort tileIdx = frames[j];
                            if ((tileIdx & maxTiles) > 0) {
                                flipX = true;
                                tileIdx -= maxTiles;
                            }

                            if (tileIdx >= lastTilesetTileIndex) {
                                fixed (ushort* fixFrames = animatedTiles[tileIdx - lastTilesetTileIndex].Frames) {
                                    Log.PushIndent();
                                    Log.Write(LogType.Warning, "Level \"" + levelToken + "\" has animated tile in animated tile (" + (tileIdx - lastTilesetTileIndex) + " -> " + fixFrames[0] + ")! Applying quick tile redirection.");
                                    Log.PopIndent();

                                    tileIdx = fixFrames[0];
                                }
                            }

                            byte tileFlags = 0x00;
                            if (flipX) {
                                tileFlags |= 0x01; // Flip X
                            }
                            if (flipY) {
                                tileFlags |= 0x02; // Flip Y
                            }

                            if (staticTiles[frames[j]].Type == 1) {
                                tileFlags |= 0x10; // Legacy Translucent
                            } else if (staticTiles[frames[j]].Type == 3) {
                                tileFlags |= 0x20; // Invisible
                            }

                            w.Write(tileIdx);
                            w.Write(tileFlags);
                        }
                    }

                    byte reverse = (byte)(tile.IsReverse ? 1 : 0);
                    w.Write(tile.Speed);
                    w.Write(tile.Delay);
                    w.Write(tile.DelayJitter);
                    w.Write(reverse);
                    w.Write(tile.ReverseDelay);
                }
            }
        }

        private void WritePalette(string path)
        {
            if (palette != null && palette.Length > 1) {
                using (FileStream s = File.Open(path, FileMode.Create, FileAccess.Write))
                using (BinaryWriter w = new BinaryWriter(s)) {
                    w.Write((ushort)palette.Length);
                    w.Write((int)0); // Empty color
                    for (int i = 1; i < palette.Length; i++) {
                        w.Write((byte)palette[i].R);
                        w.Write((byte)palette[i].G);
                        w.Write((byte)palette[i].B);
                        w.Write((byte)palette[i].A);
                    }
                }
            }
        }
    }
}