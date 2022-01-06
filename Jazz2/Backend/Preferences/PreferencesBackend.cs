using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Duality;
using Duality.Backend;
using Duality.IO;
using Jazz2.Storage;

namespace Jazz2.Backend
{
    public class PreferencesBackend : IPreferencesBackend
    {
        private Dictionary<string, object> data;
        private bool dirty;

        string IDualityBackend.Id => "PreferencesBackend";

        string IDualityBackend.Name => "Preferences";

        int IDualityBackend.Priority => 0;

        bool IDualityBackend.CheckAvailable()
        {
            return true;
        }

        void IDualityBackend.Init()
        {
            data = new Dictionary<string, object>();

            string path = GetSettingsPath();
            if (!FileOp.Exists(path)) {
                dirty = true;
                return;
            }

            try {
                using (Stream s = FileOp.Open(path, FileAccessMode.Read)) {
                    if (!ReadCompressedFormat(s)) {
                        // Reset position and try to read legacy file format
                        s.Position = 0;
                        ReadLegacyFormat(s);
                        dirty = true;
                    }
                }
            } catch (Exception ex) {
                Log.Write(LogType.Error, "Can't load preferences: " + ex);
            }
        }

        void IDualityBackend.Shutdown()
        {
            ((IPreferencesBackend)this).Commit();
        }

        T IPreferencesBackend.Get<T>(string key, T defaultValue)
        {
            object value;
            if (data.TryGetValue(key, out value) && value is T) {
                return (T)value;
            } else {
                return defaultValue;
            }
        }

        void IPreferencesBackend.Set<T>(string key, T value)
        {
            IsTypeSupported<T>(value);

            data[key] = value;

            dirty = true;
        }

        void IPreferencesBackend.Remove(string key)
        {
            data.Remove(key);

            dirty = true;
        }

        void IPreferencesBackend.Commit()
        {
            if (!dirty) {
                return;
            }

            dirty = false;

            string path = GetSettingsPath();

            try {
                DirectoryOp.Create(PathOp.GetDirectoryName(path));
            } catch {
                // Nothing to do...
            }

            try {
                using (Stream s = FileOp.Create(path)) {
                    var Header = new byte[] { 0xEF, 0xBB, 0xBF, 0xF0, 0x9F, 0xA5, 0x95, 0x20 };
                    s.Write(Header, 0, Header.Length);

                    using (DeflateStream d = new DeflateStream(s, CompressionLevel.Optimal, true))
                    using (BinaryWriter w = new BinaryWriter(d, Encoding.UTF8, true)) {
                        w.Write((ushort)data.Count);

                        foreach (var pair in data) {
                            w.Write(pair.Key);

                            switch (pair.Value) {
                                case string value: {
                                    w.Write((byte)0);
                                    w.Write(value);
                                    break;
                                }
                                case bool value: {
                                    w.Write((byte)1);
                                    w.Write(value);
                                    break;
                                }
                                case byte value: {
                                    w.Write((byte)2);
                                    w.Write(value);
                                    break;
                                }
                                case int value: {
                                    w.Write((byte)3);
                                    w.Write(value);
                                    break;
                                }
                                case long value: {
                                    w.Write((byte)4);
                                    w.Write(value);
                                    break;
                                }
                                case short value: {
                                    w.Write((byte)5);
                                    w.Write(value);
                                    break;
                                }
                                case uint value: {
                                    w.Write((byte)6);
                                    w.Write(value);
                                    break;
                                }

                                case string[] value: {
                                    w.Write((byte)10);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case bool[] value: {
                                    w.Write((byte)11);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case byte[] value: {
                                    w.Write((byte)12);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case int[] value: {
                                    w.Write((byte)13);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case long[] value: {
                                    w.Write((byte)14);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case short[] value: {
                                    w.Write((byte)15);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }
                                case uint[] value: {
                                    w.Write((byte)16);
                                    w.Write((byte)value.Length);
                                    for (int j = 0; j < value.Length; j++) {
                                        w.Write(value[j]);
                                    }
                                    break;
                                }

                                default:
                                    Log.Write(LogType.Warning, "Unknown preference type: " + pair.Value.GetType().FullName);
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Log.Write(LogType.Error, "Can't save preferences: " + ex);
            }
        }

        private void IsTypeSupported<T>(T value)
        {
            if (!(value is string ||
                  value is string[] ||
                  value is bool ||
                  value is bool[] ||
                  value is byte ||
                  value is byte[] ||
                  value is int ||
                  value is int[] ||
                  value is uint ||
                  value is uint[] ||
                  value is long ||
                  value is long[] ||
                  value is short ||
                  value is short[])) {
                throw new ArgumentException("The type is not supported: " + value.GetType());
            }
        }

        private static string GetSettingsPath()
        {
            string path = PathOp.Combine(PathOp.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Jazz2.settings");
            if (!FileOp.Exists(path)) {
                path = PathOp.Combine(DualityApp.SystemBackend.GetNamedPath(NamedDirectory.ApplicationData), "Jazz2", "Jazz2.settings");
                if (!FileOp.Exists(path)) {
                    string savedGamesPath = DualityApp.SystemBackend.GetNamedPath(NamedDirectory.SavedGames);
                    if (!string.IsNullOrEmpty(savedGamesPath) && Directory.Exists(savedGamesPath)) {
                        path = PathOp.Combine(savedGamesPath, "Jazz² Resurrection", "Jazz2.settings");
                    }
                }
            }

            return path;
        }

        private bool ReadCompressedFormat(Stream s)
        {
            var header = new byte[8];
            if (s.Read(header, 0, header.Length) != header.Length) {
                return false;
            }

            if (header[0] != 0xEF || header[1] != 0xBB || header[2] != 0xBF || header[3] != 0xF0 ||
                header[4] != 0x9F || header[5] != 0xA5 || header[6] != 0x95 || header[7] != 0x20) {
                return false;
            }

            using (DeflateStream d = new DeflateStream(s, CompressionMode.Decompress, true))
            using (BinaryReader r = new BinaryReader(d, Encoding.UTF8, true)) {
                ushort n = r.ReadUInt16();

                for (ushort i = 0; i < n; i++) {
                    string key = r.ReadString();
                    byte type = r.ReadByte();

                    switch (type) {
                        case 0: data[key] = r.ReadString(); break; // String
                        case 1: data[key] = r.ReadBoolean(); break; // Bool
                        case 2: data[key] = r.ReadByte(); break; // Byte
                        case 3: data[key] = r.ReadInt32(); break; // Int
                        case 4: data[key] = r.ReadInt64(); break; // Long
                        case 5: data[key] = r.ReadInt16(); break; // Short
                        case 6: data[key] = r.ReadUInt32(); break; // Uint

                        case 10: { // String Array
                            byte count = r.ReadByte();
                            string[] values = new string[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadString();
                            }
                            data[key] = values;
                            break;
                        }
                        case 11: { // Bool Array
                            byte count = r.ReadByte();
                            bool[] values = new bool[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadBoolean();
                            }
                            data[key] = values;
                            break;
                        }
                        case 12: { // Byte Array
                            byte count = r.ReadByte();
                            byte[] values = new byte[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadByte();
                            }
                            data[key] = values;
                            break;
                        }
                        case 13: { // Int Array
                            byte count = r.ReadByte();
                            int[] values = new int[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt32();
                            }
                            data[key] = values;
                            break;
                        }
                        case 14: { // Long Array
                            byte count = r.ReadByte();
                            long[] values = new long[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt64();
                            }
                            data[key] = values;
                            break;
                        }
                        case 15: { // Short Array
                            byte count = r.ReadByte();
                            short[] values = new short[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt16();
                            }
                            data[key] = values;
                            break;
                        }
                        case 16: { // Uint Array
                            byte count = r.ReadByte();
                            uint[] values = new uint[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadUInt32();
                            }
                            data[key] = values;
                            break;
                        }
                    }
                }
            }

            return true;
        }

        private void ReadLegacyFormat(Stream s)
        {
            using (BinaryReader r = new BinaryReader(s, Encoding.UTF8, true)) {
                ushort n = r.ReadUInt16();

                for (ushort i = 0; i < n; i++) {
                    string key = r.ReadString();
                    byte type = r.ReadByte();

                    switch (type) {
                        case 0: data[key] = r.ReadString(); break; // String
                        case 1: data[key] = r.ReadBoolean(); break; // Bool
                        case 2: data[key] = r.ReadByte(); break; // Byte
                        case 3: data[key] = r.ReadInt32(); break; // Int
                        case 4: data[key] = r.ReadInt64(); break; // Long
                        case 5: data[key] = r.ReadInt16(); break; // Short
                        case 6: data[key] = r.ReadUInt32(); break; // Uint

                        case 10: { // String Array
                            byte count = r.ReadByte();
                            string[] values = new string[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadString();
                            }
                            data[key] = values;
                            break;
                        }
                        case 11: { // Bool Array
                            byte count = r.ReadByte();
                            bool[] values = new bool[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadBoolean();
                            }
                            data[key] = values;
                            break;
                        }
                        case 12: { // Byte Array
                            byte count = r.ReadByte();
                            byte[] values = new byte[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadByte();
                            }
                            data[key] = values;
                            break;
                        }
                        case 13: { // Int Array
                            byte count = r.ReadByte();
                            int[] values = new int[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt32();
                            }
                            data[key] = values;
                            break;
                        }
                        case 14: { // Long Array
                            byte count = r.ReadByte();
                            long[] values = new long[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt64();
                            }
                            data[key] = values;
                            break;
                        }
                        case 15: { // Short Array
                            byte count = r.ReadByte();
                            short[] values = new short[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadInt16();
                            }
                            data[key] = values;
                            break;
                        }
                        case 16: { // Uint Array
                            byte count = r.ReadByte();
                            uint[] values = new uint[count];
                            for (int j = 0; j < count; j++) {
                                values[j] = r.ReadUInt32();
                            }
                            data[key] = values;
                            break;
                        }
                    }
                }
            }
        }
    }
}