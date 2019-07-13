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
using Jazz2.Game.Tiles;
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
        }

        public void Init()
        {
            jsonParser = new JsonParser();

#if !UNCOMPRESSED_CONTENT
            string dz = PathOp.Combine(DualityApp.DataDirectory, "Main.dz");
            PathOp.Mount(dz, new CompressedContent(dz));
#endif
        }

        public void InitPostWindow()
        {
            defaultNormalMap = new Texture(new Pixmap(new PixelData(2, 2, new ColorRgba(0.5f, 0.5f, 1f))), TextureSizeMode.Default, TextureMagFilter.Nearest, TextureMinFilter.Nearest);
            defaultNormalMap.Res.DetachSource();

            cachedMetadata = new ConcurrentDictionary<string, Metadata>(2, 31);
            cachedGraphics = new Dictionary<string, GenericGraphicResource>();
            cachedShaders = new Dictionary<string, ContentRef<DrawTechnique>>();
            //cachedSounds = new Dictionary<string, ContentRef<Sound>>();

            basicNormal = RequestShader("BasicNormal");
            paletteNormal = RequestShader("PaletteNormal");

#if !DISABLE_ASYNC
            AllowAsyncLoading();
#endif

#if DEBUG && UNCOMPRESSED_CONTENT
            InitWatchForFileChanges();
#endif
        }

        private void OnDualityAppTerminating(object sender, EventArgs e)
        {
            DualityApp.Terminating -= OnDualityAppTerminating;

#if DEBUG && UNCOMPRESSED_CONTENT
            DestroyWatchForFileChanges();
#endif

#if !DISABLE_ASYNC
            asyncThread = null;

            lock (metadataAsyncRequests) {
                metadataAsyncRequests.Clear();
            }

            asyncThreadEvent.Set();
            asyncResourceReadyEvent.Set();
#endif

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
#if !DISABLE_ASYNC
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
#else
            return RequestMetadataInner(path, false);
#endif
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

                foreach (var sound in json.Sounds) {
                    if (sound.Value.Paths == null || sound.Value.Paths.Count == 0) {
                        // No path provided, skip resource...
                        continue;
                    }

#if !THROW_ON_MISSING_RESOURCES
                    try {
#endif
                        IList<string> filenames = sound.Value.Paths;
                        ContentRef<AudioData>[] data = new ContentRef<AudioData>[filenames.Count];
                        for (int i = 0; i < data.Length; i++) {
#if UNCOMPRESSED_CONTENT
                            using (Stream s = FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Animations", filenames[i]), FileAccessMode.Read))
#else
                            using (Stream s = FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", filenames[i]), FileAccessMode.Read))
#endif
                            {
                                data[i] = new AudioData(s);
                            }
                        }

                        SoundResource resource = new SoundResource();
                        resource.Sound = new Sound(data);
                        metadata.Sounds[sound.Key] = resource;
#if !THROW_ON_MISSING_RESOURCES
                    } catch (Exception ex) {
                        App.Log("Can't load sound \"" + sound.Key + "\" from metadata \"" + path + "\": " + ex.Message);
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
                }

                PixelData pixelData;
                using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                    pixelData = new Png(s).GetPixelData();
                }

                // Use external palette
                if ((json.Flags & 0x01) != 0x00) {
                    ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.MainLayer.Data;

                    ColorRgba[] data = pixelData.Data;
#if !DISABLE_ASYNC
                    Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
#else
                        for (int i = 0; i < data.Length; i++) {
#endif

                            int colorIdx = data[i].R;
                            data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));

                            // ToDo: Pinball sprites have strange palette (1-3 indices down), CandionV looks bad, other levels look different
                        }
#if !DISABLE_ASYNC
                    });
#endif
                }

                bool linearSampling = (json.Flags & 0x02) != 0x00;

                Pixmap map = new Pixmap(pixelData);
                map.GenerateAnimAtlas(resource.FrameConfiguration.X, resource.FrameConfiguration.Y, 0);
                if (async) {
                    GenericGraphicResourceAsyncFinalize asyncFinalize = new GenericGraphicResourceAsyncFinalize();
                    asyncFinalize.TextureMap = map;

#if !DISABLE_NORMAL_MAPPING
                    string filenameNormal = pathAbsolute.Replace(".png", ".n.png");
                    if (FileOp.Exists(filenameNormal)) {
                        using (Stream s = FileOp.Open(filenameNormal, FileAccessMode.Read)) {
                            asyncFinalize.TextureNormalMap = new Pixmap(new Png(s).GetPixelData());
                        }
                    } else {
                        resource.TextureNormal = defaultNormalMap;
                    }
#else
                    resource.TextureNormal = defaultNormalMap;
#endif

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

#if !DISABLE_NORMAL_MAPPING
                    string filenameNormal = pathAbsolute.Replace(".png", ".n.png");
                    if (FileOp.Exists(filenameNormal)) {
                        using (Stream s = FileOp.Open(filenameNormal, FileAccessMode.Read)) {
                            pixelData = new Png(s).GetPixelData();
                        }

                        resource.TextureNormal = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo,
                            magFilter, minFilter, json.TextureWrap, json.TextureWrap);

                        resource.TextureNormal.Res.DetachSource();
                    } else {
                        resource.TextureNormal = defaultNormalMap;
                    }
#else
                    resource.TextureNormal = defaultNormalMap;
#endif
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
#if UNCOMPRESSED_CONTENT
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Shaders", path + ".res");
#elif PLATFORM_ANDROID || PLATFORM_WASM
                string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Shaders.ES30", path + ".res");
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

#if FAIL_ON_SHADER_COMPILE_ERROR && PLATFORM_ANDROID
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

        public void RequestTileset(string path, bool applyPalette, ColorRgba[] customPalette, out ContentRef<Material> materialRef, out PixelData mask)
        {
            IFileSystem tilesetPackage = new CompressedContent(PathOp.Combine(DualityApp.DataDirectory, "Tilesets", path + ".set"));

            // Palette
            if (applyPalette) {
                if (customPalette != null) {
                    ApplyBasePalette(customPalette);
                } else if (tilesetPackage.FileExists("Main.palette")) {
                    ApplyBasePalette(TileSet.LoadPalette(tilesetPackage.OpenFile("Main.palette", FileAccessMode.Read)));
                }
            }

            // Load textures
            PixelData texturePixels;
            using (Stream s = tilesetPackage.OpenFile("Diffuse.png", FileAccessMode.Read)) {
                texturePixels = new Png(s).GetPixelData();
            }

#if !DISABLE_NORMAL_MAPPING
            PixelData normalPixels;
            if (tilesetPackage.FileExists("Normals.png")) {
                using (Stream s = tilesetPackage.OpenFile("Normals.png", FileAccessMode.Read)) {
                    normalPixels = new Png(s).GetPixelData();
                }
            } else {
                normalPixels = null;
            }
#endif

            using (Stream s = tilesetPackage.OpenFile("Mask.png", FileAccessMode.Read)) {
                mask = new Png(s).GetPixelData();
            }

            // Apply palette to tileset
            ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.MainLayer.Data;

            ColorRgba[] data = texturePixels.Data;
#if !DISABLE_ASYNC
            Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                for (int i = range.Item1; i < range.Item2; i++) {
#else
                for (int i = 0; i < data.Length; i++) {
#endif
                    int colorIdx = data[i].R;
                    data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));
                }
#if !DISABLE_ASYNC
            });
#endif

            ContentRef<Texture> mainTex = new Texture(new Pixmap(texturePixels));

            ContentRef<Texture> normalTex;
#if !DISABLE_NORMAL_MAPPING
            if (normalPixels == null) {
                normalTex = DefaultNormalMap;
            } else {
                normalTex = new Texture(new Pixmap(normalPixels));
                normalTex.Res.DetachSource();
            }
#else
            normalTex = DefaultNormalMap;
#endif

            // Create material
            Material material = new Material(RequestShader("BasicNormal"));
            material.SetTexture("mainTex", mainTex);
            material.SetTexture("normalTex", normalTex);
            material.SetValue("normalMultiplier", Vector2.One);

            materialRef = material;
        }

#if DEBUG && UNCOMPRESSED_CONTENT
        private FileSystemWatcher shaderDirWatcher;
        private HashSet<string> changedShaders;

        private void InitWatchForFileChanges()
        {
            changedShaders = new HashSet<string>();

            shaderDirWatcher = new FileSystemWatcher();
            shaderDirWatcher.EnableRaisingEvents = false;
            shaderDirWatcher.IncludeSubdirectories = true;
            shaderDirWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            shaderDirWatcher.Path = Path.Combine(DualityApp.DataDirectory, "Shaders");
            shaderDirWatcher.Created += OnFileChanged;
            shaderDirWatcher.Changed += OnFileChanged;
            shaderDirWatcher.Deleted += OnFileChanged;
            shaderDirWatcher.Renamed += OnFileChanged;
            shaderDirWatcher.EnableRaisingEvents = true;
        }

        private void DestroyWatchForFileChanges()
        {
            changedShaders.Clear();

            shaderDirWatcher.EnableRaisingEvents = false;
            shaderDirWatcher.Created -= OnFileChanged;
            shaderDirWatcher.Changed -= OnFileChanged;
            shaderDirWatcher.Deleted -= OnFileChanged;
            shaderDirWatcher.Renamed -= OnFileChanged;
            shaderDirWatcher.Dispose();
            shaderDirWatcher = null;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string path = Path.GetFileNameWithoutExtension(e.FullPath);

            lock (changedShaders) {
                changedShaders.Add(path);
            }

            DualityApp.DisposeLater(new ActionDisposable(ReloadChangedShaders));
        }

        private void ReloadChangedShaders()
        {
            lock (changedShaders) {

                foreach (string path in changedShaders) {
                    ContentRef<DrawTechnique> shader;
                    if (cachedShaders.TryGetValue(path, out shader)) {

                        string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Shaders", path + ".res");

                        ShaderJson json;
                        using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                            lock (jsonParser) {
                                json = jsonParser.Parse<ShaderJson>(s);
                            }
                        }

                        if (json.Fragment == null && json.Vertex == null) {
                            // ToDo: ...
                            /*switch (json.BlendMode) {
                                default:
                                case BlendMode.Solid: shader = DrawTechnique.Solid; break;
                                case BlendMode.Mask: shader = DrawTechnique.Mask; break;
                                case BlendMode.Add: shader = DrawTechnique.Add; break;
                                case BlendMode.Alpha: shader = DrawTechnique.Alpha; break;
                                case BlendMode.Multiply: shader = DrawTechnique.Multiply; break;
                                case BlendMode.Light: shader = DrawTechnique.Light; break;
                                case BlendMode.Invert: shader = DrawTechnique.Invert; break;
                            }*/
                            return;
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

                            // Update shader
                            shader.Res.Blending = json.BlendMode;
                            shader.Res.PreferredVertexFormat = vertexFormat;
                            shader.Res.Vertex = vertex;
                            shader.Res.Fragment = fragment;
                        }
                    }
                }

                changedShaders.Clear();
            }
        }

        // ToDo: Refactor this
        private class ActionDisposable : IDisposable
        {
            private Action action;

            public ActionDisposable(Action action)
            {
                this.action = action;
            }

            void IDisposable.Dispose()
            {
                System.Threading.Interlocked.Exchange(ref action, null)();
            }
        }
#endif
    }
}
 