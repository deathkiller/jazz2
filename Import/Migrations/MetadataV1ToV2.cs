using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Jazz2.Migrations
{
    public class MetadataV1ToV2
    {
        private class MetadataV1Json
        {
            public class GraphicsSection
            {
                public string filename { get; set; }
                public IList<int> states { get; set; }

                public int frameOffset { get; set; }
                public object frameCount { get; set; }
                public object fps { get; set; }

                public bool onlyOnce { get; set; }

                public IList<int> color { get; set; }
                public string shader { get; set; }
            }

            public class SoundsSection
            {
                public IList<string> filenames { get; set; }
            }

            public IDictionary<string, GraphicsSection> graphics { get; set; }
            public IDictionary<string, SoundsSection> sounds { get; set; }
            public IList<string> children { get; set; }
            public IList<int> boundingBox { get; set; }
        }

        private class MetadataV2JsonStub
        {
            public class VersionSection
            {
                public string Target { get; set; }
            }

            public VersionSection Version { get; set; }
        }

        public static bool Convert(string path)
        {
            JsonParser jsonParser = new JsonParser();

            MetadataV2JsonStub jsonV2;
            using (Stream s = File.Open(path, FileMode.Open)) {
                jsonV2 = jsonParser.Parse<MetadataV2JsonStub>(s);
            }

            if (jsonV2.Version != null && !string.IsNullOrEmpty(jsonV2.Version.Target)) {
                return false;
            }

            MetadataV1Json jsonV1;
            using (Stream s = File.Open(path, FileMode.Open)) {
                jsonV1 = jsonParser.Parse<MetadataV1Json>(s);
            }

            if (jsonV1.graphics == null && jsonV1.sounds == null && jsonV1.children == null) {
                Console.WriteLine("[ERROR] Corrupted or empty metadata!");
                return false;
            }

            using (Stream so = File.Create(path))
            using (StreamWriter w = new StreamWriter(so, new UTF8Encoding(false))) {
                w.WriteLine("{");
                w.WriteLine("    \"Version\": {");
                w.WriteLine("        \"Target\": \"Jazz² Resurrection\"");
                w.Write("    }");

                // Misc.
                if (jsonV1.boundingBox != null) {
                    w.WriteLine(",");
                    w.WriteLine();
                    w.Write("    \"BoundingBox\": [ " + string.Join(", ", jsonV1.boundingBox) + " ]");

                    //Console.WriteLine("[WARNING] Metadata has a BoundingBox!");

                    if (jsonV1.boundingBox.Count != 2) {
                        Console.WriteLine("[ERROR] BoundingBox has " + jsonV1.boundingBox.Count + " items!");
                    }
                }

                // Animations
                if (jsonV1.graphics != null && jsonV1.graphics.Count > 0) {
                    w.WriteLine(",");
                    w.WriteLine();
                    w.WriteLine("    \"Animations\": {");

                    bool isFirst = true;
                    foreach (var graphic in jsonV1.graphics) {
                        if (isFirst) {
                            isFirst = false;
                        } else {
                            w.WriteLine(",");
                        }
                        w.WriteLine("        \"" + graphic.Key + "\": {");

                        w.Write("            \"Path\": \"" + graphic.Value.filename + "\"");

                        int flags = 0;
                        if (graphic.Value.onlyOnce)
                            flags |= 1;
                        if (flags != 0) {
                            w.WriteLine(",");
                            w.Write("            \"Flags\": " + flags + "");
                        }

                        if (graphic.Value.frameOffset > 0) {
                            w.WriteLine(",");
                            w.Write("            \"FrameOffset\": " + graphic.Value.frameOffset.ToString() + "");
                        }
                        if (graphic.Value.frameCount != null) {
                            w.WriteLine(",");
                            w.Write("            \"FrameCount\": " + graphic.Value.frameCount.ToString() + "");
                        }
                        if (graphic.Value.fps != null) {
                            w.WriteLine(",");
                            w.Write("            \"FrameRate\": " + graphic.Value.fps.ToString() + "");
                        }
                        if (graphic.Value.states != null && graphic.Value.states.Count > 0) {
                            w.WriteLine(",");
                            w.Write("            \"States\": [ " + string.Join(", ", graphic.Value.states) + " ]");
                        }

                        if (!string.IsNullOrEmpty(graphic.Value.shader)) {
                            w.WriteLine(",");
                            w.Write("            \"Shader\": \"" + string.Join(", ", graphic.Value.shader) + "\"");
                        }
                        if (graphic.Value.color != null) {
                            w.WriteLine(",");
                            w.Write("            \"ShaderColor\": [ " + string.Join(", ", graphic.Value.color) + " ]");

                            if (graphic.Value.color.Count != 4) {
                                Console.WriteLine("[ERROR] Animations[\"" + graphic.Key + "\"].ShaderColor has " + graphic.Value.color.Count + " items!");
                            }
                        }

                        w.WriteLine();
                        w.Write("        }");
                    }

                    w.WriteLine();
                    w.Write("    }");
                }

                // Sounds
                if (jsonV1.sounds != null && jsonV1.sounds.Count > 0) {
                    w.WriteLine(",");
                    w.WriteLine();
                    w.WriteLine("    \"Sounds\": {");

                    bool isFirst = true;
                    foreach (var sound in jsonV1.sounds) {
                        if (sound.Value.filenames == null || sound.Value.filenames.Count == 0) {
                            Console.WriteLine("[ERROR] Sounds[\"" + sound.Key + "\"] has no paths!");
                            continue;
                        }

                        if (isFirst) {
                            isFirst = false;
                        } else {
                            w.WriteLine(",");
                        }
                        w.WriteLine("        \"" + sound.Key + "\": {");

                        w.WriteLine("            \"Paths\": [ " + string.Join("", sound.Value.filenames.Select((p, i) => (i == 0 || sound.Value.filenames.Count == 1) ? "\"" + p + "\"" : ", \"" + p + "\"")) + " ]");

                        w.Write("        }");
                    }

                    w.WriteLine();
                    w.Write("    }");
                }

                // Preload
                if (jsonV1.children != null && jsonV1.children.Count > 0) {
                    w.WriteLine(",");
                    w.WriteLine();
                    w.WriteLine("    \"Preload\": [");

                    w.WriteLine("        " + string.Join("", jsonV1.children.Select((p, i) => (i == 0 || jsonV1.children.Count == 1) ? "\"" + p + "\"" : ", \"" + p + "\"")) + "");

                    w.Write("    ]");

                }

                w.WriteLine();
                w.Write("}");
            }

            return true;
        }
    }
}