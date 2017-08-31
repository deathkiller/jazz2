using System;
using System.Collections.Generic;
using Import;
using Jazz2.Game.Structs;

namespace Jazz2.Compatibility
{
    public class EventConverter
    {
        public enum JJ2Event : byte
        {
            JJ2_EMPTY = 0x00, // -
            JJ2_MODIFIER_ONE_WAY = 0x01, // -
            JJ2_MODIFIER_HURT = 0x02, // -
            JJ2_MODIFIER_VINE = 0x03, // -
            JJ2_MODIFIER_HOOK = 0x04, // -
            JJ2_MODIFIER_SLIDE = 0x05, // -
            JJ2_MODIFIER_H_POLE = 0x06, // -
            JJ2_MODIFIER_V_POLE = 0x07, // -
            JJ2_AREA_FLY_OFF = 0x08, // -
            JJ2_MODIFIER_RICOCHET = 0x09, // -
            JJ2_MODIFIER_BELT_RIGHT = 0x0A, // Speed: 8s
            JJ2_MODIFIER_BELT_LEFT = 0x0B, // Speed: 8s
            JJ2_MODIFIER_ACC_BELT_RIGHT = 0x0C, // Speed: 8s
            JJ2_MODIFIER_ACC_BELT_LEFT = 0x0D, // Speed: 8s
            JJ2_AREA_STOP_ENEMY = 0x0E, // -
            JJ2_MODIFIER_WIND_LEFT = 0x0F, // Speed: 8s
            JJ2_MODIFIER_WIND_RIGHT = 0x10, // Speed: 8s
            JJ2_AREA_EOL = 0x11, // Secret: bool
            JJ2_AREA_EOL_WARP = 0x12, // -
            JJ2_AREA_REVERT_MORPH = 0x13, // -
            JJ2_AREA_FLOAT_UP = 0x14, // -
            JJ2_TRIGGER_ROCK = 0x15, // ID: 8u
            JJ2_LIGHT_DIM = 0x16, // -
            JJ2_LIGHT_SET = 0x17, // Intensity: 7u, Red: 4s, Green: 4s, Blue: 4s, Flicker: bool
            JJ2_AREA_LIMIT_X_SCROLL = 0x18, // -
            JJ2_LIGHT_RESET = 0x19, // -
            JJ2_AREA_SECRET_WARP = 0x1A, // Coins: 10u
            JJ2_MODIFIER_ECHO = 0x1B, // Amount: 8u
            JJ2_AREA_ACTIVATE_BOSS = 0x1C, // Music 2: bool
            JJ2_JAZZ_LEVEL_START = 0x1D, // Pos: 4u *CHECK*
            JJ2_SPAZ_LEVEL_START = 0x1E, // Pos: 4u *CHECK*
            JJ2_MP_LEVEL_START = 0x1F, // Team: bool
            JJ2_LORI_LEVEL_START = 0x20,
            JJ2_AMMO_FREEZER = 0x21, // -
            JJ2_AMMO_BOUNCER = 0x22, // -
            JJ2_AMMO_SEEKER = 0x23, // -
            JJ2_AMMO_RF = 0x24, // -
            JJ2_AMMO_TOASTER = 0x25, // -
            JJ2_AMMO_TNT = 0x26, // -
            JJ2_AMMO_PEPPER = 0x27, // -
            JJ2_AMMO_ELECTRO = 0x28, // -
            JJ2_TURTLE_SHELL = 0x29, // -
            JJ2_SWINGING_VINE = 0x2A, // -
            JJ2_SCENERY_BOMB = 0x2B, // -
            JJ2_COIN_SILVER = 0x2C, // -
            JJ2_COIN_GOLD = 0x2D, // -
            JJ2_CRATE_AMMO = 0x2E, // Extra Event: 8u, Num. Extra Events: 4u, Random Fly: bool
            JJ2_CRATE_CARROT = 0x2F, // Extra Event: 8u, Num. Extra Events: 4u, Random Fly: bool
            JJ2_CRATE_ONEUP = 0x30, // Extra Event: 8u, Num. Extra Events: 4u, Random Fly: bool
            JJ2_BARREL_GEM = 0x31, // Red: 4u, Green: 4u, Blue: 4u, Purple: 4u
            JJ2_BARREL_CARROT = 0x32, // -
            JJ2_BARREL_ONEUP = 0x33, // -
            JJ2_CRATE_BOMB = 0x34, // Extra Event: 8u, Num. Extra Events: 4u, Random Fly: bool
            JJ2_CRATE_AMMO_FREEZER = 0x35, // -
            JJ2_CRATE_AMMO_BOUNCER = 0x36, // -
            JJ2_CRATE_AMMO_SEEKER = 0x37, // -
            JJ2_CRATE_AMMO_RF = 0x38, // -
            JJ2_CRATE_AMMO_TOASTER = 0x39, // -
            JJ2_SCENERY_TNT = 0x3A, // -
            JJ2_AIRBOARD = 0x3B, // -
            JJ2_SPRING_GREEN_FROZEN = 0x3C, // Ceiling: bool, Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_FAST_FIRE = 0x3D, // -
            JJ2_CRATE_SPRING = 0x3E, // Extra Event: 8u, Num. Extra Events: 4u, Random Fly: bool
            JJ2_GEM_RED = 0x3F, // -
            JJ2_GEM_GREEN = 0x40, // -
            JJ2_GEM_BLUE = 0x41, // -
            JJ2_GEM_PURPLE = 0x42, // -
            JJ2_GEM_SUPER = 0x43, // -
            JJ2_BIRDY = 0x44, // Chuck: bool
            JJ2_BARREL_AMMO = 0x45, // -
            JJ2_CRATE_GEM = 0x46, // Red: 4u, Green: 4u, Blue: 4u, Purple: 4u
            JJ2_POWERUP_SWAP = 0x47, // -
            JJ2_CARROT = 0x48, // -
            JJ2_CARROT_FULL = 0x49, // -
            JJ2_SHIELD_FIRE = 0x4A, // -
            JJ2_SHIELD_WATER = 0x4B, // -
            JJ2_SHIELD_LIGHTNING = 0x4C, // -
            JJ2_MAX_WEAPON = 0x4D, // -
            JJ2_AREA_AUTO_FIRE = 0x4E, // -
            JJ2_FAST_FEET = 0x4F, // -
            JJ2_ONEUP = 0x50, // -
            JJ2_EOL_SIGN = 0x51, // Secret: bool; *CHECK* function
            JJ2_SAVE_POINT = 0x53, // -
            JJ2_BONUS_SIGN = 0x54, // -
            JJ2_SPRING_RED = 0x55, // Ceiling: bool, Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_SPRING_GREEN = 0x56, // Ceiling: bool, Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_SPRING_BLUE = 0x57, // Ceiling: bool, Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_CARROT_INVINCIBLE = 0x58, // -
            JJ2_SHIELD_TIME = 0x59, // -
            JJ2_FREEZE = 0x5A, // -
            JJ2_SPRING_RED_HOR = 0x5B, // Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_SPRING_GREEN_HOR = 0x5C, // Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_SPRING_BLUE_HOR = 0x5D, // Keep X Speed: bool, Keep Y Speed: bool, Delay: 4u
            JJ2_POWERUP_BIRD = 0x5E, // -
            JJ2_TRIGGER_CRATE = 0x5F, // Trigger ID: 5u
            JJ2_CARROT_FLY = 0x60, // -
            JJ2_GEM_RED_RECT = 0x61, // -
            JJ2_GEM_GREEN_RECT = 0x62, // -
            JJ2_GEM_BLUE_RECT = 0x63, // -
            JJ2_ENEMY_TUF_TURT = 0x64, // -
            JJ2_BOSS_TUF_TURT = 0x65, // Text: 4u
            JJ2_ENEMY_LAB_RAT = 0x66, // -
            JJ2_ENEMY_DRAGON = 0x67, // -
            JJ2_ENEMY_LIZARD = 0x68, // -
            JJ2_ENEMY_BEE = 0x69, // -
            JJ2_ENEMY_RAPIER = 0x6A, // -
            JJ2_ENEMY_SPARKS = 0x6B, // -
            JJ2_ENEMY_BAT = 0x6C, // -
            JJ2_ENEMY_SUCKER = 0x6D, // -
            JJ2_ENEMY_CATERPILLAR = 0x6E, // -
            JJ2_CHESHIRE_HOOK = 0x6F, // - *CHECK* verify which one was hook
            JJ2_CHESHIRE_2 = 0x70, // Duration: 8u *CHECK*
            JJ2_ENEMY_MADDER_HATTER = 0x71, // -
            JJ2_BOSS_BILSY = 0x72, // Text: 4u
            JJ2_ENEMY_SKELETON = 0x73, // -
            JJ2_ENEMY_DOGGY_DOGG = 0x74, // -
            JJ2_ENEMY_TURTLE_NORMAL = 0x75, // -
            JJ2_ENEMY_HELMUT = 0x76, // -
            JJ2_LEAF = 0x77, // -
            JJ2_ENEMY_DEMON = 0x78, // -
            JJ2_FIRE = 0x79, // -
            JJ2_LAVA = 0x7A, // -
            JJ2_ENEMY_DRAGONFLY = 0x7B, // -
            JJ2_ENEMY_MONKEY = 0x7C, // -
            JJ2_ENEMY_FAT_CHICK = 0x7D, // -
            JJ2_ENEMY_FENCER = 0x7E, // -
            JJ2_ENEMY_FISH = 0x7F, // -
            JJ2_MOTH = 0x80, // Type: 3u
            JJ2_STEAM = 0x81, // -
            JJ2_ROTATING_ROCK = 0x82, // Rock ID: 8u, X Speed: 4s, Y Speed: 4s
            JJ2_POWERUP_BLASTER = 0x83, // -
            JJ2_POWERUP_BOUNCER = 0x84, // -
            JJ2_POWERUP_FREEZER = 0x85, // -
            JJ2_POWERUP_SEEKER = 0x86, // -
            JJ2_POWERUP_RF = 0x87, // -
            JJ2_POWERUP_TOASTER = 0x88, // -
            JJ2_PINBALL_PADDLE_L = 0x89, // -
            JJ2_PINBALL_PADDLE_R = 0x8A, // -
            JJ2_PINBALL_BUMP_500 = 0x8B, // -
            JJ2_PINBALL_BUMP_CARROT = 0x8C, // -
            JJ2_FOOD_APPLE = 0x8D, // -
            JJ2_FOOD_BANANA = 0x8E, // -
            JJ2_FOOD_CHERRY = 0x8F, // -
            JJ2_FOOD_ORANGE = 0x90, // -
            JJ2_FOOD_PEAR = 0x91, // -
            JJ2_FOOD_PRETZEL = 0x92, // -
            JJ2_FOOD_STRAWBERRY = 0x93, // -
            JJ2_LIGHT_STEADY = 0x94, // -
            JJ2_LIGHT_PULSE = 0x95, // Speed: 8u, Sync: 4u
            JJ2_LIGHT_FLICKER = 0x96, // Sample: 8u
            JJ2_BOSS_QUEEN = 0x97, // Text: 4u
            JJ2_ENEMY_SUCKER_FLOAT = 0x98, // -
            JJ2_BRIDGE = 0x99, // Width: 4u, Type: 3u, Toughness: 4u
            JJ2_FOOD_LEMON = 0x9A, // -
            JJ2_FOOD_LIME = 0x9B, // -
            JJ2_FOOD_THING = 0x9C, // - *CHECK* give a better name
            JJ2_FOOD_WATERMELON = 0x9D, // -
            JJ2_FOOD_PEACH = 0x9E, // -
            JJ2_FOOD_GRAPES = 0x9F, // -
            JJ2_FOOD_LETTUCE = 0xA0, // -
            JJ2_FOOD_EGGPLANT = 0xA1, // -
            JJ2_FOOD_CUCUMBER = 0xA2, // -
            JJ2_FOOD_PEPSI = 0xA3, // -
            JJ2_FOOD_COKE = 0xA4, // -
            JJ2_FOOD_MILK = 0xA5, // -
            JJ2_FOOD_PIE = 0xA6, // -
            JJ2_FOOD_CAKE = 0xA7, // -
            JJ2_FOOD_DONUT = 0xA8, // -
            JJ2_FOOD_CUPCAKE = 0xA9, // -
            JJ2_FOOD_CHIPS = 0xAA, // -
            JJ2_FOOD_CANDY = 0xAB, // -
            JJ2_FOOD_CHOCOLATE = 0xAC, // -
            JJ2_FOOD_ICE_CREAM = 0xAD, // -
            JJ2_FOOD_BURGER = 0xAE, // -
            JJ2_FOOD_PIZZA = 0xAF, // -
            JJ2_FOOD_FRIES = 0xB0, // -
            JJ2_FOOD_CHICKEN_LEG = 0xB1, // -
            JJ2_FOOD_SANDWICH = 0xB2, // -
            JJ2_FOOD_TACO = 0xB3, // -
            JJ2_FOOD_HOT_DOG = 0xB4, // -
            JJ2_FOOD_HAM = 0xB5, // -
            JJ2_FOOD_CHEESE = 0xB6, // -
            JJ2_ENEMY_LIZARD_FLOAT = 0xB7, // Copter Duration: 8u, Copter Drop: bool
            JJ2_ENEMY_MONKEY_STAND = 0xB8, // -
            JJ2_SCENERY_DESTRUCT = 0xB9, // Empty: 10u, SpeedDestr: 5u, Weapon: 4u
            JJ2_SCENERY_DESTR_BOMB = 0xBA, // Empty: 10 + 5 + 4u
            JJ2_SCENERY_COLLAPSE = 0xBB, // Wait: 10u, FPS: 5u, Empty: 4u
            JJ2_SCENERY_BUTTSTOMP = 0xBC, // Empty: 10 + 5 + 4u
            JJ2_SCENERY_GEMSTOMP = 0xBD, // -
            JJ2_ENEMY_RAVEN = 0xBE, // -
            JJ2_ENEMY_TURTLE_TUBE = 0xBF, // -
            JJ2_GEM_RING = 0xC0, // Length: 5u, Speed: 5u
            JJ2_SMALL_TREE = 0xC1, // Adjust Y: 5u, Adjust X: 6s
            JJ2_AMBIENT_SOUND = 0xC2, // Sample: 8u, Amplify: 8u, Fade: bool, Sine: bool
            JJ2_BOSS_UTERUS = 0xC3, // Text: 4u
            JJ2_ENEMY_CRAB = 0xC4, // -
            JJ2_ENEMY_WITCH = 0xC5, // -
            JJ2_BOSS_TURTLE_ROCKET = 0xC6, // Text: 4u
            JJ2_BOSS_BUBBA = 0xC7, // Text: 4u
            JJ2_BOSS_DEVAN_DEVIL = 0xC8, // Text: 4u
            JJ2_BOSS_DEVAN_ROBOT = 0xC9, // Intro Text: 4u, Text: 4u
            JJ2_BOSS_ROBOT = 0xCA, // -
            JJ2_POLE_CARROTUS = 0xCB, // Adjust Y: 5u, Adjust X: 6s
            JJ2_POLE_PSYCH = 0xCC, // Adjust Y: 5u, Adjust X: 6s
            JJ2_POLE_DIAMONDUS = 0xCD, // Adjust Y: 5u, Adjust X: 6s
            JJ2_MODIFIER_TUBE = 0xCE, // X Speed: 7s, Y Speed: 7s, Trig Sample (unknown) 3u, Wait Time: 3u
            JJ2_AREA_TEXT = 0xCF, // Text: 8u, Vanish: bool
            JJ2_MODIFIER_SET_WATER = 0xD0, // Height: 8u, Instant: bool
            JJ2_PLATFORM_FRUIT = 0xD1, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_PLATFORM_BOLL = 0xD2, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_PLATFORM_GRASS = 0xD3, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_PLATFORM_PINK = 0xD4, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_PLATFORM_SONIC = 0xD5, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_PLATFORM_SPIKE = 0xD6, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_BOLL_SPIKE = 0xD7, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool
            JJ2_MODIFIER_GENERATOR = 0xD8, // Event: 8u, Delay: 5u
            JJ2_EVA = 0xD9, // -
            JJ2_SCENERY_BUBBLER = 0xDA, // Speed: 4u
            JJ2_POWERUP_TNT = 0xDB, // -
            JJ2_POWERUP_PEPPER = 0xDC, // -
            JJ2_POWERUP_ELECTRO = 0xDD, // -
            JJ2_AREA_MORPH_FROG = 0xDE, // -
            JJ2_BOLL_SPIKE_3D = 0xDF, // Sync: 2u, Speed: 6s, Length: 4u, Swing: bool, Shade: bool
            JJ2_SPRINGCORD = 0xE0, // -
            JJ2_ENEMY_BEE_SWARM = 0xE1, // Num: 8u
            JJ2_COPTER = 0xE2, // Duration: 8u
            JJ2_SHIELD_LASER = 0xE3, // -
            JJ2_STOPWATCH = 0xE4, // -
            JJ2_POLE_JUNGLE = 0xE5, // Adjust Y: 5u, Adjust X: 6s
            JJ2_WARP_ORIGIN = 0xE6, // ID: 8u, Coins: 8u, Set Lap: bool, Show Anim: bool
            JJ2_PUSHABLE_ROCK = 0xE7, // -
            JJ2_PUSHABLE_BOX = 0xE8, // -
            JJ2_WATER_BLOCK = 0xE9, // Adjust Y: 8s
            JJ2_TRIGGER_AREA = 0xEA, // Trigger ID: 5u
            JJ2_BOSS_BOLLY = 0xEB, // -
            JJ2_ENEMY_BUTTERFLY = 0xEC, // -
            JJ2_ENEMY_BEEBOY = 0xED, // Swarm: 8u
            JJ2_SNOW = 0xEE, // -
            JJ2_WARP_TARGET = 0xF0, // ID: 8u
            JJ2_BOSS_TWEEDLE = 0xF1, // -
            JJ2_AREA_ID = 0xF2, // Text: 8u
            JJ2_CTF_BASE = 0xF4, // Team: bool, Direction: bool *CHECK*
            JJ2_AREA_NO_FIRE = 0xF5, // -
            JJ2_TRIGGER_ZONE = 0xF6, // Trigger ID: 5u, Set On: bool, Switch: bool

            // Base game - unnamed ones
            JJ2_EMPTY_32 = 0x20, //
            JJ2_EMPTY_82 = 0x52, //
            JJ2_EMPTY_239 = 0xEF, //
            JJ2_EMPTY_243 = 0xF3, // SP Airboard = ?
            JJ2_EMPTY_247 = 0xF7, // Text: 4u
            JJ2_EMPTY_248 = 0xF8, // -
            JJ2_EMPTY_249 = 0xF9, // -
            JJ2_EMPTY_250 = 0xFA, // Copter Duration: 8u, Copter Drop: bool
            JJ2_EMPTY_251 = 0xFB, // Text: 4u
            JJ2_EMPTY_252 = 0xFC, // -
            JJ2_EMPTY_253 = 0xFD, // -
            JJ2_EMPTY_254 = 0xFE, //
            JJ2_EMPTY_255 = 0xFF, //

            // Alternate names for TSF+ additions
            JJ2_BILSY_DUMMY = 0xF7,
            JJ2_ENEMY_NORMAL_TURTLE_XMAS = 0xF8,
            JJ2_ENEMY_LIZARD_XMAS = 0xF9,
            JJ2_ENEMY_LIZARD_FLOAT_XMAS = 0xFA,
            JJ2_EMPTY_BOSS_BILSY_XMAS = 0xFB,
            JJ2_EMPTY_TSF_DOG = 0xFC,
            JJ2_EMPTY_TSF_GHOST = 0xFD

        }

        public struct ConversionResult
        {
            public EventType eventType;
            public ushort[] eventParams;
        }

        public enum JJ2EventParamType
        {
            None,
            Bool,
            UInt,
            Int
        }

        public delegate ConversionResult ConversionFunction(JJ2Level level, uint e);

        private static Dictionary<JJ2Event, ConversionFunction> convert = new Dictionary<JJ2Event, ConversionFunction>();

        static EventConverter()
        {
            convert.Add(JJ2Event.JJ2_EMPTY, NoParamList(EventType.Empty));

            // Basic
            convert.Add(JJ2Event.JJ2_JAZZ_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x01));
            convert.Add(JJ2Event.JJ2_SPAZ_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x02));
            convert.Add(JJ2Event.JJ2_LORI_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x04));

            convert.Add(JJ2Event.JJ2_MP_LEVEL_START, ParamIntToParamList(EventType.LevelStartMP,
                Pair.Create(JJ2EventParamType.UInt, 2)  // Team (JJ2+)
            ));

            convert.Add(JJ2Event.JJ2_SAVE_POINT, NoParamList(EventType.Checkpoint));

            // Scenery
            convert.Add(JJ2Event.JJ2_SCENERY_DESTRUCT, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10), // Empty
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Speed
                    Pair.Create(JJ2EventParamType.UInt, 4)); // Weapon

                if (eventParams[1] > 0) {
                    return new ConversionResult {
                        eventType = EventType.SceneryDestructSpeed,
                        eventParams = new[] { eventParams[1] }
                    };
                } else {
                    return new ConversionResult {
                        eventType = EventType.SceneryDestruct,
                        eventParams = new[] { eventParams[2] }
                    };
                }
            });
            convert.Add(JJ2Event.JJ2_SCENERY_DESTR_BOMB, ConstantParamList(EventType.SceneryDestruct, 7 /*TNT*/));
            convert.Add(JJ2Event.JJ2_SCENERY_BUTTSTOMP, NoParamList(EventType.SceneryDestructButtstomp));
            convert.Add(JJ2Event.JJ2_SCENERY_COLLAPSE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10), // Wait Time
                    Pair.Create(JJ2EventParamType.UInt, 5)); // FPS

                return new ConversionResult {
                    eventType = EventType.SceneryCollapse,
                    eventParams = new[] { (ushort)(eventParams[0] * 25), eventParams[1] }
                };
            });

            // Modifiers
            convert.Add(JJ2Event.JJ2_MODIFIER_HOOK, NoParamList(EventType.ModifierHook));
            convert.Add(JJ2Event.JJ2_MODIFIER_ONE_WAY, NoParamList(EventType.ModifierOneWay));
            convert.Add(JJ2Event.JJ2_MODIFIER_VINE, NoParamList(EventType.ModifierVine));
            convert.Add(JJ2Event.JJ2_MODIFIER_HURT, ParamIntToParamList(EventType.ModifierHurt,
                Pair.Create(JJ2EventParamType.Bool, 1), // Up (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1), // Down (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1), // Left (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1)  // Right (JJ2+)
            ));
            convert.Add(JJ2Event.JJ2_MODIFIER_RICOCHET, NoParamList(EventType.ModifierRicochet));
            convert.Add(JJ2Event.JJ2_MODIFIER_H_POLE, NoParamList(EventType.ModifierHPole));
            convert.Add(JJ2Event.JJ2_MODIFIER_V_POLE, NoParamList(EventType.ModifierVPole));
            convert.Add(JJ2Event.JJ2_MODIFIER_TUBE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Int, 7),   // X Speed
                    Pair.Create(JJ2EventParamType.Int, 7),   // Y Speed
                    Pair.Create(JJ2EventParamType.UInt, 1),  // Trig Sample
                    Pair.Create(JJ2EventParamType.Bool, 1),  // BecomeNoclip (JJ2+)
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Noclip Only (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 3)); // Wait Time (JJ2+)

                return new ConversionResult {
                    eventType = EventType.ModifierTube,
                    eventParams = new[] { eventParams[0], eventParams[1], eventParams[5], eventParams[2], eventParams[3], eventParams[4] }
                };
            });

            convert.Add(JJ2Event.JJ2_MODIFIER_SLIDE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2)); // Strength

                return new ConversionResult {
                    eventType = EventType.ModifierSlide,
                    eventParams = new ushort[] { eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_MODIFIER_BELT_LEFT, (level, jj2Params) => {
                ushort left, right;
                if (jj2Params == 0) {
                    left = 3;
                    right = 0;
                } else if (jj2Params > 127) {
                    left = 0;
                    right = (ushort)(256 - jj2Params);
                } else {
                    left = (ushort)jj2Params;
                    right = 0;
                }

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { left, right, 0, 0, 0, 0, 0, 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_MODIFIER_BELT_RIGHT, (level, jj2Params) => {
                ushort left, right;
                if (jj2Params == 0) {
                    left = 0;
                    right = 3;
                } else if (jj2Params > 127) {
                    left = (ushort)(256 - jj2Params);
                    right = 0;
                } else {
                    left = 0;
                    right = (ushort)jj2Params;
                }

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { left, right, 0, 0, 0, 0, 0, 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_MODIFIER_ACC_BELT_LEFT, (level, jj2Params) => {
                if (jj2Params == 0)
                    jj2Params = 3;

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, (ushort)jj2Params, 0, 0, 0, 0, 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_MODIFIER_ACC_BELT_RIGHT, (level, jj2Params) => {
                if (jj2Params == 0)
                    jj2Params = 3;

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, 0, (ushort)jj2Params, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_MODIFIER_WIND_LEFT, (level, jj2Params) => {
                ushort left, right;
                if (jj2Params > 127) {
                    left = (ushort)(256 - jj2Params);
                    right = 0;
                } else {
                    left = 0;
                    right = (ushort)jj2Params;
                }

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, 0, 0, left, right, 0, 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_MODIFIER_WIND_RIGHT, (level, jj2Params) => {
                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, 0, 0, 0, (ushort)jj2Params, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_MODIFIER_SET_WATER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // Height (Tiles)
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Instant [ToDo]
                    Pair.Create(JJ2EventParamType.UInt, 2)); // Lighting [ToDo]

                return new ConversionResult {
                    eventType = EventType.ModifierSetWater,
                    eventParams = new ushort[] { (ushort)(eventParams[0] * 32), eventParams[1], eventParams[2], 0, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_AREA_LIMIT_X_SCROLL, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10),  // Left (Tiles)
                    Pair.Create(JJ2EventParamType.UInt, 10)); // Width (Tiles)

                return new ConversionResult {
                    eventType = EventType.ModifierLimitCameraView,
                    eventParams = new ushort[] { eventParams[0], eventParams[1], 0, 0, 0, 0, 0, 0 }
                };
            });

            // Area
            convert.Add(JJ2Event.JJ2_AREA_STOP_ENEMY, NoParamList(EventType.AreaStopEnemy));
            convert.Add(JJ2Event.JJ2_AREA_FLOAT_UP, NoParamList(EventType.AreaFloatUp));
            convert.Add(JJ2Event.JJ2_AREA_ACTIVATE_BOSS, ParamIntToParamList(EventType.AreaActivateBoss,
                Pair.Create(JJ2EventParamType.UInt, 1)  // Music
            ));

            convert.Add(JJ2Event.JJ2_AREA_EOL, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1), // Secret
                    Pair.Create(JJ2EventParamType.Bool, 1), // Fast (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 4), // TextID (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 4)  // Offset (JJ2+)
                );

                if (eventParams[2] != 0) {
                    level.AddLevelTokenTextID(eventParams[2]);
                }

                return new ConversionResult {
                    eventType = EventType.AreaEndOfLevel,
                    eventParams = new ushort[] { (ushort)(eventParams[0] == 1 ? 4 : 1), eventParams[1], eventParams[2], eventParams[3], 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_AREA_EOL_WARP, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1), // Empty (JJ2+)
                    Pair.Create(JJ2EventParamType.Bool, 1), // Fast (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 4), // TextID (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 4)  // Offset (JJ2+)
                );

                if (eventParams[2] != 0) {
                    level.AddLevelTokenTextID(eventParams[2]);
                }

                return new ConversionResult {
                    eventType = EventType.AreaEndOfLevel,
                    eventParams = new ushort[] { 2, eventParams[1], eventParams[2], eventParams[3], 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_AREA_SECRET_WARP, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10), // Coins
                    Pair.Create(JJ2EventParamType.UInt, 4),  // TextID (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 4)   // Offset (JJ2+)
                );

                if (eventParams[1] != 0) {
                    level.AddLevelTokenTextID(eventParams[1]);
                }

                return new ConversionResult {
                    eventType = EventType.AreaEndOfLevel,
                    eventParams = new ushort[] { 3, 0, eventParams[1], eventParams[2], eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_EOL_SIGN, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Secret

                return new ConversionResult {
                    eventType = EventType.SignEOL,
                    eventParams = new ushort[] { (ushort)(eventParams[0] == 1 ? 4 : 1), 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_BONUS_SIGN, ConstantParamList(EventType.AreaEndOfLevel, 3, 0, 0, 0, 0));

            convert.Add(JJ2Event.JJ2_AREA_TEXT, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // Text
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Vanish
                    Pair.Create(JJ2EventParamType.Bool, 1),  // AngelScript (JJ2+)
                    Pair.Create(JJ2EventParamType.UInt, 8)); // Offset (JJ2+)

                if (eventParams[2] != 0) {
                    return new ConversionResult {
                        eventType = EventType.AreaCallback,
                        eventParams = new[] { eventParams[0], eventParams[3], eventParams[1] }
                    };
                } else {
                    return new ConversionResult {
                        eventType = EventType.AreaText,
                        eventParams = new[] { eventParams[0], eventParams[3], eventParams[1] }
                    };
                }
            });

            convert.Add(JJ2Event.JJ2_AREA_FLY_OFF, NoParamList(EventType.AreaFlyOff));
            convert.Add(JJ2Event.JJ2_AREA_REVERT_MORPH, NoParamList(EventType.AreaRevertMorph));

            // Triggers
            convert.Add(JJ2Event.JJ2_TRIGGER_CRATE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Trigger ID
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Set to (0: on, 1: off)
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Switch

                return new ConversionResult {
                    eventType = EventType.TriggerCrate, // Swap values - 0: off, 1: on
                    eventParams = new[] { eventParams[0], (ushort)(eventParams[1] == 0 ? 1 : 0), eventParams[2] }
                };
            });
            convert.Add(JJ2Event.JJ2_TRIGGER_AREA, ParamIntToParamList(EventType.TriggerArea,
                Pair.Create(JJ2EventParamType.UInt, 5)  // Trigger ID
            ));
            convert.Add(JJ2Event.JJ2_TRIGGER_ZONE, ParamIntToParamList(EventType.TriggerZone,
                Pair.Create(JJ2EventParamType.UInt, 5), // Trigger ID
                Pair.Create(JJ2EventParamType.Bool, 1), // Set to (0: off, 1: on)
                Pair.Create(JJ2EventParamType.Bool, 1)  // Switch
            ));

            // Warp
            convert.Add(JJ2Event.JJ2_WARP_ORIGIN, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // Warp ID
                    Pair.Create(JJ2EventParamType.UInt, 8),  // Coins
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Set Lap
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Show Anim
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Fast (JJ2+)

                if (eventParams[1] > 0 || eventParams[3] != 0) {
                    return new ConversionResult {
                        eventType = EventType.WarpCoinBonus,
                        eventParams = new[] { eventParams[0], eventParams[4], eventParams[2], eventParams[1], eventParams[3] }
                    };
                } else {
                    return new ConversionResult {
                        eventType = EventType.WarpOrigin,
                        eventParams = new[] { eventParams[0], eventParams[4], eventParams[2] }
                    };
                }
            });
            convert.Add(JJ2Event.JJ2_WARP_TARGET, ParamIntToParamList(EventType.WarpTarget,
                Pair.Create(JJ2EventParamType.UInt, 8) // Warp ID
            ));

            // Lights
            convert.Add(JJ2Event.JJ2_LIGHT_SET, ParamIntToParamList(EventType.LightSet,
                Pair.Create(JJ2EventParamType.UInt, 7), // Intensity
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.Bool, 1)  // Flicker
            ));
            convert.Add(JJ2Event.JJ2_LIGHT_RESET, NoParamList(EventType.LightReset));
            convert.Add(JJ2Event.JJ2_LIGHT_DIM, ConstantParamList(EventType.LightSteady, 127, 60, 100, 0, 0, 0, 0, 0));
            convert.Add(JJ2Event.JJ2_LIGHT_STEADY, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 3),  // Type
                    Pair.Create(JJ2EventParamType.UInt, 7)); // Size

                switch (eventParams[0]) {
                    default:
                    case 0: { // Normal
                        ushort radiusNear = (ushort)(eventParams[1] == 0 ? 60 : eventParams[1] * 6);
                        ushort radiusFar = (ushort)(radiusNear * 1.666f);

                        return new ConversionResult {
                            eventType = EventType.LightSteady,
                            eventParams = new ushort[] { 255, 10, radiusNear, radiusFar, 0, 0, 0, 0 }
                        };
                    }

                    case 1: // Single point (ignores the "Size" parameter)
                        return new ConversionResult {
                            eventType = EventType.LightSteady,
                            eventParams = new ushort[] { 127, 10, 0, 16, 0, 0, 0, 0 }
                        };

                    case 2: // Single point (brighter) (ignores the "Size" parameter)
                        return new ConversionResult {
                            eventType = EventType.LightSteady,
                            eventParams = new ushort[] { 255, 200, 0, 16, 0, 0, 0, 0 }
                        };

                    case 3: { // Flicker light
                        ushort radiusNear = (ushort)(eventParams[1] == 0 ? 60 : eventParams[1] * 6);
                        ushort radiusFar = (ushort)(radiusNear * 1.666f);

                        return new ConversionResult {
                            eventType = EventType.LightFlicker,
                            eventParams = new ushort[] { (ushort)Math.Min(110 + eventParams[1] * 2, 255), 40, radiusNear, radiusFar, 0, 0, 0, 0 }
                        };
                    }

                    case 4: { // Bright normal light
                        ushort radiusNear = (ushort)(eventParams[1] == 0 ? 80 : eventParams[1] * 7);
                        ushort radiusFar = (ushort)(radiusNear * 1.25f);

                        return new ConversionResult {
                            eventType = EventType.LightSteady,
                            eventParams = new ushort[] { 255, 200, radiusNear, radiusFar, 0, 0, 0, 0 }
                        };
                    }

                    case 5: { // Laser shield/Illuminate Surroundings
                        return new ConversionResult {
                            eventType = EventType.LightIlluminate,
                            eventParams = new ushort[] { (ushort)(eventParams[1] < 1 ? 1 : eventParams[1]), 0, 0, 0, 0, 0, 0, 0 }
                        };
                    }

                    case 6: // Ring of light
                        // ToDo
                        return new ConversionResult {
                            eventType = EventType.Empty
                        };

                    case 7: // Ring of light 2
                        // ToDo
                        return new ConversionResult {
                            eventType = EventType.Empty
                        };
                }
            });
            convert.Add(JJ2Event.JJ2_LIGHT_PULSE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8), // Speed
                    Pair.Create(JJ2EventParamType.UInt, 4), // Sync
                    Pair.Create(JJ2EventParamType.UInt, 3), // Type
                    Pair.Create(JJ2EventParamType.UInt, 5)  // Size
                );

                ushort radiusNear1 = (ushort)(eventParams[3] == 0 ? 20 : eventParams[3] * 4.8f);
                ushort radiusNear2 = (ushort)(radiusNear1 * 2);
                ushort radiusFar = (ushort)(radiusNear1 * 2.4f);

                ushort speed = (ushort)(eventParams[0] == 0 ? 6 : eventParams[0]); // Quickfix for Tube2.j2l to look better

                ushort sync = eventParams[1];

                switch (eventParams[2]) {
                    default:
                    case 0: { // Normal
                        return new ConversionResult {
                            eventType = EventType.LightPulse,
                            eventParams = new ushort[] { 255, 10, radiusNear1, radiusNear2, radiusFar, speed, sync, 0 }
                        };
                    }

                    case 4: { // Bright normal light
                        return new ConversionResult {
                            eventType = EventType.LightPulse,
                            eventParams = new ushort[] { 255, 200, radiusNear1, radiusNear2, radiusFar, speed, sync, 0 }
                        };
                    }

                    case 5: { // Laser shield/Illuminate Surroundings
                        // ToDo: Not pulsating yet
                        return new ConversionResult {
                            eventType = EventType.LightIlluminate,
                            eventParams = new ushort[] { (ushort)(eventParams[1] < 1 ? 1 : eventParams[1]), 0, 0, 0, 0, 0, 0, 0 }
                        };
                    }
                }
            });
            convert.Add(JJ2Event.JJ2_LIGHT_FLICKER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8) // Sample (not used)
                );

                return new ConversionResult {
                    eventType = EventType.LightFlicker,
                    eventParams = new ushort[] { 110, 40, 60, 110, 0, 0, 0, 0 }
                };
            });

            // Environment
            convert.Add(JJ2Event.JJ2_PUSHABLE_ROCK, ConstantParamList(EventType.PushableBox, 0, 0, 0, 0, 0, 0, 0, 0));
            convert.Add(JJ2Event.JJ2_PUSHABLE_BOX, ConstantParamList(EventType.PushableBox, 1, 0, 0, 0, 0, 0, 0, 0));

            convert.Add(JJ2Event.JJ2_PLATFORM_FRUIT, GetPlatformConverter(1));
            convert.Add(JJ2Event.JJ2_PLATFORM_BOLL, GetPlatformConverter(2));
            convert.Add(JJ2Event.JJ2_PLATFORM_GRASS, GetPlatformConverter(3));
            convert.Add(JJ2Event.JJ2_PLATFORM_PINK, GetPlatformConverter(4));
            convert.Add(JJ2Event.JJ2_PLATFORM_SONIC, GetPlatformConverter(5));
            convert.Add(JJ2Event.JJ2_PLATFORM_SPIKE, GetPlatformConverter(6));
            convert.Add(JJ2Event.JJ2_BOLL_SPIKE, GetPlatformConverter(7));

            convert.Add(JJ2Event.JJ2_BOLL_SPIKE_3D, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2), // Sync
                    Pair.Create(JJ2EventParamType.Int, 6),  // Speed
                    Pair.Create(JJ2EventParamType.UInt, 4), // Length
                    Pair.Create(JJ2EventParamType.Bool, 1), // Swing
                    Pair.Create(JJ2EventParamType.Bool, 1)  // Shade
                );

                return new ConversionResult {
                    eventType = EventType.SpikeBall,
                    eventParams = new ushort[] { eventParams[0], eventParams[1], eventParams[2], eventParams[3], eventParams[4] }
                };
            });

            convert.Add(JJ2Event.JJ2_SPRING_RED, GetSpringConverter(0 /*Red*/, false, false));
            convert.Add(JJ2Event.JJ2_SPRING_GREEN, GetSpringConverter(1 /*Green*/, false, false));
            convert.Add(JJ2Event.JJ2_SPRING_BLUE, GetSpringConverter(2 /*Blue*/, false, false));
            convert.Add(JJ2Event.JJ2_SPRING_RED_HOR, GetSpringConverter(0 /*Red*/, true, false));
            convert.Add(JJ2Event.JJ2_SPRING_GREEN_HOR, GetSpringConverter(1 /*Green*/, true, false));
            convert.Add(JJ2Event.JJ2_SPRING_BLUE_HOR, GetSpringConverter(2 /*Blue*/, true, false));
            // ToDo: Implement fronzen springs
            convert.Add(JJ2Event.JJ2_SPRING_GREEN_FROZEN, GetSpringConverter(1 /*Green*/, false, true));

            convert.Add(JJ2Event.JJ2_BRIDGE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4),  // Width
                    Pair.Create(JJ2EventParamType.UInt, 3),  // Type
                    Pair.Create(JJ2EventParamType.UInt, 4)); // Toughness

                return new ConversionResult {
                    eventType = EventType.Bridge,
                    eventParams = new ushort[] { (ushort)(eventParams[0] * 2), eventParams[1], eventParams[2], 0, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_POLE_CARROTUS, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)); // Adjust X

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { 0, eventParams[1], eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_POLE_DIAMONDUS, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)); // Adjust X

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { 1, eventParams[1], eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_SMALL_TREE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)); // Adjust X

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { 2, eventParams[1], eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_POLE_JUNGLE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)); // Adjust X

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { 3, eventParams[1], eventParams[0] }
                };
            });

            convert.Add(JJ2Event.JJ2_POLE_PSYCH, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)); // Adjust X

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { 4, eventParams[1], eventParams[0] }
                };
            });

            // Enemies
            convert.Add(JJ2Event.JJ2_ENEMY_TURTLE_NORMAL, ConstantParamList(EventType.EnemyTurtle, 0));
            convert.Add(JJ2Event.JJ2_ENEMY_NORMAL_TURTLE_XMAS, ConstantParamList(EventType.EnemyTurtle, 1));
            convert.Add(JJ2Event.JJ2_ENEMY_LIZARD, ConstantParamList(EventType.EnemyLizard, 0));
            convert.Add(JJ2Event.JJ2_ENEMY_LIZARD_XMAS, ConstantParamList(EventType.EnemyLizard, 1));
            convert.Add(JJ2Event.JJ2_ENEMY_LIZARD_FLOAT, ConstantParamList(EventType.EnemyLizardFloat, 0));
            convert.Add(JJ2Event.JJ2_ENEMY_LIZARD_FLOAT_XMAS, ConstantParamList(EventType.EnemyLizardFloat, 1));
            convert.Add(JJ2Event.JJ2_ENEMY_DRAGON, NoParamList(EventType.EnemyDragon));
            convert.Add(JJ2Event.JJ2_ENEMY_LAB_RAT, NoParamList(EventType.EnemyLabRat));
            convert.Add(JJ2Event.JJ2_ENEMY_SUCKER_FLOAT, NoParamList(EventType.EnemySuckerFloat));
            convert.Add(JJ2Event.JJ2_ENEMY_SUCKER, NoParamList(EventType.EnemySucker));
            convert.Add(JJ2Event.JJ2_ENEMY_HELMUT, NoParamList(EventType.EnemyHelmut));
            convert.Add(JJ2Event.JJ2_ENEMY_BAT, NoParamList(EventType.EnemyBat));
            convert.Add(JJ2Event.JJ2_ENEMY_FAT_CHICK, NoParamList(EventType.EnemyFatChick));
            convert.Add(JJ2Event.JJ2_ENEMY_FENCER, NoParamList(EventType.EnemyFencer));
            convert.Add(JJ2Event.JJ2_ENEMY_RAPIER, NoParamList(EventType.EnemyRapier));
            convert.Add(JJ2Event.JJ2_ENEMY_SPARKS, NoParamList(EventType.EnemySparks));

            convert.Add(JJ2Event.JJ2_ENEMY_MONKEY, ConstantParamList(EventType.EnemyMonkey, 1));
            convert.Add(JJ2Event.JJ2_ENEMY_MONKEY_STAND, ConstantParamList(EventType.EnemyMonkey, 0));
            convert.Add(JJ2Event.JJ2_ENEMY_DEMON, NoParamList(EventType.EnemyDemon));
            convert.Add(JJ2Event.JJ2_ENEMY_BEE, NoParamList(EventType.EnemyBee));
            convert.Add(JJ2Event.JJ2_ENEMY_BEE_SWARM, NoParamList(EventType.EnemyBeeSwarm));
            convert.Add(JJ2Event.JJ2_ENEMY_CATERPILLAR, NoParamList(EventType.EnemyCaterpillar));
            convert.Add(JJ2Event.JJ2_ENEMY_CRAB, NoParamList(EventType.EnemyCrab));
            convert.Add(JJ2Event.JJ2_ENEMY_DOGGY_DOGG, ConstantParamList(EventType.EnemyDoggy, 0));
            convert.Add(JJ2Event.JJ2_EMPTY_TSF_DOG, ConstantParamList(EventType.EnemyDoggy, 1));
            convert.Add(JJ2Event.JJ2_ENEMY_DRAGONFLY, NoParamList(EventType.EnemyDragonfly));
            convert.Add(JJ2Event.JJ2_ENEMY_FISH, NoParamList(EventType.EnemyFish));
            convert.Add(JJ2Event.JJ2_ENEMY_MADDER_HATTER, NoParamList(EventType.EnemyMadderHatter));
            convert.Add(JJ2Event.JJ2_ENEMY_RAVEN, NoParamList(EventType.EnemyRaven));
            convert.Add(JJ2Event.JJ2_ENEMY_SKELETON, NoParamList(EventType.EnemySkeleton));
            convert.Add(JJ2Event.JJ2_ENEMY_TUF_TURT, NoParamList(EventType.EnemyTurtleTough));
            convert.Add(JJ2Event.JJ2_ENEMY_TURTLE_TUBE, NoParamList(EventType.EnemyTurtleTube));
            convert.Add(JJ2Event.JJ2_ENEMY_WITCH, NoParamList(EventType.EnemyWitch));

            convert.Add(JJ2Event.JJ2_BOSS_TWEEDLE, GetBossConverter(EventType.BossTweedle));
            convert.Add(JJ2Event.JJ2_BOSS_BILSY, GetBossConverter(EventType.BossBilsy, 0));
            convert.Add(JJ2Event.JJ2_EMPTY_BOSS_BILSY_XMAS, GetBossConverter(EventType.BossBilsy, 1));
            convert.Add(JJ2Event.JJ2_BOSS_DEVAN_DEVIL, GetBossConverter(EventType.BossDevan));
            convert.Add(JJ2Event.JJ2_BOSS_ROBOT, NoParamList(EventType.BossRobot));
            convert.Add(JJ2Event.JJ2_BOSS_QUEEN, GetBossConverter(EventType.BossQueen));
            convert.Add(JJ2Event.JJ2_BOSS_UTERUS, GetBossConverter(EventType.BossUterus));
            convert.Add(JJ2Event.JJ2_BOSS_BUBBA, GetBossConverter(EventType.BossBubba));
            convert.Add(JJ2Event.JJ2_BOSS_TUF_TURT, GetBossConverter(EventType.BossTurtleTough));
            convert.Add(JJ2Event.JJ2_BOSS_DEVAN_ROBOT, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4), // IntroText
                    Pair.Create(JJ2EventParamType.UInt, 4)  // EndText
                );

                return new ConversionResult {
                    eventType = EventType.BossDevanRemote,
                    eventParams = new ushort[] { 0, eventParams[0], eventParams[1], 0, 0, 0, 0, 0 }
                };
            });
            convert.Add(JJ2Event.JJ2_BOSS_BOLLY, GetBossConverter(EventType.BossBolly));

            convert.Add(JJ2Event.JJ2_TURTLE_SHELL, NoParamList(EventType.TurtleShell));

            // Collectibles
            convert.Add(JJ2Event.JJ2_COIN_SILVER, ConstantParamList(EventType.Coin, 0));
            convert.Add(JJ2Event.JJ2_COIN_GOLD, ConstantParamList(EventType.Coin, 1));

            convert.Add(JJ2Event.JJ2_GEM_RED, ConstantParamList(EventType.Gem, 0));
            convert.Add(JJ2Event.JJ2_GEM_GREEN, ConstantParamList(EventType.Gem, 1));
            convert.Add(JJ2Event.JJ2_GEM_BLUE, ConstantParamList(EventType.Gem, 2));
            convert.Add(JJ2Event.JJ2_GEM_PURPLE, ConstantParamList(EventType.Gem, 3));

            convert.Add(JJ2Event.JJ2_GEM_RED_RECT, ConstantParamList(EventType.Gem, 0));
            convert.Add(JJ2Event.JJ2_GEM_GREEN_RECT, ConstantParamList(EventType.Gem, 1));
            convert.Add(JJ2Event.JJ2_GEM_BLUE_RECT, ConstantParamList(EventType.Gem, 2));

            convert.Add(JJ2Event.JJ2_GEM_SUPER, ConstantParamList(EventType.GemGiant));
            convert.Add(JJ2Event.JJ2_GEM_RING, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Length
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Speed
                    Pair.Create(JJ2EventParamType.Bool, 8)); // Event

                return new ConversionResult {
                    eventType = EventType.GemRing,
                    eventParams = new ushort[] { eventParams[0], eventParams[1] }
                };
            });

            convert.Add(JJ2Event.JJ2_CARROT, ConstantParamList(EventType.Carrot, 0));
            convert.Add(JJ2Event.JJ2_CARROT_FULL, ConstantParamList(EventType.Carrot, 1));
            convert.Add(JJ2Event.JJ2_CARROT_FLY, NoParamList(EventType.CarrotFly));
            convert.Add(JJ2Event.JJ2_CARROT_INVINCIBLE, NoParamList(EventType.CarrotInvincible));
            convert.Add(JJ2Event.JJ2_ONEUP, NoParamList(EventType.OneUp));

            convert.Add(JJ2Event.JJ2_AMMO_BOUNCER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Bouncer));
            convert.Add(JJ2Event.JJ2_AMMO_FREEZER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Freezer));
            convert.Add(JJ2Event.JJ2_AMMO_SEEKER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Seeker));
            convert.Add(JJ2Event.JJ2_AMMO_RF, ConstantParamList(EventType.Ammo, (ushort)WeaponType.RF));
            convert.Add(JJ2Event.JJ2_AMMO_TOASTER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Toaster));
            convert.Add(JJ2Event.JJ2_AMMO_TNT, ConstantParamList(EventType.Ammo, (ushort)WeaponType.TNT));
            convert.Add(JJ2Event.JJ2_AMMO_PEPPER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Pepper));
            convert.Add(JJ2Event.JJ2_AMMO_ELECTRO, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Electro));

            convert.Add(JJ2Event.JJ2_FAST_FIRE, NoParamList(EventType.FastFire));
            convert.Add(JJ2Event.JJ2_POWERUP_BLASTER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Blaster));
            convert.Add(JJ2Event.JJ2_POWERUP_BOUNCER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Bouncer));
            convert.Add(JJ2Event.JJ2_POWERUP_FREEZER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Freezer));
            convert.Add(JJ2Event.JJ2_POWERUP_SEEKER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Seeker));
            convert.Add(JJ2Event.JJ2_POWERUP_RF, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.RF));
            convert.Add(JJ2Event.JJ2_POWERUP_TOASTER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Toaster));
            convert.Add(JJ2Event.JJ2_POWERUP_TNT, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.TNT));
            convert.Add(JJ2Event.JJ2_POWERUP_PEPPER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Pepper));
            convert.Add(JJ2Event.JJ2_POWERUP_ELECTRO, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Electro));

            convert.Add(JJ2Event.JJ2_FOOD_APPLE, ConstantParamList(EventType.Food, (ushort)FoodType.Apple));
            convert.Add(JJ2Event.JJ2_FOOD_BANANA, ConstantParamList(EventType.Food, (ushort)FoodType.Banana));
            convert.Add(JJ2Event.JJ2_FOOD_CHERRY, ConstantParamList(EventType.Food, (ushort)FoodType.Cherry));
            convert.Add(JJ2Event.JJ2_FOOD_ORANGE, ConstantParamList(EventType.Food, (ushort)FoodType.Orange));
            convert.Add(JJ2Event.JJ2_FOOD_PEAR, ConstantParamList(EventType.Food, (ushort)FoodType.Pear));
            convert.Add(JJ2Event.JJ2_FOOD_PRETZEL, ConstantParamList(EventType.Food, (ushort)FoodType.Pretzel));
            convert.Add(JJ2Event.JJ2_FOOD_STRAWBERRY, ConstantParamList(EventType.Food, (ushort)FoodType.Strawberry));
            convert.Add(JJ2Event.JJ2_FOOD_LEMON, ConstantParamList(EventType.Food, (ushort)FoodType.Lemon));
            convert.Add(JJ2Event.JJ2_FOOD_LIME, ConstantParamList(EventType.Food, (ushort)FoodType.Lime));
            convert.Add(JJ2Event.JJ2_FOOD_THING, ConstantParamList(EventType.Food, (ushort)FoodType.Thing));
            convert.Add(JJ2Event.JJ2_FOOD_WATERMELON, ConstantParamList(EventType.Food, (ushort)FoodType.WaterMelon));
            convert.Add(JJ2Event.JJ2_FOOD_PEACH, ConstantParamList(EventType.Food, (ushort)FoodType.Peach));
            convert.Add(JJ2Event.JJ2_FOOD_GRAPES, ConstantParamList(EventType.Food, (ushort)FoodType.Grapes));
            convert.Add(JJ2Event.JJ2_FOOD_LETTUCE, ConstantParamList(EventType.Food, (ushort)FoodType.Lettuce));
            convert.Add(JJ2Event.JJ2_FOOD_EGGPLANT, ConstantParamList(EventType.Food, (ushort)FoodType.Eggplant));
            convert.Add(JJ2Event.JJ2_FOOD_CUCUMBER, ConstantParamList(EventType.Food, (ushort)FoodType.Cucumber));
            convert.Add(JJ2Event.JJ2_FOOD_PEPSI, ConstantParamList(EventType.Food, (ushort)FoodType.Pepsi));
            convert.Add(JJ2Event.JJ2_FOOD_COKE, ConstantParamList(EventType.Food, (ushort)FoodType.Coke));
            convert.Add(JJ2Event.JJ2_FOOD_MILK, ConstantParamList(EventType.Food, (ushort)FoodType.Milk));
            convert.Add(JJ2Event.JJ2_FOOD_PIE, ConstantParamList(EventType.Food, (ushort)FoodType.Pie));
            convert.Add(JJ2Event.JJ2_FOOD_CAKE, ConstantParamList(EventType.Food, (ushort)FoodType.Cake));
            convert.Add(JJ2Event.JJ2_FOOD_DONUT, ConstantParamList(EventType.Food, (ushort)FoodType.Donut));
            convert.Add(JJ2Event.JJ2_FOOD_CUPCAKE, ConstantParamList(EventType.Food, (ushort)FoodType.Cupcake));
            convert.Add(JJ2Event.JJ2_FOOD_CHIPS, ConstantParamList(EventType.Food, (ushort)FoodType.Chips));
            convert.Add(JJ2Event.JJ2_FOOD_CANDY, ConstantParamList(EventType.Food, (ushort)FoodType.Candy));
            convert.Add(JJ2Event.JJ2_FOOD_CHOCOLATE, ConstantParamList(EventType.Food, (ushort)FoodType.Chocolate));
            convert.Add(JJ2Event.JJ2_FOOD_ICE_CREAM, ConstantParamList(EventType.Food, (ushort)FoodType.IceCream));
            convert.Add(JJ2Event.JJ2_FOOD_BURGER, ConstantParamList(EventType.Food, (ushort)FoodType.Burger));
            convert.Add(JJ2Event.JJ2_FOOD_PIZZA, ConstantParamList(EventType.Food, (ushort)FoodType.Pizza));
            convert.Add(JJ2Event.JJ2_FOOD_FRIES, ConstantParamList(EventType.Food, (ushort)FoodType.Fries));
            convert.Add(JJ2Event.JJ2_FOOD_CHICKEN_LEG, ConstantParamList(EventType.Food, (ushort)FoodType.ChickenLeg));
            convert.Add(JJ2Event.JJ2_FOOD_SANDWICH, ConstantParamList(EventType.Food, (ushort)FoodType.Sandwich));
            convert.Add(JJ2Event.JJ2_FOOD_TACO, ConstantParamList(EventType.Food, (ushort)FoodType.Taco));
            convert.Add(JJ2Event.JJ2_FOOD_HOT_DOG, ConstantParamList(EventType.Food, (ushort)FoodType.HotDog));
            convert.Add(JJ2Event.JJ2_FOOD_HAM, ConstantParamList(EventType.Food, (ushort)FoodType.Ham));
            convert.Add(JJ2Event.JJ2_FOOD_CHEESE, ConstantParamList(EventType.Food, (ushort)FoodType.Cheese));

            convert.Add(JJ2Event.JJ2_CRATE_AMMO, GetAmmoCrateConverter(0));
            convert.Add(JJ2Event.JJ2_CRATE_AMMO_BOUNCER, GetAmmoCrateConverter(1));
            convert.Add(JJ2Event.JJ2_CRATE_AMMO_FREEZER, GetAmmoCrateConverter(2));
            convert.Add(JJ2Event.JJ2_CRATE_AMMO_SEEKER, GetAmmoCrateConverter(3));
            convert.Add(JJ2Event.JJ2_CRATE_AMMO_RF, GetAmmoCrateConverter(4));
            convert.Add(JJ2Event.JJ2_CRATE_AMMO_TOASTER, GetAmmoCrateConverter(5));
            convert.Add(JJ2Event.JJ2_CRATE_CARROT, ConstantParamList(EventType.Crate, (ushort)EventType.Carrot, 1, 0));
            convert.Add(JJ2Event.JJ2_CRATE_SPRING, ConstantParamList(EventType.Crate, (ushort)EventType.Spring, 1, 1));
            convert.Add(JJ2Event.JJ2_CRATE_ONEUP, ConstantParamList(EventType.Crate, (ushort)EventType.OneUp, 1));
            convert.Add(JJ2Event.JJ2_CRATE_BOMB, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // ExtraEvent
                    Pair.Create(JJ2EventParamType.UInt, 4),  // NumEvent
                    Pair.Create(JJ2EventParamType.Bool, 1),  // RandomFly
                    Pair.Create(JJ2EventParamType.Bool, 1)); // NoBomb

                // ToDo: Implement RandomFly parameter

                if (eventParams[0] > 0 && eventParams[1] > 0) {
                    return new ConversionResult {
                        eventType = EventType.Crate,
                        eventParams = new ushort[] { eventParams[0], eventParams[1] }
                    };
                } else if (eventParams[3] == 0) {
                    return new ConversionResult {
                        eventType = EventType.Crate,
                        eventParams = new ushort[] { (ushort)EventType.Bomb, 1 }
                    };
                } else {
                    return new ConversionResult {
                        eventType = EventType.Crate,
                        eventParams = new ushort[] { 0, 0 }
                    };
                }
            });
            convert.Add(JJ2Event.JJ2_BARREL_AMMO, ConstantParamList(EventType.BarrelAmmo, 0));
            convert.Add(JJ2Event.JJ2_BARREL_CARROT, ConstantParamList(EventType.Barrel, (ushort)EventType.Carrot, 1, 0));
            convert.Add(JJ2Event.JJ2_BARREL_ONEUP, ConstantParamList(EventType.Barrel, (ushort)EventType.OneUp, 1));
            convert.Add(JJ2Event.JJ2_CRATE_GEM, ParamIntToParamList(EventType.CrateGem,
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.UInt, 4)  // Purple
            ));
            convert.Add(JJ2Event.JJ2_BARREL_GEM, ParamIntToParamList(EventType.BarrelGem,
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.UInt, 4)  // Purple
            ));

            convert.Add(JJ2Event.JJ2_POWERUP_SWAP, (level, jj2Params) => {
                if (level.Version == JJ2Version.TSF || level.Version == JJ2Version.CC) {
                    return new ConversionResult {
                        eventType = EventType.PowerUpMorph,
                        eventParams = new ushort[] { 1 }
                    };
                } else {
                    return new ConversionResult {
                        eventType = EventType.PowerUpMorph,
                        eventParams = new ushort[] { 0 }
                    };
                }
            });

            convert.Add(JJ2Event.JJ2_POWERUP_BIRD, ConstantParamList(EventType.PowerUpMorph, 2));

            convert.Add(JJ2Event.JJ2_BIRDY, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Chuck (Yellow)

                return new ConversionResult {
                    eventType = EventType.BirdyCage,
                    eventParams = new ushort[] { eventParams[0], 0 }
                };
            });

            // Misc.
            convert.Add(JJ2Event.JJ2_EVA, NoParamList(EventType.Eva));
            convert.Add(JJ2Event.JJ2_MOTH, ParamIntToParamList(EventType.Moth,
                Pair.Create(JJ2EventParamType.UInt, 3)
            ));
            convert.Add(JJ2Event.JJ2_STEAM, NoParamList(EventType.SteamNote));
            convert.Add(JJ2Event.JJ2_SCENERY_BOMB, NoParamList(EventType.Bomb));
            convert.Add(JJ2Event.JJ2_PINBALL_BUMP_500, ConstantParamList(EventType.PinballBumper, 0));
            convert.Add(JJ2Event.JJ2_PINBALL_BUMP_CARROT, ConstantParamList(EventType.PinballBumper, 1));
            convert.Add(JJ2Event.JJ2_PINBALL_PADDLE_L, ConstantParamList(EventType.PinballPaddle, 0));
            convert.Add(JJ2Event.JJ2_PINBALL_PADDLE_R, ConstantParamList(EventType.PinballPaddle, 1));

            convert.Add(JJ2Event.JJ2_SNOW, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2),  // Intensity
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Outdoors
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Off
                    Pair.Create(JJ2EventParamType.UInt, 2)); // Type

                return new ConversionResult {
                    eventType = EventType.Weather,
                    eventParams = new ushort[] { (ushort)(eventParams[2] == 1 ? 0 : eventParams[3] + 1), (ushort)((eventParams[0] + 1) * 5 / 3), eventParams[1], 0, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_AMBIENT_SOUND, ParamIntToParamList(EventType.AmbientSound,
                Pair.Create(JJ2EventParamType.UInt, 8), // Sample
                Pair.Create(JJ2EventParamType.UInt, 8), // Amplify
                Pair.Create(JJ2EventParamType.Bool, 1), // Fade [ToDo]
                Pair.Create(JJ2EventParamType.Bool, 1)  // Sine [ToDo]
            ));

            convert.Add(JJ2Event.JJ2_SCENERY_BUBBLER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4)); // Speed

                return new ConversionResult {
                    eventType = EventType.AmbientBubbles,
                    eventParams = new ushort[] { (ushort)((eventParams[0] + 1) * 5 / 3), 0, 0, 0, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_AIRBOARD, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5)); // Delay (Secs.) - Default: 30

                return new ConversionResult {
                    eventType = EventType.AirboardGenerator,
                    eventParams = new ushort[] { (ushort)(eventParams[0] == 0 ? 30 : eventParams[0]), 0, 0, 0, 0, 0, 0, 0 }
                };
            });

            convert.Add(JJ2Event.JJ2_SHIELD_FIRE, ConstantParamList(EventType.PowerUpShield, 1));
            convert.Add(JJ2Event.JJ2_SHIELD_WATER, ConstantParamList(EventType.PowerUpShield, 2));
            convert.Add(JJ2Event.JJ2_SHIELD_LIGHTNING, ConstantParamList(EventType.PowerUpShield, 3));
            convert.Add(JJ2Event.JJ2_SHIELD_LASER, ConstantParamList(EventType.PowerUpShield, 4));
            convert.Add(JJ2Event.JJ2_STOPWATCH, NoParamList(EventType.Stopwatch));
        }

        public static ConversionResult Convert(JJ2Level level, JJ2Event old, uint eventParams)
        {
            ConversionFunction f;
            ConversionResult result;
            if (convert.TryGetValue(old, out f)) {
                result = f(level, eventParams);
            } else {
                result = new ConversionResult { eventType = EventType.Empty, eventParams = null };
            }
            return result;
        }

        private static ConversionFunction NoParamList(EventType ev)
        {
            return (level, jj2Params) => new ConversionResult {
                eventType = ev,
                eventParams = null
            };
        }

        private static ConversionFunction ConstantParamList(EventType ev, params ushort[] eventParams)
        {
            return (level, jj2Params) => new ConversionResult {
                eventType = ev,
                eventParams = eventParams
            };
        }

        private static ConversionFunction ParamIntToParamList(EventType ev, params Pair<JJ2EventParamType, int>[] paramDefs)
        {
            return (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params, paramDefs);
                return new ConversionResult {
                    eventType = ev,
                    eventParams = eventParams
                };
            };
        }

        public static ushort[] ConvertParamInt(uint paramInt, params Pair<JJ2EventParamType, int>[] paramTypes)
        {
            ushort[] eventParams = new ushort[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++) {
                if (paramTypes[i].Second == 0) {
                    continue;
                }

                switch (paramTypes[i].First) {
                    case JJ2EventParamType.Bool:
                        eventParams[i] = (ushort)(paramInt % 2);
                        paramInt = paramInt >> 1;
                        break;
                    case JJ2EventParamType.UInt:
                        eventParams[i] = (ushort)(paramInt % (1 << paramTypes[i].Second));
                        paramInt = paramInt >> paramTypes[i].Second;
                        break;
                    case JJ2EventParamType.Int: {
                            uint val = (uint)(paramInt % (1 << paramTypes[i].Second));

                            // Complement of two, with variable bit length
                            int highestBitValue = (1 << (paramTypes[i].Second - 1));
                            if (val >= highestBitValue) {
                                val = (uint)(-highestBitValue + (val - highestBitValue));
                            }

                            eventParams[i] = (ushort)val;
                            paramInt = paramInt >> paramTypes[i].Second;
                        }
                        break;

                    default:
                        break;
                }
            }

            return eventParams;
        }

        private static ConversionFunction GetSpringConverter(ushort type, bool horizontal, bool frozen)
        {
            return (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1), // Orientation (vertical only)
                    Pair.Create(JJ2EventParamType.Bool, 1), // Keep X Speed (vertical only)
                    Pair.Create(JJ2EventParamType.Bool, 1), // Keep Y Speed
                    Pair.Create(JJ2EventParamType.UInt, 4), // Delay
                    Pair.Create(JJ2EventParamType.Bool, 1)  // Reverse (horzontal only, JJ2+)
                );

                return new ConversionResult {
                    eventType = EventType.Spring,
                    eventParams = new ushort[] { type, (ushort)(horizontal ? (eventParams[4] != 0 ? 5 : 4) : eventParams[0] * 2), eventParams[1], eventParams[2], eventParams[3], (ushort)(frozen ? 1 : 0), 0, 0 }
                };
            };
        }

        private static ConversionFunction GetPlatformConverter(byte type)
        {
            return (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2), // Sync
                    Pair.Create(JJ2EventParamType.Int, 6),  // Speed
                    Pair.Create(JJ2EventParamType.UInt, 4), // Length
                    Pair.Create(JJ2EventParamType.Bool, 1)  // Swing
                );

                return new ConversionResult {
                    eventType = EventType.MovingPlatform,
                    eventParams = new ushort[] { type, eventParams[0], eventParams[1], eventParams[2], eventParams[3], 0, 0, 0 }
                };
            };
        }

        private static ConversionFunction GetAmmoCrateConverter(byte type)
        {
            return (level, jj2Params) => {
                return new ConversionResult {
                    eventType = EventType.CrateAmmo,
                    eventParams = new ushort[] { type, 0, 0, 0, 0, 0, 0, 0 }
                };
            };
        }

        private static ConversionFunction GetBossConverter(EventType ev, ushort customParam = 0)
        {
            return (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4) // EndText
                );

                return new ConversionResult {
                    eventType = ev,
                    eventParams = new ushort[] { customParam, eventParams[0], 0, 0, 0, 0, 0, 0 }
                };
            };
        }
    }
}