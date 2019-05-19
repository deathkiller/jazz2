using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Util;
using Duality.Backend;
using Jazz2.Game;
using Jazz2.Storage;

namespace Jazz2.Backend.Android
{
    public class PreferencesBackend : IPreferencesBackend
    {
        private ISharedPreferences sharedPrefs;
        private Dictionary<string, object> data;
        private bool dirty, isFirstRun;

        string IDualityBackend.Id => "SharedPreferencesBackend";

        string IDualityBackend.Name => "Android SharedPreferences";

        int IDualityBackend.Priority => 0;

        bool IPreferencesBackend.IsFirstRun => isFirstRun;

        bool IDualityBackend.CheckAvailable()
        {
            return true;
        }

        void IDualityBackend.Init()
        {
            data = new Dictionary<string, object>();

            sharedPrefs = Application.Context.GetSharedPreferences(App.AssemblyTitle, FileCreationMode.Private);

            string base64 = sharedPrefs.GetString("Root", null);
            if (string.IsNullOrEmpty(base64)) {
                // No preferences found
                isFirstRun = true;
                dirty = true;
                return;
            }

            byte[] buffer = Base64.Decode(base64, Base64Flags.NoPadding | Base64Flags.NoWrap);

            using (BinaryReader r = new BinaryReader(new MemoryStream(buffer, false))) {
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
                    }
                }
            }
        }

        void IDualityBackend.Shutdown()
        {
            if (sharedPrefs != null) {
                ((IPreferencesBackend)this).Commit();

                sharedPrefs.Dispose();
                sharedPrefs = null;
            }
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

            using (MemoryStream s = new MemoryStream()) {
                using (BinaryWriter w = new BinaryWriter(s)) {
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

                            default:
                                App.Log("Unknown preference type: " + pair.Value.GetType().FullName);
                                break;
                        }
                    }
                }

                ArraySegment<byte> buffer;
                if (s.TryGetBuffer(out buffer)) {
                    string base64 = Base64.EncodeToString(buffer.Array, buffer.Offset, buffer.Count, Base64Flags.NoPadding | Base64Flags.NoWrap);

                    ISharedPreferencesEditor editor = sharedPrefs.Edit();

                    editor.PutString("Root", base64);

                    editor.Commit();
                } else {
                    App.Log("Can't get memory buffer to save preferences.");
                }
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
                  value is long ||
                  value is long[] ||
                  value is short ||
                  value is short[])) {
                throw new ArgumentException("The type is not supported: " + value.GetType());
            }
        }
    }
}