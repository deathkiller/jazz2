using System.Collections.Generic;
using System.Reflection;
using Duality;
using Jazz2.Actors;
using Jazz2.Actors.Bosses;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Lighting;
using Jazz2.Actors.Solid;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Events
{
    public class EventSpawner
    {
        public delegate ActorBase SpawnFunction(ActorInstantiationFlags flags, float x, float y, float z, ushort[] spawnParams);

        private readonly ActorApi api;

        private Dictionary<EventType, SpawnFunction> spawnableEvents;

        public EventSpawner(ActorApi api)
        {
            this.api = api;

            InitializeSpawnableList();
        }

        private void InitializeSpawnableList()
        {
            spawnableEvents = new Dictionary<EventType, SpawnFunction>();

            // Basic
            RegisterSpawnable<SavePoint>(EventType.SavePoint);

            // Triggers
            RegisterSpawnable<TriggerCrate>(EventType.TriggerCrate);

            // Warp
            RegisterSpawnable<BonusWarp>(EventType.WarpCoinBonus);

            // Lights
            RegisterSpawnable<StaticRadialLight>(EventType.LightSteady);
            RegisterSpawnable<PulsatingRadialLight>(EventType.LightPulse);
            RegisterSpawnable<FlickerLight>(EventType.LightFlicker);

            // Environment
            RegisterSpawnable<Spring>(EventType.Spring);
            RegisterSpawnable<DynamicBridge>(EventType.Bridge);
            RegisterSpawnable<MovingPlatform>(EventType.MovingPlatform);
            RegisterSpawnable<PushBox>(EventType.PushableBox);
            RegisterSpawnable<Eva>(EventType.Eva);
            RegisterSpawnable<Pole>(EventType.Pole);
            RegisterSpawnable<SignEol>(EventType.SignEOL);
            RegisterSpawnable<Moth>(EventType.Moth);
            RegisterSpawnable<SteamNote>(EventType.SteamNote);
            RegisterSpawnable<Bomb>(EventType.Bomb);
            RegisterSpawnable<PinballBumper>(EventType.PinballBumper);
            RegisterSpawnable<PinballPaddle>(EventType.PinballPaddle);

            RegisterSpawnable<AmbientSound>(EventType.AmbientSound);
            RegisterSpawnable<AmbientBubbles>(EventType.AmbientBubbles);

            // Enemies
            RegisterSpawnable<Turtle>(EventType.EnemyTurtle);
            RegisterSpawnable<Lizard>(EventType.EnemyLizard);
            RegisterSpawnable<LizardFloat>(EventType.EnemyLizardFloat);
            RegisterSpawnable<Dragon>(EventType.EnemyDragon);
            RegisterSpawnable<SuckerFloat>(EventType.EnemySuckerFloat);
            RegisterSpawnable<Sucker>(EventType.EnemySucker);
            RegisterSpawnable<LabRat>(EventType.EnemyLabRat);
            RegisterSpawnable<Helmut>(EventType.EnemyHelmut);
            RegisterSpawnable<Bat>(EventType.EnemyBat);
            RegisterSpawnable<FatChick>(EventType.EnemyFatChick);
            RegisterSpawnable<Fencer>(EventType.EnemyFencer);
            RegisterSpawnable<Rapier>(EventType.EnemyRapier);
            RegisterSpawnable<Sparks>(EventType.EnemySparks);
            RegisterSpawnable<Monkey>(EventType.EnemyMonkey);
            RegisterSpawnable<Demon>(EventType.EnemyDemon);
            RegisterSpawnable<Bee>(EventType.EnemyBee);
            //RegisterSpawnable<BeeSwarm>(EventType.EnemyBeeSwarm);
            RegisterSpawnable<Caterpillar>(EventType.EnemyCaterpillar);
            RegisterSpawnable<Crab>(EventType.EnemyCrab);
            RegisterSpawnable<Doggy>(EventType.EnemyDoggy);
            RegisterSpawnable<Dragonfly>(EventType.EnemyDragonfly);
            RegisterSpawnable<Fish>(EventType.EnemyFish);
            RegisterSpawnable<MadderHatter>(EventType.EnemyMadderHatter);
            RegisterSpawnable<Raven>(EventType.EnemyRaven);
            RegisterSpawnable<Skeleton>(EventType.EnemySkeleton);
            RegisterSpawnable<Actors.Enemies.TurtleTough>(EventType.EnemyTurtleTough);
            RegisterSpawnable<TurtleTube>(EventType.EnemyTurtleTube);
            RegisterSpawnable<Witch>(EventType.EnemyWitch);

            RegisterSpawnable<TurtleShell>(EventType.TurtleShell);

            RegisterSpawnable<Bilsy>(EventType.BossBilsy);
            RegisterSpawnable<Devan>(EventType.BossDevan);
            RegisterSpawnable<DevanRemote>(EventType.BossDevanRemote);
            RegisterSpawnable<Queen>(EventType.BossQueen);
            RegisterSpawnable<Robot>(EventType.BossRobot);
            RegisterSpawnable<Tweedle>(EventType.BossTweedle);
            RegisterSpawnable<Uterus>(EventType.BossUterus);
            RegisterSpawnable<Actors.Bosses.TurtleTough>(EventType.BossTurtleTough);
            RegisterSpawnable<Bubba>(EventType.BossBubba);
            RegisterSpawnable<Bolly>(EventType.BossBolly);

            // Collectibles
            RegisterSpawnable<GemCollectible>(EventType.Gem);
            RegisterSpawnable<CoinCollectible>(EventType.Coin);
            RegisterSpawnable<CarrotCollectible>(EventType.Carrot);
            RegisterSpawnable<CarrotFlyCollectible>(EventType.CarrotFly);
            RegisterSpawnable<CarrotInvincibleCollectible>(EventType.CarrotInvincible);
            RegisterSpawnable<OneUpCollectible>(EventType.OneUp);
            RegisterSpawnable<FastFireCollectible>(EventType.FastFire);

            RegisterSpawnable<AmmoCrate>(EventType.CrateAmmo);
            RegisterSpawnable<AmmoBarrel>(EventType.BarrelAmmo);
            RegisterSpawnable<CrateContainer>(EventType.Crate);
            RegisterSpawnable<BarrelContainer>(EventType.Barrel);
            RegisterSpawnable<GemCrate>(EventType.CrateGem);
            RegisterSpawnable<GemBarrel>(EventType.BarrelGem);
            RegisterSpawnable<GemGiant>(EventType.GemGiant);

            RegisterSpawnable<PowerUpSwapMonitor>(EventType.PowerUpSwap);

            RegisterSpawnable<AirboardGenerator>(EventType.AirboardGenerator);

            for (EventType eventNo = EventType.AmmoBouncer; eventNo <= EventType.AmmoElectro; eventNo++) {
                RegisterSpawnable<AmmoCollectible>(eventNo, (ushort)(eventNo - EventType.AmmoBouncer + WeaponType.Bouncer));
            }

            for (EventType eventNo = EventType.PowerUpBlaster; eventNo <= EventType.PowerUpElectro; eventNo++) {
                RegisterSpawnable<PowerUpWeaponMonitor>(eventNo, (ushort)(eventNo - EventType.PowerUpBlaster));
            }

            for (EventType eventNo = EventType.FoodApple; eventNo <= EventType.FoodCheese; eventNo++) {
                RegisterSpawnable<FoodCollectible>(eventNo, (ushort)eventNo);
            }
        }

        public ActorBase SpawnEvent(ActorInstantiationFlags flags, EventType type, int x, int y, float z, ushort[] spawnParams)
        {
            return SpawnEvent(flags, type, new Vector3(x * 32 + 16, y * 32 + 16, z), spawnParams);
        }

        public ActorBase SpawnEvent(ActorInstantiationFlags flags, EventType type, Vector3 pos, ushort[] spawnParams)
        {
            SpawnFunction f;
            if (!spawnableEvents.TryGetValue(type, out f)) {
                return null;
            }

            return f(flags, pos.X, pos.Y, pos.Z, spawnParams);
        }

        public void RegisterSpawnable(EventType type, SpawnFunction spawner)
        {
            spawnableEvents.Add(type, spawner);
        }

        public void RegisterSpawnable<T>(EventType type) where T : ActorBase//, new()
        {
            RegisterSpawnable(type, CreateCommonActorEvent<T>);
        }

        public void RegisterSpawnable<T>(EventType type, params ushort[] spawnParams) where T : ActorBase//, new()
        {
            RegisterSpawnable(type, (fromEventMap, x, y, z, p) => {
                return CreateCommonActorEvent<T>(fromEventMap, x, y, z, spawnParams);
            });
        }

        private ActorBase CreateCommonActorEvent<T>(ActorInstantiationFlags flags, float x, float y, float z, params ushort[] spawnParams) where T : ActorBase
        {
            T actor = typeof(T).GetTypeInfo().CreateInstanceOf() as T;
            actor.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = new Vector3(x, y, z),
                Flags = flags,
                Params = spawnParams
            });
            return actor;
        }
    }
}