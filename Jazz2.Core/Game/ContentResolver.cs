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
using Jazz2.Storage.Content;

namespace Jazz2.Game
{
    public partial class ContentResolver
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
                public IList<int> ShaderColor { get; set; }
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

        private ContentRef<Texture> defaultNormalMap;

        private ConcurrentDictionary<string, Metadata> cachedMetadata;
        private Dictionary<string, GenericGraphicResource> cachedGraphics;
        private Dictionary<string, ContentRef<DrawTechnique>> cachedShaders;
        //private Dictionary<string, ContentRef<Sound>> cachedSounds;

        private ContentRef<DrawTechnique> basicNormal, paletteNormal;
        private int requestShaderNesting;

        public ContentRef<Texture> DefaultNormalMap => defaultNormalMap;

        private ContentResolver()
        {
            jsonParser = new JsonParser();
            imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);

#if !UNCOMPRESSED_CONTENT
            string dz = PathOp.Combine(DualityApp.DataDirectory, "Main.dz");
            PathOp.Mount(dz, new CompressedContent(dz));
#endif

            defaultNormalMap = new Texture(new Pixmap(new PixelData(2, 2, new ColorRgba(0.5f, 0.5f, 1f))), TextureSizeMode.Default, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            defaultNormalMap.Res.DetachPixmap();

            cachedMetadata = new ConcurrentDictionary<string, Metadata>(2, 31);
            cachedGraphics = new Dictionary<string, GenericGraphicResource>();
            cachedShaders = new Dictionary<string, ContentRef<DrawTechnique>>();
            //cachedSounds = new Dictionary<string, ContentRef<Sound>>();

            basicNormal = RequestShader("BasicNormal");
            paletteNormal = RequestShader("PaletteNormal");

            AllowAsyncLoading();
        }

        private void OnDualityAppTerminating(object sender, EventArgs e)
        {
            DualityApp.Terminating -= OnDualityAppTerminating;

            asyncThread = null;

            lock (metadataAsyncRequests) {
                metadataAsyncRequests.Clear();
            }

            asyncThreadEvent.Set();
            asyncResourceReadyEvent.Set();

            // Release this static instance
            current = null;
        }

        public void ResetReferenceFlag()
        {
            foreach (var resource in cachedMetadata) {
                resource.Value.Referenced = false;
            }

            foreach (var resource in cachedGraphics) {
                resource.Value.Referenced = false;
            }
        }

        public void ReleaseUnreferencedResources()
        {
            // Clear unreferenced resources
            {
                // Metadata
                List<string> unreferenced = new List<string>();
                foreach (var resource in cachedMetadata) {
                    if (!resource.Value.Referenced) {
                        unreferenced.Add(resource.Key);
                    }
                }

                foreach (string path in unreferenced) {
                    Metadata metadata;
                    cachedMetadata.TryRemove(path, out metadata);

                    if (metadata.Sounds != null) {
                        foreach (var sound in metadata.Sounds) {
                            sound.Value.Sound.Res?.Dispose();
                        }
                    }

                    metadata.Graphics = null;
                    metadata.Sounds = null;

                    //System.Diagnostics.Debug.WriteLine("Releasing metadata \"" + path + "\"...");
                }

                // Graphics
                unreferenced.Clear();
                foreach (var resource in cachedGraphics) {
                    if (!resource.Value.Referenced) {
                        unreferenced.Add(resource.Key);

                        resource.Value.AsyncFinalize = null;

                        resource.Value.Texture.Res?.DisposeLater();
                        resource.Value.TextureNormal.Res?.DisposeLater();
                    }
                }

                foreach (string path in unreferenced) {
                    cachedGraphics.Remove(path);

                    //System.Diagnostics.Debug.WriteLine("Releasing graphics \"" + path + "\"...");
                }
            }

            // Force GC Collect
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public Metadata RequestMetadata(string path)
        {
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (metadataAsyncRequests.Add(path)) {
                        asyncThreadEvent.Set();
                    }
                }

                do {
                    asyncResourceReadyEvent.WaitOne();
                } while (!cachedMetadata.TryGetValue(path, out metadata));
            } else {
                MarkAsReferenced(metadata);
            }

            if (metadata.AsyncFinalizingRequired) {
                metadata.AsyncFinalizingRequired = false;
                FinalizeAsyncLoadedResources(metadata);
            }

            return metadata;
        }

        private void MarkAsReferenced(Metadata metadata)
        {
            metadata.Referenced = true;

            if (metadata.Graphics != null) {
                foreach (var res in metadata.Graphics) {
                    res.Value.Base.Referenced = true;
                }
            }
        }

        private Metadata RequestMetadataInner(string path, bool async)
        {
            //System.Diagnostics.Debug.WriteLine("Loading metadata \"" + path + "\"...");

#if UNCOMPRESSED_CONTENT
            string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Metadata", path + ".res");
#else
            string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Metadata", path + ".res");
#endif

            MetadataJson json;
            using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                lock (jsonParser) {
                    json = jsonParser.Parse<MetadataJson>(s);
                }
            }

            Metadata metadata = new Metadata();
            metadata.Referenced = true;
            metadata.AsyncFinalizingRequired = async;

            // Pre-load graphics
            if (json.Animations != null) {
                metadata.Graphics = new Dictionary<string, GraphicResource>();

                foreach (KeyValuePair<string, MetadataJson.AnimationsSection> g in json.Animations) {
                    if (g.Value.Path == null) {
                        // No path provided, skip resource...
                        continue;
                    }

#if !THROW_ON_MISSING_RESOURCES
                    try {
#endif
                        bool isIndexed = (g.Value.Flags & 0x02) != 0x00;

                        ColorRgba color;
                        if (g.Value.ShaderColor == null || g.Value.ShaderColor.Count < 4) {
                            color = (isIndexed ? new ColorRgba(0, 255) : ColorRgba.White);
                        } else {
                            color = new ColorRgba((byte)g.Value.ShaderColor[0], (byte)g.Value.ShaderColor[1], (byte)g.Value.ShaderColor[2], (byte)g.Value.ShaderColor[3]);
                        }

                        GenericGraphicResource resBase = RequestGraphicResource(g.Value.Path, async);

                        // Create copy of generic resource
                        GraphicResource res;
                        if (async) {
                            res = GraphicResource.From(resBase, g.Value.Shader, color, isIndexed);
                        } else {
                            ContentRef<DrawTechnique> drawTechnique;
                            if (g.Value.Shader == null) {
                                drawTechnique = (isIndexed ? paletteNormal : basicNormal);
                            } else {
                                drawTechnique = RequestShader(g.Value.Shader);
                            }

                            res = GraphicResource.From(resBase, drawTechnique, color, isIndexed, paletteTexture);
                        }

                        res.FrameOffset = g.Value.FrameOffset;

                        string raw1, raw2; int raw3;
                        if ((raw1 = g.Value.FrameCount as string) != null && int.TryParse(raw1, out raw3)) {
                            res.FrameCount = raw3;
                        } else {
                            res.FrameCount -= res.FrameOffset;
                        }
                        if ((raw2 = g.Value.FrameRate as string) != null && int.TryParse(raw2, out raw3)) {
                            res.FrameDuration = (1f / raw3) * 5; // ToDo: I don't know...
                        }

                        res.OnlyOnce = (g.Value.Flags & 0x01) != 0x00;

                        if (g.Value.States != null) {
                            res.State = new HashSet<AnimState>();
                            for (int i = 0; i < g.Value.States.Count; i++) {
                                res.State.Add((AnimState)g.Value.States[i]);
                            }
                        }

                        metadata.Graphics[g.Key] = res;
#if !THROW_ON_MISSING_RESOURCES
                    } catch (Exception ex) {
                        App.Log("Can't load animation \"" + g.Key + "\" from metadata \"" + path + "\": " + ex.Message);
                    }
#endif
                }
            }

            // Pre-load sounds
            if (json.Sounds != null) {
                metadata.Sounds = new Dictionary<string, SoundResource>();

                foreach (var s in json.Sounds) {
                    if (s.Value.Paths == null || s.Value.Paths.Count == 0) {
                        // No path provided, skip resource...
                        continue;
                    }

#if !THROW_ON_MISSING_RESOURCES
                    try {
#endif
                        IList<string> filenames = s.Value.Paths;
                        ContentRef<AudioData>[] data = new ContentRef<AudioData>[filenames.Count];
                        for (int i = 0; i < data.Length; i++) {
#if UNCOMPRESSED_CONTENT
                            data[i] = new AudioData(FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Animations", filenames[i]), FileAccessMode.Read));
#else
                            data[i] = new AudioData(FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", filenames[i]), FileAccessMode.Read));
#endif
                        }

                        SoundResource resource = new SoundResource();
                        resource.Sound = new Sound(data);
                        metadata.Sounds[s.Key] = resource;
#if !THROW_ON_MISSING_RESOURCES
                    } catch (Exception ex) {
                        App.Log("Can't load sound \"" + s.Key + "\" from metadata \"" + path + "\": " + ex.Message);
                    }
#endif
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
                    PreloadAsync(json.Preload[i]);
                }
            }

            return metadata;
        }

        public GenericGraphicResource RequestGraphicResource(string path, bool async = false)
        {
            GenericGraphicResource resource;
            if (!cachedGraphics.TryGetValue(path, out resource)) {
#if UNCOMPRESSED_CONTENT
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Animations", path);
#else
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", path);
#endif

                SpriteJson json;
                using (Stream s = FileOp.Open(pathAbsolute + ".res", FileAccessMode.Read)) {
                    lock (jsonParser) {
                        json = jsonParser.Parse<SpriteJson>(s);
                    }
                }

                resource = new GenericGraphicResource {
                    FrameDimensions = new Point2(json.FrameSize[0], json.FrameSize[1]),
                    FrameConfiguration = new Point2(json.FrameConfiguration[0], json.FrameConfiguration[1]),
                    FrameDuration = (1f / json.FrameRate) * 5,
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

                PixelData pixelData;
                using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                    pixelData = imageCodec.Read(s);
                }

                // Use external palette
                if ((json.Flags & 0x01) != 0x00) {
                    ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.PixelData[0].Data;

                    ColorRgba[] data = pixelData.Data;
                    Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            int colorIdx = data[i].R;
                            data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));

                            // ToDo: Pinball sprites have strange palette (1-3 indices down), CandionV looks bad, other levels look different
                        }
                    });
                }

                bool linearSampling = (json.Flags & 0x02) != 0x00;

                Pixmap map = new Pixmap(pixelData);
                map.GenerateAnimAtlas(resource.FrameConfiguration.X, resource.FrameConfiguration.Y, 0);
                if (async) {
                    GenericGraphicResourceAsyncFinalize asyncFinalize = new GenericGraphicResourceAsyncFinalize();
                    asyncFinalize.TextureMap = map;

                    string filenameNormal = pathAbsolute.Replace(".png", ".n.png");
                    if (FileOp.Exists(filenameNormal)) {
                        asyncFinalize.TextureNormalMap = new Pixmap(imageCodec.Read(FileOp.Open(filenameNormal, FileAccessMode.Read)));
                    } else {
                        resource.TextureNormal = defaultNormalMap;
                    }

                    asyncFinalize.TextureWrap = json.TextureWrap;
                    asyncFinalize.LinearSampling = linearSampling;

                    resource.AsyncFinalize = asyncFinalize;
                } else {
                    TextureMagFilter magFilter; TextureMinFilter minFilter;
                    if (linearSampling) {
                        magFilter = TextureMagFilter.Linear;
                        minFilter = TextureMinFilter.LinearMipmapLinear;
                    } else {
                        magFilter = TextureMagFilter.Nearest;
                        minFilter = TextureMinFilter.Nearest;
                    }

                    resource.Texture = new Texture(map, TextureSizeMode.NonPowerOfTwo,
                        magFilter, minFilter, json.TextureWrap, json.TextureWrap);

                    string filenameNormal = pathAbsolute.Replace(".png", ".n.png");
                    if (FileOp.Exists(filenameNormal)) {
                        pixelData = imageCodec.Read(FileOp.Open(filenameNormal, FileAccessMode.Read));

                        resource.TextureNormal = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo,
                            magFilter, minFilter, json.TextureWrap, json.TextureWrap);

                        resource.TextureNormal.Res.DetachPixmap();
                    } else {
                        resource.TextureNormal = defaultNormalMap;
                    }
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
                case "Add": return DrawTechnique.Add;
                //case "Alpha": return DrawTechnique.Alpha;
                //case "Invert": return DrawTechnique.Invert;
                //case "Light": return DrawTechnique.Light;
                //case "Mask": return DrawTechnique.Mask;
                //case "Multiply": return DrawTechnique.Multiply;
                case "SharpAlpha": return DrawTechnique.SharpAlpha;
                case "Picking": return DrawTechnique.Picking;
            }

            ContentRef<DrawTechnique> shader;
            if (!cachedShaders.TryGetValue(path, out shader)) {
                // Shaders for Android are always uncompressed for now, so the compressed
                // content package can be used in Android version as well.
#if UNCOMPRESSED_CONTENT || __ANDROID__
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Shaders", path + ".res");
#else
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Shaders", path + ".res");
#endif

                ShaderJson json;
                using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                    lock (jsonParser) {
                        json = jsonParser.Parse<ShaderJson>(s);
                    }
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
                    requestShaderNesting++;

                    ContentRef<VertexShader> vertex;
                    ContentRef<FragmentShader> fragment;
                    try {
                        if (json.Vertex == null) {
                            vertex = VertexShader.Minimal;
                        } else if (json.Vertex.StartsWith("#inherit ")) {
                            string parentPath = json.Vertex.Substring(9).Trim();
                            ContentRef<DrawTechnique> parent = RequestShader(parentPath);
                            vertex = parent.Res.Vertex;
                        } else if (json.Vertex.StartsWith("#include ")) {
                            string includePath = Path.Combine(DualityApp.DataDirectory, "Shaders", json.Vertex.Substring(9).Trim());
                            using (Stream s = FileOp.Open(includePath, FileAccessMode.Read))
                            using (StreamReader r = new StreamReader(s)) {
                                vertex = new VertexShader(r.ReadToEnd());
                            }
                        } else {
                            vertex = new VertexShader(json.Vertex.TrimStart());
                        }

                        if (json.Fragment == null) {
                            fragment = FragmentShader.Minimal;
                        } else if (json.Fragment.StartsWith("#inherit ")) {
                            string parentPath = json.Fragment.Substring(9).Trim();
                            ContentRef<DrawTechnique> parent = RequestShader(parentPath);
                            fragment = parent.Res.Fragment;
                        } else if (json.Fragment.StartsWith("#include ")) {
                            string includePath = Path.Combine(DualityApp.DataDirectory, "Shaders", json.Fragment.Substring(9).Trim());
                            using (Stream s = FileOp.Open(includePath, FileAccessMode.Read))
                            using (StreamReader r = new StreamReader(s)) {
                                fragment = new FragmentShader(r.ReadToEnd());
                            }
                        } else {
                            fragment = new FragmentShader(json.Fragment.TrimStart());
                        }
                    } finally {
                        requestShaderNesting--;
                    }

                    VertexDeclaration vertexFormat;
                    switch (json.VertexFormat) {
                        case "C1P3": vertexFormat = VertexC1P3.Declaration; break;
                        case "C1P3T2": vertexFormat = VertexC1P3T2.Declaration; break;
                        case "C1P3T4A1": vertexFormat = VertexC1P3T4A1.Declaration; break;
                        default: vertexFormat = null; break;
                    }

                    DrawTechnique result = new DrawTechnique(json.BlendMode, vertex, fragment);
                    result.PreferredVertexFormat = vertexFormat;

#if FAIL_ON_SHADER_COMPILE_ERROR && __ANDROID__
                    if (requestShaderNesting == 0 && result.DeclaredFields.Count == 0) {
                        Android.CrashHandlerActivity.ShowErrorDialog(new InvalidDataException("Shader \"" + path + "\" cannot be compiled on your device."));
                    }
#endif
                    shader = result;
                }

                cachedShaders[path] = shader;
            }

            return shader;
        }

        public void RequestTileset(string path, out ContentRef<Material> materialRef, out PixelData mask)
        {
            path = PathOp.Combine(DualityApp.DataDirectory, "Tilesets", path);

            // Load textures
            string texturePath = PathOp.Combine(path, "tiles.png");
            string maskPath = PathOp.Combine(path, "mask.png");
            string normalPath = PathOp.Combine(path, "normal.png");

            PixelData texturePixels = imageCodec.Read(FileOp.Open(texturePath, FileAccessMode.Read));
            PixelData normalPixels = (FileOp.Exists(normalPath) ? imageCodec.Read(FileOp.Open(normalPath, FileAccessMode.Read)) : null);

            // Apply palette to tileset
            ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.PixelData[0].Data;

            ColorRgba[] data = texturePixels.Data;
            Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                for (int i = range.Item1; i < range.Item2; i++) {
                    int colorIdx = data[i].R;
                    data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));
                }
            });

            ContentRef<Texture> mainTex = new Texture(new Pixmap(texturePixels));
            ContentRef<Texture> normalTex;
            if (normalPixels == null) {
                normalTex = DefaultNormalMap;
            } else {
                normalTex = new Texture(new Pixmap(normalPixels));
                normalTex.Res.DetachPixmap();
            }

            // Create material
            Material material = new Material(RequestShader("BasicNormal"));
            material.SetTexture("mainTex", mainTex);
            material.SetTexture("normalTex", normalTex);
            material.SetValue("normalMultiplier", Vector2.One);

            materialRef = material;
            mask = imageCodec.Read(FileOp.Open(maskPath, FileAccessMode.Read));
        }
    }
}