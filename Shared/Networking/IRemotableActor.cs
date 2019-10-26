using Duality;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Client;

namespace Jazz2.Networking
{
    public interface IRemotableActor
    {
        int Index { get; set; }

        //void OnCreateRemotableActor(ref CreateRemotableActor p);

        //void OnUpdateRemotableActor(ref UpdateRemotableActor p);

        void OnUpdateRemoteActor(Vector3 pos, Vector2 speed, AnimState animState, float animTime, bool isFacingLeft);

    }
}
