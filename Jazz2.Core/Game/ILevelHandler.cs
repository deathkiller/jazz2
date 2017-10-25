using Duality;
using Jazz2.Actors;
using Jazz2.Game.Events;
using Jazz2.Game.Tiles;

namespace Jazz2.Game
{
    public interface ILevelHandler
    {
        ActorApi Api { get; }

        TileMap TileMap { get; }
        EventMap EventMap { get; }
        EventSpawner EventSpawner { get; }

        void AddActor(ActorBase actor);
        void RemoveActor(ActorBase actor);

        void PlayCommonSound(string name, Vector3 pos, float gain = 1f);
    }
}