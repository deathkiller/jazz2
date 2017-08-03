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
using Console = System.Console;

namespace Jazz2.Compatibility
{
    public class JJ2Level // .j2l
    {
        private struct Jazz2Layer
        {
            public uint flags;                  // all except Parallax Stars supported
            public byte type;                   // ignored
            public bool used;                   // supported
            public int width;                   // supported
            public int internalWidth;           // supported
            public int height;                  // supported
            public int depth;                   // supported
            public byte detailLevel;            // ignored
            public double waveX;                // ignored
            public double waveY;                // ignored
            public double speedX;               // supported
            public double speedY;               // supported
            public double autoSpeedX;           // supported
            public double autoSpeedY;           // supported
            public byte texturedType;           // supported
            public byte texturedParams1;        // supported
            public byte texturedParams2;        // supported
            public byte texturedParams3;        // supported
            public List<List<ushort>> tiles;
        }

        private struct Jazz2TileEvent
        {
            public JJ2Event eventType;          // subset of events supported
            public byte difficulty;             // supported
            public bool illuminate;             // not yet supported
            public uint tileParams;             // supported to some degree
        }

        private struct Jazz2TileProperty
        {
            public Jazz2TileEvent eventType;    // supported
            public bool flipped;                // supported
            public byte type;                   // translucent: supported, caption: ignored
        }

        private struct Jazz2AniTile
        {
            public ushort delay;
            public ushort delayJitter;
            public ushort reverseDelay;
            public bool isReverse;
            public byte speed;
            public byte frameCount;
            public ushort[] frames; // 64
        }

        private struct Jazz2DictionaryEntry
        {
            public ushort[] tiles; // 4
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

        private RawList<Jazz2Layer> layers;
        private RawList<Jazz2TileProperty> staticTiles;
        private RawList<Jazz2AniTile> animatedTiles;
        private RawList<Jazz2TileEvent> events;

        private RawList<string> textEventStrings;
        private HashSet<int> levelTokenTextIDs;

        private JJ2Version version;
        private ushort lightingMin, lightingStart;
        private ushort animCount;
        private bool verticalMPSplitscreen;
        private bool isMpLevel;
        private bool hasPit, hasCTF, hasLaps;

        private Dictionary<JJ2Event, int> unsupportedEvents;

        public ushort MaxSupportedTiles => (ushort)(version == JJ2Version.BaseGame ? 1024 : 4096);
        public ushort MaxSupportedAnims => (ushort)(version == JJ2Version.BaseGame ? 128 : 256);

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
                s.Seek(180, SeekOrigin.Current);

                JJ2Level level = new JJ2Level();
                level.levelToken = Path.GetFileNameWithoutExtension(path);

                JJ2Block headerBlock = new JJ2Block(s, 262 - 180);

                // Read the next four bytes; should spell out "LEVL"
                uint id = headerBlock.ReadUInt32();
                if (id != 0x4C56454C) {
                    throw new InvalidOperationException("Invalid magic number");
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
            staticTiles = new RawList<Jazz2TileProperty>(tileCount);

            for (int i = 0; i < tileCount; ++i) {
                Jazz2TileProperty tileProperties = new Jazz2TileProperty();
                int tileEvent = block.ReadInt32();
                tileProperties.eventType.eventType = (JJ2Event)(byte)(tileEvent & 0x000000FF);
                tileProperties.eventType.difficulty = (byte)((tileEvent & 0x0000C000) >> 14);
                tileProperties.eventType.illuminate = ((tileEvent & 0x00002000) >> 13 == 1);
                tileProperties.eventType.tileParams = (uint)(((tileEvent >> 12) & 0x000FFFF0) | ((tileEvent >> 8) & 0x0000000F));

                staticTiles.Add(tileProperties);
            }
            for (int i = 0; i < tileCount; ++i) {
                staticTiles.Data[i].flipped = block.ReadBool();
            }

            for (int i = 0; i < tileCount; ++i) {
                staticTiles.Data[i].type = block.ReadByte();
            }
        }

        private void LoadAnimatedTiles(JJ2Block block, bool strictParser)
        {
            animatedTiles = new RawList<Jazz2AniTile>(/*maxSupportedAnims*/animCount);

            for (int i = 0; i < /*maxSupportedAnims*/animCount; i++) {
                Jazz2AniTile animatedTile;
                animatedTile.delay = block.ReadUInt16();
                animatedTile.delayJitter = block.ReadUInt16();
                animatedTile.reverseDelay = block.ReadUInt16();
                animatedTile.isReverse = block.ReadBool();
                animatedTile.speed = block.ReadByte(); // 0-70
                animatedTile.frameCount = block.ReadByte();

                animatedTile.frames = new ushort[64];
                for (int j = 0; j < 64; j++) {
                    animatedTile.frames[j] = block.ReadUInt16();
                }
                animatedTiles.Add(animatedTile);
            }
        }

        private void LoadLayerMetadata(JJ2Block block, bool strictParser)
        {
            layers = new RawList<Jazz2Layer>(JJ2LayerCount);

            for (int i = 0; i < JJ2LayerCount; ++i) {
                layers.Add(new Jazz2Layer());
            }

            Jazz2Layer[] data = layers.Data;
            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].flags = block.ReadUInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].type = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].used = block.ReadBool();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].width = block.ReadInt32();
            }

            // This is related to how data is presented in the file; the above is a WYSIWYG version, solely shown on the UI
            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].internalWidth = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].height = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].depth = block.ReadInt32();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].detailLevel = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].waveX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].waveY = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].speedX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].speedY = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].autoSpeedX = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].autoSpeedY = block.ReadFloat();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].texturedType = block.ReadByte();
            }

            for (int i = 0; i < JJ2LayerCount; ++i) {
                data[i].texturedParams1 = block.ReadByte();
                data[i].texturedParams2 = block.ReadByte();
                data[i].texturedParams3 = block.ReadByte();
            }
        }

        private void LoadEvents(JJ2Block block, bool strictParser)
        {
            events = new RawList<Jazz2TileEvent>();

            try {
                for (int i = 0; i < layers[3].height; ++i) {
                    for (int j = 0; j < layers[3].width; ++j) {
                        Jazz2TileEvent tileEvent;
                        uint eventData = block.ReadUInt32();
                        tileEvent.eventType = (JJ2Event)(byte)(eventData & 0x000000FF);
                        tileEvent.difficulty = (byte)((eventData & 0x00000300) >> 8);
                        tileEvent.illuminate = ((eventData & 0x00000400) >> 10 == 1);
                        tileEvent.tileParams = ((eventData & 0xFFFFF000) >> 12);
                        events.Add(tileEvent);
                    }
                }
            } catch (Exception) {
                throw new InvalidOperationException("Event block length mismatch");
            }

            if (events.Count > 0) {
                ref Jazz2TileEvent lastTileEvent = ref events.Data[events.Count - 1];
                if (lastTileEvent.eventType == JJ2Event.JJ2_EMPTY_255 /*MCE Event*/) {
                    hasPit = true;
                }

                for (int i = 0; i < events.Count; i++) {
                    if (events[i].eventType == JJ2Event.JJ2_CTF_BASE) {
                        hasCTF = true;
                    } else if (events[i].eventType == JJ2Event.JJ2_WARP_ORIGIN) {
                        if (((events[i].tileParams >> 16) & 1) == 1) {
                            hasLaps = true;
                        }
                    }
                }
            }
        }

        private void LoadLayers(JJ2Block dictBlock, int dictLength, JJ2Block layoutBlock, bool strictParser)
        {
            List<Jazz2DictionaryEntry> dictionary = new List<Jazz2DictionaryEntry>();
            for (int i = 0; i < dictLength; ++i) {
                Jazz2DictionaryEntry entry;
                entry.tiles = new ushort[4];
                for (int j = 0; j < 4; ++j) {
                    entry.tiles[j] = dictBlock.ReadUInt16();
                }
                dictionary.Add(entry);
            }

            for (int i = 0; i < layers.Count; ++i) {
                layers.Data[i].tiles = new List<List<ushort>>();
                if (layers[i].used) {
                    for (int y = 0; y < layers[i].height; ++y) {
                        List<ushort> currentRow = new List<ushort>();
                        for (int x = 0; x < layers[i].internalWidth; x += 4) {
                            ushort s_dict = layoutBlock.ReadUInt16();
                            for (int j = 0; j < 4; j++) {
                                if (x + j >= layers[i].width) {
                                    break;
                                }
                                currentRow.Add(dictionary[s_dict].tiles[j]);
                            }
                        }
                        layers[i].tiles.Add(currentRow);
                    }
                } else {
                    for (int y = 0; y < layers[i].height; ++y) {
                        List<ushort> currentRow = new List<ushort>();
                        for (int x = 0; x < layers[i].width; ++x) {
                            currentRow.Add(0);
                        }
                        layers[i].tiles.Add(currentRow);
                    }
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

            WriteEvents(Path.Combine(path, "Events.layer"), layers[3].width, layers[3].height);

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
                if (!music.Contains('.')) {
                    music = music + ".j2b";
                }
                w.WriteLine("        \"DefaultMusic\": \"" + music.ToLowerInvariant() + "\",");
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

                if (layers[7].used) {
                    WriteResFileLayerSection(w, "Sky", layers[7], true);
                }

                for (int i = 0; i < 7; i++) {
                    if (i != 3 && layers[i].used) {
                        w.WriteLine(",");
                        WriteResFileLayerSection(w, (i + 1).ToString(CultureInfo.InvariantCulture), layers[i], false);
                    }
                }
                w.WriteLine();

                w.WriteLine("    }");
                w.Write("}");
            }
        }

        private void WriteResFileLayerSection(StreamWriter w, string sectionName, Jazz2Layer layer, bool addBackgroundFields)
        {
            w.WriteLine("        \"" + sectionName + "\": {");
            w.WriteLine("            \"XSpeed\": " + layer.speedX.ToString(CultureInfo.InvariantCulture) + ",");
            w.WriteLine("            \"YSpeed\": " + layer.speedY.ToString(CultureInfo.InvariantCulture) + ",");

            if (layer.autoSpeedX != 0 || layer.autoSpeedY != 0) {
                w.WriteLine("            \"XAutoSpeed\": " + layer.autoSpeedX.ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("            \"YAutoSpeed\": " + layer.autoSpeedY.ToString(CultureInfo.InvariantCulture) + ",");
            }

            w.WriteLine("            \"XRepeat\": " + ((layer.flags & 0x00000001) > 0 ? "true" : "false") + ",");
            w.WriteLine("            \"YRepeat\": " + ((layer.flags & 0x00000002) > 0 ? "true" : "false") + ",");
            w.WriteLine("            \"Depth\": " + layer.depth.ToString(CultureInfo.InvariantCulture) + ",");
            w.Write("            \"InherentOffset\": " + ((layer.flags & 0x00000004) > 0 ? "true" : "false"));

            if (addBackgroundFields) {
                w.WriteLine(",");

                w.WriteLine("            \"BackgroundStyle\": " + ((layer.flags & 0x00000008) > 0 ? (layer.texturedType + 1) : 0).ToString(CultureInfo.InvariantCulture) + ",");
                w.WriteLine("            \"BackgroundColor\": [ " + layer.texturedParams1.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                    layer.texturedParams2.ToString(CultureInfo.InvariantCulture) + ", " +
                                                                    layer.texturedParams3.ToString(CultureInfo.InvariantCulture) + " ],");
                w.WriteLine("            \"ParallaxStarsEnabled\": " + ((layer.flags & 0x00000010) > 0 ? "true" : "false"));
            } else {
                w.WriteLine();
            }

            w.Write("        }");
        }

        private void WriteLayer(string filename, Jazz2Layer layer)
        {
            if (!layer.used) {
                return;
            }

            using (Stream s = File.Create(filename))
            using (DeflateStream deflate = new DeflateStream(s, CompressionLevel.Optimal))
            using (BinaryWriter w = new BinaryWriter(deflate)) {

                ushort maxTiles = MaxSupportedTiles;
                ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

                w.Write(layer.width);
                w.Write(layer.height);

                for (int y = 0; y < layer.height; ++y) {
                    for (int x = 0; x < layer.width; ++x) {
                        ushort tileIdx = layer.tiles[y][x];

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
                            legacyTranslucent = ((staticTiles[tileIdx].type & 0x01) != 0);
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
                        Jazz2TileEvent tileEvent = events[x + y * width];

                        int flags = 0;
                        if (tileEvent.illuminate) flags |= 0x04; // Illuminated
                        if (tileEvent.difficulty != 2 /*DIFFICULTY_HARD*/) flags |= 0x10; // Difficulty: Easy
                        if (tileEvent.difficulty == 0 /*DIFFICULTY_ALL*/) flags |= 0x20; // Difficulty: Normal
                        if (tileEvent.difficulty != 1 /*DIFFICULTY_EASY*/) flags |= 0x40; // Difficulty: Hard
                        if (tileEvent.difficulty == 3 /*DIFFICULTY_MULTIPLAYER*/) flags |= 0x80; // Multiplayer Only

                        // ToDo: Flag 0x08 not used

                        JJ2Event eventType;
                        int generatorDelay;
                        byte generatorFlags;
                        if (tileEvent.eventType == JJ2Event.JJ2_MODIFIER_GENERATOR) {
                            ushort[] eventParams = ConvertParamInt(tileEvent.tileParams,
                                Pair.Create(JJ2EventParamType.UInt, 8),  // Event
                                Pair.Create(JJ2EventParamType.UInt, 8),  // Delay
                                Pair.Create(JJ2EventParamType.Bool, 1)); // Initial Delay

                            eventType = (JJ2Event)eventParams[0];
                            generatorDelay = eventParams[1];
                            generatorFlags = (byte)eventParams[2];
                        } else {
                            eventType = tileEvent.eventType;
                            generatorDelay = -1;
                            generatorFlags = 0;
                        }

                        ConversionResult converted = EventConverter.Convert(this, eventType, tileEvent.tileParams);

                        if (eventType != JJ2Event.JJ2_EMPTY && converted.eventType == EventType.Empty) {
                            int count;
                            unsupportedEvents.TryGetValue(eventType, out count);
                            unsupportedEvents[eventType] = (count + 1);
                        }

                        w.Write((ushort)converted.eventType);
                        if (converted.eventParams == null || converted.eventParams.All(p => p == 0)) {
                            if (generatorDelay == -1) {
                                w.Write((byte)(flags | 0x01));
                            } else {
                                w.Write((byte)(flags | 0x01 | 0x02));
                                w.Write((byte)generatorFlags);
                                w.Write((byte)generatorDelay);
                            }
                        } else {
                            if (generatorDelay == -1) {
                                w.Write((byte)flags);
                            } else {
                                w.Write((byte)(flags | 0x02));
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

        private void WriteAnimatedTiles(string filename)
        {
            ushort maxTiles = MaxSupportedTiles;
            ushort lastTilesetTileIndex = (ushort)(maxTiles - animCount);

            using (Stream s = File.Create(filename))
            using (DeflateStream ds = new DeflateStream(s, CompressionLevel.Optimal))
            using (BinaryWriter w = new BinaryWriter(ds)) {
                w.Write(animatedTiles.Count);

                foreach (Jazz2AniTile tile in animatedTiles) {
                    //if (tile.frameCount <= 0) {
                    //    continue;
                    //}

                    w.Write((ushort)tile.frameCount);

                    for (int i = 0; i < tile.frameCount; i++) {
                        // Max. tiles is either 0x0400 or 0x1000 and doubles as a mask to separate flipped tiles.
                        // In J2L, each flipped tile had a separate entry in the tile list, probably to make
                        // the dictionary concept easier to handle.
                        bool flipX = false, flipY = false;
                        ushort tileIdx = tile.frames[i];
                        if ((tileIdx & maxTiles) > 0) {
                            flipX = true;
                            tileIdx -= maxTiles;
                        }

                        if (tileIdx >= lastTilesetTileIndex) {
                            Console.WriteLine("[" + levelToken + "] Level has animated tile in animated tile (" + (tileIdx - lastTilesetTileIndex) + " -> " + animatedTiles[tileIdx - lastTilesetTileIndex].frames[0] + "). Applying quick redirection!");

                            tileIdx = animatedTiles[tileIdx - lastTilesetTileIndex].frames[0];
                        }

                        byte tileFlags = 0;
                        if (flipX)
                            tileFlags |= 0x01;
                        if (flipY)
                            tileFlags |= 0x02;
                        if ((staticTiles[tile.frames[i]].type & 0x01) != 0)
                            tileFlags |= 0x80;

                        w.Write(tileIdx);
                        w.Write(tileFlags);
                    }

                    byte reverse = (byte)(tile.isReverse ? 1 : 0);
                    w.Write(tile.speed);
                    w.Write(tile.delay);
                    w.Write(tile.delayJitter);
                    w.Write(reverse);
                    w.Write(tile.reverseDelay);
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