using System;
using System.Collections.Generic;
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

        public struct LevelToken
        {
            public string Episode;
            public string Level;
        }

        private const int JJ2LayerCount = 8;

        private string levelToken, name;
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

        private Dictionary<JJ2Event, int> unsupportedEvents;

        public int MaxSupportedTiles => (version == JJ2Version.BaseGame ? 1024 : 4096);
        public int MaxSupportedAnims => (version == JJ2Version.BaseGame ? 128 : 256);

        public string CurrentLevelToken => levelToken;

        public string Tileset => tileset;
        public string Music => music;
        public JJ2Version Version => version;
        public bool VerticalMpSplitscreen => verticalMPSplitscreen;
        public bool IsMpLevel => isMpLevel;

        public Dictionary<JJ2Event, int> UnsupportedEvents => unsupportedEvents;

        public static JJ2Level Open(string path, bool strictParser)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                // Skip copyright notice
                s.Seek(180, SeekOrigin.Current);

                JJ2Level level = new JJ2Level();
                level.levelToken = Path.GetFileNameWithoutExtension(path);

                JJ2Block headerBlock = new JJ2Block(s, 262 - 180);

                uint magic = headerBlock.ReadUInt32();
                if (magic != 0x4C56454C /*LEVL*/) {
                    throw new InvalidOperationException("Invalid magic string");
                }

                uint passwordHash = headerBlock.ReadUInt32();

                bool hasMlleStream = (passwordHash == 0xEFBEBACA);
                // ToDo: Read MLLE stream data

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

                return level;
            }
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
                layers[i].WaveX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].WaveY = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].SpeedX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].SpeedY = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].AutoSpeedX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers[i].AutoSpeedY = block.ReadFloat();
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

        public void Convert(string path, Func<string, LevelToken> levelTokenConversion = null)
        {
            WriteLayer(Path.Combine(path, "Sprite.layer"), layers[3]);
            WriteLayer(Path.Combine(path, "Sky.layer"), layers[7]);

            for (int i = 0; i < 7; i++) {
                if (i != 3) {
                    WriteLayer(Path.Combine(path, (i + 1).ToString(CultureInfo.InvariantCulture) + ".layer"), layers[i]);
                }
            }

            WriteEvents(Path.Combine(path, "Events.layer"), layers[3].Width, layers[3].Height);

            WriteAnimatedTiles(Path.Combine(path, "Animated.tiles"));

            WriteResFile(Path.Combine(path, ".res"), levelTokenConversion);
        }

        public void AddLevelTokenTextID(ushort textID)
        {
            levelTokenTextIDs.Add(textID);
        }

        private void WriteResFile(string filename, Func<string, LevelToken> levelTokenConversion = null)
        {
            const int LayerFormatVersion = 1;
            const int EventSetVersion = 2;

            using (Stream s = File.Create(filename))
            using (StreamWriter w = new StreamWriter(s, new UTF8Encoding(false))) {
                w.WriteLine("{");
                w.WriteLine("    \"Version\": {");
                w.WriteLine("        \"Target\": \"Jazz² Resurrection\",");
                w.WriteLine("        \"LayerFormat\": " + LayerFormatVersion.ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("        \"EventSet\": " + EventSetVersion.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("    },");

                w.WriteLine("    \"Description\": {");
                w.WriteLine("        \"Name\": \"" + ConvertFormattedString(name ?? "", true) + "\",");

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
                            current = ConvertFormattedString(current);
                        }

                        w.Write("        \"" + i.ToString(CultureInfo.InvariantCulture) + "\": \"" + current + "\"");
                    }
                }
                if (textFound) {
                    w.WriteLine();
                    w.WriteLine("    },");
                }

                w.WriteLine("    \"Layers\": {");

                if (layers[7].Used) {
                    WriteResFileLayerSection(w, "Sky", layers[7], true);
                }

                for (int i = 0; i < 7; i++) {
                    if (i != 3 && layers[i].Used) {
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

        private void WriteLayer(string filename, LayerSection layer)
        {
            if (!layer.Used) {
                return;
            }

            using (Stream s = File.Create(filename))
            using (DeflateStream deflate = new DeflateStream(s, CompressionLevel.Optimal))
            using (BinaryWriter w = new BinaryWriter(deflate)) {

                ushort maxTiles = (ushort)MaxSupportedTiles;
                ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

                w.Write(layer.Width);
                w.Write(layer.Height);

                for (int y = 0; y < layer.Height; ++y) {
                    for (int x = 0; x < layer.Width; ++x) {
                        ushort tileIdx = layer.Tiles[x + y * layer.InternalWidth];

                        if ((tileIdx & ~(maxTiles | (maxTiles - 1))) != 0) {
                            // Fix of bug in updated Psych2.j2l
                            tileIdx = (ushort)((tileIdx & (maxTiles | (maxTiles - 1))) | maxTiles);
                        }

                        // Max. tiles is either 0x0400 or 0x1000 and doubles as a mask to separate flipped tiles.
                        // In J2L, each flipped tile had a separate entry in the tile list, probably to make
                        // the dictionary concept easier to handle.
                        bool flipX = false, flipY = false;
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
                        if (!animated && tileIdx < lastTilesetTileIndex) {
                            legacyTranslucent = ((staticTiles[tileIdx].Type & 0x01) != 0);
                        }

                        byte tileFlags = 0;
                        if (flipX)
                            tileFlags |= 0x01;
                        if (flipY)
                            tileFlags |= 0x02;
                        if (animated)
                            tileFlags |= 0x04;
                        if (legacyTranslucent)
                            tileFlags |= 0x80;

                        w.Write(tileIdx);
                        w.Write(tileFlags);
                    }
                }
            }
        }

        private void WriteEvents(string filename, int width, int height)
        {
            unsupportedEvents = new Dictionary<JJ2Event, int>();

            using (Stream s = File.Create(filename))
            using (DeflateStream deflate = new DeflateStream(s, CompressionLevel.Optimal))
            using (BinaryWriter w = new BinaryWriter(deflate)) {
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

        private unsafe void WriteAnimatedTiles(string filename)
        {
            ushort maxTiles = (ushort)MaxSupportedTiles;
            ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

            using (Stream s = File.Create(filename))
            using (DeflateStream ds = new DeflateStream(s, CompressionLevel.Optimal))
            using (BinaryWriter w = new BinaryWriter(ds)) {
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
                            if (flipX)
                                tileFlags |= 0x01; // Flip X
                            if (flipY)
                                tileFlags |= 0x02; // Flip Y
                            if ((staticTiles[frames[j]].Type & 0x01) != 0)
                                tileFlags |= 0x80; // Legacy Translucent

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

        private static string ConvertFormattedString(string current, bool keepColors = true)
        {
            StringBuilder sb = new StringBuilder();
            bool randomColor = false;
            int colorIndex = -1;
            bool colorEmitted = true;
            for (int j = 0; j < current.Length; j++) {
                if (current[j] == '"') {
                    sb.Append("\\\"");
                } else if (current[j] == '@') {
                    // New line
                    sb.Append("\\n");
                } else if (current[j] == '§' && j + 1 < current.Length && char.IsDigit(current[j + 1])) {
                    // Char spacing
                    j++;
                    int spacing = current[j] - '0';
                    int converted = 100 - (spacing * 10);

                    sb.Append("\\f[");
                    sb.Append("w:");
                    sb.Append(converted);
                    sb.Append("]");
                } else if (current[j] == '#') {
                    // Random color
                    colorEmitted = false;
                    randomColor ^= true;
                    colorIndex = -1;
                } else if (current[j] == '~') {
                    // Freeze the active color
                    randomColor = false;
                } else if (current[j] == '|') {
                    // Custom color
                    colorIndex++;
                    colorEmitted = false;
                } else {
                    if (keepColors && !colorEmitted) {
                        colorEmitted = true;
                        sb.Append("\\f[");
                        sb.Append("c:");
                        sb.Append(colorIndex);
                        sb.Append("]");
                    }

                    sb.Append(current[j]);

                    if (randomColor && colorIndex > -1) {
                        colorIndex = -1;
                        colorEmitted = false;
                    }
                }
            }

            return sb.ToString();
        }
    }
}