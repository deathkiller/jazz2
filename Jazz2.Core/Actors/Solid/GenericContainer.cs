using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public abstract class GenericContainer : SolidObjectBase
    {
        public RawList<ActorBase> content = new RawList<ActorBase>();

        protected override bool OnPerish(ActorBase collider)
        {
            SpawnContent();

            return base.OnPerish(collider);
        }

        protected void SpawnContent()
        {
            Vector3 pos = Transform.Pos;

            for (int i = 0; i < content.Count; i++) {
                float fx, fy;
                if (content.Count > 1) {
                    fx = MathF.Rnd.NextFloat(-6f, 6f);
                    fy = MathF.Rnd.NextFloat(-2f, 0.2f);
                } else {
                    fx = fy = 0f;
                }
                content.Data[i].MoveInstantly(new Vector2(pos.X + fx * (2f + content.Count * 0.1f), pos.Y + fy * (12f + content.Count * 0.2f)), MoveType.Absolute, true);
                content.Data[i].AddExternalForce(fx, fy);

                api.AddActor(content.Data[i]);
            }
            content.Clear();
        }

        protected void InsertActor(ActorBase actor)
        {
            content.Add(actor);
        }

        protected void GenerateContents(EventType type, uint count, params ushort[] eventParams)
        {
            Vector3 pos = Transform.Pos;

            for (uint i = 0; i < count; ++i) {
                ActorBase actor = api.CreateActor(type, pos.X, pos.Y, pos.Z + 10, eventParams, true);
                if (actor != null) {
                    InsertActor(actor);
                }
            }
        }
    }
}