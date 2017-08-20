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
        // Single purpose exception
    }

    partial class ContentResolver
    {
        private HashSet<string> metadataAsyncRequests;

        private Thread asyncThread;
        private AutoResetEvent asyncThreadEvent;


        public Metadata RequestMetadataAsync(string path)
        {
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (!metadataAsyncRequests.Contains(path)) {
                        metadataAsyncRequests.Add(path);

                        asyncThreadEvent.Set();
                    }
                }

                throw new ResourcesNotReady();
            }

            if (metadata.AsyncFinalizingRequired) {
                metadata.AsyncFinalizingRequired = false;
                FinalizeAsyncLoadedResources(metadata);
            }

            return metadata;
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
                            magFilter, minFilter, resBase.AsyncFinalize.TextureWrap, resBase.AsyncFinalize.TextureWrap);

                        if (resBase.AsyncFinalize.TextureNormalMap != null) {
                            resBase.TextureNormal = new Texture(resBase.AsyncFinalize.TextureNormalMap, TextureSizeMode.NonPowerOfTwo,
                                magFilter, minFilter, resBase.AsyncFinalize.TextureWrap, resBase.AsyncFinalize.TextureWrap);
                        }

                        resBase.AsyncFinalize = null;
                    }


                    if (res.AsyncFinalize != null) {
                        Dictionary<string, ContentRef<Texture>> textures = new Dictionary<string, ContentRef<Texture>>();
                        textures.Add("mainTex", resBase.Texture);
                        if (resBase.TextureNormal != null) {
                            textures.Add("normalTex", resBase.TextureNormal);
                        }

                        if (res.AsyncFinalize.BindPaletteToMaterial) {
                            textures.Add("paletteTex", paletteTexture);
                        }

                        ContentRef<DrawTechnique> drawTechnique;
                        if (res.AsyncFinalize.Shader == null) {
                            drawTechnique = (res.AsyncFinalize.BindPaletteToMaterial ? paletteNormal : basicNormal);
                        } else {
                            drawTechnique = RequestShader(res.AsyncFinalize.Shader);
                        }

                        res.Material = new Material(drawTechnique, res.AsyncFinalize.Color, textures);

                        res.AsyncFinalize = null;
                    }
                }
            }
        }

        private void AllowAsyncLoading()
        {
            metadataAsyncRequests = new HashSet<string>();

            asyncThreadEvent = new AutoResetEvent(false);

            asyncThread = new Thread(OnAsyncThread);
            asyncThread.IsBackground = true;
            asyncThread.Start();
        }

        private void CleanupAsyncLoading()
        {
            asyncThread = null;

            lock (metadataAsyncRequests) {
                metadataAsyncRequests.Clear();
            }

            asyncThreadEvent.Set();
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
                        cachedMetadata[path] = metadata;

                        metadataAsyncRequests.Remove(path);
                    }
                }
            }
        }
    }
}