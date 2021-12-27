using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duality;

namespace Jazz2.Compatibility
{
    public class JJ2Strings // .j2s
    {
        public struct LevelEntry
        {
            public string LevelName;
            public string[] TextEvents;
        }


        private string[] common;
        private LevelEntry[] levels;

        public unsafe static JJ2Strings Open(string path)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                byte[] internalBuffer = new byte[256];
                JJ2Strings strings = new JJ2Strings();

                int offsetArraySize = s.ReadInt32(ref internalBuffer);

                int textArraySize = s.ReadInt32(ref internalBuffer);
                byte[] textArray = new byte[textArraySize];
                s.Read(textArray, 0, textArraySize);

                strings.common = new string[offsetArraySize];

                for (int i = 0; i < offsetArraySize; i++) {
                    int offset = s.ReadInt32(ref internalBuffer);

                    string text = "";
                    while (textArray[offset] != 0) {
                        text += (char)textArray[offset++];
                    }
                    strings.common[i] = text;
                }

                //Log.Write(LogType.Info, "Found " + strings.common.Length + " common strings");

                int levelEntryCount = s.ReadInt32(ref internalBuffer);

                strings.levels = new LevelEntry[levelEntryCount];

                byte[] counts = new byte[levelEntryCount];
                ushort[] offsets = new ushort[levelEntryCount + 1];

                for (int i = 0; i < levelEntryCount; i++) {
                    s.Read(internalBuffer, 0, 8);

                    string levelName = "";
                    for (int j = 0; j < 8 && internalBuffer[j] != 0; j++) {
                        levelName += (char)internalBuffer[j];
                    }

                    strings.levels[i].LevelName = levelName;

                    s.ReadUInt8(ref internalBuffer);
                    counts[i] = s.ReadUInt8(ref internalBuffer);
                    offsets[i] = s.ReadUInt16(ref internalBuffer);
                }

                int textArray2Size = s.ReadInt32(ref internalBuffer);
                offsets[offsets.Length - 1] = (ushort)textArray2Size;

                int k = 0;
                for (int i = 0; i < levelEntryCount; i++) {
                    ushort offset = offsets[i + 1];
                    byte count = counts[i];

                    strings.levels[i].TextEvents = new string[count + 1];

                    int l = 0;
                    while (k < offset) {
                        int index = s.ReadUInt8(ref internalBuffer) % 16;
                        k++;
                        byte size = s.ReadUInt8(ref internalBuffer);
                        k++;

                        s.Read(internalBuffer, 0, size);
                        k += size;

                        string text = "";
                        for (int j = 0; j < size; j++) {
                            text += (char)internalBuffer[j];
                        }
                        strings.levels[i].TextEvents[index] = text;
                        l++;
                    }

                    //Log.Write(LogType.Info, "Found " + strings.levels[i].TextEvents.Length + " strings for level " + strings.levels[i].LevelName);
                }

                return strings;
            }
        }

        private JJ2Strings()
        {
        }

        public void Convert(string path, string langSuffix, Dictionary<string, Tuple<string, string>> knownLevels, bool onlyExisting)
        {
            if (levels.Length == 0) {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Translation \"" + langSuffix + "\" contains ");

            for (int i = 0; i < levels.Length; i++) {
                if (levels[i].TextEvents.Length == 0) {
                    continue;
                }

                string levelToken = levels[i].LevelName.ToLower().Replace(" ", "_").Replace("\"", "").Replace("'", "");

                if (i != 0) {
                    sb.Append(", ");
                }

                sb.Append(levelToken).Append(" (").Append(levels[i].TextEvents.Length).Append(")");

                string targetPathInner = Path.Combine(path, "Content", "Episodes");
                Tuple<string, string> knownLevel;
                if (knownLevels.TryGetValue(levelToken, out knownLevel)) {
                    if (string.IsNullOrEmpty(knownLevel.Item2)) {
                        targetPathInner = Path.Combine(targetPathInner, knownLevel.Item1, levelToken);
                    } else {
                        targetPathInner = Path.Combine(targetPathInner, knownLevel.Item1, knownLevel.Item2 + "_" + levelToken);
                    }
                } else {
                    targetPathInner = Path.Combine(targetPathInner, "unknown", levelToken);
                }

                if (!onlyExisting) {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPathInner));
                } else if (!File.Exists(targetPathInner + ".level") || File.Exists(targetPathInner + ".level." + langSuffix)) {
                    // Don't overwrite existing translations and skip missing levels
                    continue;
                }

                WriteResFile(levels[i], targetPathInner + ".level." + langSuffix);
            }

            Log.Write(LogType.Info, sb.ToString());
        }

        private void WriteResFile(LevelEntry level, string path)
        {
            int textEventsCount = 0;
            for (int i = 0; i < level.TextEvents.Length; i++) {
                if (!string.IsNullOrEmpty(level.TextEvents[i])) {
                    textEventsCount = i + 1;
                }
            }
            if (textEventsCount <= 0) {
                return;
            }

            using (Stream s = File.Create(path))
            using (StreamWriter w = new StreamWriter(s, new UTF8Encoding(false))) {
                w.WriteLine("{");
                w.WriteLine("    \"TextEvents\": [");
                for (int i = 0; i < textEventsCount; i++) {
                    if (i != 0) {
                        w.WriteLine(",");
                    }

                    if (!string.IsNullOrEmpty(level.TextEvents[i])) {
                        string current = JJ2Text.ConvertToFormattedString(level.TextEvents[i]);
                        w.Write("        \"" + current + "\"");
                    } else {
                        w.Write("        null");
                    }
                }
                w.WriteLine();
                w.WriteLine("    ]");
                w.Write("}");
            }
        }
    }
}