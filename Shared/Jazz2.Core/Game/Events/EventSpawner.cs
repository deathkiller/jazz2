using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Actors.Bosses;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Lighting;
using Jazz2.Actors.Solid;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Events
{
    public class EventSpawner
    {
        public delegate ActorBase CreateFunction(ActorActivationDetails details);

        public delegate void PreloadFunction(ActorActivationDetails details);

        private struct SpawnableEvent
        {
            public CreateFunction CreateFunction;
            public PreloadFunction PreloadFunction;
        }

        private readonly ILevelHandler levelHandler;
        private readonly Dictionary<EventType, SpawnableEvent> spawnableEvents = new Dictionary<EventType, SpawnableEvent>();

        public EventSpawner(ILevelHandler levelHandler)
        {
            this.levelHandler = levelHandler;

            RegisterKnownSpawnables();
        }

        private void RegisterKnownSpawnables()
        {
            // Basic
            RegisterSpawnable(EventType.Checkpoint, Checkpoint.Create, Checkpoint.Preload);

            // Area
            RegisterSpawnable(EventType.AreaAmbientSound, AmbientSound.Create, AmbientSound.Preload);
            RegisterSpawnable(EventType.AreaAmbientBubbles, AmbientBubbles.Create, AmbientBubbles.Preload);

            // Triggers
            RegisterSpawnable(EventType.TriggerCrate, TriggerCrate.Create, TriggerCrate.Preload);

            // Warp
            RegisterSpawnable(EventType.WarpCoinBonus, BonusWarp.Create, BonusWarp.Preload);

            // Lights
            RegisterSpawnable(EventType.LightSteady, StaticRadialLight.Create);
            RegisterSpawnable(EventType.LightPulse, PulsatingRadialLight.Create);
            RegisterSpawnable(EventType.LightFlicker, FlickerLight.Create);
            RegisterSpawnable(EventType.LightIlluminate, IlluminateLight.Create);

            // Environment
            RegisterSpawnable(EventType.Spring, Spring.Create, Spring.Preload);
            RegisterSpawnable(EventType.Bridge, Bridge.Create, Bridge.Preload);
            RegisterSpawnable(EventType.MovingPlatform, MovingPlatform.Create, MovingPlatform.Preload);
            RegisterSpawnable(EventType.SpikeBall, SpikeBall.Create, SpikeBall.Preload);
            RegisterSpawnable(EventType.PushableBox, PushBox.Create, PushBox.Preload);
            RegisterSpawnable(EventType.Eva, Eva.Create, Eva.Preload);
            RegisterSpawnable(EventType.Pole, Pole.Create, Pole.Preload);
            RegisterSpawnable(EventType.SignEOL, SignEol.Create, SignEol.Preload);
            RegisterSpawnable(EventType.Moth, Moth.Create, Moth.Preload);
            RegisterSpawnable(EventType.SteamNote, SteamNote.Create, SteamNote.Preload);
            RegisterSpawnable(EventType.Bomb, Bomb.Create, Bomb.Preload);
            RegisterSpawnable(EventType.PinballBumper, PinballBumper.Create, PinballBumper.Preload);
            RegisterSpawnable(EventType.PinballPaddle, PinballPaddle.Create, PinballPaddle.Preload);

            // Enemies
            RegisterSpawnable(EventType.EnemyTurtle, Turtle.Create, Turtle.Preload);
            RegisterSpawnable(EventType.EnemyLizard, Lizard.Create, Lizard.Preload);
            RegisterSpawnable(EventType.EnemyLizardFloat, LizardFloat.Create, LizardFloat.Preload);
            RegisterSpawnable(EventType.EnemyDragon, Dragon.Create, Dragon.Preload);
            RegisterSpawnable(EventType.EnemySuckerFloat, SuckerFloat.Create, SuckerFloat.Preload);
            RegisterSpawnable(EventType.EnemySucker, Sucker.Create, Sucker.Preload);
            RegisterSpawnable(EventType.EnemyLabRat, LabRat.Create, LabRat.Preload);
            RegisterSpawnable(EventType.EnemyHelmut, Helmut.Create, Helmut.Preload);
            RegisterSpawnable(EventType.EnemyBat, Bat.Create, Bat.Preload);
            RegisterSpawnable(EventType.EnemyFatChick, FatChick.Create, FatChick.Preload);
            RegisterSpawnable(EventType.EnemyFencer, Fencer.Create, Fencer.Preload);
            RegisterSpawnable(EventType.EnemyRapier, Rapier.Create, Rapier.Preload);
            RegisterSpawnable(EventType.EnemySparks, Sparks.Create, Sparks.Preload);
            RegisterSpawnable(EventType.EnemyMonkey, Monkey.Create, Monkey.Preload);
            RegisterSpawnable(EventType.EnemyDemon, Demon.Create, Demon.Preload);
            RegisterSpawnable(EventType.EnemyBee, Bee.Create, Bee.Preload);
            //RegisterSpawnable(EventType.EnemyBeeSwarm, BeeSwarm.Create, BeeSwarm.Preload);
            RegisterSpawnable(EventType.EnemyCaterpillar, Caterpillar.Create, Caterpillar.Preload);
            RegisterSpawnable(EventType.EnemyCrab, Crab.Create, Crab.Preload);
            RegisterSpawnable(EventType.EnemyDoggy, Doggy.Create, Doggy.Preload);
            RegisterSpawnable(EventType.EnemyDragonfly, Dragonfly.Create, Dragonfly.Preload);
            RegisterSpawnable(EventType.EnemyFish, Fish.Create, Fish.Preload);
            RegisterSpawnable(EventType.EnemyMadderHatter, MadderHatter.Create, MadderHatter.Preload);
            RegisterSpawnable(EventType.EnemyRaven, Raven.Create, Raven.Preload);
            RegisterSpawnable(EventType.EnemySkeleton, Skeleton.Create, Skeleton.Preload);
            RegisterSpawnable(EventType.EnemyTurtleTough, TurtleTough.Create, TurtleTough.Preload);
            RegisterSpawnable(EventType.EnemyTurtleTube, TurtleTube.Create, TurtleTube.Preload);
            RegisterSpawnable(EventType.EnemyWitch, Witch.Create, Witch.Preload);

            RegisterSpawnable(EventType.TurtleShell, TurtleShell.Create, TurtleShell.Preload);

            RegisterSpawnable(EventType.BossBilsy, Bilsy.Create, Bilsy.Preload);
            RegisterSpawnable(EventType.BossDevan, Devan.Create, Devan.Preload);
            RegisterSpawnable(EventType.BossDevanRemote, DevanRemote.Create, DevanRemote.Preload);
            RegisterSpawnable(EventType.BossQueen, Queen.Create, Queen.Preload);
            RegisterSpawnable(EventType.BossRobot, Robot.Create, Robot.Preload);
            RegisterSpawnable(EventType.BossTweedle, Tweedle.Create, Tweedle.Preload);
            RegisterSpawnable(EventType.BossUterus, Uterus.Create, Uterus.Preload);
            RegisterSpawnable(EventType.BossTurtleTough, TurtleToughBoss.Create, TurtleToughBoss.Preload);
            RegisterSpawnable(EventType.BossBubba, Bubba.Create, Bubba.Preload);
            RegisterSpawnable(EventType.BossBolly, Bolly.Create, Bolly.Preload);

            // Collectibles
            RegisterSpawnable(EventType.Gem, GemCollectible.Create, GemCollectible.Preload);
            RegisterSpawnable(EventType.Coin, CoinCollectible.Create, CoinCollectible.Preload);
            RegisterSpawnable(EventType.Carrot, CarrotCollectible.Create, CarrotCollectible.Preload);
            RegisterSpawnable(EventType.CarrotFly, CarrotFlyCollectible.Create, CarrotFlyCollectible.Preload);
            RegisterSpawnable(EventType.CarrotInvincible, CarrotInvincibleCollectible.Create, CarrotInvincibleCollectible.Preload);
            RegisterSpawnable(EventType.OneUp, OneUpCollectible.Create, OneUpCollectible.Preload);
            RegisterSpawnable(EventType.FastFire, FastFireCollectible.Create, FastFireCollectible.Preload);

            RegisterSpawnable(EventType.CrateAmmo, AmmoCrate.Create, AmmoCrate.Preload);
            RegisterSpawnable(EventType.BarrelAmmo, AmmoBarrel.Create, AmmoBarrel.Preload);
            RegisterSpawnable(EventType.Crate, CrateContainer.Create, CrateContainer.Preload);
            RegisterSpawnable(EventType.Barrel, BarrelContainer.Create, BarrelContainer.Preload);
            RegisterSpawnable(EventType.CrateGem, GemCrate.Create, GemCrate.Preload);
            RegisterSpawnable(EventType.BarrelGem, GemBarrel.Create, GemBarrel.Preload);
            RegisterSpawnable(EventType.GemGiant, GemGiant.Create, GemGiant.Preload);
            RegisterSpawnable(EventType.GemRing, GemRing.Create, GemRing.Preload);

            RegisterSpawnable(EventType.PowerUpMorph, PowerUpMorphMonitor.Create, PowerUpMorphMonitor.Preload);
            RegisterSpawnable(EventType.BirdCage, BirdCage.Create, BirdCage.Preload);

            RegisterSpawnable(EventType.AirboardGenerator, AirboardGenerator.Create, AirboardGenerator.Preload);
            RegisterSpawnable(EventType.Copter, Copter.Create, Copter.Preload);

            RegisterSpawnable(EventType.RollingRock, RollingRock.Create, RollingRock.Preload);
            RegisterSpawnable(EventType.SwingingVine, SwingingVine.Create, SwingingVine.Preload);

            RegisterSpawnable(EventType.PowerUpShield, PowerUpShieldMonitor.Create, PowerUpShieldMonitor.Preload);
            RegisterSpawnable(EventType.Stopwatch, Stopwatch.Create, Stopwatch.Preload);

            RegisterSpawnable(EventType.Ammo, AmmoCollectible.Create, AmmoCollectible.Preload);
            RegisterSpawnable(EventType.PowerUpWeapon, PowerUpWeaponMonitor.Create, PowerUpWeaponMonitor.Preload);
            RegisterSpawnable(EventType.Food, FoodCollectible.Create, FoodCollectible.Preload);


            // Multiplayer-only remotable actors
            RegisterSpawnable(EventType.WeaponBlaster, AmmoBlaster.Create);
            RegisterSpawnable(EventType.WeaponBouncer, AmmoBouncer.Create);
            RegisterSpawnable(EventType.WeaponElectro, AmmoElectro.Create);
            RegisterSpawnable(EventType.WeaponFreezer, AmmoFreezer.Create);
            RegisterSpawnable(EventType.WeaponPepper, AmmoPepper.Create);
            RegisterSpawnable(EventType.WeaponRF, AmmoRF.Create);
            RegisterSpawnable(EventType.WeaponSeeker, AmmoSeeker.Create);
            RegisterSpawnable(EventType.WeaponThunderbolt, AmmoThunderbolt.Create);
            RegisterSpawnable(EventType.WeaponTNT, AmmoTNT.Create);
            RegisterSpawnable(EventType.WeaponToaster, AmmoToaster.Create);
        }

        public void PreloadEvent(EventType type, ushort[] spawnParams)
        {
            if (!spawnableEvents.TryGetValue(type, out var e) || e.PreloadFunction == null) {
                return;
            }

            e.PreloadFunction(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Params = spawnParams
            });
        }

        public ActorBase SpawnEvent(EventType type, ushort[] spawnParams, ActorInstantiationFlags flags, int x, int y, float z)
        {
            return SpawnEvent(type, spawnParams, flags, new Vector3(x * 32 + 16, y * 32 + 16, z));
        }

        public ActorBase SpawnEvent(EventType type, ushort[] spawnParams, ActorInstantiationFlags flags, Vector3 pos)
        {
            if (!spawnableEvents.TryGetValue(type, out var e)) {
                return null;
            }

            return e.CreateFunction(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = pos,
                Flags = flags,
                Params = spawnParams
            });
        }

        private void RegisterSpawnable(EventType type, CreateFunction create, PreloadFunction preload = null)
        {
            spawnableEvents.Add(type, new SpawnableEvent {
                CreateFunction = create,
                PreloadFunction = preload
            });
        }
    }
}