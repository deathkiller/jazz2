using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Duality;
using Duality.IO;

namespace Jazz2
{
    public static class i18n
    {
        private static IDictionary<string, string> defaultTexts;
        private static IDictionary<string, string> currentTexts;
        private static string currentLanguage;

        public static string[] AvailableLanguages
        {
            get
            {
                List<string> languages = new List<string>();
                string path = PathOp.Combine(DualityApp.DataDirectory, "i18n");
                foreach (string file in DirectoryOp.GetFiles(path, false)) {
                    languages.Add(Path.GetFileNameWithoutExtension(file));
                }
                return languages.ToArray();
            }
        }

        public static string Language
        {
            get
            {
                return currentLanguage;
            }
            set
            {
                if (currentLanguage == value) {
                    return;
                }

                if (value == "en") {
                    currentTexts = null;
                } else {
                    currentTexts = LoadTexts(value);
                }
                currentLanguage = value;
            }
        }

        static i18n()
        {
            defaultTexts = LoadTexts("en");
        }

        private static IDictionary<string, string> LoadTexts(string language)
        {
            string path = PathOp.Combine(DualityApp.DataDirectory, "i18n", language + ".res");
            if (!FileOp.Exists(path)) {
                return null;
            }

            using (Stream s = FileOp.Open(path, FileAccessMode.Read)) {
                JsonParser jsonParser = new JsonParser();
                return jsonParser.Parse<IDictionary<string, string>>(s);
            }
        }

        public static string T(this string key)
        {
            string value;
            if (currentTexts != null && currentTexts.TryGetValue(key, out value)) {
                return value;
            }
            if (defaultTexts != null && defaultTexts.TryGetValue(key, out value)) {
                return value;
            }
            return "[" + key + "]";
        }

        public static string T(this string key, string p1)
        {
            string value = T(key);
            int idx1 = value.IndexOf('{');
            if (idx1 == -1) {
                return value;
            }
            int idx2 = value.IndexOf('}', idx1 + 1);
            if (idx2 == -1) {
                return value;
            }

            return value.Substring(0, idx1) + p1 + value.Substring(idx2 + 1);
        }
    }
}