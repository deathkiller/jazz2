using System.Threading.Tasks;
using Duality;
using Duality.Components.Renderers;
using Duality.Drawing;
using Jazz2.Game.Collisions;

namespace Jazz2.Actors.Environment
{
    public class SwingingVine : ActorBase
    {
        private VineRenderer vineRenderer;
        private float phase;
        private float angle;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/SwingingVine");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new SwingingVine();
            actor.OnActivated(details);
            return actor;
        }

        private SwingingVine()
        {
        }

        public Vector2 AttachPoint
        {
            get
            {
                return vineRenderer.ChunkPositions[vineRenderer.ChunkPositions.Length - 1];
            }
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Object/SwingingVine");

            var anim = availableAnimations["Vine"];

            vineRenderer = AddComponent<VineRenderer>();
            vineRenderer.SharedMaterial = anim.Material;
            vineRenderer.Width = anim.Base.FrameDimensions.X;

            CollisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            Vector3 pos = Transform.Pos;
            float distance = 0;
            for (int i = 0; i < vineRenderer.ChunkPositions.Length; i++) {
                angle = MathF.Sin(phase - i * 0.08f) * 1.2f + MathF.PiOver2;

                vineRenderer.ChunkPositions[i].X = pos.X + MathF.Cos(angle) * distance;
                vineRenderer.ChunkPositions[i].Y = pos.Y + MathF.Sin(angle) * distance;

                distance += 17;
            }

            AABBInner = new AABB(vineRenderer.ChunkPositions[vineRenderer.ChunkPositions.Length - 1], 20, 20);

            phase += timeMult * 0.04f;

            // ToDo
            Transform.Pos = Transform.Pos;

#if DEBUG && !SERVER
            Game.UI.Hud.ShowDebugRect(new Rect(AABBInner.LowerBound.X, AABBInner.LowerBound.Y, AABBInner.UpperBound.X - AABBInner.LowerBound.X, AABBInner.UpperBound.Y - AABBInner.LowerBound.Y));
#endif
        }

        public override void OnHandleCollision(ActorBase other)
        {
            
        }

        public class VineRenderer : SpriteRenderer
        {
            public Vector2[] ChunkPositions = new Vector2[8];
            public int Width;

            private void PrepareVineVertices(ref VertexC1P3T2[] vertices, IDrawDevice device, ColorRgba mainClr, Rect uvRect)
            {
                if (vertices == null /*|| vertices.Length != 4*/) vertices = new VertexC1P3T2[(ChunkPositions.Length - 1) * 4];

                Vector3 posTemp = this.gameobj.Transform.Pos;

                float uvStep = (uvRect.H / ChunkPositions.Length);

                int vertexOffset = 0;
                for (int i = 1; i < ChunkPositions.Length; i++) {
                    ref Vector2 source = ref ChunkPositions[i - 1];
                    ref Vector2 target = ref ChunkPositions[i];

                    Vector2 dir = (target - source).Normalized;
                    Vector2 left = dir.PerpendicularLeft * Width * 0.5f;
                    Vector2 right = dir.PerpendicularRight * Width * 0.5f;

                    vertices[vertexOffset].Pos.Xy = source + left;
                    vertices[vertexOffset].Pos.Z = posTemp.Z;
                    vertices[vertexOffset].TexCoord = new Vector2(uvRect.X, uvRect.Y + uvStep * (i - 1));
                    vertices[vertexOffset].Color = mainClr;
                    vertexOffset++;

                    vertices[vertexOffset].Pos.Xy = target + left;
                    vertices[vertexOffset].Pos.Z = posTemp.Z;
                    vertices[vertexOffset].TexCoord = new Vector2(uvRect.X, uvRect.Y + uvStep * i);
                    vertices[vertexOffset].Color = mainClr;
                    vertexOffset++;

                    vertices[vertexOffset].Pos.Xy = target + right;
                    vertices[vertexOffset].Pos.Z = posTemp.Z;
                    vertices[vertexOffset].TexCoord = new Vector2(uvRect.X + uvRect.W, uvRect.Y + uvStep * i);
                    vertices[vertexOffset].Color = mainClr;
                    vertexOffset++;

                    vertices[vertexOffset].Pos.Xy = source + right;
                    vertices[vertexOffset].Pos.Z = posTemp.Z;
                    vertices[vertexOffset].TexCoord = new Vector2(uvRect.X + uvRect.W, uvRect.Y + uvStep * (i - 1));
                    vertices[vertexOffset].Color = mainClr;
                    vertexOffset++;
                }
            }

            public override void Draw(IDrawDevice device)
            {
                Rect uvRect = new Rect(1.0f, 1.0f);

                PrepareVineVertices(ref vertices, device, this.colorTint, uvRect);
                device.AddVertices(sharedMat, VertexMode.Quads, vertices, 0, vertices.Length);
            }
        }
    }
}