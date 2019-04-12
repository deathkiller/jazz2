using System;
using System.Collections.Generic;
using System.ComponentModel;
using Import;
using Jazz2.Actors.Collectibles;
using Jazz2.Game.Structs;

namespace Jazz2.Compatibility
{
    public class EventConverter
    {
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

        private static Dictionary<JJ2Event, ConversionFunction> converters;

        static EventConverter()
        {
            AddDefaultConverters();
        }

        public static ConversionResult TryConvert(JJ2Level level, JJ2Event old, uint eventParams)
        {
            ConversionFunction converter;
            if (converters.TryGetValue(old, out converter)) {
                return converter(level, eventParams);
            } else {
                return new ConversionResult { eventType = EventType.Empty, eventParams = null };
            }
        }

        public static void Add(JJ2Event originalEvent, ConversionFunction converter)
        {
            if (converters.ContainsKey(originalEvent)) {
                throw new InvalidOperationException("Converter for event \"" + originalEvent + "\" is already defined.");
            }

            converters[originalEvent] = converter;
        }

        public static void Override(JJ2Event originalEvent, ConversionFunction converter)
        {
            converters[originalEvent] = converter;
        }

        public static ConversionFunction NoParamList(EventType ev)
        {
            return (level, jj2Params) => new ConversionResult {
                eventType = ev,
                eventParams = null
            };
        }

        public static ConversionFunction ConstantParamList(EventType ev, params ushort[] eventParams)
        {
            return (level, jj2Params) => new ConversionResult {
                eventType = ev,
                eventParams = eventParams
            };
        }

        public static ConversionFunction ParamIntToParamList(EventType ev, params Pair<JJ2EventParamType, int>[] paramDefs)
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
                    case JJ2EventParamType.Bool: {
                        eventParams[i] = (ushort)(paramInt % 2);
                        paramInt = paramInt >> 1;
                        break;
                    }
                    case JJ2EventParamType.UInt: {
                        eventParams[i] = (ushort)(paramInt % (1 << paramTypes[i].Second));
                        paramInt = paramInt >> paramTypes[i].Second;
                        break;
                    }
                    case JJ2EventParamType.Int: {
                        uint val = (uint)(paramInt % (1 << paramTypes[i].Second));

                        // Complement of two, with variable bit length
                        int highestBitValue = (1 << (paramTypes[i].Second - 1));
                        if (val >= highestBitValue) {
                            val = (uint)(-highestBitValue + (val - highestBitValue));
                        }

                        eventParams[i] = (ushort)val;
                        paramInt = paramInt >> paramTypes[i].Second;
                        break;
                    }

                    default:
                        throw new InvalidEnumArgumentException("paramType", (int)paramTypes[i].First, typeof(JJ2EventParamType));
                }
            }

            return eventParams;
        }

        #region Default/Custom Converters
        private static void AddDefaultConverters()
        {
            converters = new Dictionary<JJ2Event, ConversionFunction>();

            Add(JJ2Event.EMPTY, NoParamList(EventType.Empty));

            // Basic
            Add(JJ2Event.JAZZ_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x01 /*Jazz*/));
            Add(JJ2Event.SPAZ_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x02 /*Spaz*/));
            Add(JJ2Event.LORI_LEVEL_START, ConstantParamList(EventType.LevelStart, 0x04 /*Lori*/));

            Add(JJ2Event.MP_LEVEL_START, ParamIntToParamList(EventType.LevelStartMP,
                Pair.Create(JJ2EventParamType.UInt, 2)  // Team (JJ2+)
            ));

            Add(JJ2Event.SAVE_POINT, NoParamList(EventType.Checkpoint));

            // Scenery
            Add(JJ2Event.SCENERY_DESTRUCT, (level, jj2Params) => {
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
            Add(JJ2Event.SCENERY_DESTR_BOMB, ConstantParamList(EventType.SceneryDestruct, 7 /*TNT*/));
            Add(JJ2Event.SCENERY_BUTTSTOMP, NoParamList(EventType.SceneryDestructButtstomp));
            Add(JJ2Event.SCENERY_COLLAPSE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10), // Wait Time
                    Pair.Create(JJ2EventParamType.UInt, 5)); // FPS

                return new ConversionResult {
                    eventType = EventType.SceneryCollapse,
                    eventParams = new[] { (ushort)(eventParams[0] * 25), eventParams[1] }
                };
            });

            // Modifiers
            Add(JJ2Event.MODIFIER_HOOK, NoParamList(EventType.ModifierHook));
            Add(JJ2Event.MODIFIER_ONE_WAY, NoParamList(EventType.ModifierOneWay));
            Add(JJ2Event.MODIFIER_VINE, NoParamList(EventType.ModifierVine));
            Add(JJ2Event.MODIFIER_HURT, ParamIntToParamList(EventType.ModifierHurt,
                Pair.Create(JJ2EventParamType.Bool, 1), // Up (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1), // Down (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1), // Left (JJ2+)
                Pair.Create(JJ2EventParamType.Bool, 1)  // Right (JJ2+)
            ));
            Add(JJ2Event.MODIFIER_RICOCHET, NoParamList(EventType.ModifierRicochet));
            Add(JJ2Event.MODIFIER_H_POLE, NoParamList(EventType.ModifierHPole));
            Add(JJ2Event.MODIFIER_V_POLE, NoParamList(EventType.ModifierVPole));
            Add(JJ2Event.MODIFIER_TUBE, (level, jj2Params) => {
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

            Add(JJ2Event.MODIFIER_SLIDE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2)); // Strength

                return new ConversionResult {
                    eventType = EventType.ModifierSlide,
                    eventParams = new[] { eventParams[0] }
                };
            });

            Add(JJ2Event.MODIFIER_BELT_LEFT, (level, jj2Params) => {
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
            Add(JJ2Event.MODIFIER_BELT_RIGHT, (level, jj2Params) => {
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
            Add(JJ2Event.MODIFIER_ACC_BELT_LEFT, (level, jj2Params) => {
                if (jj2Params == 0) {
                    jj2Params = 3;
                }

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, (ushort)jj2Params, 0, 0, 0, 0, 0 }
                };
            });
            Add(JJ2Event.MODIFIER_ACC_BELT_RIGHT, (level, jj2Params) => {
                if (jj2Params == 0) {
                    jj2Params = 3;
                }

                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, 0, (ushort)jj2Params, 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.MODIFIER_WIND_LEFT, (level, jj2Params) => {
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
            Add(JJ2Event.MODIFIER_WIND_RIGHT, (level, jj2Params) => {
                return new ConversionResult {
                    eventType = EventType.AreaHForce,
                    eventParams = new ushort[] { 0, 0, 0, 0, 0, (ushort)jj2Params, 0, 0 }
                };
            });

            Add(JJ2Event.MODIFIER_SET_WATER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // Height (Tiles)
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Instant [ToDo]
                    Pair.Create(JJ2EventParamType.UInt, 2)); // Lighting [ToDo]

                return new ConversionResult {
                    eventType = EventType.ModifierSetWater,
                    eventParams = new ushort[] { (ushort)(eventParams[0] * 32), eventParams[1], eventParams[2], 0, 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.AREA_LIMIT_X_SCROLL, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 10),  // Left (Tiles)
                    Pair.Create(JJ2EventParamType.UInt, 10)); // Width (Tiles)

                return new ConversionResult {
                    eventType = EventType.ModifierLimitCameraView,
                    eventParams = new ushort[] { eventParams[0], eventParams[1], 0, 0, 0, 0, 0, 0 }
                };
            });

            // Area
            Add(JJ2Event.AREA_STOP_ENEMY, NoParamList(EventType.AreaStopEnemy));
            Add(JJ2Event.AREA_FLOAT_UP, NoParamList(EventType.AreaFloatUp));
            Add(JJ2Event.AREA_ACTIVATE_BOSS, ParamIntToParamList(EventType.AreaActivateBoss,
                Pair.Create(JJ2EventParamType.UInt, 1)  // Music
            ));

            Add(JJ2Event.AREA_EOL, (level, jj2Params) => {
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
            Add(JJ2Event.AREA_EOL_WARP, (level, jj2Params) => {
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
            Add(JJ2Event.AREA_SECRET_WARP, (level, jj2Params) => {
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

            Add(JJ2Event.EOL_SIGN, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Secret

                return new ConversionResult {
                    eventType = EventType.SignEOL,
                    eventParams = new ushort[] { (ushort)(eventParams[0] == 1 ? 4 : 1), 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.BONUS_SIGN, ConstantParamList(EventType.AreaEndOfLevel, 3, 0, 0, 0, 0));

            Add(JJ2Event.AREA_TEXT, (level, jj2Params) => {
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

            Add(JJ2Event.AREA_FLY_OFF, NoParamList(EventType.AreaFlyOff));
            Add(JJ2Event.AREA_REVERT_MORPH, NoParamList(EventType.AreaRevertMorph));

            // Triggers
            Add(JJ2Event.TRIGGER_CRATE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Trigger ID
                    Pair.Create(JJ2EventParamType.Bool, 1),  // Set to (0: on, 1: off)
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Switch

                return new ConversionResult {
                    eventType = EventType.TriggerCrate, // Swap values - 0: off, 1: on
                    eventParams = new[] { eventParams[0], (ushort)(eventParams[1] == 0 ? 1 : 0), eventParams[2] }
                };
            });
            Add(JJ2Event.TRIGGER_AREA, ParamIntToParamList(EventType.TriggerArea,
                Pair.Create(JJ2EventParamType.UInt, 5)  // Trigger ID
            ));
            Add(JJ2Event.TRIGGER_ZONE, ParamIntToParamList(EventType.TriggerZone,
                Pair.Create(JJ2EventParamType.UInt, 5), // Trigger ID
                Pair.Create(JJ2EventParamType.Bool, 1), // Set to (0: off, 1: on)
                Pair.Create(JJ2EventParamType.Bool, 1)  // Switch
            ));

            // Warp
            Add(JJ2Event.WARP_ORIGIN, (level, jj2Params) => {
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
            Add(JJ2Event.WARP_TARGET, ParamIntToParamList(EventType.WarpTarget,
                Pair.Create(JJ2EventParamType.UInt, 8) // Warp ID
            ));

            // Lights
            Add(JJ2Event.LIGHT_SET, ParamIntToParamList(EventType.LightSet,
                Pair.Create(JJ2EventParamType.UInt, 7), // Intensity
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.Bool, 1)  // Flicker
            ));
            Add(JJ2Event.LIGHT_RESET, NoParamList(EventType.LightReset));
            Add(JJ2Event.LIGHT_DIM, ConstantParamList(EventType.LightSteady, 127, 60, 100, 0, 0, 0, 0, 0));
            Add(JJ2Event.LIGHT_STEADY, (level, jj2Params) => {
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
            Add(JJ2Event.LIGHT_PULSE, (level, jj2Params) => {
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
            Add(JJ2Event.LIGHT_FLICKER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8) // Sample (not used)
                );

                return new ConversionResult {
                    eventType = EventType.LightFlicker,
                    eventParams = new ushort[] { 110, 40, 60, 110, 0, 0, 0, 0 }
                };
            });

            // Environment
            Add(JJ2Event.PUSHABLE_ROCK, ConstantParamList(EventType.PushableBox, 0, 0, 0, 0, 0, 0, 0, 0));
            Add(JJ2Event.PUSHABLE_BOX, ConstantParamList(EventType.PushableBox, 1, 0, 0, 0, 0, 0, 0, 0));

            Add(JJ2Event.PLATFORM_FRUIT, GetPlatformConverter(1));
            Add(JJ2Event.PLATFORM_BOLL, GetPlatformConverter(2));
            Add(JJ2Event.PLATFORM_GRASS, GetPlatformConverter(3));
            Add(JJ2Event.PLATFORM_PINK, GetPlatformConverter(4));
            Add(JJ2Event.PLATFORM_SONIC, GetPlatformConverter(5));
            Add(JJ2Event.PLATFORM_SPIKE, GetPlatformConverter(6));
            Add(JJ2Event.BOLL_SPIKE, GetPlatformConverter(7));

            Add(JJ2Event.BOLL_SPIKE_3D, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 2), // Sync
                    Pair.Create(JJ2EventParamType.Int, 6),  // Speed
                    Pair.Create(JJ2EventParamType.UInt, 4), // Length
                    Pair.Create(JJ2EventParamType.Bool, 1), // Swing
                    Pair.Create(JJ2EventParamType.Bool, 1)  // Shade
                );

                return new ConversionResult {
                    eventType = EventType.SpikeBall,
                    eventParams = new[] { eventParams[0], eventParams[1], eventParams[2], eventParams[3], eventParams[4] }
                };
            });

            Add(JJ2Event.SPRING_RED, GetSpringConverter(0 /*Red*/, false, false));
            Add(JJ2Event.SPRING_GREEN, GetSpringConverter(1 /*Green*/, false, false));
            Add(JJ2Event.SPRING_BLUE, GetSpringConverter(2 /*Blue*/, false, false));
            Add(JJ2Event.SPRING_RED_HOR, GetSpringConverter(0 /*Red*/, true, false));
            Add(JJ2Event.SPRING_GREEN_HOR, GetSpringConverter(1 /*Green*/, true, false));
            Add(JJ2Event.SPRING_BLUE_HOR, GetSpringConverter(2 /*Blue*/, true, false));
            // ToDo: Implement fronzen springs
            Add(JJ2Event.SPRING_GREEN_FROZEN, GetSpringConverter(1 /*Green*/, false, true));

            Add(JJ2Event.BRIDGE, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4),  // Width
                    Pair.Create(JJ2EventParamType.UInt, 3),  // Type
                    Pair.Create(JJ2EventParamType.UInt, 4)); // Toughness

                return new ConversionResult {
                    eventType = EventType.Bridge,
                    eventParams = new ushort[] { (ushort)(eventParams[0] * 2), eventParams[1], eventParams[2], 0, 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.POLE_CARROTUS, GetPoleConverter(0));

            Add(JJ2Event.POLE_DIAMONDUS, GetPoleConverter(1));

            Add(JJ2Event.SMALL_TREE, GetPoleConverter(2));

            Add(JJ2Event.POLE_JUNGLE, GetPoleConverter(3));

            Add(JJ2Event.POLE_PSYCH, GetPoleConverter(4));

            Add(JJ2Event.ROTATING_ROCK, ParamIntToParamList(EventType.RollingRock,
                Pair.Create(JJ2EventParamType.UInt, 8), // ID
                Pair.Create(JJ2EventParamType.Int, 4),  // X-Speed
                Pair.Create(JJ2EventParamType.Int, 4)   // Y-Speed
            ));

            Add(JJ2Event.TRIGGER_ROCK, ParamIntToParamList(EventType.RollingRockTrigger,
                Pair.Create(JJ2EventParamType.UInt, 8)  // ID
            ));

            // Enemies
            Add(JJ2Event.ENEMY_TURTLE_NORMAL, ConstantParamList(EventType.EnemyTurtle, 0));
            Add(JJ2Event.ENEMY_NORMAL_TURTLE_XMAS, ConstantParamList(EventType.EnemyTurtle, 1));
            Add(JJ2Event.ENEMY_LIZARD, ConstantParamList(EventType.EnemyLizard, 0));
            Add(JJ2Event.ENEMY_LIZARD_XMAS, ConstantParamList(EventType.EnemyLizard, 1));
            Add(JJ2Event.ENEMY_LIZARD_FLOAT, ConstantParamList(EventType.EnemyLizardFloat, 0));
            Add(JJ2Event.ENEMY_LIZARD_FLOAT_XMAS, ConstantParamList(EventType.EnemyLizardFloat, 1));
            Add(JJ2Event.ENEMY_DRAGON, NoParamList(EventType.EnemyDragon));
            Add(JJ2Event.ENEMY_LAB_RAT, NoParamList(EventType.EnemyLabRat));
            Add(JJ2Event.ENEMY_SUCKER_FLOAT, NoParamList(EventType.EnemySuckerFloat));
            Add(JJ2Event.ENEMY_SUCKER, NoParamList(EventType.EnemySucker));
            Add(JJ2Event.ENEMY_HELMUT, NoParamList(EventType.EnemyHelmut));
            Add(JJ2Event.ENEMY_BAT, NoParamList(EventType.EnemyBat));
            Add(JJ2Event.ENEMY_FAT_CHICK, NoParamList(EventType.EnemyFatChick));
            Add(JJ2Event.ENEMY_FENCER, NoParamList(EventType.EnemyFencer));
            Add(JJ2Event.ENEMY_RAPIER, NoParamList(EventType.EnemyRapier));
            Add(JJ2Event.ENEMY_SPARKS, NoParamList(EventType.EnemySparks));
            Add(JJ2Event.ENEMY_MONKEY, ConstantParamList(EventType.EnemyMonkey, 1));
            Add(JJ2Event.ENEMY_MONKEY_STAND, ConstantParamList(EventType.EnemyMonkey, 0));
            Add(JJ2Event.ENEMY_DEMON, NoParamList(EventType.EnemyDemon));
            Add(JJ2Event.ENEMY_BEE, NoParamList(EventType.EnemyBee));
            Add(JJ2Event.ENEMY_BEE_SWARM, NoParamList(EventType.EnemyBeeSwarm));
            Add(JJ2Event.ENEMY_CATERPILLAR, NoParamList(EventType.EnemyCaterpillar));
            Add(JJ2Event.ENEMY_CRAB, NoParamList(EventType.EnemyCrab));
            Add(JJ2Event.ENEMY_DOGGY_DOGG, ConstantParamList(EventType.EnemyDoggy, 0));
            Add(JJ2Event.EMPTY_TSF_DOG, ConstantParamList(EventType.EnemyDoggy, 1));
            Add(JJ2Event.ENEMY_DRAGONFLY, NoParamList(EventType.EnemyDragonfly));
            Add(JJ2Event.ENEMY_FISH, NoParamList(EventType.EnemyFish));
            Add(JJ2Event.ENEMY_MADDER_HATTER, NoParamList(EventType.EnemyMadderHatter));
            Add(JJ2Event.ENEMY_RAVEN, NoParamList(EventType.EnemyRaven));
            Add(JJ2Event.ENEMY_SKELETON, NoParamList(EventType.EnemySkeleton));
            Add(JJ2Event.ENEMY_TUF_TURT, NoParamList(EventType.EnemyTurtleTough));
            Add(JJ2Event.ENEMY_TURTLE_TUBE, NoParamList(EventType.EnemyTurtleTube));
            Add(JJ2Event.ENEMY_WITCH, NoParamList(EventType.EnemyWitch));

            Add(JJ2Event.TURTLE_SHELL, NoParamList(EventType.TurtleShell));

            // Bosses
            Add(JJ2Event.BOSS_TWEEDLE, GetBossConverter(EventType.BossTweedle));
            Add(JJ2Event.BOSS_BILSY, GetBossConverter(EventType.BossBilsy, 0));
            Add(JJ2Event.EMPTY_BOSS_BILSY_XMAS, GetBossConverter(EventType.BossBilsy, 1));
            Add(JJ2Event.BOSS_DEVAN_DEVIL, GetBossConverter(EventType.BossDevan));
            Add(JJ2Event.BOSS_ROBOT, NoParamList(EventType.BossRobot));
            Add(JJ2Event.BOSS_QUEEN, GetBossConverter(EventType.BossQueen));
            Add(JJ2Event.BOSS_UTERUS, GetBossConverter(EventType.BossUterus));
            Add(JJ2Event.BOSS_BUBBA, GetBossConverter(EventType.BossBubba));
            Add(JJ2Event.BOSS_TUF_TURT, GetBossConverter(EventType.BossTurtleTough));
            Add(JJ2Event.BOSS_DEVAN_ROBOT, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4), // IntroText
                    Pair.Create(JJ2EventParamType.UInt, 4)  // EndText
                );

                return new ConversionResult {
                    eventType = EventType.BossDevanRemote,
                    eventParams = new ushort[] { 0, eventParams[0], eventParams[1], 0, 0, 0, 0, 0 }
                };
            });
            Add(JJ2Event.BOSS_BOLLY, GetBossConverter(EventType.BossBolly));

            // Collectibles
            Add(JJ2Event.COIN_SILVER, ConstantParamList(EventType.Coin, 0));
            Add(JJ2Event.COIN_GOLD, ConstantParamList(EventType.Coin, 1));

            Add(JJ2Event.GEM_RED, ConstantParamList(EventType.Gem, 0));
            Add(JJ2Event.GEM_GREEN, ConstantParamList(EventType.Gem, 1));
            Add(JJ2Event.GEM_BLUE, ConstantParamList(EventType.Gem, 2));
            Add(JJ2Event.GEM_PURPLE, ConstantParamList(EventType.Gem, 3));

            Add(JJ2Event.GEM_RED_RECT, ConstantParamList(EventType.Gem, 0));
            Add(JJ2Event.GEM_GREEN_RECT, ConstantParamList(EventType.Gem, 1));
            Add(JJ2Event.GEM_BLUE_RECT, ConstantParamList(EventType.Gem, 2));

            Add(JJ2Event.GEM_SUPER, ConstantParamList(EventType.GemGiant));
            Add(JJ2Event.GEM_RING, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Length
                    Pair.Create(JJ2EventParamType.UInt, 5),  // Speed
                    Pair.Create(JJ2EventParamType.Bool, 8)); // Event

                return new ConversionResult {
                    eventType = EventType.GemRing,
                    eventParams = new[] { eventParams[0], eventParams[1] }
                };
            });

            Add(JJ2Event.CARROT, ConstantParamList(EventType.Carrot, 0));
            Add(JJ2Event.CARROT_FULL, ConstantParamList(EventType.Carrot, 1));
            Add(JJ2Event.CARROT_FLY, NoParamList(EventType.CarrotFly));
            Add(JJ2Event.CARROT_INVINCIBLE, NoParamList(EventType.CarrotInvincible));
            Add(JJ2Event.ONEUP, NoParamList(EventType.OneUp));

            Add(JJ2Event.AMMO_BOUNCER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Bouncer));
            Add(JJ2Event.AMMO_FREEZER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Freezer));
            Add(JJ2Event.AMMO_SEEKER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Seeker));
            Add(JJ2Event.AMMO_RF, ConstantParamList(EventType.Ammo, (ushort)WeaponType.RF));
            Add(JJ2Event.AMMO_TOASTER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Toaster));
            Add(JJ2Event.AMMO_TNT, ConstantParamList(EventType.Ammo, (ushort)WeaponType.TNT));
            Add(JJ2Event.AMMO_PEPPER, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Pepper));
            Add(JJ2Event.AMMO_ELECTRO, ConstantParamList(EventType.Ammo, (ushort)WeaponType.Electro));

            Add(JJ2Event.FAST_FIRE, NoParamList(EventType.FastFire));
            Add(JJ2Event.POWERUP_BLASTER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Blaster));
            Add(JJ2Event.POWERUP_BOUNCER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Bouncer));
            Add(JJ2Event.POWERUP_FREEZER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Freezer));
            Add(JJ2Event.POWERUP_SEEKER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Seeker));
            Add(JJ2Event.POWERUP_RF, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.RF));
            Add(JJ2Event.POWERUP_TOASTER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Toaster));
            Add(JJ2Event.POWERUP_TNT, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.TNT));
            Add(JJ2Event.POWERUP_PEPPER, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Pepper));
            Add(JJ2Event.POWERUP_ELECTRO, ConstantParamList(EventType.PowerUpWeapon, (ushort)WeaponType.Electro));

            Add(JJ2Event.FOOD_APPLE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Apple));
            Add(JJ2Event.FOOD_BANANA, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Banana));
            Add(JJ2Event.FOOD_CHERRY, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Cherry));
            Add(JJ2Event.FOOD_ORANGE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Orange));
            Add(JJ2Event.FOOD_PEAR, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Pear));
            Add(JJ2Event.FOOD_PRETZEL, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Pretzel));
            Add(JJ2Event.FOOD_STRAWBERRY, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Strawberry));
            Add(JJ2Event.FOOD_LEMON, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Lemon));
            Add(JJ2Event.FOOD_LIME, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Lime));
            Add(JJ2Event.FOOD_THING, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Thing));
            Add(JJ2Event.FOOD_WATERMELON, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.WaterMelon));
            Add(JJ2Event.FOOD_PEACH, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Peach));
            Add(JJ2Event.FOOD_GRAPES, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Grapes));
            Add(JJ2Event.FOOD_LETTUCE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Lettuce));
            Add(JJ2Event.FOOD_EGGPLANT, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Eggplant));
            Add(JJ2Event.FOOD_CUCUMBER, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Cucumber));
            Add(JJ2Event.FOOD_PEPSI, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Pepsi));
            Add(JJ2Event.FOOD_COKE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Coke));
            Add(JJ2Event.FOOD_MILK, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Milk));
            Add(JJ2Event.FOOD_PIE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Pie));
            Add(JJ2Event.FOOD_CAKE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Cake));
            Add(JJ2Event.FOOD_DONUT, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Donut));
            Add(JJ2Event.FOOD_CUPCAKE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Cupcake));
            Add(JJ2Event.FOOD_CHIPS, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Chips));
            Add(JJ2Event.FOOD_CANDY, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Candy));
            Add(JJ2Event.FOOD_CHOCOLATE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Chocolate));
            Add(JJ2Event.FOOD_ICE_CREAM, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.IceCream));
            Add(JJ2Event.FOOD_BURGER, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Burger));
            Add(JJ2Event.FOOD_PIZZA, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Pizza));
            Add(JJ2Event.FOOD_FRIES, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Fries));
            Add(JJ2Event.FOOD_CHICKEN_LEG, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.ChickenLeg));
            Add(JJ2Event.FOOD_SANDWICH, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Sandwich));
            Add(JJ2Event.FOOD_TACO, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Taco));
            Add(JJ2Event.FOOD_HOT_DOG, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.HotDog));
            Add(JJ2Event.FOOD_HAM, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Ham));
            Add(JJ2Event.FOOD_CHEESE, ConstantParamList(EventType.Food, (ushort)FoodCollectible.FoodType.Cheese));

            Add(JJ2Event.CRATE_AMMO, GetAmmoCrateConverter(0));
            Add(JJ2Event.CRATE_AMMO_BOUNCER, GetAmmoCrateConverter(1));
            Add(JJ2Event.CRATE_AMMO_FREEZER, GetAmmoCrateConverter(2));
            Add(JJ2Event.CRATE_AMMO_SEEKER, GetAmmoCrateConverter(3));
            Add(JJ2Event.CRATE_AMMO_RF, GetAmmoCrateConverter(4));
            Add(JJ2Event.CRATE_AMMO_TOASTER, GetAmmoCrateConverter(5));
            Add(JJ2Event.CRATE_CARROT, ConstantParamList(EventType.Crate, (ushort)EventType.Carrot, 1, 0));
            Add(JJ2Event.CRATE_SPRING, ConstantParamList(EventType.Crate, (ushort)EventType.Spring, 1, 1));
            Add(JJ2Event.CRATE_ONEUP, ConstantParamList(EventType.Crate, (ushort)EventType.OneUp, 1));
            Add(JJ2Event.CRATE_BOMB, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 8),  // ExtraEvent
                    Pair.Create(JJ2EventParamType.UInt, 4),  // NumEvent
                    Pair.Create(JJ2EventParamType.Bool, 1),  // RandomFly
                    Pair.Create(JJ2EventParamType.Bool, 1)); // NoBomb

                // ToDo: Implement RandomFly parameter

                if (eventParams[0] > 0 && eventParams[1] > 0) {
                    return new ConversionResult {
                        eventType = EventType.Crate,
                        eventParams = new[] { eventParams[0], eventParams[1] }
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
            Add(JJ2Event.BARREL_AMMO, ConstantParamList(EventType.BarrelAmmo, 0));
            Add(JJ2Event.BARREL_CARROT, ConstantParamList(EventType.Barrel, (ushort)EventType.Carrot, 1, 0));
            Add(JJ2Event.BARREL_ONEUP, ConstantParamList(EventType.Barrel, (ushort)EventType.OneUp, 1));
            Add(JJ2Event.CRATE_GEM, ParamIntToParamList(EventType.CrateGem,
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.UInt, 4)  // Purple
            ));
            Add(JJ2Event.BARREL_GEM, ParamIntToParamList(EventType.BarrelGem,
                Pair.Create(JJ2EventParamType.UInt, 4), // Red
                Pair.Create(JJ2EventParamType.UInt, 4), // Green
                Pair.Create(JJ2EventParamType.UInt, 4), // Blue
                Pair.Create(JJ2EventParamType.UInt, 4)  // Purple
            ));

            Add(JJ2Event.POWERUP_SWAP, (level, jj2Params) => {
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

            Add(JJ2Event.POWERUP_BIRD, ConstantParamList(EventType.PowerUpMorph, 2));

            Add(JJ2Event.BIRDY, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.Bool, 1)); // Chuck (Yellow)

                return new ConversionResult {
                    eventType = EventType.BirdyCage,
                    eventParams = new ushort[] { eventParams[0], 0 }
                };
            });

            // Misc.
            Add(JJ2Event.EVA, NoParamList(EventType.Eva));
            Add(JJ2Event.MOTH, ParamIntToParamList(EventType.Moth,
                Pair.Create(JJ2EventParamType.UInt, 3)
            ));
            Add(JJ2Event.STEAM, NoParamList(EventType.SteamNote));
            Add(JJ2Event.SCENERY_BOMB, NoParamList(EventType.Bomb));
            Add(JJ2Event.PINBALL_BUMP_500, ConstantParamList(EventType.PinballBumper, 0));
            Add(JJ2Event.PINBALL_BUMP_CARROT, ConstantParamList(EventType.PinballBumper, 1));
            Add(JJ2Event.PINBALL_PADDLE_L, ConstantParamList(EventType.PinballPaddle, 0));
            Add(JJ2Event.PINBALL_PADDLE_R, ConstantParamList(EventType.PinballPaddle, 1));

            Add(JJ2Event.SNOW, (level, jj2Params) => {
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

            Add(JJ2Event.AMBIENT_SOUND, ParamIntToParamList(EventType.AmbientSound,
                Pair.Create(JJ2EventParamType.UInt, 8), // Sample
                Pair.Create(JJ2EventParamType.UInt, 8), // Amplify
                Pair.Create(JJ2EventParamType.Bool, 1), // Fade [ToDo]
                Pair.Create(JJ2EventParamType.Bool, 1)  // Sine [ToDo]
            ));

            Add(JJ2Event.SCENERY_BUBBLER, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 4)); // Speed

                return new ConversionResult {
                    eventType = EventType.AmbientBubbles,
                    eventParams = new ushort[] { (ushort)((eventParams[0] + 1) * 5 / 3), 0, 0, 0, 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.AIRBOARD, (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5)); // Delay (Secs.) - Default: 30

                return new ConversionResult {
                    eventType = EventType.AirboardGenerator,
                    eventParams = new ushort[] { (ushort)(eventParams[0] == 0 ? 30 : eventParams[0]), 0, 0, 0, 0, 0, 0, 0 }
                };
            });

            Add(JJ2Event.COPTER, NoParamList(EventType.Copter));


            Add(JJ2Event.SHIELD_FIRE, ConstantParamList(EventType.PowerUpShield, 1));
            Add(JJ2Event.SHIELD_WATER, ConstantParamList(EventType.PowerUpShield, 2));
            Add(JJ2Event.SHIELD_LIGHTNING, ConstantParamList(EventType.PowerUpShield, 3));
            Add(JJ2Event.SHIELD_LASER, ConstantParamList(EventType.PowerUpShield, 4));
            Add(JJ2Event.STOPWATCH, NoParamList(EventType.Stopwatch));
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

        private static ConversionFunction GetPoleConverter(byte theme)
        {
            return (level, jj2Params) => {
                ushort[] eventParams = ConvertParamInt(jj2Params,
                    Pair.Create(JJ2EventParamType.UInt, 5), // Adjust Y
                    Pair.Create(JJ2EventParamType.Int, 6)   // Adjust X
                );

                const int AdjustX = 2;
                const int AdjustY = 2;

                ushort x = unchecked((ushort)((short)eventParams[1] + 16 - AdjustX));
                ushort y = unchecked((ushort)((eventParams[0] == 0 ? 24 : eventParams[0]) - AdjustY));

                return new ConversionResult {
                    eventType = EventType.Pole,
                    eventParams = new ushort[] { theme, x, y }
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
        #endregion
    }
}