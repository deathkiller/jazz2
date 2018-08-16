using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.UI
{
    public class TransitionManager
    {
        private struct Block
        {
            public float Current;
            public float Offset;
        }

        private const int BlockSize = 6;
        private const int BlockSizeSmooth = 3;

        private TransitionMode mode;
        private int blockSize;
        private Block[] blocks;
        private int w, h;
        private bool isCompleted;
        private float time;
        private VertexC1P3[] vertices;


        public TransitionMode Mode => mode;

        public bool IsCompleted => isCompleted;

        public TransitionManager(TransitionMode mode, Point2 targetSize, bool smooth)
        {
            this.mode = mode;

            this.blockSize = (smooth ? BlockSizeSmooth : BlockSize);

            this.w = (int)MathF.Ceiling((float)targetSize.X / blockSize);
            this.h = (int)MathF.Ceiling((float)targetSize.Y / blockSize);

            float m = MathF.Max(this.w, this.h);

            this.blocks = new Block[this.w * this.h];
            this.vertices = new VertexC1P3[this.blocks.Length * 4];
            for (int y = 0; y < this.h; y++) {
                for (int x = 0; x < this.w; x++) {
                    ref Block block = ref blocks[this.w * y + x];

                    float cx = (((float)x / this.w) - 0.5f) * 2f;
                    float cy = (((float)y / this.h) * 0.6f - 0.4f) * 2f;
                    float distanceFromCenter = MathF.Sqrt(cx * cx + cy * cy);

                    block.Offset = 0.2f + distanceFromCenter;

                    if (!smooth) {
                        block.Offset += MathF.Rnd.Next(-4, 4) * 0.05f;
                    }
                }
            }
        }

        public void Draw(IDrawDevice device, Canvas canvas)
        {
            if (mode == TransitionMode.None) {
                return;
            }

            float timeMult = Time.TimeMult;

            Vector2 offset = device.TargetSize;
            offset.X = (offset.X - this.w * blockSize) * 0.5f;
            offset.Y = (offset.Y - this.h * blockSize) * 0.5f;

            isCompleted = true;

            for (int y = 0; y < this.h; y++) {
                for (int x = 0; x < this.w; x++) {
                    ref Block block = ref blocks[this.w * y + x];

                    float alpha = MathF.Clamp(block.Current - block.Offset, 0, 1);
                    if (mode == TransitionMode.FadeIn) {
                        alpha = 1f - alpha;

                        if (alpha > 0f) {
                            isCompleted = false;
                        }
                    } else {
                        if (alpha < 1f) {
                            isCompleted = false;
                        }
                    }

                    block.Current += 0.1f * timeMult;

                    int idx = (this.w * y + x) * 4;

                    ColorRgba color = new ColorRgba(0, alpha);

                    Vector3 pos = new Vector3(offset.X + x * blockSize, offset.Y + y * blockSize, 0);
                    float scale = 1.0f;
                    device.PreprocessCoords(ref pos, ref scale);

                    vertices[idx + 0].Pos = new Vector3(pos.X, pos.Y, pos.Z);
                    vertices[idx + 1].Pos = new Vector3(pos.X + blockSize, pos.Y, pos.Z);
                    vertices[idx + 2].Pos = new Vector3(pos.X + blockSize, pos.Y + blockSize, pos.Z);
                    vertices[idx + 3].Pos = new Vector3(pos.X, pos.Y + blockSize, pos.Z);

                    vertices[idx + 0].Color = color;
                    vertices[idx + 1].Color = color;
                    vertices[idx + 2].Color = color;
                    vertices[idx + 3].Color = color;
                }
            }

            BatchInfo material = device.RentMaterial();
            material.Technique = DrawTechnique.Alpha;
            device.AddVertices(material, VertexMode.Quads, vertices, 0, vertices.Length);

            if (time < 30f) {
                isCompleted = false;
                time += timeMult;
            }
        }
    }
}