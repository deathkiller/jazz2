#if MULTIPLAYER

using Duality;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkDriver : Component, ICmpInitializable, ICmpUpdatable
    {
        private LevelHandler levelHandler;

        public void OnInit(InitContext context)
        {
            levelHandler = GameObj.ParentScene as LevelHandler;
        }

        public void OnShutdown(ShutdownContext context)
        {
            levelHandler = null;
        }

        public void OnUpdate()
        {
            // ToDo
        }
    }
}

#endif