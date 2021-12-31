using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duality;
using Duality.Async;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game
{
    partial class ContentResolver
    {
#if !DISABLE_ASYNC
        private HashSet<string> metadataAsyncRequests;

        private Thread asyncThread;
        private bool asyncThreadPaused;
        private AutoResetEvent asyncThreadEvent;
        private AutoResetEvent asyncResourceReadyEvent;
#endif

        public Metadata TryFetchMetadata(string path)
        {
#if !DISABLE_ASYNC
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (metadataAsyncRequests.Add(path) && !asyncThreadPaused) {
                        asyncThreadEvent.Set();
                    }
                }

                return null;
            } else {
                // ToDo
                //MarkAsReferenced(metadata);
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

        public void PreloadAsync(string path)
        {
#if !DISABLE_ASYNC
            Metadata metadata;
            if (!cachedMetadata.TryGetValue(path, out metadata)) {
                lock (metadataAsyncRequests) {
                    if (metadataAsyncRequests.Add(path) && !asyncThreadPaused) {
                        asyncThreadEvent.Set();
                    }
                }
            } else {
                MarkAsReferenced(metadata);
            }
#endif
        }

#if !DISABLE_ASYNC
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
            asyncThread.Priority = ThreadPriority.BelowNormal;
            asyncThread.Start();

            DualityApp.Terminating += OnDualityAppTerminating;
        }

        private void OnAsyncThread()
        {
            while (asyncThread != null) {
                asyncThreadEvent.WaitOne();

                while (!asyncThreadPaused) {
                    string path;
                    lock (metadataAsyncRequests) {
                        path = metadataAsyncRequests.FirstOrDefault();
                    }

                    if (path == null) {
                        break;
                    }

                    _ = RequestMetadataInner(path, true);

                    lock (metadataAsyncRequests) {
                        metadataAsyncRequests.Remove(path);
                    }

                    asyncResourceReadyEvent.Set();
                }
            }
        }
#endif

        public void SuspendAsync()
        {
#if !DISABLE_ASYNC
            asyncThreadPaused = true;
#endif
        }

#if !DISABLE_ASYNC
        public async Task ResumeAsync()
        {
            if (!asyncThreadPaused) {
                return;
            }

            asyncThreadPaused = false;

            asyncThreadEvent.Set();

            while (true) {
                await Await.NextUpdate();

                bool queueIsEmpty = (metadataAsyncRequests.Count == 0);

                foreach (var pair in cachedMetadata) {
                    var metadata = pair.Value;

                    if (metadata.AsyncFinalizingRequired) {
                        metadata.AsyncFinalizingRequired = false;
                        FinalizeAsyncLoadedResources(metadata);
                    }
                }

                if (queueIsEmpty) {
                    break;
                }
            }
        }
#else
        public Task ResumeAsync()
        {
            return Task.CompletedTask;
        }
#endif
    }
}