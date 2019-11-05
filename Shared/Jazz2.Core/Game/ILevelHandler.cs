using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game
{
    public interface ILevelHandler
    {
        TileMap TileMap { get; }
        EventMap EventMap { get; }
        EventSpawner EventSpawner { get; }

        IEnumerable<GameObject> ActiveObjects { get; }

        GameDifficulty Difficulty { get; }
        Rect LevelBounds { get; }
        float Gravity { get; }
        float AmbientLightCurrent { get; set; }
        int WaterLevel { get; set; }
        List<Player> Players { get; }

        void AddActor(ActorBase actor);
        void RemoveActor(ActorBase actor);

        void PlayCommonSound(string name, ActorBase target, float gain = 1f, float pitch = 1f);
        void PlayCommonSound(string name, Vector3 pos, float gain = 1f, float pitch = 1f);

        void FindCollisionActorsByAABB(ActorBase self, AABB aabb, Func<ActorBase, bool> callback);
        void FindCollisionActorsByRadius(float x, float y, float radius, Func<ActorBase, bool> callback);
        bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards, out ActorBase collider);
        bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards);
        IEnumerable<Player> GetCollidingPlayers(AABB aabb);

        void WarpCameraToTarget(ActorBase target);
        void LimitCameraView(float left, float width);
        void ShakeCameraView(float duration);

        string GetLevelText(int textID);
        void BroadcastLevelText(string text);
        void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams);
        void BroadcastAnimationChanged(ActorBase actor, string identifier);
        void InitLevelChange(ExitType exitType, string nextLevel);
        void HandleGameOver();
        bool HandlePlayerDied(Player player);
    }
}