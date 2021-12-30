using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public abstract class GenericContainer : SolidObjectBase
    {
        protected RawList<ActorBase> Content = new RawList<ActorBase>();

        protected override bool OnPerish(ActorBase collider)
        {
            SpawnContent();

            return base.OnPerish(collider);
        }

        protected void SpawnContent()
        {
            Vector3 pos = Transform.Pos;

            for (int i = 0; i < Content.Count; i++) {
                float fx, fy;
                if (Content.Count > 1) {
                    fx = MathF.Rnd.NextFloat(-6f, 6f);
                    fy = MathF.Rnd.NextFloat(-2f, 0.2f);
                } else {
                    fx = 0f;
                    fy = 0f;
                }
                Content.Data[i].MoveInstantly(new Vector2(pos.X + fx * (2f + Content.Count * 0.1f), pos.Y + fy * (12f + Content.Count * 0.2f)), MoveType.Absolute, true);
                Content.Data[i].AddExternalForce(fx, fy);

                levelHandler.AddActor(Content.Data[i]);
            }
            Content.Clear();
        }

        protected void StoreActor(ActorBase actor)
        {
            Content.Add(actor);
        }

        protected void GenerateContents(EventType type, uint count, params ushort[] eventParams)
        {
            Vector3 pos = Transform.Pos;

            for (uint i = 0; i < count; ++i) {
                ActorBase actor = levelHandler.EventSpawner.SpawnEvent(type, eventParams, ActorInstantiationFlags.None, pos + new Vector3(0f, 0f, 10f));
                if (actor != null) {
                    StoreActor(actor);
                }
            }
        }
    }
}