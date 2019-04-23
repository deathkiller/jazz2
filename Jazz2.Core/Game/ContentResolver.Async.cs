using System;
using System.Collections.Generic;
using Duality.Drawing;
using Jazz2.Game.Structs;
using System.Linq;
using System.Threading;
using Duality.Resources;
using Duality;

namespace Jazz2.Game
{
    public class ResourcesNotReady : Exception
    {
        public string Path;

        public ResourcesNotReady(string path)
        {
            this.Path = path;
        }
    }

    partial class ContentResolver
    {
        public delegate void ResourceReadyDelegate(string path);

        private HashSet<string> metadataAsyncRequests;

        private Thread asyncThread;
        private AutoResetEvent asyncThreadEvent;
        private AutoResetEvent asyncResourceReadyEvent;

        public event ResourceReadyDelegate ResourceReady;

        public Metadata RequestMetadataAsync(string path)
        {
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (metadataAsyncRequests.Add(path)) {
                        asyncThreadEvent.Set();
                    }
                }

                throw new ResourcesNotReady(path);
            } else {
                MarkAsReferenced(metadata);
            }

            if (metadata.AsyncFinalizingRequired) {
                metadata.AsyncFinalizingRequired = false;
                FinalizeAsyncLoadedResources(metadata);
            }

            return metadata;
        }

        public void PreloadAsync(string path)
        {
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (metadataAsyncRequests.Add(path)) {
                        asyncThreadEvent.Set();
                    }
                }
            } else {
                MarkAsReferenced(metadata);
            }
        }

        private void FinalizeAsyncLoadedResources(Metadata metadata)
        {
            if (metadata.Graphics != null) {
                foreach (var pair in metadata.Graphics) {
                    GraphicResource res = pair.Value;
                    GenericGraphicResource resBase = res.Base;
                    if (resBase.AsyncFinalize != null) {
                        TextureMagFilter magFilter; TextureMinFilter minFilter;
                        if (resBase.AsyncFinalize.LinearSampling) {
                            magFilter = TextureMagFilter.Linear;
                            minFilter = TextureMinFilter.LinearMipmapLinear;
                        } else {
                            magFilter = TextureMagFilter.Nearest;
                            minFilter = TextureMinFilter.Nearest;
                        }

                        resBase.Texture = new Texture(resBase.AsyncFinalize.TextureMap, TextureSizeMode.NonPowerOfTwo,
                            magFilter, minFilter, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

                        if (resBase.AsyncFinalize.TextureNormalMap != null) {
                            resBase.TextureNormal = new Texture(resBase.AsyncFinalize.TextureNormalMap, TextureSizeMode.NonPowerOfTwo,
                                magFilter, minFilter, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

                            resBase.TextureNormal.Res.DetachSource();
                        }

                        resBase.AsyncFinalize = null;
                    }

                    if (res.AsyncFinalize != null) {
                        ContentRef<DrawTechnique> drawTechnique;
                        if (res.AsyncFinalize.Shader == null) {
                            drawTechnique = (res.AsyncFinalize.BindPaletteToMaterial ? paletteNormal : basicNormal);
                        } else {
                            drawTechnique = RequestShader(res.AsyncFinalize.Shader);
                        }

                        Material material = new Material(drawTechnique, res.AsyncFinalize.Color);

                        material.SetTexture("mainTex", resBase.Texture);
                        if (resBase.TextureNormal.IsAvailable) {
                            material.SetTexture("normalTex", resBase.TextureNormal);
                        }

                        if (res.AsyncFinalize.BindPaletteToMaterial) {
                            material.SetTexture("paletteTex", paletteTexture);
                        }

                        res.Material = material;

                        res.AsyncFinalize = null;
                    }
                }
            }
        }

        private void AllowAsyncLoading()
        {
            metadataAsyncRequests = new HashSet<string>();

            asyncThreadEvent = new AutoResetEvent(false);
            asyncResourceReadyEvent = new AutoResetEvent(false);

            asyncThread = new Thread(OnAsyncThread);
            asyncThread.IsBackground = true;
            asyncThread.Start();

            DualityApp.Terminating += OnDualityAppTerminating;
        }

        private void OnAsyncThread()
        {
            while (asyncThread != null) {
                asyncThreadEvent.WaitOne();

                while (true) {
                    string path;
                    lock (metadataAsyncRequests) {
                        path = metadataAsyncRequests.FirstOrDefault();
                    }

                    if (path == null) {
                        break;
                    }

                    Metadata metadata = RequestMetadataInner(path, true);

                    lock (metadataAsyncRequests) {
                        metadataAsyncRequests.Remove(path);
                    }

                    asyncResourceReadyEvent.Set();

                    ResourceReady?.Invoke(path);
                }
            }
        }
    }
}