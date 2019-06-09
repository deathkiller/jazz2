using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game
{
    public class ActorApi
    {
        private readonly LevelHandler levelHandler;

        public TileMap TileMap
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.TileMap; }
        }

        public EventMap EventMap
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.EventMap; }
        }

        public EventSpawner EventSpawner
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.EventSpawner; }
        }

        public IEnumerable<GameObject> ActiveObjects
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.ActiveObjects; }
        }

        public float Gravity
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.Gravity; }
        }

        public Rect LevelBounds
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.LevelBounds; }
        }

        public GameDifficulty Difficulty
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.Difficulty; }
        }

        public float AmbientLight
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.AmbientLightCurrent; }
            set { levelHandler.AmbientLightCurrent = value; }
        }

        public int WaterLevel
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.WaterLevel; }
            set { levelHandler.WaterLevel = value; }
        }

        public List<Player> Players
        {
#if NET45
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get { return levelHandler.Players; }
        }

        public ActorApi(LevelHandler levelHandler)
        {
            this.levelHandler = levelHandler;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void AddActor(ActorBase actor)
        {
            levelHandler.AddActor(actor);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void RemoveActor(ActorBase actor)
        {
            levelHandler.RemoveActor(actor);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void FindCollisionActorsByAABB(ActorBase self, AABB aabb, Func<ActorBase, bool> callback)
        {
            levelHandler.FindCollisionActorsByAABB(self, aabb, callback);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void FindCollisionActorsByRadius(float x, float y, float radius, Func<ActorBase, bool> callback)
        {
            levelHandler.FindCollisionActorsByRadius(x, y, radius, callback);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards, out ActorBase collider)
        {
            return levelHandler.IsPositionEmpty(self, ref aabb, downwards, out collider);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards)
        {
            return levelHandler.IsPositionEmpty(self, ref aabb, downwards);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public IEnumerable<Player> GetCollidingPlayers(AABB aabb)
        {
            return levelHandler.GetCollidingPlayers(aabb);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void WarpCameraToTarget(ActorBase target)
        {
            levelHandler.WarpCameraToTarget(target);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void LimitCameraView(float left, float width)
        {
            levelHandler.LimitCameraView(left, width);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ShakeCameraView(float duration)
        {
            levelHandler.ShakeCameraView(duration);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void PlayCommonSound(ActorBase target, string name, float gain = 1f)
        {
            levelHandler.PlayCommonSound(name, target, gain);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public string GetLevelText(int textID)
        {
            return levelHandler.GetLevelText(textID);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void BroadcastLevelText(string text)
        {
            levelHandler.BroadcastLevelText(text);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void BroadcastLevelText(int id)
        {
            levelHandler.BroadcastLevelText(levelHandler.GetLevelText(id));
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            levelHandler.BroadcastTriggeredEvent(eventType, eventParams);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void InitLevelChange(ExitType exitType, string nextLevel)
        {
            levelHandler.InitLevelChange(exitType, nextLevel);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void HandleGameOver()
        {
            levelHandler.HandleGameOver();
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool HandlePlayerDied(Player player)
        {
            return levelHandler.HandlePlayerDied(player);
        }
    }
}