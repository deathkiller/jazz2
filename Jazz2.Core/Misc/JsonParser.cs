// On GitHub: https://github.com/ysharplanguage/FastJsonParser
/*
 * Copyright (c) 2013, 2014, 2015 Cyril Jandia
 *
 * http://www.cjandia.com/
 *
Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
``Software''), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL CYRIL JANDIA BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Cyril Jandia shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from Cyril Jandia. */
#define FASTER_GETPROPINFO

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.Text.Json
{
#if WASM
    public class JsonParser
    {
        private Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        public T Parse<T>(string input)
        {
            using (var sr = new StreamReader(input))
            using (var jtr = new Newtonsoft.Json.JsonTextReader(sr)) {
                return serializer.Deserialize<T>(jtr);
            }
        }

        public T Parse<T>(Stream input)
        {
            using (var sr = new StreamReader(input))
            using (var jtr = new Newtonsoft.Json.JsonTextReader(sr)) {
                return serializer.Deserialize<T>(jtr);
            }
        }
    }
#else
    public sealed class JsonParserOptions
    {
        internal bool Validate()
        {
            return ((StringBufferLength > byte.MaxValue) && (TypeCacheCapacity > byte.MaxValue));
        }

        public int StringBufferLength { get; set; }
        public int TypeCacheCapacity { get; set; }
    }

    public class JsonParser
    {
        private const string TypeTag1 = "__type";
        private const string TypeTag2 = "$type";

        private static readonly byte[] HEX = new byte[128];
        private static readonly bool[] HXD = new bool[128];
        private static readonly char[] ESC = new char[128];
        private static readonly bool[] IDF = new bool[128];
        private static readonly bool[] IDN = new bool[128];
        private const int EOF = (char.MaxValue + 1);
        private const int ANY = 0;

        private readonly IDictionary<Type, int> rtti = new Dictionary<Type, int>();
        private readonly TypeInfo[] types;

        private readonly Func<int, object>[] parse = new Func<int, object>[128];
        private readonly StringBuilder lsb = new StringBuilder();
        private readonly char[] stc = new char[1];
        private readonly char[] lbf;
        private TextReader str;
        private Func<int, int> Char;
        private Action<int> Next;
        private Func<int> Read;
        private Func<int> Space;
        private string txt;
        private readonly int lbs;
        private int len;
        private int lln;
        private int chr;
        private int at;

        internal class EnumInfo
        {
            internal string Name;
            internal object Value;
            internal int Len;
        }

        internal class ItemInfo
        {
            internal string Name;
            internal Action<object, JsonParser, int, int> Set;
            internal Type Type;
            internal int Outer;
            internal int Len;
            internal int Atm;
        }

        internal class TypeInfo
        {
            private static readonly HashSet<Type> WellKnown = new HashSet<Type>();

            internal Func<Type, object, object, int, Func<object, object>> Select;
            internal Func<JsonParser, int, object> Parse;
            internal Func<object> Ctor;
            internal EnumInfo[] Enums;
            internal ItemInfo[] Props;
#if FASTER_GETPROPINFO
            internal char[] Mlk;
            internal int Mnl;
#endif
            internal ItemInfo Dico;
            internal ItemInfo List;
            internal bool IsAnonymous;
            internal bool IsNullable;
            internal bool IsStruct;
            internal bool IsEnum;
            internal bool Closed;
            internal Type VType;
            internal Type EType;
            internal Type Type;
            internal int Inner;
            internal int Key;
            internal int T;

            static TypeInfo()
            {
                WellKnown.Add(typeof(bool));
                WellKnown.Add(typeof(char));
                WellKnown.Add(typeof(sbyte));
                WellKnown.Add(typeof(byte));
                WellKnown.Add(typeof(short));
                WellKnown.Add(typeof(ushort));
                WellKnown.Add(typeof(int));
                WellKnown.Add(typeof(uint));
                WellKnown.Add(typeof(long));
                WellKnown.Add(typeof(ulong));
                WellKnown.Add(typeof(float));
                WellKnown.Add(typeof(double));
                WellKnown.Add(typeof(decimal));
                WellKnown.Add(typeof(Guid));
                WellKnown.Add(typeof(DateTime));
                WellKnown.Add(typeof(DateTimeOffset));
                WellKnown.Add(typeof(string));
            }

            private static Func<object> GetCtor(Type clr, bool list)
            {
                var type = (!list ? ((clr == typeof(object)) ? typeof(Dictionary<string, object>) : clr) : typeof(List<>).MakeGenericType(clr));
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, Type.EmptyTypes, null);
                if (ctor != null)
                {
                    var dyn = new Reflection.Emit.DynamicMethod("", typeof(object), null, typeof(string), true);
                    var il = dyn.GetILGenerator();
                    il.Emit(Reflection.Emit.OpCodes.Newobj, ctor);
                    il.Emit(Reflection.Emit.OpCodes.Ret);
                    return (Func<object>)dyn.CreateDelegate(typeof(Func<object>));
                }
                return null;
            }

            private static Func<object> GetCtor(Type clr, Type key, Type value)
            {
                var type = typeof(Dictionary<,>).MakeGenericType(key, value);
                var ctor = (((type != clr) && clr.IsClass) ? clr : type).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, Type.EmptyTypes, null);
                var dyn = new Reflection.Emit.DynamicMethod("", typeof(object), null, typeof(string), true);
                var il = dyn.GetILGenerator();
                il.Emit(Reflection.Emit.OpCodes.Newobj, ctor);
                il.Emit(Reflection.Emit.OpCodes.Ret);
                return (Func<object>)dyn.CreateDelegate(typeof(Func<object>));
            }

            private static EnumInfo[] GetEnumInfos(Type type)
            {
                var actual = (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)) ? type.GetGenericArguments()[0] : type);
                var einfo = Enum.GetNames(actual).ToDictionary(name => name, name => new EnumInfo {Name = name, Value = Enum.Parse(actual, name), Len = name.Length});
                return einfo.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            }

            private ItemInfo GetItemInfo(Type type, string name, MethodInfo setter)
            {
                var method = new Reflection.Emit.DynamicMethod("Set" + name, null, new[] { typeof(object), typeof(JsonParser), typeof(int), typeof(int) }, typeof(string), true);
                var nType = (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)) ? new[] { type.GetGenericArguments()[0] } : null);
                var parse = GetParserParse(GetParseName(type));
                var il = method.GetILGenerator();
                il.Emit(Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Ldarg_2);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, parse);
                if (type.IsValueType && (parse.ReturnType == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Unbox_Any, type);
                if (parse.ReturnType.IsValueType && (type == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Box, parse.ReturnType);
                if (nType != null)
                {
                    var con = typeof (Nullable<>).MakeGenericType(nType).GetConstructor(nType);
                    if (con != null)
                        il.Emit(Reflection.Emit.OpCodes.Newobj, con);
                }
                il.Emit(Reflection.Emit.OpCodes.Callvirt, setter);
                il.Emit(Reflection.Emit.OpCodes.Ret);
                return new ItemInfo { Type = type, Name = name, Set = (Action<object, JsonParser, int, int>)method.CreateDelegate(typeof(Action<object, JsonParser, int, int>)), Len = name.Length };
            }

            private ItemInfo GetItemInfo(Type type, Type key, Type value, MethodInfo setter)
            {
                var method = new Reflection.Emit.DynamicMethod("Add", null, new[] { typeof(object), typeof(JsonParser), typeof(int), typeof(int) }, typeof(string), true);
                var sBrace = typeof(JsonParser).GetMethod("SBrace", BindingFlags.Instance | BindingFlags.NonPublic);
                var eBrace = typeof(JsonParser).GetMethod("EBrace", BindingFlags.Instance | BindingFlags.NonPublic);
                var kColon = typeof(JsonParser).GetMethod("KColon", BindingFlags.Instance | BindingFlags.NonPublic);
                var sComma = typeof(JsonParser).GetMethod("SComma", BindingFlags.Instance | BindingFlags.NonPublic);
                var vnType = (value.IsGenericType && (value.GetGenericTypeDefinition() == typeof(Nullable<>)) ? new[] { value.GetGenericArguments()[0] } : null);
                var knType = (key.IsGenericType && (key.GetGenericTypeDefinition() == typeof(Nullable<>)) ? new[] { key.GetGenericArguments()[0] } : null);
                var vParse = GetParserParse(GetParseName(value));
                var kParse = GetParserParse(GetParseName(key));
                var il = method.GetILGenerator();
                il.DeclareLocal(key);
                il.DeclareLocal(value);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, sBrace);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, kColon);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Ldarg_3);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, kParse);
                if (key.IsValueType && (kParse.ReturnType == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Unbox_Any, key);
                if (kParse.ReturnType.IsValueType && (key == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Box, kParse.ReturnType);
                if (knType != null)
                {
                    var con = typeof (Nullable<>).MakeGenericType(knType).GetConstructor(knType);
                    if (con!=null)
                        il.Emit(Reflection.Emit.OpCodes.Newobj, con);
                }
                    
                il.Emit(Reflection.Emit.OpCodes.Stloc_0);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, sComma);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, kColon);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Ldarg_2);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, vParse);
                if (value.IsValueType && (vParse.ReturnType == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Unbox_Any, value);
                if (vParse.ReturnType.IsValueType && (value == typeof(object)))
                    il.Emit(Reflection.Emit.OpCodes.Box, vParse.ReturnType);
                if (vnType != null)
                {
                    var con = typeof (Nullable<>).MakeGenericType(vnType).GetConstructor(vnType);
                    if (con != null)
                        il.Emit(Reflection.Emit.OpCodes.Newobj, con);
                }
                il.Emit(Reflection.Emit.OpCodes.Stloc_1);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, eBrace);

                il.Emit(Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(Reflection.Emit.OpCodes.Ldloc_0);
                il.Emit(Reflection.Emit.OpCodes.Ldloc_1);
                il.Emit(Reflection.Emit.OpCodes.Callvirt, setter);
                il.Emit(Reflection.Emit.OpCodes.Ret);
                return new ItemInfo { Type = type, Name = string.Empty, Set = (Action<object, JsonParser, int, int>)method.CreateDelegate(typeof(Action<object, JsonParser, int, int>)) };
            }

            private static Type GetEnumUnderlyingType(Type enumType)
            {
                return enumType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0].FieldType;
            }

            protected string GetParseName(Type type)
            {
                var actual = (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)) ? type.GetGenericArguments()[0] : type);
                var name = (!WellKnown.Contains(actual) ? ((actual.IsEnum && WellKnown.Contains(GetEnumUnderlyingType(actual))) ? GetEnumUnderlyingType(actual).Name : null) : actual.Name);
                return ((name != null) ? string.Concat("Parse", name) : null);
            }

            protected MethodInfo GetParserParse(string pName)
            {
                return typeof(JsonParser).GetMethod((pName ?? "Val"), BindingFlags.Instance | BindingFlags.NonPublic);
            }

            protected TypeInfo(Type type, int self, Type eType, Type kType, Type vType)
            {
                var props = ((self > 2) ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public) : new PropertyInfo[] { });
                var infos = new Dictionary<string, ItemInfo>();
                IsAnonymous = ((eType == null) && (type.Name[0] == '<') && type.IsSealed);
                IsStruct = type.IsValueType;
                IsNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
                IsEnum = IsNullable ? (VType = type.GetGenericArguments()[0]).IsEnum : type.IsEnum;
                EType = eType;
                Type = type;
                T = self;
                if (!IsAnonymous)
                {
                    Ctor = (((kType != null) && (vType != null)) ? GetCtor(Type, kType, vType) : GetCtor((EType ?? Type), (EType != null)));
                    foreach (PropertyInfo property in props)
                    {
                        PropertyInfo pi;
                        MethodInfo set;
                        if ((pi = property).CanWrite && ((set = pi.GetSetMethod()).GetParameters().Length == 1))
                            infos.Add(pi.Name, GetItemInfo(pi.PropertyType, pi.Name, set));
                    }
                    Dico = (((kType != null) && (vType != null)) ? GetItemInfo(Type, kType, vType, typeof(Dictionary<,>).MakeGenericType(kType, vType).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public)) : null);
                    List = ((EType != null) ? GetItemInfo(EType, string.Empty, typeof(List<>).MakeGenericType(EType).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public)) : null);
                    Enums = (IsEnum ? GetEnumInfos(Type) : null);
                }
                else
                {
                    var args = type.GetConstructors()[0].GetParameters();
                    for (var i = 0; i < args.Length; i++) infos.Add(args[i].Name, new ItemInfo { Type = args[i].ParameterType, Name = args[i].Name, Atm = i, Len = args[i].Name.Length });
                }
                Props = infos.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
#if FASTER_GETPROPINFO
                if (Props.Length > 0)
                {
                    Mnl = Props.Max(p => p.Name.Length) + 1;
                    Mlk = new char[Mnl * (Props.Length + 1)];
                    for (var i = 0; i < Props.Length; i++)
                    {
                        var p = Props[i]; var n = p.Name; var l = n.Length;
                        n.CopyTo(0, Mlk, Mnl * i, l);
                    }
                }
                else
                {
                    Mnl = 1;
                    Mlk = new char[1];
                }
#endif
            }
        }

        internal class TypeInfo<T> : TypeInfo
        {
            internal Func<JsonParser, int, T> Value;

            private Func<JsonParser, int, R> GetParseFunc<R>(string pName)
            {
                var parse = GetParserParse(pName ?? "Key");
                if (parse != null)
                {
                    var method = new Reflection.Emit.DynamicMethod(parse.Name, typeof(R), new[] { typeof(JsonParser), typeof(int) }, typeof(string), true);
                    var il = method.GetILGenerator();
                    il.Emit(Reflection.Emit.OpCodes.Ldarg_0);
                    il.Emit(Reflection.Emit.OpCodes.Ldarg_1);
                    il.Emit(Reflection.Emit.OpCodes.Callvirt, parse);
                    il.Emit(Reflection.Emit.OpCodes.Ret);
                    return (Func<JsonParser, int, R>)method.CreateDelegate(typeof(Func<JsonParser, int, R>));
                }
                return null;
            }

            internal TypeInfo(int self, Type eType, Type kType, Type vType)
                : base(typeof(T), self, eType, kType, vType)
            {
                var value = (Value = GetParseFunc<T>(GetParseName(typeof(T))));
                Parse = (parser, outer) => value(parser, outer);
            }
        }

        static JsonParser()
        {
            for (char c = '0'; c <= '9'; c++) { HXD[c] = true; HEX[c] = (byte)(c - 48); }
            for (char c = 'A'; c <= 'F'; c++) { HXD[c] = HXD[c + 32] = true; HEX[c] = HEX[c + 32] = (byte)(c - 55); }
            ESC['/'] = '/'; ESC['\\'] = '\\';
            ESC['b'] = '\b'; ESC['f'] = '\f'; ESC['n'] = '\n'; ESC['r'] = '\r'; ESC['t'] = '\t'; ESC['u'] = 'u';
            for (int c = ANY; c < 128; c++) if (ESC[c] == ANY) ESC[c] = (char)c;
            for (int c = '0'; c <= '9'; c++) IDN[c] = true;
            IDF['_'] = IDN['_'] = true;
            for (int c = 'A'; c <= 'Z'; c++) IDF[c] = IDN[c] = IDF[c + 32] = IDN[c + 32] = true;
        }

        private Exception Error(string message) { return new Exception(string.Format("{0} at {1} (found: '{2}')", message, at, ((chr < EOF) ? ("\\" + chr) : "EOF"))); }
        private void Reset(Func<int> read, Action<int> next, Func<int, int> achar, Func<int> space) { at = -1; chr = ANY; Read = read; Next = next; Char = achar; Space = space; }

        private int StreamSpace()
        {
            if (chr > ' ') 
                return chr;
            while ((chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF) <= ' ')
            {
            }
            return chr;
        }

        private int StreamRead()
        {
            return (chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF);
        }
        private void StreamNext(int ch) { if (chr != ch) throw Error("Unexpected character");
            chr = ((str.Read(stc, 0, 1) > 0) ? stc[0] : EOF);
        }
        private int StreamChar(int ch)
        {
            if (lln >= lbs)
            {
                if (lsb.Length == 0)
                    lsb.Append(new string(lbf, 0, lln));
                lsb.Append((char)ch);
            }
            else
                lbf[lln++] = (char)ch;
            return (chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF);
        }

        private int StringSpace()
        {
            if (chr > ' ') return chr;
            while ((++at < len) && ((chr = txt[at]) <= ' '))
            {
            }
            return chr;
        }

        private int StringRead() { return (chr = (++at < len) ? txt[at] : EOF); }
        private void StringNext(int ch) { if (chr != ch) throw Error("Unexpected character"); chr = ((++at < len) ? txt[at] : EOF); }
        private int StringChar(int ch)
        {
            if (lln >= lbs)
            {
                if (lsb.Length == 0)
                    lsb.Append(new string(lbf, 0, lln));
                lsb.Append((char)ch);
            }
            else
                lbf[lln++] = (char)ch;
            return (chr = (++at < len) ? txt[at] : EOF);
        }

        private int Esc(int ec)
        {
            int cp = 0, ic = -1, ch;
            if (ec == 'u')
            {
                while ((++ic < 4) && ((ch = Read()) <= 'f') && HXD[ch]) { cp *= 16; cp += HEX[ch]; }
                if (ic < 4) throw Error("Invalid Unicode character");
                ch = cp;
            }
            else
                ch = ESC[ec];
            Read();
            return ch;
        }

        private void CharEsc(int ec)
        {
            int cp = 0, ic = -1, ch;
            if (ec == 'u')
            {
                while ((++ic < 4) && ((ch = Read()) <= 'f') && HXD[ch]) { cp *= 16; cp += HEX[ch]; }
                if (ic < 4) throw Error("Invalid Unicode character");
                ch = cp;
            }
            else
                ch = ESC[ec];
            Char(ch);
        }

        private EnumInfo GetEnumInfo(TypeInfo type)
        {
            var a = type.Enums;
            int n = a.Length, c = 0, i = 0;
            if (n <= 0)
                return null;
            while (true)
            {
                int ch;
                if ((ch = chr) == '"')
                {
                    Read();
                    return (((i < n) && (c > 0)) ? a[i] : null);
                }
                bool e = ch == '\\';
                if (e)
                    ch = Read();
                if (ch < EOF)
                {
                    if (!e || (ch >= 128))
                        Read();
                    else
                        ch = Esc(ch);
                }
                else
                    break;
                EnumInfo ei;
                while ((i < n) && ((c >= (ei = a[i]).Len) || (ei.Name[c] != ch))) i++;
                c++;
            }
            return null;
        }

        private bool ParseBoolean(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            if (k)
                ch = Read();
            switch (ch)
            {
                case 'f': Read(); Next('a'); Next('l'); Next('s'); Next('e'); if (k) Next('"'); return false;
                case 't': Read(); Next('r'); Next('u'); Next('e'); if (k) Next('"'); return true;
                default: throw Error("Bad boolean");
            }
        }

        private char ParseChar(int outer)
        {
            int ch = Space();
            if (ch == '"')
            {
                ch = Read();
                lln = 0;
                switch (ch) { case '\\': ch = Read(); CharEsc(ch); Next('"'); break; default: Char(ch); Next('"'); break; }
                return lbf[0];
            }
            throw Error("Bad character");
        }

        private short ParseSByte(int outer)
        {
            sbyte it = 1;
            sbyte n = 0;
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (sbyte)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = (sbyte)-it; }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (sbyte)(it * (ch - 48)); } ch = Read(); }
            if (!b) throw Error("Bad number (sbyte)"); if (k) Next('"');
            return n;
        }

        private byte ParseByte(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            byte n = 0;
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (byte)e.Value;
                }
            }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (byte)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (byte)"); if (k) Next('"');
            return n;
        }

        private short ParseInt16(int outer)
        {
            short it = 1, n = 0;
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (short)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = (short)-it; }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (short)(it * (ch - 48)); } ch = Read(); }
            if (!b) throw Error("Bad number (short)"); if (k) Next('"');
            return n;
        }

        private ushort ParseUInt16(int outer)
        {
            ushort n = 0;
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (ushort)e.Value;
                }
            }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (ushort)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (ushort)"); if (k) Next('"');
            return n;
        }

        private int ParseInt32(int outer)
        {
            int it = 1;
            int n = 0;
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (int)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = -it; }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (it * (ch - 48)); } ch = Read(); }
            if (!b) throw Error("Bad number (int)"); if (k) Next('"');
            return n;
        }

        private uint ParseUInt32(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            uint n = 0;
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (uint)e.Value;
                }
            }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (uint)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (uint)"); if (k) Next('"');
            return n;
        }

        private long ParseInt64(int outer)
        {
            long it = 1, n = 0;
            int ch = Space();
            bool k=(outer > 0) && (ch == '"');
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (long)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = -it; }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += it * (ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (long)"); if (k) Next('"');
            return n;
        }

        private ulong ParseUInt64(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            ulong n = 0;
            if (k)
            {
                ch = Read();
                TypeInfo t;
                if ((t = types[outer]).IsEnum && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(string.Format("Bad enum value ({0})", t.Type.FullName));
                    return (ulong)e.Value;
                }
            }
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) { checked { n *= 10; n += (ulong)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (ulong)"); if (k) Next('"');
            return n;
        }

        private float ParseSingle(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            lsb.Length = 0;
            lln = 0;
            if (k)
                ch = Read();
            if (ch == '-') ch = Char(ch);
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (float)"); if (k) Next('"');
            var s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        private double ParseDouble(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            lsb.Length = 0; lln = 0;
            if (k) 
                ch = Read();
            if (ch == '-') ch = Char(ch);
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (double)"); if (k) Next('"');
            var s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        private decimal ParseDecimal(int outer)
        {
            int ch = Space();
            bool k = (outer > 0) && (ch == '"');
            lsb.Length = 0; lln = 0;
            if (k)
                ch = Read();
            if (ch == '-') ch = Char(ch);
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (decimal)"); if (k) Next('"');
            var s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return decimal.Parse(s, CultureInfo.InvariantCulture);
        }

        private Guid ParseGuid(int outer)
        {
            var s = ParseString(0);
            if (!string.IsNullOrWhiteSpace(s))
                return new Guid(s);
            throw Error("Bad GUID");
        }

        private DateTime ParseDateTime(int outer)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(ParseString(0), CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out dateTime))
                throw Error("Bad date/time");
            return dateTime;
        }

        private DateTimeOffset ParseDateTimeOffset(int outer)
        {
            DateTimeOffset dateTimeOffset;
            if (!DateTimeOffset.TryParse(ParseString(0), CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out dateTimeOffset))
                throw Error("Bad date/time offset");
            return dateTimeOffset;
        }

        private string ParseString(int outer)
        {
            var ch = Space();
            if (ch == '"')
            {
                Read();
                lsb.Length = 0;
                lln = 0;
                while (true)
                {
                    if ((ch = chr) == '"')
                    {
                        Read();
                        return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
                    }
                    bool e = ch == '\\';
                    if (e)
                        ch = Read();
                    if (ch < EOF)
                    {
                        if (!e || (ch >= 128))
                            Char(ch);
                        else
                            CharEsc(ch);


                    }
                    else
                        break;
                }
            }
            if (ch == 'n')
                return (string) Null(0);
            throw Error((outer >= 0) ? "Bad string" : "Bad key");
        }

        private void PastKey()
        {
            var ch = Space();
            if (ch != '"') throw Error("Bad key");
            Read();
            while (true)
            {
                if ((ch = chr) == '"')
                {
                    Read();
                    return;
                }
                bool e = ch == '\\';
                if (e)
                    ch = Read();
                if (ch < EOF)
                {
                    if (!e || (ch >= 128))
                        Read();
                    else
                    {
                        Esc(ch);
                    }
                }
                else break;
            }
            throw Error("Bad key");
        }

#if !FASTER_GETPROPINFO
        private ItemInfo GetPropInfo(ItemInfo[] a)
        {
            int ch = Space(), n = a.Length, c = 0, i = 0;
            if (ch == '"')
            {
                Read();
                while (true)
                {
                    if ((ch = chr) == '"')
                    {
                        Read();
                        return (((i < n) && (c > 0)) ? a[i] : null);
                    }
                    bool e = ch == '\\';
                    if (e)
                        ch = Read();
                    if (ch < EOF)
                    {
                        if (!e || (ch >= 128)) Read();
                        else
                        {
                            ch = Esc(ch);
                        }
                    }
                    else break;
                    ItemInfo pi;
                    while ((i < n) && ((c >= (pi = a[i]).Len) || (pi.Name[c] != ch))) i++;
                    c++;
                }
            }
            throw Error("Bad key");
        }
#else
        private unsafe ItemInfo FasterGetPropInfo(TypeInfo type)
        {
            var p = type.Props; var m = type.Mlk; int ch = Space(), l = type.Mnl, n = p.Length, i = 0;
            if (ch == '"')
            {
                fixed (char* c = m)
                {
                    char* a = c, z = c + n * l;
                    while (i <= n)
                    {
                        if ((ch = Read()) == '"') { Read(); return i < n && c < a ? p[i] : null; }
                        while (*a != ch) { if (z <= a) break; a += l; i++; }
                        a++;
                    }
                    return null;
                }
            }
            throw Error("Bad key");
        }
#endif

        private object Error(int outer) { throw Error("Bad value"); }
        private object Null(int outer) { Read(); Next('u'); Next('l'); Next('l'); return null; }
        private object False(int outer) { Read(); Next('a'); Next('l'); Next('s'); Next('e'); return false; }
        private object True(int outer) { Read(); Next('r'); Next('u'); Next('e'); return true; }

        private object Num(int outer)
        {
            var ch = chr;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            var b = (ch >= '0') && (ch <= '9');
            if (b) while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number");
            return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
        }

        private object Key(int outer) { return ParseString(-1); }

        private object Str(int outer)
        {
            var s = ParseString(0);
            if ((outer != 2) || ((s != null) && (s.Length == 1)))
                return ((outer == 2) ? (object) s[0] : s);
            throw Error("Bad character");
        }

        private static object Cat(TypeInfo atinfo, object[] atargs)
        {
            foreach (var prop in atinfo.Props.Where(prop => prop.Type.IsValueType && (atargs[prop.Atm] == null)))
            {
                atargs[prop.Atm] = Activator.CreateInstance(prop.Type);
            }
            return Activator.CreateInstance(atinfo.Type, atargs);
        }

        private object Parse(int typed)
        {
            if ((Space() != 'n') || !types[typed].IsNullable)
                return (types[typed].Type.IsValueType ? (types[typed].IsNullable ? types[types[typed].Inner].Parse(this, types[typed].Inner) : types[typed].Parse(this, typed)) : Val(typed));
            return Null(0);
        }

        private object Obj(int outer)
        {
            var cached = types[outer]; var isAnon = cached.IsAnonymous; var hash = types[cached.Key]; var select = cached.Select; var props = cached.Props; var ctor = cached.Ctor;
            var atargs = (isAnon ? new object[props.Length] : null);
            var mapper = (null as Func<object, object>);
            var typed = ((outer > 0) && (cached.Dico == null) && ((ctor != null) || isAnon));
            var keyed = hash.T;
            var ch = chr;
            if (ch != '{') throw Error("Bad object");
            Read();
            ch = Space();
            if (ch == '}')
            {
                Read();
                return (isAnon ? Cat(cached, atargs) : ctor());
            }
            object obj = null;
            while (ch < EOF)
            {
#if FASTER_GETPROPINFO
                var prop = (typed ? FasterGetPropInfo(cached) : null);
#else
                var prop = (typed ? GetPropInfo(props) : null);
#endif
                var slot = (!typed ? Parse(keyed) : null);
                Func<object, object> read = null;
                Space();
                Next(':');
                if (slot != null)
                {
                    if ((@select == null) || ((read = @select(cached.Type, obj, slot, -1)) != null))
                    {
                        var val = Parse(cached.Inner);
                        var key = (slot as string);
                        if (obj == null)
                        {
                            if ((key != null) && ((string.CompareOrdinal(key, TypeTag1) == 0) || (string.CompareOrdinal(key, TypeTag2) == 0)))
                            {
                                obj = (((key = (val as string)) != null) ? (cached = types[Entry(Type.GetType(key, true))]).Ctor() : ctor());
                                typed = !(obj is IDictionary);
                            }
                            else
                                obj = ctor();
                        }
                        if (!typed)
                            ((IDictionary)obj).Add(slot, val);
                    }
                    else
                        Val(0);
                }
                else if (prop != null)
                {
                    if (!isAnon)
                    {
                        if ((@select == null) || ((read = @select(cached.Type, obj, prop.Name, -1)) != null))
                        {
                            if ((Space() != 'n') || !types[prop.Outer].IsNullable)
                            {
                                obj = (obj ?? ctor());
                                prop.Set(obj, this, prop.Outer, 0);
                            }
                            else
                                Null(0);
                        }
                        else
                            Val(0);
                    }
                    else
                        atargs[prop.Atm] = Parse(prop.Outer);
                }
                else
                    Val(0);
                mapper = (mapper ?? read);
                ch = Space();
                if (ch == '}')
                {
                    mapper = (mapper ?? Identity);
                    Read();
                    return mapper(isAnon ? Cat(cached, atargs) : (obj ?? ctor()));
                }
                Next(',');
                ch = Space();
            }
            throw Error("Bad object");
        }

        private void SBrace() { Next('{'); }
        private void EBrace() { Space(); Next('}'); }
        private void KColon() { PastKey(); Space(); Next(':'); }
        private void SComma() { Space(); Next(','); }

        private object Arr(int outer)
        {
            var cached = types[(outer != 0) ? outer : 1]; var select = cached.Select; var dico = (cached.Dico != null);
            var mapper = (null as Func<object, object>);
            var items = (dico ? cached.Dico : cached.List);
            var val = cached.Inner;
            var key = cached.Key;
            var ch = chr;
            var i = -1;
            if (ch != '[') throw Error("Bad array");
            Read();
            ch = Space();
            var obj = cached.Ctor();
            if (ch == ']')
            {
                Read();
                if (!cached.Type.IsArray) return obj;
                IList list = (IList)obj;
                var array = Array.CreateInstance(cached.EType, list.Count);
                list.CopyTo(array, 0);
                return array;
            }
            while (ch < EOF)
            {
                Func<object, object> read = null;
                i++;
                if ((ch == 'n') && types[val].IsNullable && !dico)
                {
                    Null(0);
                    ((IList)obj).Add(null);
                }
                else if (dico || (@select == null) || ((read = @select(cached.Type, obj, null, i)) != null))
                    items.Set(obj, this, (types[val].IsNullable ? types[val].Inner : val), (types[key].IsNullable ? types[key].Inner : key));
                else
                    Val(0);
                mapper = (mapper ?? read);
                ch = Space();
                if (ch == ']')
                {
                    mapper = (mapper ?? Identity);
                    Read();
                    if (!cached.Type.IsArray) return mapper(obj);
                    IList list = (IList)obj;
                    var array = Array.CreateInstance(cached.EType, list.Count);
                    list.CopyTo(array, 0);
                    return mapper(array);
                }
                Next(',');
                ch = Space();
            }
            throw Error("Bad array");
        }

        private object Val(int outer)
        {
            return parse[Space() & 0x7f](outer);
        }

        private static Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            if ((type != typeof(string)) && typeof(IEnumerable).IsAssignableFrom(type))
                return (type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object));
            return null;
        }

        private static Type Realizes(Type type, Type generic)
        {
            while (true)
            {
                var itfs = type.GetInterfaces();
                if (itfs.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == generic))
                {
                    return type;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
                    return type;
                if (type.BaseType == null) return null;
                type = type.BaseType;
            }
        }

        private static bool GetKeyValueTypes(Type type, out Type key, out Type value)
        {
            var generic = (Realizes(type, typeof(Dictionary<,>)) ?? Realizes(type, typeof(IDictionary<,>)));
            var kvPair = ((generic != null) && (generic.GetGenericArguments().Length == 2));
            value = (kvPair ? generic.GetGenericArguments()[1] : null);
            key = (kvPair ? generic.GetGenericArguments()[0] : null);
            return kvPair;
        }

        private int Closure(int outer, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            if (types[outer].Closed) return outer;
            var prop = types[outer].Props;
            types[outer].Closed = true;
            foreach (ItemInfo p in prop)
                p.Outer = Entry(p.Type, filter);
            return outer;
        }

        private int Entry(Type type, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter = null)
        {
            Func<Type, object, object, int, Func<object, object>> select;
            int outer;
            if (!rtti.TryGetValue(type, out outer))
            {
                Type kt, vt;
                bool dico = GetKeyValueTypes(type, out kt, out vt);
                var et = (!dico ? GetElementType(type) : null);
                outer = rtti.Count;
                types[outer] = (TypeInfo)Activator.CreateInstance(typeof(TypeInfo<>).MakeGenericType(type), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { outer, et, kt, vt }, null);
                rtti.Add(type, outer);
                types[outer].Inner = ((et != null) ? Entry(et, filter) : (dico ? Entry(vt, filter) : (types[outer].IsNullable ? Entry(types[outer].VType, filter) : 0)));
                if (dico) types[outer].Key = Entry(kt, filter);
            }
            if ((filter != null) && filter.TryGetValue(type, out select))
                types[outer].Select = select;
            return Closure(outer, filter);
        }

        private T DoParse<T>(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            var outer = Entry(typeof(T), filter);
            len = input.Length;
            txt = input;
            Reset(StringRead, StringNext, StringChar, StringSpace);
            return (typeof(T).IsValueType ? ((TypeInfo<T>)types[outer]).Value(this, outer) : (T)Val(outer));
        }

        private T DoParse<T>(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            var outer = Entry(typeof(T), filter);
            str = input;
            Reset(StreamRead, StreamNext, StreamChar, StreamSpace);
            return (typeof(T).IsValueType ? ((TypeInfo<T>)types[outer]).Value(this, outer) : (T)Val(outer));
        }

        public static object Identity(object obj) { return obj; }

        public static readonly Func<object, object> Skip = null;

        public JsonParser() : this(null) { }

        public JsonParser(JsonParserOptions options)
        {
            options = (options ?? new JsonParserOptions { StringBufferLength = (byte.MaxValue + 1), TypeCacheCapacity = (byte.MaxValue + 1) });
            if (!options.Validate()) throw new ArgumentException("Invalid JSON parser options", "options");
            lbf = new char[lbs = options.StringBufferLength];
            types = new TypeInfo[options.TypeCacheCapacity];
            parse['n'] = Null; parse['f'] = False; parse['t'] = True;
            parse['0'] = parse['1'] = parse['2'] = parse['3'] = parse['4'] =
            parse['5'] = parse['6'] = parse['7'] = parse['8'] = parse['9'] =
            parse['-'] = Num; parse['"'] = Str; parse['{'] = Obj; parse['['] = Arr;
            for (var input = 0; input < 128; input++) parse[input] = (parse[input] ?? Error);
            Entry(typeof(object));
            Entry(typeof(List<object>));
            Entry(typeof(char));
        }

        public object Parse(string input) { return Parse<object>(input); }

        public object Parse(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(TextReader input) { return Parse<object>(input); }

        public object Parse(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(Stream input) { return Parse<object>(input); }

        public object Parse(Stream input, Encoding encoding) { return Parse<object>(input, encoding); }

        public object Parse(Stream input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(Stream input, Encoding encoding, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, encoding, mappers); }

        public T Parse<T>(string input) { return Parse(default(T), input); }

        public T Parse<T>(T prototype, string input) { return Parse<T>(input, null); }

        public T Parse<T>(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse(default(T), input, mappers); }

        public T Parse<T>(T prototype, string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input, mappers);
        }

        public T Parse<T>(TextReader input) { return Parse(default(T), input); }

        public T Parse<T>(T prototype, TextReader input) { return Parse<T>(input, null); }

        public T Parse<T>(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse(default(T), input, mappers); }

        public T Parse<T>(T prototype, TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input, mappers);
        }

        public T Parse<T>(Stream input) { return Parse(default(T), input); }

        public T Parse<T>(T prototype, Stream input) { return Parse<T>(input, null as Encoding); }

        public T Parse<T>(Stream input, Encoding encoding) { return Parse(default(T), input, encoding); }

        public T Parse<T>(T prototype, Stream input, Encoding encoding) { return Parse<T>(input, encoding, null); }

        public T Parse<T>(Stream input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse(default(T), input, mappers); }

        public T Parse<T>(T prototype, Stream input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<T>(input, null, mappers); }

        public T Parse<T>(Stream input, Encoding encoding, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse(default(T), input, encoding, mappers); }

        public T Parse<T>(T prototype, Stream input, Encoding encoding, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>((encoding != null) ? new StreamReader(input, encoding) : new StreamReader(input), mappers);
        }
    }
#endif
    }