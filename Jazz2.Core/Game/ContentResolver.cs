using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game
{
    public class ContentResolver
    {
        #region JSON
        public class MetadataJson
        {
            public class AnimationsSection
            {
                public string Path { get; set; }
                public int Flags { get; set; }

                public int FrameOffset { get; set; }
                public object FrameCount { get; set; }
                public object FrameRate { get; set; }

                public IList<int> States { get; set; }

                public string Shader { get; set; }
                public IList<int> ShaderColors { get; set; }
            }

            public class SoundsSection
            {
                public IList<string> Paths { get; set; }
            }

            public IDictionary<string, AnimationsSection> Animations { get; set; }
            public IDictionary<string, SoundsSection> Sounds { get; set; }
            public IList<string> Preload { get; set; }
            public IList<int> BoundingBox { get; set; }
        }

        private class SpriteJson
        {
            public int Flags { get; set; }
            public IList<int> Coldspot { get; set; }
            public int FrameRate { get; set; }
            public int FrameCount { get; set; }
            public IList<int> FrameConfiguration { get; set; }
            public IList<int> Gunspot { get; set; }
            public IList<int> FrameSize { get; set; }
            public IList<int> Hotspot { get; set; }

            public TextureWrapMode TextureWrap { get; set; }
        }

        private class ShaderJson
        {
            public string Fragment { get; set; }
            public string Vertex { get; set; }
            public BlendMode BlendMode { get; set; }
            public string VertexFormat { get; set; }
        }
        #endregion

        private static ContentResolver current;

        public static ContentResolver Current
        {
            get
            {
                if (current == null) {
                    current = new ContentResolver();
                }
                return current;
            }
        }

        private JsonParser jsonParser;
        private IImageCodec imageCodec;

        private ContentRef<Texture> defaultNormal;

        private Dictionary<string, Metadata> cachedMetadata;
        private Dictionary<string, GenericGraphicResource> cachedGraphics;
        private Dictionary<string, ContentRef<DrawTechnique>> cachedShaders;
        //private Dictionary<string, ContentRef<Sound>> cachedSounds;

        private ContentRef<DrawTechnique> basicNormal;

        private ContentResolver()
        {
            jsonParser = new JsonParser();
            imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);

            defaultNormal = new Texture(new Pixmap(new PixelData(2, 2, new ColorRgba(0.5f, 0.5f, 1f))));

            cachedMetadata = new Dictionary<string, Metadata>();
            cachedGraphics = new Dictionary<string, GenericGraphicResource>();
            cachedShaders = new Dictionary<string, ContentRef<DrawTechnique>>();
            //cachedSounds = new Dictionary<string, ContentRef<Sound>>();

            basicNormal = RequestShader("BasicNormal");
        }

        public void ResetReferenceFlag()
        {
            // ToDo: Do this better somehow...
            /*foreach (var resource in cachedMetadata) {
                resource.Value.Referenced = false;
            }

            foreach (var resource in cachedGraphics) {
                resource.Value.Referenced = false;
            }*/
        }

        public void ReleaseUnreferencedResources()
        {
            // ToDo: Do this better somehow...
            // Clear unreferenced resources
            /*{
                List<string> unreferenced = new List<string>();

                // Metadata
                foreach (var resource in cachedMetadata) {
                    if (!resource.Value.Referenced) {
                        unreferenced.Add(resource.Key);
                    }
                }

                foreach (var key in unreferenced) {
                    cachedMetadata.Remove(key);
                }

                // Graphics
                foreach (var resource in cachedGraphics) {
                    if (!resource.Value.Referenced) {
                        unreferenced.Add(resource.Key);
                    }
                }

                foreach (var key in unreferenced) {
                    cachedGraphics.Remove(key);
                }
            }*/
            cachedMetadata.Clear();
            cachedGraphics.Clear();
            //cachedSounds.Clear();

            // Force GC Collect
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public Metadata RequestMetadata(string path, ColorRgba[] palette)
        {
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Metadata", path + ".res");

                MetadataJson json;
                using (Stream s = DualityApp.SystemBackend.FileSystem.OpenFile(pathAbsolute, FileAccessMode.Read)) {
                    json = jsonParser.Parse<MetadataJson>(s);
                }

                metadata = new Metadata();

                // Pre-load graphics
                if (json.Animations != null) {
                    metadata.Graphics = new Dictionary<string, GraphicResource>();

                    foreach (KeyValuePair<string, MetadataJson.AnimationsSection> g in json.Animations) {
                        ContentRef<DrawTechnique> drawTechnique;
                        if (g.Value.Shader == null) {
                            drawTechnique = basicNormal;
                        } else {
                            drawTechnique = RequestShader(g.Value.Shader);
                        }

                        ColorRgba color;
                        if (g.Value.ShaderColors == null || g.Value.ShaderColors.Count < 4) {
                            color = ColorRgba.White;
                        } else {
                            color = new ColorRgba((byte)g.Value.ShaderColors[0], (byte)g.Value.ShaderColors[1], (byte)g.Value.ShaderColors[2], (byte)g.Value.ShaderColors[3]);
                        }

                        // Create copy of generic resource
                        GraphicResource resource = GraphicResource.From(RequestGraphicResource(g.Value.Path, palette), drawTechnique, color);

                        resource.FrameOffset = g.Value.FrameOffset;

                        string raw1, raw2; int raw3;
                        if ((raw1 = g.Value.FrameCount as string) != null && int.TryParse(raw1, out raw3)) {
                            resource.FrameCount = raw3;
                        } else {
                            resource.FrameCount -= resource.FrameOffset;
                        }
                        if ((raw2 = g.Value.FrameRate as string) != null && int.TryParse(raw2, out raw3)) {
                            resource.FrameDuration = (1f / raw3) * 5; // ToDo: I don't know...
                        }

                        resource.OnlyOnce = (g.Value.Flags & 0x01) != 0;

                        if (g.Value.States != null) {
                            resource.State = new HashSet<AnimState>();
                            for (int i = 0; i < g.Value.States.Count; i++) {
                                resource.State.Add((AnimState)g.Value.States[i]);
                            }
                        }

                        metadata.Graphics[g.Key] = resource;
                    }
                }

                // Pre-load sounds
                if (json.Sounds != null) {
                    metadata.Sounds = new Dictionary<string, SoundResource>();

                    foreach (var s in json.Sounds) {
                        if (s.Value.Paths == null || s.Value.Paths.Count == 0) {
                            continue;
                        }

                        IList<string> filenames = s.Value.Paths;
                        ContentRef<AudioData>[] data = new ContentRef<AudioData>[filenames.Count];
                        for (int i = 0; i < data.Length; i++) {

                            data[i] = new AudioData(FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Animations", filenames[i]), FileAccessMode.Read));
                        }

                        SoundResource resource = new SoundResource();
                        resource.Sound = new Sound(data);
                        metadata.Sounds[s.Key] = resource;
                    }
                }

                // Bounding Box
                if (json.BoundingBox != null && json.BoundingBox.Count == 2) {
                    metadata.BoundingBox = new Point2(json.BoundingBox[0], json.BoundingBox[1]);
                }

                cachedMetadata[path] = metadata;

                // Request children
                if (json.Preload != null) {
                    for (int i = 0; i < json.Preload.Count; i++) {
                        RequestMetadata(json.Preload[i], palette);
                    }
                }
            }

            metadata.Referenced = true;
            return metadata;
        }

        public GenericGraphicResource RequestGraphicResource(string path, ColorRgba[] palette)
        {
            GenericGraphicResource resource;
            if (!cachedGraphics.TryGetValue(path, out resource)) {
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Animations", path);

                SpriteJson json;
                using (Stream s = FileOp.Open(pathAbsolute + ".res", FileAccessMode.Read)) {
                    json = jsonParser.Parse<SpriteJson>(s);
                }

                resource = new GenericGraphicResource {
                    FrameDimensions = new Point2(json.FrameSize[0], json.FrameSize[1]),
                    FrameConfiguration = new Point2(json.FrameConfiguration[0], json.FrameConfiguration[1]),
                    FrameDuration = (1f / json.FrameRate) * 5, // ToDo: I don't know...
                    FrameCount = json.FrameCount
                };

                if (json.Hotspot != null) {
                    resource.Hotspot = new Point2(json.Hotspot[0], json.Hotspot[1]);
                }

                if (json.Coldspot != null) {
                    resource.Coldspot = new Point2(json.Coldspot[0], json.Coldspot[1]);
                    resource.HasColdspot = true;
                }

                if (json.Gunspot != null) {
                    resource.Gunspot = new Point2(json.Gunspot[0], json.Gunspot[1]);
                    resource.HasGunspot = true;
                }


                PixelData pixelData = imageCodec.Read(FileOp.Open(pathAbsolute, FileAccessMode.Read));
                // Use external palette
                if ((json.Flags & 0x01) != 0x00 && palette != null) {
                    ColorRgba[] data = pixelData.Data;
                    Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            int colorIdx = data[i].R;
                            data[i] = palette[colorIdx];

                            // ToDo: Pinball sprites have strange palette (1-3 indices down), CandionV looks bad, other levels look different
                        }
                    });
                }

                Pixmap map = new Pixmap(pixelData);
                map.GenerateAnimAtlas(resource.FrameConfiguration.X, resource.FrameConfiguration.Y, 0);
                resource.Texture = new Texture(map, TextureSizeMode.NonPowerOfTwo, wrapX: json.TextureWrap, wrapY: json.TextureWrap);

                string filenameNormal = pathAbsolute.Replace(".png", ".n.png");
                if (FileOp.Exists(filenameNormal)) {
                    pixelData = imageCodec.Read(FileOp.Open(filenameNormal, FileAccessMode.Read));
                    resource.TextureNormal = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo);
                } else {
                    resource.TextureNormal = defaultNormal;
                }

                cachedGraphics[path] = resource;
            }

            resource.Referenced = true;
            return resource;
        }


        public ContentRef<DrawTechnique> RequestShader(string path)
        {
            switch (path) {
                case "Solid": return DrawTechnique.Solid;
                //case "Add": return DrawTechnique.Add;
                //case "Alpha": return DrawTechnique.Alpha;
                //case "Invert": return DrawTechnique.Invert;
                //case "Light": return DrawTechnique.Light;
                //case "Mask": return DrawTechnique.Mask;
                //case "Multiply": return DrawTechnique.Multiply;
                case "SharpAlpha": return DrawTechnique.SharpAlpha;
                case "Picking": return DrawTechnique.Picking;
                case "SmoothAnim": return DrawTechnique.SmoothAnim_Alpha;
            }

            ContentRef<DrawTechnique> shader;
            if (!cachedShaders.TryGetValue(path, out shader)) {
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Shaders", path + ".res");

                ShaderJson json;
                using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                    json = jsonParser.Parse<ShaderJson>(s);
                }

                if (json.Fragment == null && json.Vertex == null) {
                    switch (json.BlendMode) {
                        default:
                        case BlendMode.Solid: shader = DrawTechnique.Solid; break;
                        case BlendMode.Mask: shader = DrawTechnique.Mask; break;
                        case BlendMode.Add: shader = DrawTechnique.Add; break;
                        case BlendMode.Alpha: shader = DrawTechnique.Alpha; break;
                        case BlendMode.Multiply: shader = DrawTechnique.Multiply; break;
                        case BlendMode.Light: shader = DrawTechnique.Light; break;
                        case BlendMode.Invert: shader = DrawTechnique.Invert; break;
                    }
                } else {
                    ContentRef<VertexShader> vertex;
                    if (json.Vertex == null) {
                        vertex = VertexShader.Minimal;
                    } else if (json.Vertex.StartsWith("#inherit ")) {
                        string parentPath = json.Vertex.Substring(9).Trim();
                        ContentRef<DrawTechnique> parent = RequestShader(parentPath);
                        vertex = parent.Res.Shader.Res.Vertex;
                    } else if (json.Vertex.StartsWith("#include ")) {
                        string includePath = Path.Combine(DualityApp.DataDirectory, "Shaders", json.Vertex.Substring(9).Trim());
                        using (Stream s = FileOp.Open(includePath, FileAccessMode.Read))
                        using (StreamReader r = new StreamReader(s)) {
                            vertex = new VertexShader(r.ReadToEnd());
                        }
                    } else {
                        vertex = new VertexShader(json.Vertex);
                    }

                    ContentRef<FragmentShader> fragment;
                    if (json.Fragment == null) {
                        fragment = FragmentShader.Minimal;
                    } else if (json.Fragment.StartsWith("#inherit ")) {
                        string parentPath = json.Fragment.Substring(9).Trim();
                        ContentRef<DrawTechnique> parent = RequestShader(parentPath);
                        fragment = parent.Res.Shader.Res.Fragment;
                    } else if (json.Fragment.StartsWith("#include ")) {
                        string includePath = Path.Combine(DualityApp.DataDirectory, "Shaders", json.Fragment.Substring(9).Trim());
                        using (Stream s = FileOp.Open(includePath, FileAccessMode.Read))
                        using (StreamReader r = new StreamReader(s)) {
                            fragment = new FragmentShader(r.ReadToEnd());
                        }
                    } else {
                        fragment = new FragmentShader(json.Fragment);
                    }

                    VertexDeclaration vertexFormat;
                    switch (json.VertexFormat) {
                        case "C1P3": vertexFormat = VertexC1P3.Declaration; break;
                        case "C1P3T2": vertexFormat = VertexC1P3T2.Declaration; break;
                        case "C1P3T4A1": vertexFormat = VertexC1P3T4A1.Declaration; break;
                        default: vertexFormat = null; break;
                    }

                    shader = new DrawTechnique(json.BlendMode, new ShaderProgram(vertex, fragment), vertexFormat);
                }

                cachedShaders[path] = shader;
            }

            return shader;
        }
    }
}