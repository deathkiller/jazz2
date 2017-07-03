using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Duality;
using Jazz2.Actors;
using Jazz2.Actors.Solid;
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

        public float Gravity => levelHandler.Gravity;

        public GameDifficulty Difficulty => levelHandler.Difficulty;

        public float AmbientLight {
            get { return levelHandler.AmbientLightCurrent; }
            set { levelHandler.AmbientLightCurrent = value; }
        }

        public int WaterLevel
        {
            get { return levelHandler.WaterLevel; }
            set { levelHandler.WaterLevel = value; }
        }

        public List<Player> Players
        {
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

        public ActorBase CreateActor(EventType type, float x, float y, float z, ushort[] spawnParams, bool returnOnly)
        {
            ActorBase actor = levelHandler.EventSpawner.SpawnEvent(ActorInstantiationFlags.None, type, (int)x / 32, (int)y / 32, z, spawnParams);
            if (!returnOnly) {
                levelHandler.AddActor(actor);
            }
            return actor;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public List<ActorBase> FindCollisionActors(ActorBase me)
        {
            return levelHandler.FindCollisionActors(me);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public List<ActorBase> FindCollisionActorsFast(ref Hitbox hitbox, ActorBase me)
        {
            return levelHandler.FindCollisionActorsFast(ref hitbox, me);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public List<ActorBase> FindCollisionActorsRadius(float x, float y, float radius)
        {
            return levelHandler.FindCollisionActorsRadius(x, y, radius);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsPositionEmpty(ref Hitbox hitbox, bool downwards, ActorBase me, out ActorBase collider) {
            return levelHandler.IsPositionEmpty(ref hitbox, downwards, me, out collider);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsPositionEmpty(ref Hitbox hitbox, bool downwards, ActorBase me)
        {
            return levelHandler.IsPositionEmpty(ref hitbox, downwards, me);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public List<Player> GetCollidingPlayers(ref Hitbox hitbox)
        {
            return levelHandler.GetCollidingPlayers(ref hitbox);
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
        public string GetLevelText(int textID)
        {
            return levelHandler.GetLevelText(textID);
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
        public Metadata RequestMetadata(string path)
        {
            return levelHandler.RequestMetadata(path);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void PlayCommonSound(ActorBase target, string name, float volume = 1f)
        {
            levelHandler.PlayCommonSound(name, target, volume);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool ActivateBoss(ushort musicFile)
        {
            return levelHandler.ActivateBoss(musicFile);
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
    }
}