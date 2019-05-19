using System;
using System.IO;
using System.IO.Compression;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.UI.Menu;

namespace Jazz2.Game.UI
{
    public class Cinematics : Scene
    {
        private Action<bool> endCallback;

        private Stream[] streams;
        private Texture videoTexture;
        private ColorRgba[] palette;
        private Pixmap currentFrame;
        private byte[] currentBuffer;
        private byte[] lastBuffer;

        private Canvas canvas;
        private OpenMptStream music;

        private int width;
        private int height;
        private float frameDelay;
        private float frameProgress;
        private int framesLeft;

        public Cinematics(App root, string name, Action<bool> endCallback)
        {
            this.endCallback = endCallback;

            // Prepare for playback
            root.Title = null;
            root.Immersive = false;

            GameObject rootObject = new GameObject();
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Setup camera
            GameObject camera = new GameObject();
            camera.AddComponent<Transform>();

            Camera cameraInner = camera.AddComponent<Camera>();
            cameraInner.Projection = ProjectionMode.Orthographic;
            cameraInner.NearZ = 10;
            cameraInner.FarZ = 1000;

            cameraInner.RenderingSetup = new MainMenuRenderSetup();
            camera.Parent = rootObject;

            canvas = new Canvas();

            // Try to load cinematics file
            if (!LoadFile(name)) {
                framesLeft = 0;
                return;
            }

            // Play music
            music = new OpenMptStream(PathOp.Combine(DualityApp.DataDirectory, "Music", name + ".j2b"), false);
            music.BeginFadeIn(0.5f);
            DualityApp.Sound.PlaySound(music);
        }

        protected override void OnDisposing(bool manually)
        {
            if (streams != null) {
                for (int i = 0; i < streams.Length; i++) {
                    streams[i].Dispose();
                }
                streams = null;
            }

            if (videoTexture != null) {
                videoTexture.Dispose();
                videoTexture = null;
            }

            if (music != null) {
                music.Dispose();
                music = null;
            }

            base.OnDisposing(manually);
        }

        private void OnCinematicsEnd(bool endOfStream)
        {
            if (endCallback != null) {
                endCallback(endOfStream);
                endCallback = null;
            }
        }

        private void OnUpdate()
        {
            if (ControlScheme.MenuActionPressed(PlayerActions.Menu)) {
                framesLeft = 0;
                OnCinematicsEnd(false);
            }
        }

        private void OnRender(IDrawDevice device)
        {
            if (framesLeft <= 0) {
                OnCinematicsEnd(true);
                return;
            }

            if (frameProgress < frameDelay) {
                frameProgress += Time.TimeMult;
            } else {
                frameProgress -= frameDelay;
                framesLeft--;

                PrepareNextFrame();
            }

            // Render current frame
            canvas.Begin(device);

            BatchInfo material = device.RentMaterial();
            material.MainTexture = videoTexture;
            canvas.State.SetMaterial(material);

            Vector2 targetSize = device.TargetSize;

            float ratioTarget = targetSize.Y / targetSize.X; ;
            float ratioSource = (float)height / width;
            float ratio = MathF.Clamp(ratioTarget, ratioSource - 0.16f, ratioSource);

            float fillHeight = targetSize.X * ratio;
            float yOffset = (targetSize.Y - fillHeight) * 0.5f;
            canvas.FillRect(0, yOffset, targetSize.X, fillHeight);

            canvas.End();
        }

        private bool LoadFile(string name)
        {
            string path = PathOp.Combine(DualityApp.DataDirectory, "Cinematics", name + ".j2v");

            if (!FileOp.Exists(path)) {
                return false;
            }

            using (Stream s = FileOp.Open(path, FileAccessMode.Read)) {
                byte[] internalBuffer = new byte[4096];

                // "CineFeed" + file size + CRC
                s.Read(internalBuffer, 0, 16);

                // ToDo: Check signature

                width = s.ReadInt32(ref internalBuffer);
                height = s.ReadInt32(ref internalBuffer);
                s.Seek(2, SeekOrigin.Current); // Bits per pixel
                frameDelay = s.ReadUInt16(ref internalBuffer) * 0.5f / Time.MillisecondsPerFrame;
                framesLeft = s.ReadInt32(ref internalBuffer);
                s.Seek(20, SeekOrigin.Current);

                videoTexture = new Texture(width, height, TextureSizeMode.NonPowerOfTwo);
                currentFrame = new Pixmap(new PixelData(width, height));
                currentBuffer = new byte[width * height];
                lastBuffer = new byte[width * height];
                palette = new ColorRgba[256];

                MemoryStream[] memoryStreams = new MemoryStream[4];
                for (int i = 0; i < 4; i++) {
                    memoryStreams[i] = new MemoryStream();
                }
                for (int i = 0; s.Position < s.Length; i++) {
                    for (int j = 0; j < 4; j++) {
                        int bytesLeft = s.ReadInt32(ref internalBuffer);
                        while (bytesLeft > 0) {
                            int bytesRead = s.Read(internalBuffer, 0, Math.Min(internalBuffer.Length, bytesLeft));
                            memoryStreams[j].Write(internalBuffer, 0, bytesRead);
                            bytesLeft -= bytesRead;
                        }
                    }
                }
                streams = new Stream[4];
                for (int i = 0; i < 4; i++) {
                    memoryStreams[i].Position = 2;
                    streams[i] = new DeflateStream(memoryStreams[i], CompressionMode.Decompress);
                }
            }

            return true;
        }

        private void PrepareNextFrame()
        {
            byte[] internalBuffer = new byte[8];

            if (streams[0].ReadUInt8(ref internalBuffer) == 1) {
                for (int i = 0; i < 256; i++) {
                    byte r = streams[3].ReadUInt8(ref internalBuffer);
                    byte g = streams[3].ReadUInt8(ref internalBuffer);
                    byte b = streams[3].ReadUInt8(ref internalBuffer);
                    /*byte a =*/
                    streams[3].ReadUInt8(ref internalBuffer);
                    palette[i] = new ColorRgba(r, g, b);
                }
            }

            for (int y = 0; y < height; y++) {
                byte c;
                int x = 0;
                while ((c = streams[0].ReadUInt8(ref internalBuffer)) != 128) {
                    if (c < 128) {
                        int u;
                        if (c == 0x00) {
                            u = streams[0].ReadInt16(ref internalBuffer);
                        } else {
                            u = c;
                        }

                        for (int i = 0; i < u; i++) {
                            byte pixel = streams[3].ReadUInt8(ref internalBuffer);
                            currentBuffer[y * width + x] = pixel;
                            x++;
                        }
                    } else {
                        int u;
                        if (c == 0x81) {
                            u = streams[0].ReadInt16(ref internalBuffer);
                        } else {
                            u = c - 106;
                        }

                        int n = streams[1].ReadInt16(ref internalBuffer) + (streams[2].ReadUInt8(ref internalBuffer) + y - 127) * width;
                        for (int i = 0; i < u; i++) {
                            currentBuffer[y * width + x] = lastBuffer[n];
                            x++;
                            n++;
                        }
                    }
                }
            }

            for (int i = 0; i < currentBuffer.Length; i++) {
                currentFrame.MainLayer.Data[i] = palette[currentBuffer[i]];
            }

            videoTexture.LoadData(currentFrame, TextureSizeMode.NonPowerOfTwo);

            Buffer.BlockCopy(currentBuffer, 0, lastBuffer, 0, currentBuffer.Length);
        }

        private class LocalController : Component, ICmpUpdatable, ICmpRenderer
        {
            private readonly Cinematics cinematics;

            public LocalController(Cinematics cinematics)
            {
                this.cinematics = cinematics;
            }

            void ICmpUpdatable.OnUpdate()
            {
                cinematics.OnUpdate();
            }

            void ICmpRenderer.GetCullingInfo(out CullingInfo info)
            {
                info.Position = Vector3.Zero;
                info.Radius = float.MaxValue;
                info.Visibility = VisibilityFlag.Group0 | VisibilityFlag.ScreenOverlay;
            }

            void ICmpRenderer.Draw(IDrawDevice device)
            {
                cinematics.OnRender(device);
            }
        }
    }
}
