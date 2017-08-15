using System.Collections.Generic;
using System.Drawing;
using Import;

namespace Jazz2.Compatibility
{
    public class AnimSetMapping
    {
        public struct Data
        {
            public string Category;
            public string Name;

            public Color[] Palette;
            public bool SkipNormalMap;
            public int AddBorder;
        }

        public const string Discard = ":discard";

        public static AnimSetMapping GetAnimMapping(JJ2Version version)
        {
            AnimSetMapping mapping = new AnimSetMapping(version);

            if (version == JJ2Version.PlusExtension) {
                mapping.SkipItems(5); // Unimplemented weapon
                mapping.Add("Pickup", "fast_fire_lori");
                mapping.Add("UI", "blaster_upgraded_lori");

                mapping.NextSet();
                mapping.DiscardItems(4); // Beta version sprites

                mapping.NextSet();
                mapping.Add("Object", "crate_ammo_pepper");
                mapping.Add("Object", "crate_ammo_electro");
                mapping.Add("Object", "powerup_shield_laser");
                mapping.Add("Object", "powerup_unknown");
                mapping.Add("Object", "powerup_empty");
                mapping.Add("Object", "powerup_upgrade_blaster_lori");
                mapping.Add("Common", "SugarRushStars");
                mapping.SkipItems(); // Carrotade

                mapping.NextSet(); // 3
                mapping.DiscardItems(3); // Lori's continue animations

                mapping.NextSet(); // 4

                mapping.Add("UI", "font_medium");
                mapping.Add("UI", "font_small");
                mapping.Add("UI", "font_large");

                //mapping.Add("UI", "logo_plus", skipNormalMap: true);
                mapping.DiscardItems(1);

                mapping.NextSet(); // 5
                mapping.Add("Object", "powerup_swap_characters_lori");

                //mapping.Add("UI", "logo_plus_large", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add("UI", "logo_plus_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(2);

                mapping.NextSet(); // 6
                mapping.DiscardItems(5); // Reticles

            } else if (version != JJ2Version.Unknown) {
                bool isFull = (version & JJ2Version.SharewareDemo) == 0;

                // set 0 (all)
                mapping.Add("Unknown", "flame_blue");
                mapping.Add("Common", "Bomb");
                mapping.Add("Common", "smoke_poof");
                mapping.Add("Common", "explosion_rf");
                mapping.Add("Common", "explosion_small");
                mapping.Add("Common", "explosion_large");
                mapping.Add("Common", "smoke_circling_gray");
                mapping.Add("Common", "smoke_circling_brown");
                mapping.Add("Unknown", "bubble");

                //mapping.Add("Unknown", "brown_thing");
                mapping.DiscardItems(1);

                mapping.Add("Common", "explosion_pepper");

                //mapping.Add("Unknown", "bullet_maybe_electro");
                mapping.Add("Weapon", "bullet_maybe_electro", JJ2DefaultPalette.ByIndex);
                //mapping.Add("Unknown", "bullet_maybe_electro_trail");
                mapping.Add("Weapon", "bullet_maybe_electro_trail", JJ2DefaultPalette.ByIndex);

                mapping.Add("Unknown", "flame_red");
                mapping.Add("Weapon", "bullet_shield_fireball");
                mapping.Add("Unknown", "flare_diag_downleft");
                mapping.Add("Unknown", "flare_hor");
                mapping.Add("Weapon", "bullet_blaster");
                mapping.Add("UI", "blaster_upgraded_jazz");
                mapping.Add("UI", "blaster_upgraded_spaz");
                mapping.Add("Weapon", "bullet_blaster_upgraded");

                //mapping.Add("Weapon", "bullet_blaster_upgraded_ver");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_blaster_ver");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_bouncer");
                mapping.Add("Pickup", "ammo_bouncer_upgraded");
                mapping.Add("Pickup", "ammo_bouncer");
                mapping.Add("Weapon", "bullet_bouncer_upgraded");
                mapping.Add("Weapon", "bullet_freezer_hor");
                mapping.Add("Pickup", "ammo_freezer_upgraded");
                mapping.Add("Pickup", "ammo_freezer");
                mapping.Add("Weapon", "bullet_freezer_upgraded_hor");

                //mapping.Add("Weapon", "bullet_freezer_ver");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_freezer_upgraded_ver");
                mapping.DiscardItems(1);

                mapping.Add("Pickup", "ammo_seeker_upgraded");
                mapping.Add("Pickup", "ammo_seeker");

                //mapping.Add("Weapon", "bullet_seeker_ver_down");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_diag_downright");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_seeker_hor");

                //mapping.Add("Weapon", "bullet_seeker_ver_up");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_diag_upright");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_ver_down");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_diag_downright");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_seeker_upgraded_hor");

                //mapping.Add("Weapon", "bullet_seeker_upgraded_ver_up");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_diag_upright");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_rf_hor");

                //mapping.Add("Weapon", "bullet_rf_diag_downright");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_upgraded_diag_downright");
                mapping.DiscardItems(1);

                mapping.Add("Pickup", "ammo_rf_upgraded");
                mapping.Add("Pickup", "ammo_rf");
                mapping.Add("Weapon", "bullet_rf_upgraded_hor");

                //mapping.Add("Weapon", "bullet_rf_upgraded_ver");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_upgraded_diag_upright");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_ver");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_diag_upright");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_toaster");
                mapping.Add("Pickup", "ammo_toaster_upgraded");
                mapping.Add("Pickup", "ammo_toaster");
                mapping.Add("Weapon", "bullet_toaster_upgraded");
                mapping.Add("Weapon", "bullet_tnt");
                mapping.Add("Weapon", "bullet_fireball_hor");
                mapping.Add("Pickup", "ammo_pepper_upgraded");
                mapping.Add("Pickup", "ammo_pepper");
                mapping.Add("Weapon", "bullet_fireball_upgraded_hor");

                //mapping.Add("Weapon", "bullet_fireball_ver");
                mapping.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_fireball_upgraded_ver");
                mapping.DiscardItems(1);

                mapping.Add("Weapon", "bullet_bladegun");
                mapping.Add("Pickup", "ammo_electro_upgraded");
                mapping.Add("Pickup", "ammo_electro");
                mapping.Add("Weapon", "bullet_bladegun_upgraded");
                mapping.Add("Common", "explosion_tiny");
                mapping.Add("Common", "explosion_freezer_maybe");
                mapping.Add("Common", "explosion_tiny_black");
                mapping.Add("Weapon", "bullet_fireball_upgraded_hor_2");
                mapping.Add("Unknown", "flare_hor_2");
                mapping.Add("Unknown", "green_explosion");
                mapping.Add("Weapon", "bullet_bladegun_alt");
                mapping.Add("Weapon", "bullet_tnt_explosion");
                mapping.Add("Object", "container_ammo_shrapnel_1");
                mapping.Add("Object", "container_ammo_shrapnel_2");
                mapping.Add("Common", "explosion_upwards");
                mapping.Add("Common", "explosion_bomb");
                mapping.Add("Common", "smoke_circling_white");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Bat", "idle");
                    mapping.Add("Bat", "resting");
                    mapping.Add("Bat", "takeoff_1");
                    mapping.Add("Bat", "takeoff_2");
                    mapping.Add("Bat", "roost");
                    mapping.NextSet();
                    mapping.Add("Bee", "swarm");
                    mapping.NextSet();
                    mapping.Add("Bee", "swarm_2");
                    mapping.NextSet();
                    mapping.Add("Object", "PushBoxCrate");
                    mapping.NextSet();
                    mapping.Add("Object", "PushBoxRock");
                    mapping.NextSet();

                    //mapping.Add("Unknown", "diamondus_tileset_tree");
                    mapping.DiscardItems(1);

                    mapping.NextSet();
                    mapping.Add("Bilsy", "throw_fireball");
                    mapping.Add("Bilsy", "appear");
                    mapping.Add("Bilsy", "vanish");
                    mapping.Add("Bilsy", "bullet_fireball");
                    mapping.Add("Bilsy", "idle");
                }

                mapping.NextSet();
                mapping.Add("Birdy", "charge_diag_downright");
                mapping.Add("Birdy", "charge_ver");
                mapping.Add("Birdy", "charge_diag_upright");
                mapping.Add("Birdy", "caged");
                mapping.Add("Birdy", "cage_destroyed");
                mapping.Add("Birdy", "die");
                mapping.Add("Birdy", "feather_green");
                mapping.Add("Birdy", "feather_red");
                mapping.Add("Birdy", "feather_green_and_red");
                mapping.Add("Birdy", "fly");
                mapping.Add("Birdy", "hurt");
                mapping.Add("Birdy", "idle_worm");
                mapping.Add("Birdy", "idle_turn_head_left");
                mapping.Add("Birdy", "idle_look_left");
                mapping.Add("Birdy", "idle_turn_head_left_back");
                mapping.Add("Birdy", "idle_turn_head_right");
                mapping.Add("Birdy", "idle_look_right");
                mapping.Add("Birdy", "idle_turn_head_right_back");
                mapping.Add("Birdy", "idle");
                mapping.Add("Birdy", "corpse");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Unimplemented", "BonusBirdy");
                    mapping.NextSet(); // set 10 (all)
                    mapping.Add("Platform", "ball");
                    mapping.Add("Platform", "ball_chain");
                    mapping.NextSet();
                    mapping.Add("Object", "BonusActive");
                    mapping.Add("Object", "BonusInactive");
                }

                mapping.NextSet();
                mapping.Add("UI", "boss_health_bar", skipNormalMap: true);
                mapping.NextSet();
                mapping.Add("Bridge", "Rope");
                mapping.Add("Bridge", "Stone");
                mapping.Add("Bridge", "Vine");
                mapping.Add("Bridge", "StoneRed");
                mapping.Add("Bridge", "Log");
                mapping.Add("Bridge", "Gem");
                mapping.Add("Bridge", "Lab");
                mapping.NextSet();
                mapping.Add("Bubba", "spew_fireball");
                mapping.Add("Bubba", "corpse");
                mapping.Add("Bubba", "jump");
                mapping.Add("Bubba", "jump_fall");
                mapping.Add("Bubba", "fireball");
                mapping.Add("Bubba", "hop");
                mapping.Add("Bubba", "tornado");
                mapping.Add("Bubba", "tornado_start");
                mapping.Add("Bubba", "tornado_end");
                mapping.NextSet();
                mapping.Add("Bee", "Bee");
                mapping.Add("Bee", "bee_turn");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Unimplemented", "butterfly");
                    mapping.NextSet();
                    mapping.Add("Pole", "Carrotus", /*JJ2DefaultPalette.CARROTUS_POLE_PALETTE*/ JJ2DefaultPalette.ByIndex);
                    mapping.NextSet();
                    mapping.Add("Cheshire", "platform_appear");
                    mapping.Add("Cheshire", "platform_vanish");
                    mapping.Add("Cheshire", "platform_idle");
                    mapping.Add("Cheshire", "platform_invisible");
                }

                mapping.NextSet();
                mapping.Add("Cheshire", "hook_appear");
                mapping.Add("Cheshire", "hook_vanish");
                mapping.Add("Cheshire", "hook_idle");
                mapping.Add("Cheshire", "hook_invisible");

                mapping.NextSet(); // set 20 (all)
                mapping.Add("Caterpillar", "exhale_start");
                mapping.Add("Caterpillar", "exhale");
                mapping.Add("Caterpillar", "disoriented");
                mapping.Add("Caterpillar", "idle");
                mapping.Add("Caterpillar", "inhale_start");
                mapping.Add("Caterpillar", "inhale");
                mapping.Add("Caterpillar", "smoke");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("BirdyYellow", "charge_diag_downright_placeholder");
                    mapping.Add("BirdyYellow", "charge_ver");
                    mapping.Add("BirdyYellow", "charge_diag_upright");
                    mapping.Add("BirdyYellow", "caged");
                    mapping.Add("BirdyYellow", "cage_destroyed");
                    mapping.Add("BirdyYellow", "die");
                    mapping.Add("BirdyYellow", "feather_blue");
                    mapping.Add("BirdyYellow", "feather_yellow");
                    mapping.Add("BirdyYellow", "feather_blue_and_yellow");
                    mapping.Add("BirdyYellow", "fly");
                    mapping.Add("BirdyYellow", "hurt");
                    mapping.Add("BirdyYellow", "idle_worm");
                    mapping.Add("BirdyYellow", "idle_turn_head_left");
                    mapping.Add("BirdyYellow", "idle_look_left");
                    mapping.Add("BirdyYellow", "idle_turn_head_left_back");
                    mapping.Add("BirdyYellow", "idle_turn_head_right");
                    mapping.Add("BirdyYellow", "idle_look_right");
                    mapping.Add("BirdyYellow", "idle_turn_head_right_back");
                    mapping.Add("BirdyYellow", "idle");
                    mapping.Add("BirdyYellow", "corpse");
                }

                mapping.NextSet();
                mapping.Add("Common", "water_bubble_1");
                mapping.Add("Common", "water_bubble_2");
                mapping.Add("Common", "water_bubble_3");
                mapping.Add("Common", "water_splash");

                mapping.NextSet();
                mapping.Add("Jazz", "gameover_continue");
                mapping.Add("Jazz", "gameover_idle");
                mapping.Add("Jazz", "gameover_end");
                mapping.Add("Spaz", "gameover_continue");
                mapping.Add("Spaz", "gameover_idle");
                mapping.Add("Spaz", "gameover_end");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Demon", "idle");
                    mapping.Add("Demon", "attack_start");
                    mapping.Add("Demon", "attack");
                    mapping.Add("Demon", "attack_end");
                }

                mapping.NextSet();
                mapping.DiscardItems(4); // Green rectangles
                mapping.Add("Common", "IceBlock");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Devan", "bullet_small");
                    mapping.Add("Devan", "remote_idle");
                    mapping.Add("Devan", "remote_fall_warp_out");
                    mapping.Add("Devan", "remote_fall");
                    mapping.Add("Devan", "remote_fall_rotate");
                    mapping.Add("Devan", "remote_fall_warp_in");
                    mapping.Add("Devan", "remote_warp_out");

                    mapping.NextSet();
                    mapping.Add("Devan", "demon_spew_fireball");
                    mapping.Add("Devan", "disoriented");
                    mapping.Add("Devan", "freefall");
                    mapping.Add("Devan", "disoriented_start");
                    mapping.Add("Devan", "demon_fireball");
                    mapping.Add("Devan", "demon_fly");
                    mapping.Add("Devan", "demon_transform_start");
                    mapping.Add("Devan", "demon_transform_end");
                    mapping.Add("Devan", "disarmed_idle");
                    mapping.Add("Devan", "demon_turn");
                    mapping.Add("Devan", "disarmed_warp_in");
                    mapping.Add("Devan", "disoriented_warp_out");
                    mapping.Add("Devan", "disarmed");
                    mapping.Add("Devan", "crouch");
                    mapping.Add("Devan", "shoot");
                    mapping.Add("Devan", "disarmed_gun");
                    mapping.Add("Devan", "jump");
                    mapping.Add("Devan", "bullet");
                    mapping.Add("Devan", "run");
                    mapping.Add("Devan", "run_end");
                    mapping.Add("Devan", "jump_end");
                    mapping.Add("Devan", "idle");
                    mapping.Add("Devan", "warp_in");
                    mapping.Add("Devan", "warp_out");
                }

                mapping.NextSet();
                mapping.Add("Pole", "Diamondus", /*JJ2DefaultPalette.DIAMONDUS_POLE_PALETTE*/ JJ2DefaultPalette.ByIndex);

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Doggy", "attack");
                    mapping.Add("Doggy", "walk");

                    mapping.NextSet(); // set 30 (all)
                    mapping.Add("Unimplemented", "door");
                    mapping.Add("Unimplemented", "door_enter_jazz_spaz");
                }

                mapping.NextSet();
                mapping.Add("Dragonfly", "idle");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Dragon", "attack");
                    mapping.Add("Dragon", "idle");
                    mapping.Add("Dragon", "turn");

                    mapping.NextSet(1, JJ2Version.BaseGame | JJ2Version.HH);
                    mapping.NextSet(2, JJ2Version.TSF | JJ2Version.CC);
                }

                mapping.NextSet(4);
                mapping.Add("Eva", "Blink");
                mapping.Add("Eva", "Idle");
                mapping.Add("Eva", "KissStart");
                mapping.Add("Eva", "KissEnd");

                mapping.NextSet();
                mapping.Add("UI", "icon_birdy");
                mapping.Add("UI", "icon_birdy_yellow");
                mapping.Add("UI", "icon_frog");
                mapping.Add("UI", "icon_jazz");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "icon_lori");
                mapping.Add("UI", "icon_spaz");

                if (isFull) {
                    mapping.NextSet(2); // set 41 (1.24) / set 40 (1.23)
                    mapping.Add("FatChick", "attack");
                    mapping.Add("FatChick", "walk");
                    mapping.NextSet();
                    mapping.Add("Fencer", "attack");
                    mapping.Add("Fencer", "idle");
                    mapping.NextSet();
                    mapping.Add("Fish", "attack");
                    mapping.Add("Fish", "idle");
                }

                mapping.NextSet();
                mapping.Add("CTF", "arrow");
                mapping.Add("CTF", "base");
                mapping.Add("CTF", "lights");
                mapping.Add("CTF", "flag_blue");
                mapping.Add("UI", "ctf_flag_blue");
                mapping.Add("CTF", "base_eva");
                mapping.Add("CTF", "base_eva_cheer");
                mapping.Add("CTF", "flag_red");
                mapping.Add("UI", "ctf_flag_red");

                if (isFull) {
                    mapping.NextSet();
                    mapping.DiscardItems(1); // Strange green circles
                }

                mapping.NextSet();
                mapping.Add("UI", "font_medium");
                mapping.Add("UI", "font_small");
                mapping.Add("UI", "font_large");

                //mapping.Add("UI", "logo", skipNormalMap: true);
                mapping.DiscardItems(1);
                //mapping.Add(JJ2Version.CC, "UI", "cc_logo");
                mapping.DiscardItems(1, JJ2Version.CC);

                mapping.NextSet();
                mapping.Add("Frog", "fall_land");
                mapping.Add("Frog", "hurt");
                mapping.Add("Frog", "idle");
                mapping.Add("Jazz", "transform_frog");
                mapping.Add("Frog", "fall");
                mapping.Add("Frog", "jump_start");
                mapping.Add("Frog", "crouch");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "transform_frog");
                mapping.Add("Frog", "tongue_diag_upright");
                mapping.Add("Frog", "tongue_hor");
                mapping.Add("Frog", "tongue_ver");
                mapping.Add("Spaz", "transform_frog");
                mapping.Add("Frog", "run");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Platform", "carrotus_fruit");
                    mapping.Add("Platform", "carrotus_fruit_chain");
                    mapping.NextSet();
                    //mapping.Add("Pickup", "gem_gemring", JJ2DefaultPalette.Gem);
                    mapping.DiscardItems(1);
                    mapping.NextSet(); // set 50 (1.24) / set 49 (1.23)
                    mapping.Add("Unimplemented", "boxing_glove_stiff");
                    mapping.Add("Unimplemented", "boxing_glove_stiff_idle");
                    mapping.Add("Unimplemented", "boxing_glove_normal");
                    mapping.Add("Unimplemented", "boxing_glove_normal_idle");
                    mapping.Add("Unimplemented", "boxing_glove_relaxed");
                    mapping.Add("Unimplemented", "boxing_glove_relaxed_idle");

                    mapping.NextSet();
                    mapping.Add("Platform", "carrotus_grass");
                    mapping.Add("Platform", "carrotus_grass_chain");
                }

                mapping.NextSet();
                mapping.Add("MadderHatter", "cup");
                mapping.Add("MadderHatter", "hat");
                mapping.Add("MadderHatter", "attack");
                mapping.Add("MadderHatter", "bullet_spit");
                mapping.Add("MadderHatter", "walk");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Helmut", "idle");
                    mapping.Add("Helmut", "walk");
                }

                mapping.NextSet(2);
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unknown_disoriented");
                mapping.Add("Jazz", "airboard");
                mapping.Add("Jazz", "airboard_turn");
                mapping.Add("Jazz", "buttstomp_end");
                mapping.Add("Jazz", "corpse");
                mapping.Add("Jazz", "die");
                mapping.Add("Jazz", "crouch_start");
                mapping.Add("Jazz", "crouch");
                mapping.Add("Jazz", "crouch_shoot");
                mapping.Add("Jazz", "crouch_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_door_enter");
                mapping.Add("Jazz", "vine_walk");
                mapping.Add("Jazz", "eol");
                mapping.Add("Jazz", "fall");
                mapping.Add("Jazz", "buttstomp");
                mapping.Add("Jazz", "fall_end");
                mapping.Add("Jazz", "shoot");
                mapping.Add("Jazz", "shoot_ver");
                mapping.Add("Jazz", "shoot_end");
                mapping.Add("Jazz", "transform_frog_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_ledge_climb");
                mapping.Add("Jazz", "vine_shoot_start");
                mapping.Add("Jazz", "vine_shoot_up_end");
                mapping.Add("Jazz", "vine_shoot_up");
                mapping.Add("Jazz", "vine_idle");
                mapping.Add("Jazz", "vine_idle_flavor");
                mapping.Add("Jazz", "vine_shoot_end");
                mapping.Add("Jazz", "vine_shoot");
                mapping.Add("Jazz", "copter");
                mapping.Add("Jazz", "copter_shoot_start");
                mapping.Add("Jazz", "copter_shoot");
                mapping.Add("Jazz", "pole_h");
                mapping.Add("Jazz", "hurt");
                mapping.Add("Jazz", "idle_flavor_1");
                mapping.Add("Jazz", "idle_flavor_2");
                mapping.Add("Jazz", "idle_flavor_3");
                mapping.Add("Jazz", "idle_flavor_4");
                mapping.Add("Jazz", "idle_flavor_5");
                mapping.Add("Jazz", "vine_shoot_up_start");
                mapping.Add("Jazz", "fall_shoot");
                mapping.Add("Jazz", "jump_unknown_1");
                mapping.Add("Jazz", "jump_unknown_2");
                mapping.Add("Jazz", "jump");
                mapping.Add("Jazz", "ledge");
                mapping.Add("Jazz", "lift");
                mapping.Add("Jazz", "lift_jump_light");
                mapping.Add("Jazz", "lift_jump_heavy");
                mapping.Add("Jazz", "lookup_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_upright");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_ver_up");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_upleft_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_downleft_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_ver_down");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_downright");
                mapping.Add("Jazz", "dizzy_walk");
                mapping.Add("Jazz", "push");
                mapping.Add("Jazz", "shoot_start");
                mapping.Add("Jazz", "revup_start");
                mapping.Add("Jazz", "revup");
                mapping.Add("Jazz", "revup_end");
                mapping.Add("Jazz", "fall_diag");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unknown_mid_frame");
                mapping.Add("Jazz", "jump_diag");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_ver_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_ver");
                mapping.Add("Jazz", "ball");
                mapping.Add("Jazz", "run");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_aim_diag");
                mapping.Add("Jazz", "dash_start");
                mapping.Add("Jazz", "dash");
                mapping.Add("Jazz", "dash_stop");
                mapping.Add("Jazz", "walk_stop");
                mapping.Add("Jazz", "run_stop");
                mapping.Add("Jazz", "Spring");
                mapping.Add("Jazz", "idle");
                mapping.Add("Jazz", "uppercut");
                mapping.Add("Jazz", "uppercut_end");
                mapping.Add("Jazz", "uppercut_start");
                mapping.Add("Jazz", "dizzy");
                mapping.Add("Jazz", "swim_diag_downright");
                mapping.Add("Jazz", "swim_right");
                mapping.Add("Jazz", "swim_diag_right_to_downright");
                mapping.Add("Jazz", "swim_diag_right_to_upright");
                mapping.Add("Jazz", "swim_diag_upright");
                mapping.Add("Jazz", "swing");
                mapping.Add("Jazz", "warp_in");
                mapping.Add("Jazz", "warp_out_freefall");
                mapping.Add("Jazz", "freefall");
                mapping.Add("Jazz", "warp_in_freefall");
                mapping.Add("Jazz", "warp_out");
                mapping.Add("Jazz", "pole_v");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_fall");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_hurt");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_idle");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_jump");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_end_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_lookup_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_run");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_stare");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_lookup_start_2");

                mapping.NextSet();
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_idle_flavor_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_jump_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_dash_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_rotate_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_ball_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_run_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_idle_2");
                mapping.Add("Unimplemented", "bonus_jazz_idle_flavor");
                mapping.Add("Unimplemented", "bonus_jazz_jump");
                mapping.Add("Unimplemented", "bonus_jazz_ball");
                mapping.Add("Unimplemented", "bonus_jazz_run");
                mapping.Add("Unimplemented", "bonus_jazz_dash");
                mapping.Add("Unimplemented", "bonus_jazz_rotate");
                mapping.Add("Unimplemented", "bonus_jazz_idle");

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Pole", "Jungle", /*JJ2DefaultPalette.JUNGLE_POLE_PALETTE*/ JJ2DefaultPalette.ByIndex);
                }

                mapping.NextSet();
                mapping.Add("LabRat", "attack");
                mapping.Add("LabRat", "idle");
                mapping.Add("LabRat", "walk");

                mapping.NextSet(); // set 60 (1.24) / set 59 (1.23)
                mapping.Add("Lizard", "copter_attack");
                mapping.Add("Lizard", "bomb");
                mapping.Add("Lizard", "copter_idle");
                mapping.Add("Lizard", "copter");
                mapping.Add("Lizard", "walk");

                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "airboard");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "airboard_turn");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "buttstomp_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "corpse");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "die");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_shoot");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_walk");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "eol");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "buttstomp");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_ver");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "transform_frog_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_idle");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_idle_flavor");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter_shoot_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter_shoot");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "pole_h");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_3");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_4");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_5");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_shoot");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_unknown_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_unknown_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "ledge");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift_jump_light");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift_jump_heavy");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lookup_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dizzy_walk");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "push");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_diag");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_diag");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "ball");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "run");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash_stop");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "walk_stop");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "run_stop");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "Spring");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "uppercut_placeholder_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "uppercut_placeholder_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "sidekick");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dizzy");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_downright");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_right");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_right_to_downright");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_right_to_upright");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_upright");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swing");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_in");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_out_freefall");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "freefall");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_in_freefall");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_out");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "pole_v");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "gun");

                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.NextSet();
                //mapping.Add("UI", "multiplayer_char", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(1);

                //mapping.Add("UI", "multiplayer_color", JJ2DefaultPalette.Menu);
                mapping.DiscardItems(1);

                mapping.Add("UI", "character_art_difficulty_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_art_difficulty_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.Add("UI", "character_art_difficulty_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.Add("Unimplemented", "key", JJ2DefaultPalette.Menu, skipNormalMap: true);

                //mapping.Add("UI", "loading_bar", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(1);

                mapping.Add("UI", "multiplayer_mode", JJ2DefaultPalette.Menu, skipNormalMap: true);

                //mapping.Add("UI", "character_name_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(1);
                //mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_name_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(1, JJ2Version.TSF | JJ2Version.CC);
                //mapping.Add("UI", "character_name_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(1);

                mapping.Add("UI", "character_art_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_art_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.Add("UI", "character_art_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.NextSet();

                //mapping.Add("UI", "font_medium_2", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "font_small_2", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "logo_large", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "tsf_title", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "menu_snow", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "logo_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "cc_title", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "cc_title_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                mapping.DiscardItems(8);

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Monkey", "Banana");
                    mapping.Add("Monkey", "BananaSplat");
                    mapping.Add("Monkey", "Jump");
                    mapping.Add("Monkey", "WalkStart");
                    mapping.Add("Monkey", "WalkEnd");
                    mapping.Add("Monkey", "Attack");
                    mapping.Add("Monkey", "Walk");

                    mapping.NextSet();
                    mapping.Add("Moth", "Green");
                    mapping.Add("Moth", "Gray");
                    mapping.Add("Moth", "Purple");
                    mapping.Add("Moth", "Pink");
                } else {
                    mapping.NextSet();
                }

                mapping.NextSet(3); // set 71 (1.24) / set 67 (1.23)
                mapping.Add("Pickup", "1up");
                mapping.Add("Pickup", "food_apple");
                mapping.Add("Pickup", "food_banana");
                mapping.Add("Object", "container_barrel");
                mapping.Add("Common", "poof_brown");
                mapping.Add("Object", "container_box_crush");
                mapping.Add("Object", "container_barrel_shrapnel_1");
                mapping.Add("Object", "container_barrel_shrapnel_2");
                mapping.Add("Object", "container_barrel_shrapnel_3");
                mapping.Add("Object", "container_barrel_shrapnel_4");
                mapping.Add("Object", "powerup_shield_bubble");
                mapping.Add("Pickup", "food_burger");
                mapping.Add("Pickup", "food_cake");
                mapping.Add("Pickup", "food_candy");
                mapping.Add("Object", "Savepoint");
                mapping.Add("Pickup", "food_cheese");
                mapping.Add("Pickup", "food_cherry");
                mapping.Add("Pickup", "food_chicken");
                mapping.Add("Pickup", "food_chips");
                mapping.Add("Pickup", "food_chocolate");
                mapping.Add("Pickup", "food_cola");
                mapping.Add("Pickup", "carrot");
                mapping.Add("Pickup", "Gem", JJ2DefaultPalette.Gem, addBorder: 1);
                mapping.Add("Pickup", "food_cucumber");
                mapping.Add("Pickup", "food_cupcake");
                mapping.Add("Pickup", "food_donut");
                mapping.Add("Pickup", "food_eggplant");
                mapping.Add("Unknown", "green_blast_thing");
                mapping.Add("Object", "ExitSign");
                mapping.Add("Pickup", "fast_fire_jazz");
                mapping.Add("Pickup", "fast_fire_spaz");
                mapping.Add("Object", "powerup_shield_fire");
                mapping.Add("Pickup", "food_fries");
                mapping.Add("Pickup", "fast_feet");
                mapping.Add("Object", "GemSuper", JJ2DefaultPalette.Gem);

                //mapping.Add("Pickup", "Gem2", JJ2DefaultPalette.Gem);
                mapping.DiscardItems(1);

                mapping.Add("Pickup", "airboard");
                mapping.Add("Pickup", "coin_gold");
                mapping.Add("Pickup", "food_grapes");
                mapping.Add("Pickup", "food_ham");
                mapping.Add("Pickup", "carrot_fly");
                mapping.Add("UI", "heart", skipNormalMap: true);
                mapping.Add("Pickup", "freeze_enemies");
                mapping.Add("Pickup", "food_ice_cream");
                mapping.Add("Common", "ice_break_shrapnel_1");
                mapping.Add("Common", "ice_break_shrapnel_2");
                mapping.Add("Common", "ice_break_shrapnel_3");
                mapping.Add("Common", "ice_break_shrapnel_4");
                mapping.Add("Pickup", "food_lemon");
                mapping.Add("Pickup", "food_lettuce");
                mapping.Add("Pickup", "food_lime");
                mapping.Add("Object", "powerup_shield_lightning");
                mapping.Add("Object", "TriggerCrate");
                mapping.Add("Pickup", "food_milk");
                mapping.Add("Object", "crate_ammo_bouncer");
                mapping.Add("Object", "crate_ammo_freezer");
                mapping.Add("Object", "crate_ammo_seeker");
                mapping.Add("Object", "crate_ammo_rf");
                mapping.Add("Object", "crate_ammo_toaster");
                mapping.Add("Object", "crate_ammo_tnt");
                mapping.Add("Object", "powerup_upgrade_blaster_jazz");
                mapping.Add("Object", "powerup_upgrade_bouncer");
                mapping.Add("Object", "powerup_upgrade_freezer");
                mapping.Add("Object", "powerup_upgrade_seeker");
                mapping.Add("Object", "powerup_upgrade_rf");
                mapping.Add("Object", "powerup_upgrade_toaster");
                mapping.Add("Object", "powerup_upgrade_pepper");
                mapping.Add("Object", "powerup_upgrade_electro");
                mapping.Add("Object", "powerup_transform_birdy");
                mapping.Add("Object", "powerup_transform_birdy_yellow");
                mapping.Add("Object", "powerup_swap_characters");
                mapping.Add("Pickup", "food_orange");
                mapping.Add("Pickup", "carrot_invincibility");
                mapping.Add("Pickup", "food_peach");
                mapping.Add("Pickup", "food_pear");
                mapping.Add("Pickup", "food_soda");
                mapping.Add("Pickup", "food_pie");
                mapping.Add("Pickup", "food_pizza");
                mapping.Add("Pickup", "potion");
                mapping.Add("Pickup", "food_pretzel");
                mapping.Add("Pickup", "food_sandwich");
                mapping.Add("Pickup", "food_strawberry");
                mapping.Add("Pickup", "carrot_full");
                mapping.Add("Object", "powerup_upgrade_blaster_spaz");
                mapping.Add("Pickup", "coin_silver");
                mapping.Add("Unknown", "green_blast_thing_2");
                mapping.Add("Common", "generator");
                mapping.Add("Pickup", "stopwatch");
                mapping.Add("Pickup", "food_taco");
                mapping.Add("Pickup", "food_thing");
                mapping.Add("Object", "tnt");
                mapping.Add("Pickup", "food_hotdog");
                mapping.Add("Pickup", "food_watermelon");
                mapping.Add("Object", "container_crate_shrapnel_1");
                mapping.Add("Object", "container_crate_shrapnel_2");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Pinball", "Bumper500", /*JJ2DefaultPalette.PINBALL_PALETTE*/ JJ2DefaultPalette.ByIndex);
                    mapping.Add("Pinball", "Bumper500Hit", /*JJ2DefaultPalette.PINBALL_PALETTE*/ JJ2DefaultPalette.ByIndex);
                    mapping.Add("Pinball", "BumperCarrot", /*JJ2DefaultPalette.PINBALL_PALETTE*/ JJ2DefaultPalette.ByIndex);
                    mapping.Add("Pinball", "BumperCarrotHit", /*JJ2DefaultPalette.PINBALL_PALETTE*/ JJ2DefaultPalette.ByIndex);

                    mapping.Add("Pinball", "PaddleLeft", JJ2DefaultPalette.ByIndex, addBorder: 1);
                    //mapping.Add("Pinball", "PaddleRight", JJ2DefaultPalette.ByIndex);
                    mapping.DiscardItems(1);

                    mapping.NextSet();
                    mapping.Add("Platform", "lab");
                    mapping.Add("Platform", "lab_chain");

                    mapping.NextSet();
                    mapping.Add("Pole", "Psych", /*JJ2DefaultPalette.PSYCH_POLE_PALETTE*/ JJ2DefaultPalette.ByIndex);

                    mapping.NextSet();
                    mapping.Add("Queen", "scream");
                    mapping.Add("Queen", "ledge");
                    mapping.Add("Queen", "ledge_recover");
                    mapping.Add("Queen", "idle");
                    mapping.Add("Queen", "brick");
                    mapping.Add("Queen", "fall");
                    mapping.Add("Queen", "stomp");
                    mapping.Add("Queen", "backstep");

                    mapping.NextSet();
                    mapping.Add("Rapier", "attack");
                    mapping.Add("Rapier", "attack_swing");
                    mapping.Add("Rapier", "idle");
                    mapping.Add("Rapier", "attack_start");
                    mapping.Add("Rapier", "attack_end");

                    mapping.NextSet();
                    mapping.Add("Raven", "Attack");
                    mapping.Add("Raven", "Idle");
                    mapping.Add("Raven", "Turn");

                    mapping.NextSet();
                    mapping.Add("Robot", "spike_ball");
                    mapping.Add("Robot", "attack_start");
                    mapping.Add("Robot", "attack");
                    mapping.Add("Robot", "copter");
                    mapping.Add("Robot", "idle");
                    mapping.Add("Robot", "attack_end");
                    mapping.Add("Robot", "shrapnel_1");
                    mapping.Add("Robot", "shrapnel_2");
                    mapping.Add("Robot", "shrapnel_3");
                    mapping.Add("Robot", "shrapnel_4");
                    mapping.Add("Robot", "shrapnel_5");
                    mapping.Add("Robot", "shrapnel_6");
                    mapping.Add("Robot", "shrapnel_7");
                    mapping.Add("Robot", "shrapnel_8");
                    mapping.Add("Robot", "shrapnel_9");
                    mapping.Add("Robot", "run");
                    mapping.Add("Robot", "copter_start");
                    mapping.Add("Robot", "copter_end");

                    mapping.NextSet();
                    mapping.Add("Object", "rolling_rock");

                    mapping.NextSet(); // set 80 (1.24) / set 76 (1.23)
                    mapping.Add("TurtleRocket", "downright");
                    mapping.Add("TurtleRocket", "upright");
                    mapping.Add("TurtleRocket", "smoke");
                    mapping.Add("TurtleRocket", "upright_to_downright");

                    mapping.NextSet(3);
                    mapping.Add("Skeleton", "Bone");
                    mapping.Add("Skeleton", "Skull");
                    mapping.Add("Skeleton", "Walk");
                } else {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Pole", "DiamondusTree", /*JJ2DefaultPalette.DIAMONDUS_POLE_PALETTE*/ JJ2DefaultPalette.ByIndex);

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Common", "Snow", JJ2DefaultPalette.Snow);

                    mapping.NextSet();
                    mapping.Add("Bolly", "rocket");
                    mapping.Add("Bolly", "mace_chain");
                    mapping.Add("Bolly", "bottom");
                    mapping.Add("Bolly", "top");
                    mapping.Add("Bolly", "puff");
                    mapping.Add("Bolly", "mace");
                    mapping.Add("Bolly", "turret");
                    mapping.Add("Bolly", "crosshairs");
                    mapping.NextSet();
                    mapping.Add("Platform", "sonic");
                    mapping.Add("Platform", "sonic_chain");
                    mapping.NextSet();
                    mapping.Add("Sparks", "idle");
                }

                mapping.NextSet();
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unknown_disoriented");
                mapping.Add("Spaz", "airboard");
                mapping.Add("Spaz", "airboard_turn");
                mapping.Add("Spaz", "buttstomp_end");
                mapping.Add("Spaz", "corpse");
                mapping.Add("Spaz", "die");
                mapping.Add("Spaz", "crouch_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "crouch_shoot_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Spaz", "crouch");
                mapping.Add("Spaz", "crouch_shoot");
                mapping.Add("Spaz", "crouch_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_door_enter");
                mapping.Add("Spaz", "vine_walk");
                mapping.Add("Spaz", "eol");
                mapping.Add("Spaz", "fall");
                mapping.Add("Spaz", "buttstomp");
                mapping.Add("Spaz", "fall_end");
                mapping.Add("Spaz", "shoot");
                mapping.Add("Spaz", "shoot_ver");
                mapping.Add("Spaz", "shoot_end");
                mapping.Add("Spaz", "transform_frog_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_ledge_climb");
                mapping.Add("Spaz", "vine_shoot_start");
                mapping.Add("Spaz", "vine_shoot_up_end");
                mapping.Add("Spaz", "vine_shoot_up");
                mapping.Add("Spaz", "vine_idle");
                mapping.Add("Spaz", "vine_idle_flavor");
                mapping.Add("Spaz", "vine_shoot_end");
                mapping.Add("Spaz", "vine_shoot");
                mapping.Add("Spaz", "copter");
                mapping.Add("Spaz", "copter_shoot_start");
                mapping.Add("Spaz", "copter_shoot");
                mapping.Add("Spaz", "pole_h");
                mapping.Add("Spaz", "hurt");
                mapping.Add("Spaz", "idle_flavor_1");
                mapping.Add("Spaz", "idle_flavor_2");
                mapping.Add("Spaz", "idle_flavor_3_placeholder");
                mapping.Add("Spaz", "idle_flavor_4");
                mapping.Add("Spaz", "idle_flavor_5");
                mapping.Add("Spaz", "vine_shoot_up_start");
                mapping.Add("Spaz", "fall_shoot");
                mapping.Add("Spaz", "jump_unknown_1");
                mapping.Add("Spaz", "jump_unknown_2");
                mapping.Add("Spaz", "jump");
                mapping.Add("Spaz", "ledge");
                mapping.Add("Spaz", "lift");
                mapping.Add("Spaz", "lift_jump_light");
                mapping.Add("Spaz", "lift_jump_heavy");
                mapping.Add("Spaz", "lookup_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_upright");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_ver_up");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_upleft_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_downleft_reverse");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_ver_down");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_downright");
                mapping.Add("Spaz", "dizzy_walk");
                mapping.Add("Spaz", "push");
                mapping.Add("Spaz", "shoot_start");
                mapping.Add("Spaz", "revup_start");
                mapping.Add("Spaz", "revup");
                mapping.Add("Spaz", "revup_end");
                mapping.Add("Spaz", "fall_diag");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unknown_mid_frame");
                mapping.Add("Spaz", "jump_diag");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_ver_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_ver");
                mapping.Add("Spaz", "ball");
                mapping.Add("Spaz", "run");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_aim_diag");
                mapping.Add("Spaz", "dash_start");
                mapping.Add("Spaz", "dash");
                mapping.Add("Spaz", "dash_stop");
                mapping.Add("Spaz", "walk_stop");
                mapping.Add("Spaz", "run_stop");
                mapping.Add("Spaz", "Spring");
                mapping.Add("Spaz", "idle");
                mapping.Add("Spaz", "sidekick");
                mapping.Add("Spaz", "sidekick_end");
                mapping.Add("Spaz", "sidekick_start");
                mapping.Add("Spaz", "dizzy");
                mapping.Add("Spaz", "swim_diag_downright");
                mapping.Add("Spaz", "swim_right");
                mapping.Add("Spaz", "swim_diag_right_to_downright");
                mapping.Add("Spaz", "swim_diag_right_to_upright");
                mapping.Add("Spaz", "swim_diag_upright");
                mapping.Add("Spaz", "swing");
                mapping.Add("Spaz", "warp_in");
                mapping.Add("Spaz", "warp_out_freefall");
                mapping.Add("Spaz", "freefall");
                mapping.Add("Spaz", "warp_in_freefall");
                mapping.Add("Spaz", "warp_out");
                mapping.Add("Spaz", "pole_v");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_end");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_fall");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_hurt");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_idle");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_jump");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_end_2");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_lookup_start");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_run");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_stare");
                mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_lookup_start_2");
                mapping.NextSet(); // set 90 (1.24) / set 86 (1.23)
                mapping.Add("Spaz", "idle_flavor_3_start");
                mapping.Add("Spaz", "idle_flavor_3");
                mapping.Add("Spaz", "idle_flavor_3_bird");
                mapping.Add("Spaz", "idle_flavor_5_spaceship");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Unimplemented", "bonus_spaz_idle_flavor");
                    mapping.Add("Unimplemented", "bonus_spaz_jump");
                    mapping.Add("Unimplemented", "bonus_spaz_ball");
                    mapping.Add("Unimplemented", "bonus_spaz_run");
                    mapping.Add("Unimplemented", "bonus_spaz_dash");
                    mapping.Add("Unimplemented", "bonus_spaz_rotate");
                    mapping.Add("Unimplemented", "bonus_spaz_idle");

                    mapping.NextSet(2);
                    mapping.Add("Object", "3d_spike");
                    mapping.Add("Object", "3d_spike_chain");

                    mapping.NextSet();
                    //mapping.Add("Object", "3d_spike_2");
                    //mapping.Add("Object", "3d_spike_2_chain");
                    mapping.DiscardItems(2);

                    mapping.NextSet();
                    mapping.Add("Platform", "spike");
                    mapping.Add("Platform", "spike_chain");
                } else {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Spring", "spring_blue_ver");
                mapping.Add("Spring", "spring_blue_hor");
                mapping.Add("Spring", "spring_blue_ver_reverse");
                mapping.Add("Spring", "spring_green_ver_reverse");
                mapping.Add("Spring", "spring_red_ver_reverse");
                mapping.Add("Spring", "spring_green_ver");
                mapping.Add("Spring", "spring_green_hor");
                mapping.Add("Spring", "spring_red_ver");
                mapping.Add("Spring", "spring_red_hor");

                mapping.NextSet();
                mapping.Add("Common", "SteamNote");

                if (isFull) {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Sucker", "walk_top");
                mapping.Add("Sucker", "inflated_deflate");
                mapping.Add("Sucker", "walk_ver_down");
                mapping.Add("Sucker", "fall");
                mapping.Add("Sucker", "inflated");
                mapping.Add("Sucker", "poof");
                mapping.Add("Sucker", "walk");
                mapping.Add("Sucker", "walk_ver_up");

                if (isFull) {
                    mapping.NextSet(); // set 100 (1.24) / set 96 (1.23)
                    mapping.Add("TurtleTube", "Idle");

                    mapping.NextSet();
                    mapping.Add("TurtleToughBoss", "attack_start");
                    mapping.Add("TurtleToughBoss", "attack_end");
                    mapping.Add("TurtleToughBoss", "shell");
                    mapping.Add("TurtleToughBoss", "mace");
                    mapping.Add("TurtleToughBoss", "idle");
                    mapping.Add("TurtleToughBoss", "walk");

                    mapping.NextSet();
                    mapping.Add("TurtleTough", "Walk");
                }

                mapping.NextSet();
                mapping.Add("Turtle", "attack");
                mapping.Add("Turtle", "idle_flavor");
                mapping.Add("Turtle", "turn_start");
                mapping.Add("Turtle", "turn_end");
                mapping.Add("Turtle", "shell_reverse");
                mapping.Add("Turtle", "shell");
                mapping.Add("Turtle", "idle");
                mapping.Add("Turtle", "walk");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Tweedle", "magnet_start");
                    mapping.Add("Tweedle", "spin");
                    mapping.Add("Tweedle", "magnet_end");
                    mapping.Add("Tweedle", "shoot_jazz");
                    mapping.Add("Tweedle", "shoot_spaz");
                    mapping.Add("Tweedle", "hurt");
                    mapping.Add("Tweedle", "idle");
                    mapping.Add("Tweedle", "magnet");
                    mapping.Add("Tweedle", "walk");

                    mapping.NextSet();
                    mapping.Add("Uterus", "closed_start");
                    mapping.Add("Uterus", "crab_spawn");
                    mapping.Add("Uterus", "closed_idle");
                    mapping.Add("Uterus", "idle");
                    mapping.Add("Crab", "fall_end");
                    mapping.Add("Uterus", "closed_end");
                    mapping.Add("Uterus", "shield");
                    mapping.Add("Crab", "walk");

                    mapping.NextSet();
                    mapping.DiscardItems(1); // Red dot

                    mapping.Add("Object", "vine");
                    mapping.NextSet();
                    mapping.Add("Object", "Bonus10");
                    mapping.NextSet();
                    mapping.Add("Object", "Bonus100");
                }

                mapping.NextSet();
                mapping.Add("Object", "Bonus20");

                if (isFull) {
                    mapping.NextSet(); // set 110 (1.24) / set 106 (1.23)
                    mapping.Add("Object", "Bonus50");
                }

                mapping.NextSet(2);
                mapping.Add("Witch", "attack");
                mapping.Add("Witch", "die");
                mapping.Add("Witch", "idle");
                mapping.Add("Witch", "bullet_magic");

                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_throw_fireball");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_appear");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_vanish");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_bullet_fireball");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_idle");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter_attack");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_bomb");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter_idle");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_walk");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_attack");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle_flavor");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_end");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_shell_reverse");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_shell");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_walk");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Doggy", "xmas_attack");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Doggy", "xmas_walk");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Sparks", "ghost_idle");
            }

            return mapping;
        }

        public static AnimSetMapping GetSampleMapping(JJ2Version version)
        {
            AnimSetMapping mapping = new AnimSetMapping(version);

            if (version == JJ2Version.PlusExtension) {
                // Nothing is here...
            } else if (version != JJ2Version.Unknown) {
                bool isFull = (version & JJ2Version.SharewareDemo) == 0;

                // set 0 (all)
                mapping.Add("Weapon", "bullet_shield_bubble_1");
                mapping.Add("Weapon", "bullet_shield_bubble_2");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_1");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_2");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_3");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_4");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_5");
                mapping.Add("Weapon", "bullet_bouncer_upgraded_6");
                mapping.Add("Weapon", "tnt_explosion");
                mapping.Add("Weapon", "ricochet_contact");
                mapping.Add("Weapon", "ricochet_bullet_1");
                mapping.Add("Weapon", "ricochet_bullet_2");
                mapping.Add("Weapon", "ricochet_bullet_3");
                mapping.Add("Weapon", "bullet_shield_fire_1");
                mapping.Add("Weapon", "bullet_shield_fire_2");
                mapping.Add("Weapon", "bullet_bouncer_1");
                mapping.Add("Weapon", "bullet_blaster_jazz_1");
                mapping.Add("Weapon", "bullet_blaster_jazz_2");
                mapping.Add("Weapon", "bullet_blaster_jazz_3");
                mapping.Add("Weapon", "bullet_bouncer_2");
                mapping.Add("Weapon", "bullet_bouncer_3");
                mapping.Add("Weapon", "bullet_bouncer_4");
                mapping.Add("Weapon", "bullet_bouncer_5");
                mapping.Add("Weapon", "bullet_bouncer_6");
                mapping.Add("Weapon", "bullet_bouncer_7");
                mapping.Add("Weapon", "bullet_blaster_jazz_4");
                mapping.Add("Weapon", "bullet_pepper");
                mapping.Add("Weapon", "bullet_freezer_1");
                mapping.Add("Weapon", "bullet_freezer_2");
                mapping.Add("Weapon", "bullet_freezer_upgraded_1");
                mapping.Add("Weapon", "bullet_freezer_upgraded_2");
                mapping.Add("Weapon", "bullet_freezer_upgraded_3");
                mapping.Add("Weapon", "bullet_freezer_upgraded_4");
                mapping.Add("Weapon", "bullet_freezer_upgraded_5");
                mapping.Add("Weapon", "bullet_electro_1");
                mapping.Add("Weapon", "bullet_electro_2");
                mapping.Add("Weapon", "bullet_electro_3");
                mapping.Add("Weapon", "bullet_rf");
                mapping.Add("Weapon", "bullet_seeker");
                mapping.Add("Weapon", "bullet_blaster_spaz_1");
                mapping.Add("Weapon", "bullet_blaster_spaz_2");
                mapping.Add("Weapon", "bullet_blaster_spaz_3");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Bat", "noise");
                    mapping.NextSet(6);
                    mapping.Add("Bilsy", "appear_2");
                    mapping.Add("Bilsy", "snap");
                    mapping.Add("Bilsy", "throw_fireball");
                    mapping.Add("Bilsy", "fire_start");
                    mapping.Add("Bilsy", "scary");
                    mapping.Add("Bilsy", "thunder");
                    mapping.Add("Bilsy", "appear_1");
                    mapping.NextSet(4); // set 11 (all)
                    mapping.Add("Unknown", "unknown_bonus1");
                    mapping.Add("Unknown", "unknown_bonusblub");
                } else {
                    mapping.NextSet();
                }

                mapping.NextSet(3); // set 14
                mapping.Add("Bubba", "hop_1");
                mapping.Add("Bubba", "hop_2");
                mapping.Add("Bubba", "unknown_bubbaexplo");
                mapping.Add("Bubba", "unknown_frog2");
                mapping.Add("Bubba", "unknown_frog3");
                mapping.Add("Bubba", "unknown_frog4");
                mapping.Add("Bubba", "unknown_frog5");
                mapping.Add("Bubba", "sneeze");
                mapping.Add("Bubba", "tornado");
                mapping.NextSet(); // set 15
                mapping.Add("Bee", "noise");

                if (isFull) {
                    mapping.NextSet(3);
                }

                mapping.NextSet(2); // set 20 (all)
                mapping.Add("Caterpillar", "dizzy");

                if (isFull) {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Common", "char_airboard");
                mapping.Add("Common", "char_airboard_turn_1");
                mapping.Add("Common", "char_airboard_turn_2");
                mapping.Add("Common", "unknown_base");
                mapping.Add("Common", "powerup_shield_damage_1");
                mapping.Add("Common", "powerup_shield_damage_2");
                mapping.Add("Common", "bomb");
                mapping.Add("Birdy", "fly_1");
                mapping.Add("Birdy", "fly_2");
                mapping.Add("Weapon", "bouncer");
                mapping.Add("Common", "blub1");
                mapping.Add("Weapon", "shield_bubble_bullet");
                mapping.Add("Weapon", "shield_fire_bullet");
                mapping.Add("Common", "ambient_fire");
                mapping.Add("Object", "container_barrel_break");
                mapping.Add("Common", "powerup_shield_timer");
                mapping.Add("Pickup", "coin");
                mapping.Add("Common", "scenery_collapse");
                mapping.Add("Common", "cup");
                mapping.Add("Common", "scenery_destruct");
                mapping.Add("Common", "down");
                mapping.Add("Common", "downfl2");
                mapping.Add("Pickup", "food_drink_1");
                mapping.Add("Pickup", "food_drink_2");
                mapping.Add("Pickup", "food_drink_3");
                mapping.Add("Pickup", "food_drink_4");
                mapping.Add("Pickup", "food_edible_1");
                mapping.Add("Pickup", "food_edible_2");
                mapping.Add("Pickup", "food_edible_3");
                mapping.Add("Pickup", "food_edible_4");
                mapping.Add("Pickup", "shield_lightning_bullet_1");
                mapping.Add("Pickup", "shield_lightning_bullet_2");
                mapping.Add("Pickup", "shield_lightning_bullet_3");
                mapping.Add("Weapon", "tnt");
                mapping.Add("Weapon", "wall_poof");
                mapping.Add("Weapon", "toaster");
                mapping.Add("Common", "flap");
                mapping.Add("Common", "swish_9");
                mapping.Add("Common", "swish_10");
                mapping.Add("Common", "swish_11");
                mapping.Add("Common", "swish_12");
                mapping.Add("Common", "swish_13");
                mapping.Add("Object", "GemSuperBreak");
                mapping.Add("Object", "PowerupBreak");
                mapping.Add("Common", "gunsm1");
                mapping.Add("Pickup", "1up");
                mapping.Add("Unknown", "common_head");
                mapping.Add("Common", "copter_noise");
                mapping.Add("Common", "hibell");
                mapping.Add("Common", "holyflut");
                mapping.Add("UI", "weapon_change");
                mapping.Add("Common", "IceBreak");
                mapping.Add("Object", "shell_noise_1");
                mapping.Add("Object", "shell_noise_2");
                mapping.Add("Object", "shell_noise_3");
                mapping.Add("Object", "shell_noise_4");
                mapping.Add("Object", "shell_noise_5");
                mapping.Add("Object", "shell_noise_6");
                mapping.Add("Object", "shell_noise_7");
                mapping.Add("Object", "shell_noise_8");
                mapping.Add("Object", "shell_noise_9");
                mapping.Add("Unknown", "common_itemtre");
                mapping.Add("Common", "char_jump");
                mapping.Add("Common", "char_jump_alt");
                mapping.Add("Common", "land1");
                mapping.Add("Common", "land2");
                mapping.Add("Common", "land3");
                mapping.Add("Common", "land4");
                mapping.Add("Common", "land5");
                mapping.Add("Common", "char_land");
                mapping.Add("Common", "loadjazz");
                mapping.Add("Common", "loadspaz");
                mapping.Add("Common", "metalhit");
                mapping.Add("Unimplemented", "powerup_jazz1_style");
                mapping.Add("Object", "BonusNotEnoughCoins");
                mapping.Add("Pickup", "gem");
                mapping.Add("Pickup", "ammo");
                mapping.Add("Common", "pistol1");
                mapping.Add("Common", "plop_5");
                mapping.Add("Common", "plop_1");
                mapping.Add("Common", "plop_2");
                mapping.Add("Common", "plop_3");
                mapping.Add("Common", "plop_4");
                mapping.Add("Common", "plop_6");
                mapping.Add("Spaz", "idle_flavor_4_spaceship");
                mapping.Add("Common", "copter_pre");
                mapping.Add("Common", "char_revup");
                mapping.Add("Common", "ringgun1");
                mapping.Add("Common", "ringgun2");
                mapping.Add("Weapon", "shield_fire_noise");
                mapping.Add("Weapon", "shield_lightning_noise");
                mapping.Add("Weapon", "shield_lightning_noise_2");
                mapping.Add("Common", "shldof3");
                mapping.Add("Common", "slip");
                mapping.Add("Common", "splat_1");
                mapping.Add("Common", "splat_2");
                mapping.Add("Common", "splat_3");
                mapping.Add("Common", "splat_4");
                mapping.Add("Common", "splat_5");
                mapping.Add("Common", "splat_6");
                mapping.Add("Spring", "spring_2");
                mapping.Add("Common", "steam_low");
                mapping.Add("Common", "step");
                mapping.Add("Common", "stretch");
                mapping.Add("Common", "swish_1");
                mapping.Add("Common", "swish_2");
                mapping.Add("Common", "swish_3");
                mapping.Add("Common", "swish_4");
                mapping.Add("Common", "swish_5");
                mapping.Add("Common", "swish_6");
                mapping.Add("Common", "swish_7");
                mapping.Add("Common", "swish_8");
                mapping.Add("Common", "warp_in");
                mapping.Add("Common", "warp_out");
                mapping.Add("Common", "char_double_jump");
                mapping.Add("Common", "water_splash");
                mapping.Add("Object", "container_crate_break");

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Demon", "attack");
                    mapping.NextSet(3);
                    mapping.Add("Devan", "spit_fireball");
                    mapping.Add("Devan", "flap");
                    mapping.Add("Devan", "unknown_frog4");
                    mapping.Add("Devan", "jump_up");
                    mapping.Add("Devan", "laugh");
                    mapping.Add("Devan", "shoot");
                    mapping.Add("Devan", "transform_demon_stretch_2");
                    mapping.Add("Devan", "transform_demon_stretch_4");
                    mapping.Add("Devan", "transform_demon_stretch_1");
                    mapping.Add("Devan", "transform_demon_stretch_3");
                    mapping.Add("Devan", "unknown_vanish");
                    mapping.Add("Devan", "unknown_whistledescending2");
                    mapping.Add("Devan", "transform_demon_wings");
                    mapping.NextSet(2);
                    mapping.Add("Doggy", "attack");
                    mapping.Add("Doggy", "noise");
                    mapping.Add("Doggy", "woof_1");
                    mapping.Add("Doggy", "woof_2");
                    mapping.Add("Doggy", "woof_3");
                } else {
                    mapping.NextSet(2);
                }

                mapping.NextSet(2); // set 31 (all)
                mapping.Add("Dragonfly", "noise");

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Cinematic", "ending_eva_thankyou");
                }

                mapping.NextSet();
                mapping.Add("Jazz", "level_complete");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "level_complete");
                mapping.NextSet();
                mapping.Add("Spaz", "level_complete");

                mapping.NextSet();
                //mapping.Add("Cinematic", "logo_epic_1");
                //mapping.Add("Cinematic", "logo_epic_2");
                mapping.DiscardItems(2);

                mapping.NextSet();
                mapping.Add("Eva", "Kiss1");
                mapping.Add("Eva", "Kiss2");
                mapping.Add("Eva", "Kiss3");
                mapping.Add("Eva", "Kiss4");

                if (isFull) {
                    mapping.NextSet(2); // set 40 (1.24) / set 39 (1.23)
                    mapping.Add("Unknown", "unknown_fan");
                    mapping.NextSet();
                    mapping.Add("FatChick", "attack_1");
                    mapping.Add("FatChick", "attack_2");
                    mapping.Add("FatChick", "attack_3");
                    mapping.NextSet();
                    mapping.Add("Fencer", "attack");
                    mapping.NextSet();
                }

                mapping.NextSet(4);
                mapping.Add("Frog", "noise_1");
                mapping.Add("Frog", "noise_2");
                mapping.Add("Frog", "noise_3");
                mapping.Add("Frog", "noise_4");
                mapping.Add("Frog", "noise_5");
                mapping.Add("Frog", "noise_6");
                mapping.Add("Frog", "transform");
                mapping.Add("Frog", "tongue");

                if (isFull) {
                    mapping.NextSet(3); // set 50 (1.24) / set 49 (1.23)
                    mapping.Add("Unimplemented", "boxing_glove_hit");
                    mapping.NextSet();
                }
                
                mapping.NextSet();
                mapping.Add("MadderHatter", "cup");
                mapping.Add("MadderHatter", "hat");
                mapping.Add("MadderHatter", "spit");
                mapping.Add("MadderHatter", "splash_1");
                mapping.Add("MadderHatter", "splash_2");

                if (isFull) {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Cinematic", "opening_blow");
                mapping.Add("Cinematic", "opening_boom_1");
                mapping.Add("Cinematic", "opening_boom_2");
                mapping.Add("Cinematic", "opening_brake");
                mapping.Add("Cinematic", "opening_end_shoot");
                mapping.Add("Cinematic", "opening_rope_grab");
                mapping.Add("Cinematic", "opening_sweep_1");
                mapping.Add("Cinematic", "opening_sweep_2");
                mapping.Add("Cinematic", "opening_sweep_3");
                mapping.Add("Cinematic", "opening_gun_noise_1");
                mapping.Add("Cinematic", "opening_gun_noise_2");
                mapping.Add("Cinematic", "opening_gun_noise_3");
                mapping.Add("Cinematic", "opening_helicopter");
                mapping.Add("Cinematic", "opening_hit_spaz");
                mapping.Add("Cinematic", "opening_hit_turtle");
                mapping.Add("Cinematic", "opening_vo_1");
                mapping.Add("Cinematic", "opening_gun_blow");
                mapping.Add("Cinematic", "opening_insect");
                mapping.Add("Cinematic", "opening_trolley_push");
                mapping.Add("Cinematic", "opening_land");
                mapping.Add("Cinematic", "opening_turtle_growl");
                mapping.Add("Cinematic", "opening_turtle_grunt");
                mapping.Add("Cinematic", "opening_rock");
                mapping.Add("Cinematic", "opening_rope_1");
                mapping.Add("Cinematic", "opening_rope_2");
                mapping.Add("Cinematic", "opening_run");
                mapping.Add("Cinematic", "opening_shot");
                mapping.Add("Cinematic", "opening_shot_grn");
                mapping.Add("Cinematic", "opening_slide");
                mapping.Add("Cinematic", "opening_end_sfx");
                mapping.Add("Cinematic", "opening_swish_1");
                mapping.Add("Cinematic", "opening_swish_2");
                mapping.Add("Cinematic", "opening_swish_3");
                mapping.Add("Cinematic", "opening_swish_4");
                mapping.Add("Cinematic", "opening_turtle_ugh");
                mapping.Add("Cinematic", "opening_up_1");
                mapping.Add("Cinematic", "opening_up_2");
                mapping.Add("Cinematic", "opening_wind");

                if (isFull) {
                    mapping.NextSet();
                }

                mapping.NextSet(2);
                mapping.Add("Jazz", "ledge");
                mapping.Add("Jazz", "hurt_1");
                mapping.Add("Jazz", "hurt_2");
                mapping.Add("Jazz", "hurt_3");
                mapping.Add("Jazz", "hurt_4");
                mapping.Add("Jazz", "idle_flavor_3");
                mapping.Add("Jazz", "hurt_5");
                mapping.Add("Jazz", "hurt_6");
                mapping.Add("Jazz", "hurt_7");
                mapping.Add("Jazz", "hurt_8");
                mapping.Add("Jazz", "carrot");
                mapping.Add("Jazz", "idle_flavor_4");

                if (isFull) {
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("LabRat", "attack");
                mapping.Add("LabRat", "noise_1");
                mapping.Add("LabRat", "noise_2");
                mapping.Add("LabRat", "noise_3");
                mapping.Add("LabRat", "noise_4");
                mapping.Add("LabRat", "noise_5");
                mapping.NextSet(); // set 60 (1.24) / set 59 (1.23)
                mapping.Add("Lizard", "noise_1");
                mapping.Add("Lizard", "noise_2");
                mapping.Add("Lizard", "noise_3");
                mapping.Add("Lizard", "noise_4");

                mapping.NextSet(3, JJ2Version.TSF | JJ2Version.CC);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "die");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_3");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_4");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_5");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_6");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_7");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt_8");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "unknown_mic1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "unknown_mic2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "sidekick");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_3");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_4");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "unused_touch");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "yahoo");

                mapping.NextSet(3);
                mapping.Add("UI", "select_1");
                mapping.Add("UI", "select_2");
                mapping.Add("UI", "select_3");
                mapping.Add("UI", "select_4");
                mapping.Add("UI", "select_5");
                mapping.Add("UI", "select_6");
                mapping.Add("UI", "select_7");
                mapping.Add("UI", "type_char");
                mapping.Add("UI", "type_enter");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Monkey", "BananaSplat");
                    mapping.Add("Monkey", "BananaThrow");
                    mapping.NextSet();
                    mapping.Add("Moth", "flap");
                }

                mapping.NextSet();
                //mapping.Add("Cinematic", "orangegames_1_boom_l");
                //mapping.Add("Cinematic", "orangegames_1_boom_r");
                //mapping.Add("Cinematic", "orangegames_7_bubble_l");
                //mapping.Add("Cinematic", "orangegames_7_bubble_r");
                //mapping.Add("Cinematic", "orangegames_2_glass_1_l");
                //mapping.Add("Cinematic", "orangegames_2_glass_1_r");
                //mapping.Add("Cinematic", "orangegames_5_glass_2_l");
                //mapping.Add("Cinematic", "orangegames_5_glass_2_r");
                //mapping.Add("Cinematic", "orangegames_6_merge");
                //mapping.Add("Cinematic", "orangegames_3_sweep_1_l");
                //mapping.Add("Cinematic", "orangegames_3_sweep_1_r");
                //mapping.Add("Cinematic", "orangegames_4_sweep_2_l");
                //mapping.Add("Cinematic", "orangegames_4_sweep_2_r");
                //mapping.Add("Cinematic", "orangegames_5_sweep_3_l");
                //mapping.Add("Cinematic", "orangegames_5_sweep_3_r");
                mapping.DiscardItems(15);
                mapping.NextSet(); // set 70 (1.24) / set 66 (1.23)
                //mapping.Add("Cinematic", "project2_unused_crunch");
                //mapping.Add("Cinematic", "project2_10_fart");
                //mapping.Add("Cinematic", "project2_unused_foew1");
                //mapping.Add("Cinematic", "project2_unused_foew4");
                //mapping.Add("Cinematic", "project2_unused_foew5");
                //mapping.Add("Cinematic", "project2_unused_frog1");
                //mapping.Add("Cinematic", "project2_unused_frog2");
                //mapping.Add("Cinematic", "project2_unused_frog3");
                //mapping.Add("Cinematic", "project2_unused_frog4");
                //mapping.Add("Cinematic", "project2_unused_frog5");
                //mapping.Add("Cinematic", "project2_unused_kiss4");
                //mapping.Add("Cinematic", "project2_unused_open");
                //mapping.Add("Cinematic", "project2_unused_pinch1");
                //mapping.Add("Cinematic", "project2_unused_pinch2");
                //mapping.Add("Cinematic", "project2_3_plop_1");
                //mapping.Add("Cinematic", "project2_4_plop_2");
                //mapping.Add("Cinematic", "project2_5_plop_3");
                //mapping.Add("Cinematic", "project2_6_plop_4");
                //mapping.Add("Cinematic", "project2_7_plop_5");
                //mapping.Add("Cinematic", "project2_9_spit");
                //mapping.Add("Cinematic", "project2_unused_splout");
                //mapping.Add("Cinematic", "project2_2_splat");
                //mapping.Add("Cinematic", "project2_1_8_throw");
                //mapping.Add("Cinematic", "project2_unused_tong");
                mapping.DiscardItems(24);
                mapping.NextSet();
                mapping.Add("Object", "SavepointOpen");
                mapping.Add("Object", "copter");
                mapping.Add("Unknown", "unknown_pickup_stretch1a");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Pinball", "BumperHit");
                    mapping.Add("Pinball", "Flipper1");
                    mapping.Add("Pinball", "Flipper2");
                    mapping.Add("Pinball", "Flipper3");
                    mapping.Add("Pinball", "Flipper4");
                    mapping.NextSet(3);
                    mapping.Add("Queen", "Spring");
                    mapping.Add("Queen", "scream");
                    mapping.NextSet();
                    mapping.Add("Rapier", "die");
                    mapping.Add("Rapier", "noise_1");
                    mapping.Add("Rapier", "noise_2");
                    mapping.Add("Rapier", "noise_3");
                    mapping.Add("Rapier", "clunk");
                    mapping.NextSet(2);
                    mapping.Add("Robot", "unknown_big1");
                    mapping.Add("Robot", "unknown_big2");
                    mapping.Add("Robot", "unknown_can1");
                    mapping.Add("Robot", "unknown_can2");
                    mapping.Add("Robot", "attack_start");
                    mapping.Add("Robot", "attack_end");
                    mapping.Add("Robot", "attack");
                    mapping.Add("Robot", "unknown_hydropuf");
                    mapping.Add("Robot", "unknown_idle1");
                    mapping.Add("Robot", "unknown_idle2");
                    mapping.Add("Robot", "unknown_jmpcan1");
                    mapping.Add("Robot", "unknown_jmpcan10");
                    mapping.Add("Robot", "unknown_jmpcan2");
                    mapping.Add("Robot", "unknown_jmpcan3");
                    mapping.Add("Robot", "unknown_jmpcan4");
                    mapping.Add("Robot", "unknown_jmpcan5");
                    mapping.Add("Robot", "unknown_jmpcan6");
                    mapping.Add("Robot", "unknown_jmpcan7");
                    mapping.Add("Robot", "unknown_jmpcan8");
                    mapping.Add("Robot", "unknown_jmpcan9");
                    mapping.Add("Robot", "shrapnel_1");
                    mapping.Add("Robot", "shrapnel_2");
                    mapping.Add("Robot", "shrapnel_3");
                    mapping.Add("Robot", "shrapnel_4");
                    mapping.Add("Robot", "shrapnel_5");
                    mapping.Add("Robot", "attack_start_shutter");
                    mapping.Add("Robot", "unknown_out");
                    mapping.Add("Robot", "unknown_poep");
                    mapping.Add("Robot", "unknown_pole");
                    mapping.Add("Robot", "unknown_shoot");
                    mapping.Add("Robot", "walk_1");
                    mapping.Add("Robot", "walk_2");
                    mapping.Add("Robot", "walk_3");
                    mapping.NextSet();
                    mapping.Add("Object", "rolling_rock");
                    mapping.NextSet();
                }

                mapping.NextSet(); // set 81 (1.24) / set 77 (1.23)

                if (isFull) {
                    mapping.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unknown", "sugar_rush_heartbeat");
                }

                mapping.Add("Common", "SugarRush");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Common", "science_noise");
                    mapping.NextSet();
                    mapping.Add("Skeleton", "bone_1");
                    mapping.Add("Skeleton", "bone_2");
                    mapping.Add("Skeleton", "bone_3");
                    mapping.Add("Skeleton", "bone_4");
                    mapping.Add("Skeleton", "bone_5");
                    mapping.Add("Skeleton", "bone_6");
                }

                mapping.NextSet();
                mapping.Add("Pole", "TreeFall1");
                mapping.Add("Pole", "TreeFall2");
                mapping.Add("Pole", "TreeFall3");

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Bolly", "missile_1");
                    mapping.Add("Bolly", "missile_2");
                    mapping.Add("Bolly", "missile_3");
                    mapping.Add("Bolly", "noise");
                    mapping.Add("Bolly", "lock_on");
                    mapping.NextSet(3);
                }

                mapping.NextSet(3); // set 92 (1.24) / set 88 (1.23)
                mapping.Add("Spaz", "hurt_1");
                mapping.Add("Spaz", "hurt_2");
                mapping.Add("Spaz", "idle_flavor_3_bird_land");
                mapping.Add("Spaz", "idle_flavor_4");
                mapping.Add("Spaz", "idle_flavor_3_bird");
                mapping.Add("Spaz", "idle_flavor_3_eat");
                mapping.Add("Spaz", "jump_1");
                mapping.Add("Spaz", "jump_2");
                mapping.Add("Spaz", "idle_flavor_2");
                mapping.Add("Spaz", "hihi");
                mapping.Add("Spaz", "spring_1");
                mapping.Add("Spaz", "double_jump");
                mapping.Add("Spaz", "sidekick_1");
                mapping.Add("Spaz", "sidekick_2");
                mapping.Add("Spaz", "spring_2");
                mapping.Add("Spaz", "oooh");
                mapping.Add("Spaz", "ledge");
                mapping.Add("Spaz", "jump_3");
                mapping.Add("Spaz", "jump_4");

                if (isFull) {
                    mapping.NextSet(3);
                }

                mapping.NextSet();
                mapping.Add("Spring", "spring_ver_down");
                mapping.Add("Spring", "Spring");
                mapping.NextSet();
                mapping.Add("Common", "SteamNote");

                if (isFull) {
                    mapping.NextSet();
                    mapping.Add("Unimplemented", "dizzy");
                }

                mapping.NextSet();
                mapping.Add("Sucker", "deflate");
                mapping.Add("Sucker", "pinch_1");
                mapping.Add("Sucker", "pinch_2");
                mapping.Add("Sucker", "pinch_3");
                mapping.Add("Sucker", "plop_1");
                mapping.Add("Sucker", "plop_2");
                mapping.Add("Sucker", "plop_3");
                mapping.Add("Sucker", "plop_4");
                mapping.Add("Sucker", "up");

                if (isFull) {
                    mapping.NextSet(2); // set 101 (1.24) / set 97 (1.23)
                    mapping.Add("TurtleToughBoss", "attack_start");
                    mapping.Add("TurtleToughBoss", "attack_end");
                    mapping.Add("TurtleToughBoss", "mace");
                    mapping.NextSet();
                }

                mapping.NextSet();
                mapping.Add("Turtle", "attack_bite");
                mapping.Add("Turtle", "turn_start");
                mapping.Add("Turtle", "shell_collide");
                mapping.Add("Turtle", "idle_1");
                mapping.Add("Turtle", "idle_2");
                mapping.Add("Turtle", "attack_neck");
                mapping.Add("Turtle", "noise_1");
                mapping.Add("Turtle", "noise_2");
                mapping.Add("Turtle", "noise_3");
                mapping.Add("Turtle", "noise_4");
                mapping.Add("Turtle", "turn_end");

                if (isFull) {
                    mapping.NextSet(2);
                    mapping.Add("Uterus", "closed_start");
                    mapping.Add("Uterus", "closed_end");
                    mapping.Add("Crab", "noise_1");
                    mapping.Add("Crab", "noise_2");
                    mapping.Add("Crab", "noise_3");
                    mapping.Add("Crab", "noise_4");
                    mapping.Add("Crab", "noise_5");
                    mapping.Add("Crab", "noise_6");
                    mapping.Add("Crab", "noise_7");
                    mapping.Add("Crab", "noise_8");
                    mapping.Add("Uterus", "scream");
                    mapping.Add("Crab", "step_1");
                    mapping.Add("Crab", "step_2");
                    mapping.NextSet(4);
                }

                mapping.NextSet(2); // set 111 (1.24) / set 107 (1.23)
                mapping.Add("Common", "wind");
                mapping.NextSet();
                mapping.Add("Witch", "laugh");
                mapping.Add("Witch", "magic");

                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_appear_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_snap");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_throw_fireball");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_fire_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_scary");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_thunder");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_appear_1");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_noise_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_noise_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_noise_3");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_noise_4");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_attack_bite");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_start");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_shell_collide");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_attack_neck");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_noise_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_noise_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_noise_3");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_noise_4");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_end");
                mapping.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Doggy", "xmas_attack");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Doggy", "xmas_noise");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Doggy", "xmas_woof_1");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Doggy", "xmas_woof_2");
                mapping.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Doggy", "xmas_woof_3");
            }

            return mapping;
        }


        //private MappingType type;
        private JJ2Version version;
        private int currentItem, currentSet;
        private Dictionary<Pair<int, int>, Data> data = new Dictionary<Pair<int, int>, Data>();

        public AnimSetMapping(JJ2Version version)
        {
            this.version = version;
        }

        private void DiscardItems(int advanceBy, JJ2Version appliesTo = JJ2Version.All)
        {
            if ((version & appliesTo) != 0) {
                for (int i = 0; i < advanceBy; i++) {
                    data.Add(Pair.Create(currentSet, currentItem), new Data {
                        Category = Discard
                    });
                    currentItem++;
                }
            }
        }

        private void SkipItems(int advanceBy = 1)
        {
            currentItem += advanceBy;
        }

        private void NextSet(int advanceBy = 1, JJ2Version appliesTo = JJ2Version.All)
        {
            if ((version & appliesTo) != 0) {
                currentSet += advanceBy;
                currentItem = 0;
            }
        }

        private void Add(JJ2Version appliesTo, string category, string name, Color[] palette = null, bool skipNormalMap = false, int addBorder = 0) {
            if ((version & appliesTo) != 0) {
                data.Add(Pair.Create(currentSet, currentItem), new Data {
                    Category = category,
                    Name = name,
                    Palette = palette,
                    SkipNormalMap = skipNormalMap,
                    AddBorder = addBorder
                });
                currentItem++;
            }
        }

        private void Add(string category, string name, Color[] palette = null, bool skipNormalMap = false, int addBorder = 0)
        {
            data.Add(Pair.Create(currentSet, currentItem), new Data {
                Category = category,
                Name = name,
                Palette = palette,
                SkipNormalMap = skipNormalMap,
                AddBorder = addBorder
            });
            currentItem++;
        }

        public Data Get(int set, int item)
        {
            Data result;
            if (!data.TryGetValue(Pair.Create(set, item), out result)) {
                result = new Data();
            }
            return result;
        }
    }
}