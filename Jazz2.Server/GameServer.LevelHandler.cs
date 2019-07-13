using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Server
{
    partial class GameServer : ILevelHandler
    {
        ActorApi ILevelHandler.Api => api;

        TileMap ILevelHandler.TileMap => tileMap;

        // ToDo
        EventMap ILevelHandler.EventMap => null;

        EventSpawner ILevelHandler.EventSpawner => eventSpawner;

        IEnumerable<GameObject> ILevelHandler.ActiveObjects => null;

        GameDifficulty ILevelHandler.Difficulty => GameDifficulty.Multiplayer;

        Rect ILevelHandler.LevelBounds => levelBounds;

        float ILevelHandler.Gravity => LevelHandler.DefaultGravity;

        float ILevelHandler.AmbientLightCurrent
        {
            get
            {
                return 1;
            }
            set
            {
                //
            }
        }
        int ILevelHandler.WaterLevel
        {
            get
            {
                return int.MaxValue;
            }
            set
            {
                //
            }
        }

        List<Actors.Player> ILevelHandler.Players => new List<Actors.Player>();

        void ILevelHandler.AddActor(ActorBase actor)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.BroadcastLevelText(string text)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.FindCollisionActorsByAABB(ActorBase self, AABB aabb, Func<ActorBase, bool> callback)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.FindCollisionActorsByRadius(float x, float y, float radius, Func<ActorBase, bool> callback)
        {
            //throw new NotImplementedException();
        }

        IEnumerable<Actors.Player> ILevelHandler.GetCollidingPlayers(AABB aabb)
        {
            //throw new NotImplementedException();
            return new Actors.Player[0];
        }

        string ILevelHandler.GetLevelText(int textID)
        {
            //throw new NotImplementedException();
            return null;
        }

        void ILevelHandler.HandleGameOver()
        {
            //throw new NotImplementedException();
        }

        bool ILevelHandler.HandlePlayerDied(Actors.Player player)
        {
            //throw new NotImplementedException();
            return false;
        }

        void ILevelHandler.InitLevelChange(ExitType exitType, string nextLevel)
        {
            //throw new NotImplementedException();
        }

        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards, out ActorBase collider)
        {
            collider = null;

            if ((self.CollisionFlags & CollisionFlags.CollideWithTileset) != 0) {
                if (!tileMap.IsTileEmpty(ref aabb, downwards)) {
                    return false;
                }
            }

            return true;
        }

        public bool IsPositionEmpty(ActorBase self, ref AABB aabb, bool downwards)
        {
            ActorBase solidObject;
            return IsPositionEmpty(self, ref aabb, downwards, out solidObject);
        }

        void ILevelHandler.LimitCameraView(float left, float width)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.PlayCommonSound(string name, ActorBase target, float gain, float pitch)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.PlayCommonSound(string name, Vector3 pos, float gain)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.RemoveActor(ActorBase actor)
        {
            throw new NotImplementedException();
        }

        void ILevelHandler.ShakeCameraView(float duration)
        {
            //throw new NotImplementedException();
        }

        void ILevelHandler.WarpCameraToTarget(ActorBase target)
        {
            //throw new NotImplementedException();
        }
    }
}