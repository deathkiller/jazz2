using System.Collections.Generic;
using System.Drawing;
using Import;

namespace Jazz2.Compatibility
{
    public class AnimSetMapping
    {
        public struct Entry
        {
            public string Category;
            public string Name;

            public Color[] Palette;
            public bool SkipNormalMap;
            public int AddBorder;
            public bool AllowRealtimePalette;
        }

        public const string Discard = ":discard";

        public static AnimSetMapping GetAnimMapping(JJ2Version version)
        {
            AnimSetMapping m = new AnimSetMapping(version);

            if (version == JJ2Version.PlusExtension) {
                m.SkipItems(5); // Unimplemented weapon
                m.Add("Pickup", "fast_fire_lori");
                m.Add("UI", "blaster_upgraded_lori");

                m.NextSet();
                m.DiscardItems(4); // Beta version sprites

                m.NextSet();
                m.Add("Object", "crate_ammo_pepper");
                m.Add("Object", "crate_ammo_electro");
                m.Add("Object", "powerup_shield_laser");
                m.Add("Object", "powerup_unknown");
                m.Add("Object", "powerup_empty");
                m.Add("Object", "powerup_upgrade_blaster_lori");
                m.Add("Common", "SugarRushStars");
                m.SkipItems(); // Carrotade

                m.NextSet(); // 3
                m.DiscardItems(3); // Lori's continue animations

                m.NextSet(); // 4

                m.Add("UI", "font_medium");
                m.Add("UI", "font_small");
                m.Add("UI", "font_large");

                //mapping.Add("UI", "logo_plus", skipNormalMap: true);
                m.DiscardItems(1);

                m.NextSet(); // 5
                m.Add("Object", "powerup_swap_characters_lori");

                //mapping.Add("UI", "logo_plus_large", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add("UI", "logo_plus_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(2);

                m.NextSet(); // 6
                m.DiscardItems(5); // Reticles

            } else if (version != JJ2Version.Unknown) {
                bool isFull = (version & JJ2Version.SharewareDemo) == 0;

                // set 0 (all)
                m.Add("Unknown", "flame_blue");
                m.Add("Common", "Bomb");
                m.Add("Common", "smoke_poof");
                m.Add("Common", "explosion_rf");
                m.Add("Common", "explosion_small");
                m.Add("Common", "explosion_large");
                m.Add("Common", "smoke_circling_gray");
                m.Add("Common", "smoke_circling_brown");
                m.Add("Unknown", "bubble");

                //mapping.Add("Unknown", "brown_thing");
                m.DiscardItems(1);

                m.Add("Common", "explosion_pepper");

                //mapping.Add("Unknown", "bullet_maybe_electro");
                m.Add("Weapon", "bullet_maybe_electro");
                //mapping.Add("Unknown", "bullet_maybe_electro_trail");
                m.Add("Weapon", "bullet_maybe_electro_trail");

                m.Add("Unknown", "flame_red");
                m.Add("Weapon", "bullet_shield_fireball");
                m.Add("Unknown", "flare_diag_downleft");
                m.Add("Unknown", "flare_hor");
                m.Add("Weapon", "bullet_blaster");
                m.Add("UI", "blaster_upgraded_jazz");
                m.Add("UI", "blaster_upgraded_spaz");
                m.Add("Weapon", "bullet_blaster_upgraded");

                //mapping.Add("Weapon", "bullet_blaster_upgraded_ver");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_blaster_ver");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_bouncer");
                m.Add("Pickup", "ammo_bouncer_upgraded");
                m.Add("Pickup", "ammo_bouncer");
                m.Add("Weapon", "bullet_bouncer_upgraded");
                m.Add("Weapon", "bullet_freezer_hor");
                m.Add("Pickup", "ammo_freezer_upgraded");
                m.Add("Pickup", "ammo_freezer");
                m.Add("Weapon", "bullet_freezer_upgraded_hor");

                //mapping.Add("Weapon", "bullet_freezer_ver");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_freezer_upgraded_ver");
                m.DiscardItems(1);

                m.Add("Pickup", "ammo_seeker_upgraded");
                m.Add("Pickup", "ammo_seeker");

                //mapping.Add("Weapon", "bullet_seeker_ver_down");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_diag_downright");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_seeker_hor");

                //mapping.Add("Weapon", "bullet_seeker_ver_up");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_diag_upright");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_ver_down");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_diag_downright");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_seeker_upgraded_hor");

                //mapping.Add("Weapon", "bullet_seeker_upgraded_ver_up");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_seeker_upgraded_diag_upright");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_rf_hor");

                //mapping.Add("Weapon", "bullet_rf_diag_downright");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_upgraded_diag_downright");
                m.DiscardItems(1);

                m.Add("Pickup", "ammo_rf_upgraded");
                m.Add("Pickup", "ammo_rf");
                m.Add("Weapon", "bullet_rf_upgraded_hor");

                //mapping.Add("Weapon", "bullet_rf_upgraded_ver");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_upgraded_diag_upright");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_ver");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_rf_diag_upright");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_toaster");
                m.Add("Pickup", "ammo_toaster_upgraded");
                m.Add("Pickup", "ammo_toaster");
                m.Add("Weapon", "bullet_toaster_upgraded");
                m.Add("Weapon", "bullet_tnt");
                m.Add("Weapon", "bullet_fireball_hor");
                m.Add("Pickup", "ammo_pepper_upgraded");
                m.Add("Pickup", "ammo_pepper");
                m.Add("Weapon", "bullet_fireball_upgraded_hor");

                //mapping.Add("Weapon", "bullet_fireball_ver");
                m.DiscardItems(1);
                //mapping.Add("Weapon", "bullet_fireball_upgraded_ver");
                m.DiscardItems(1);

                m.Add("Weapon", "bullet_bladegun");
                m.Add("Pickup", "ammo_electro_upgraded");
                m.Add("Pickup", "ammo_electro");
                m.Add("Weapon", "bullet_bladegun_upgraded");
                m.Add("Common", "explosion_tiny");
                m.Add("Common", "explosion_freezer_maybe");
                m.Add("Common", "explosion_tiny_black");
                m.Add("Weapon", "bullet_fireball_upgraded_hor_2");
                m.Add("Unknown", "flare_hor_2");
                m.Add("Unknown", "green_explosion");
                m.Add("Weapon", "bullet_bladegun_alt");
                m.Add("Weapon", "bullet_tnt_explosion");
                m.Add("Object", "container_ammo_shrapnel_1");
                m.Add("Object", "container_ammo_shrapnel_2");
                m.Add("Common", "explosion_upwards");
                m.Add("Common", "explosion_bomb");
                m.Add("Common", "smoke_circling_white");

                if (isFull) {
                    m.NextSet();
                    m.Add("Bat", "idle");
                    m.Add("Bat", "resting");
                    m.Add("Bat", "takeoff_1");
                    m.Add("Bat", "takeoff_2");
                    m.Add("Bat", "roost");
                    m.NextSet();
                    m.Add("Bee", "swarm");
                    m.NextSet();
                    m.Add("Bee", "swarm_2");
                    m.NextSet();
                    m.Add("Object", "PushBoxCrate");
                    m.NextSet();
                    m.Add("Object", "PushBoxRock");
                    m.NextSet();

                    //mapping.Add("Unknown", "diamondus_tileset_tree");
                    m.DiscardItems(1);

                    m.NextSet();
                    m.Add("Bilsy", "throw_fireball");
                    m.Add("Bilsy", "appear");
                    m.Add("Bilsy", "vanish");
                    m.Add("Bilsy", "bullet_fireball");
                    m.Add("Bilsy", "idle");
                }

                m.NextSet();
                m.Add("Birdy", "charge_diag_downright");
                m.Add("Birdy", "charge_ver");
                m.Add("Birdy", "charge_diag_upright");
                m.Add("Birdy", "caged");
                m.Add("Birdy", "cage_destroyed");
                m.Add("Birdy", "die");
                m.Add("Birdy", "feather_green");
                m.Add("Birdy", "feather_red");
                m.Add("Birdy", "feather_green_and_red");
                m.Add("Birdy", "fly");
                m.Add("Birdy", "hurt");
                m.Add("Birdy", "idle_worm");
                m.Add("Birdy", "idle_turn_head_left");
                m.Add("Birdy", "idle_look_left");
                m.Add("Birdy", "idle_turn_head_left_back");
                m.Add("Birdy", "idle_turn_head_right");
                m.Add("Birdy", "idle_look_right");
                m.Add("Birdy", "idle_turn_head_right_back");
                m.Add("Birdy", "idle");
                m.Add("Birdy", "corpse");

                if (isFull) {
                    m.NextSet();
                    m.Add("Unimplemented", "BonusBirdy");
                    m.NextSet(); // set 10 (all)
                    m.Add("Platform", "ball");
                    m.Add("Platform", "ball_chain");
                    m.NextSet();
                    m.Add("Object", "BonusActive");
                    m.Add("Object", "BonusInactive");
                }

                m.NextSet();
                m.Add("UI", "boss_health_bar", skipNormalMap: true);
                m.NextSet();
                m.Add("Bridge", "Rope");
                m.Add("Bridge", "Stone");
                m.Add("Bridge", "Vine");
                m.Add("Bridge", "StoneRed");
                m.Add("Bridge", "Log");
                m.Add("Bridge", "Gem");
                m.Add("Bridge", "Lab");
                m.NextSet();
                m.Add("Bubba", "spew_fireball");
                m.Add("Bubba", "corpse");
                m.Add("Bubba", "jump");
                m.Add("Bubba", "jump_fall");
                m.Add("Bubba", "fireball");
                m.Add("Bubba", "hop");
                m.Add("Bubba", "tornado");
                m.Add("Bubba", "tornado_start");
                m.Add("Bubba", "tornado_end");
                m.NextSet();
                m.Add("Bee", "Bee");
                m.Add("Bee", "bee_turn");

                if (isFull) {
                    m.NextSet();
                    m.Add("Unimplemented", "butterfly");
                    m.NextSet();
                    m.Add("Pole", "Carrotus");
                    m.NextSet();
                    m.Add("Cheshire", "platform_appear");
                    m.Add("Cheshire", "platform_vanish");
                    m.Add("Cheshire", "platform_idle");
                    m.Add("Cheshire", "platform_invisible");
                }

                m.NextSet();
                m.Add("Cheshire", "hook_appear");
                m.Add("Cheshire", "hook_vanish");
                m.Add("Cheshire", "hook_idle");
                m.Add("Cheshire", "hook_invisible");

                m.NextSet(); // set 20 (all)
                m.Add("Caterpillar", "exhale_start");
                m.Add("Caterpillar", "exhale");
                m.Add("Caterpillar", "disoriented");
                m.Add("Caterpillar", "idle");
                m.Add("Caterpillar", "inhale_start");
                m.Add("Caterpillar", "inhale");
                m.Add("Caterpillar", "smoke");

                if (isFull) {
                    m.NextSet();
                    m.Add("BirdyYellow", "charge_diag_downright_placeholder");
                    m.Add("BirdyYellow", "charge_ver");
                    m.Add("BirdyYellow", "charge_diag_upright");
                    m.Add("BirdyYellow", "caged");
                    m.Add("BirdyYellow", "cage_destroyed");
                    m.Add("BirdyYellow", "die");
                    m.Add("BirdyYellow", "feather_blue");
                    m.Add("BirdyYellow", "feather_yellow");
                    m.Add("BirdyYellow", "feather_blue_and_yellow");
                    m.Add("BirdyYellow", "fly");
                    m.Add("BirdyYellow", "hurt");
                    m.Add("BirdyYellow", "idle_worm");
                    m.Add("BirdyYellow", "idle_turn_head_left");
                    m.Add("BirdyYellow", "idle_look_left");
                    m.Add("BirdyYellow", "idle_turn_head_left_back");
                    m.Add("BirdyYellow", "idle_turn_head_right");
                    m.Add("BirdyYellow", "idle_look_right");
                    m.Add("BirdyYellow", "idle_turn_head_right_back");
                    m.Add("BirdyYellow", "idle");
                    m.Add("BirdyYellow", "corpse");
                }

                m.NextSet();
                m.Add("Common", "water_bubble_1");
                m.Add("Common", "water_bubble_2");
                m.Add("Common", "water_bubble_3");
                m.Add("Common", "water_splash");

                m.NextSet();
                m.Add("Jazz", "gameover_continue");
                m.Add("Jazz", "gameover_idle");
                m.Add("Jazz", "gameover_end");
                m.Add("Spaz", "gameover_continue");
                m.Add("Spaz", "gameover_idle");
                m.Add("Spaz", "gameover_end");

                if (isFull) {
                    m.NextSet();
                    m.Add("Demon", "idle");
                    m.Add("Demon", "attack_start");
                    m.Add("Demon", "attack");
                    m.Add("Demon", "attack_end");
                }

                m.NextSet();
                m.DiscardItems(4); // Green rectangles
                m.Add("Common", "IceBlock");

                if (isFull) {
                    m.NextSet();
                    m.Add("Devan", "bullet_small");
                    m.Add("Devan", "remote_idle");
                    m.Add("Devan", "remote_fall_warp_out");
                    m.Add("Devan", "remote_fall");
                    m.Add("Devan", "remote_fall_rotate");
                    m.Add("Devan", "remote_fall_warp_in");
                    m.Add("Devan", "remote_warp_out");

                    m.NextSet();
                    m.Add("Devan", "demon_spew_fireball");
                    m.Add("Devan", "disoriented");
                    m.Add("Devan", "freefall");
                    m.Add("Devan", "disoriented_start");
                    m.Add("Devan", "demon_fireball");
                    m.Add("Devan", "demon_fly");
                    m.Add("Devan", "demon_transform_start");
                    m.Add("Devan", "demon_transform_end");
                    m.Add("Devan", "disarmed_idle");
                    m.Add("Devan", "demon_turn");
                    m.Add("Devan", "disarmed_warp_in");
                    m.Add("Devan", "disoriented_warp_out");
                    m.Add("Devan", "disarmed");
                    m.Add("Devan", "crouch");
                    m.Add("Devan", "shoot");
                    m.Add("Devan", "disarmed_gun");
                    m.Add("Devan", "jump");
                    m.Add("Devan", "bullet");
                    m.Add("Devan", "run");
                    m.Add("Devan", "run_end");
                    m.Add("Devan", "jump_end");
                    m.Add("Devan", "idle");
                    m.Add("Devan", "warp_in");
                    m.Add("Devan", "warp_out");
                }

                m.NextSet();
                m.Add("Pole", "Diamondus");

                if (isFull) {
                    m.NextSet();
                    m.Add("Doggy", "attack");
                    m.Add("Doggy", "walk");

                    m.NextSet(); // set 30 (all)
                    m.Add("Unimplemented", "door");
                    m.Add("Unimplemented", "door_enter_jazz_spaz");
                }

                m.NextSet();
                m.Add("Dragonfly", "idle");

                if (isFull) {
                    m.NextSet();
                    m.Add("Dragon", "attack");
                    m.Add("Dragon", "idle");
                    m.Add("Dragon", "turn");

                    m.NextSet(1, JJ2Version.BaseGame | JJ2Version.HH);
                    m.NextSet(2, JJ2Version.TSF | JJ2Version.CC);
                }

                m.NextSet(4);
                m.Add("Eva", "Blink");
                m.Add("Eva", "Idle");
                m.Add("Eva", "KissStart");
                m.Add("Eva", "KissEnd");

                m.NextSet();
                m.Add("UI", "icon_birdy");
                m.Add("UI", "icon_birdy_yellow");
                m.Add("UI", "icon_frog");
                m.Add("UI", "icon_jazz");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "icon_lori");
                m.Add("UI", "icon_spaz");

                if (isFull) {
                    m.NextSet(2); // set 41 (1.24) / set 40 (1.23)
                    m.Add("FatChick", "attack");
                    m.Add("FatChick", "walk");
                    m.NextSet();
                    m.Add("Fencer", "attack");
                    m.Add("Fencer", "idle");
                    m.NextSet();
                    m.Add("Fish", "attack");
                    m.Add("Fish", "idle");
                }

                m.NextSet();
                m.Add("CTF", "arrow");
                m.Add("CTF", "base");
                m.Add("CTF", "lights");
                m.Add("CTF", "flag_blue");
                m.Add("UI", "ctf_flag_blue");
                m.Add("CTF", "base_eva");
                m.Add("CTF", "base_eva_cheer");
                m.Add("CTF", "flag_red");
                m.Add("UI", "ctf_flag_red");

                if (isFull) {
                    m.NextSet();
                    m.DiscardItems(1); // Strange green circles
                }

                m.NextSet();
                m.Add("UI", "font_medium");
                m.Add("UI", "font_small");
                m.Add("UI", "font_large");

                //mapping.Add("UI", "logo", skipNormalMap: true);
                m.DiscardItems(1);
                //mapping.Add(JJ2Version.CC, "UI", "cc_logo");
                m.DiscardItems(1, JJ2Version.CC);

                m.NextSet();
                m.Add("Frog", "fall_land");
                m.Add("Frog", "hurt");
                m.Add("Frog", "idle");
                m.Add("Jazz", "transform_frog");
                m.Add("Frog", "fall");
                m.Add("Frog", "jump_start");
                m.Add("Frog", "crouch");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "transform_frog");
                m.Add("Frog", "tongue_diag_upright");
                m.Add("Frog", "tongue_hor");
                m.Add("Frog", "tongue_ver");
                m.Add("Spaz", "transform_frog");
                m.Add("Frog", "run");

                if (isFull) {
                    m.NextSet();
                    m.Add("Platform", "carrotus_fruit");
                    m.Add("Platform", "carrotus_fruit_chain");
                    m.NextSet();
                    //mapping.Add("Pickup", "gem_gemring", keepIndexed: true);
                    m.DiscardItems(1);
                    m.NextSet(); // set 50 (1.24) / set 49 (1.23)
                    m.Add("Unimplemented", "boxing_glove_stiff");
                    m.Add("Unimplemented", "boxing_glove_stiff_idle");
                    m.Add("Unimplemented", "boxing_glove_normal");
                    m.Add("Unimplemented", "boxing_glove_normal_idle");
                    m.Add("Unimplemented", "boxing_glove_relaxed");
                    m.Add("Unimplemented", "boxing_glove_relaxed_idle");

                    m.NextSet();
                    m.Add("Platform", "carrotus_grass");
                    m.Add("Platform", "carrotus_grass_chain");
                }

                m.NextSet();
                m.Add("MadderHatter", "cup");
                m.Add("MadderHatter", "hat");
                m.Add("MadderHatter", "attack");
                m.Add("MadderHatter", "bullet_spit");
                m.Add("MadderHatter", "walk");

                if (isFull) {
                    m.NextSet();
                    m.Add("Helmut", "idle");
                    m.Add("Helmut", "walk");
                }

                m.NextSet(2);
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unknown_disoriented");
                m.Add("Jazz", "airboard");
                m.Add("Jazz", "airboard_turn");
                m.Add("Jazz", "buttstomp_end");
                m.Add("Jazz", "corpse");
                m.Add("Jazz", "die");
                m.Add("Jazz", "crouch_start");
                m.Add("Jazz", "crouch");
                m.Add("Jazz", "crouch_shoot");
                m.Add("Jazz", "crouch_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_door_enter");
                m.Add("Jazz", "vine_walk");
                m.Add("Jazz", "eol");
                m.Add("Jazz", "fall");
                m.Add("Jazz", "buttstomp");
                m.Add("Jazz", "fall_end");
                m.Add("Jazz", "shoot");
                m.Add("Jazz", "shoot_ver");
                m.Add("Jazz", "shoot_end");
                m.Add("Jazz", "transform_frog_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_ledge_climb");
                m.Add("Jazz", "vine_shoot_start");
                m.Add("Jazz", "vine_shoot_up_end");
                m.Add("Jazz", "vine_shoot_up");
                m.Add("Jazz", "vine_idle");
                m.Add("Jazz", "vine_idle_flavor");
                m.Add("Jazz", "vine_shoot_end");
                m.Add("Jazz", "vine_shoot");
                m.Add("Jazz", "copter");
                m.Add("Jazz", "copter_shoot_start");
                m.Add("Jazz", "copter_shoot");
                m.Add("Jazz", "pole_h");
                m.Add("Jazz", "hurt");
                m.Add("Jazz", "idle_flavor_1");
                m.Add("Jazz", "idle_flavor_2");
                m.Add("Jazz", "idle_flavor_3");
                m.Add("Jazz", "idle_flavor_4");
                m.Add("Jazz", "idle_flavor_5");
                m.Add("Jazz", "vine_shoot_up_start");
                m.Add("Jazz", "fall_shoot");
                m.Add("Jazz", "jump_unknown_1");
                m.Add("Jazz", "jump_unknown_2");
                m.Add("Jazz", "jump");
                m.Add("Jazz", "ledge");
                m.Add("Jazz", "lift");
                m.Add("Jazz", "lift_jump_light");
                m.Add("Jazz", "lift_jump_heavy");
                m.Add("Jazz", "lookup_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_upright");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_ver_up");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_upleft_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_downleft_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_ver_down");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_diag_downright");
                m.Add("Jazz", "dizzy_walk");
                m.Add("Jazz", "push");
                m.Add("Jazz", "shoot_start");
                m.Add("Jazz", "revup_start");
                m.Add("Jazz", "revup");
                m.Add("Jazz", "revup_end");
                m.Add("Jazz", "fall_diag");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unknown_mid_frame");
                m.Add("Jazz", "jump_diag");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_ver_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_jump_shoot_ver");
                m.Add("Jazz", "ball");
                m.Add("Jazz", "run");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_run_aim_diag");
                m.Add("Jazz", "dash_start");
                m.Add("Jazz", "dash");
                m.Add("Jazz", "dash_stop");
                m.Add("Jazz", "walk_stop");
                m.Add("Jazz", "run_stop");
                m.Add("Jazz", "Spring");
                m.Add("Jazz", "idle");
                m.Add("Jazz", "uppercut");
                m.Add("Jazz", "uppercut_end");
                m.Add("Jazz", "uppercut_start");
                m.Add("Jazz", "dizzy");
                m.Add("Jazz", "swim_diag_downright");
                m.Add("Jazz", "swim_right");
                m.Add("Jazz", "swim_diag_right_to_downright");
                m.Add("Jazz", "swim_diag_right_to_upright");
                m.Add("Jazz", "swim_diag_upright");
                m.Add("Jazz", "swing");
                m.Add("Jazz", "warp_in");
                m.Add("Jazz", "warp_out_freefall");
                m.Add("Jazz", "freefall");
                m.Add("Jazz", "warp_in_freefall");
                m.Add("Jazz", "warp_out");
                m.Add("Jazz", "pole_v");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_fall");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_hurt");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_idle");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_jump");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_crouch_end_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_lookup_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_run");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_unarmed_stare");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Jazz", "unused_lookup_start_2");

                m.NextSet();
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_idle_flavor_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_jump_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_dash_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_rotate_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_ball_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_run_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Unimplemented", "bonus_jazz_idle_2");
                m.Add("Unimplemented", "bonus_jazz_idle_flavor");
                m.Add("Unimplemented", "bonus_jazz_jump");
                m.Add("Unimplemented", "bonus_jazz_ball");
                m.Add("Unimplemented", "bonus_jazz_run");
                m.Add("Unimplemented", "bonus_jazz_dash");
                m.Add("Unimplemented", "bonus_jazz_rotate");
                m.Add("Unimplemented", "bonus_jazz_idle");

                if (isFull) {
                    m.NextSet(2);
                    m.Add("Pole", "Jungle");
                }

                m.NextSet();
                m.Add("LabRat", "attack");
                m.Add("LabRat", "idle");
                m.Add("LabRat", "walk");

                m.NextSet(); // set 60 (1.24) / set 59 (1.23)
                m.Add("Lizard", "copter_attack");
                m.Add("Lizard", "bomb");
                m.Add("Lizard", "copter_idle");
                m.Add("Lizard", "copter");
                m.Add("Lizard", "walk");

                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "airboard");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "airboard_turn");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "buttstomp_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "corpse");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "die");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_shoot");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "crouch_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_walk");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "eol");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "buttstomp");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_ver");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "transform_frog_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_idle");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_idle_flavor");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter_shoot_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "copter_shoot");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "pole_h");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "hurt");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_1");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_2");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_3");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_4");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_flavor_5");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "vine_shoot_up_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_shoot");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_unknown_1");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_unknown_2");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "ledge");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift_jump_light");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lift_jump_heavy");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "lookup_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dizzy_walk");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "push");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "shoot_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "revup_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "fall_diag");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "jump_diag");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "ball");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "run");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dash_stop");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "walk_stop");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "run_stop");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "Spring");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "uppercut_placeholder_1");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "uppercut_placeholder_2");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "sidekick");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "dizzy");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_downright");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_right");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_right_to_downright");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_right_to_upright");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swim_diag_upright");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "swing");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_in");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_out_freefall");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "freefall");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_in_freefall");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "warp_out");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "pole_v");
                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "idle_2");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Lori", "gun");

                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                m.NextSet();
                //mapping.Add("UI", "multiplayer_char", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(1);

                //mapping.Add("UI", "multiplayer_color", JJ2DefaultPalette.Menu);
                m.DiscardItems(1);

                m.Add("UI", "character_art_difficulty_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_art_difficulty_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.Add("UI", "character_art_difficulty_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.Add("Unimplemented", "key", JJ2DefaultPalette.Menu, skipNormalMap: true);

                //mapping.Add("UI", "loading_bar", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(1);

                m.Add("UI", "multiplayer_mode", JJ2DefaultPalette.Menu, skipNormalMap: true);

                //mapping.Add("UI", "character_name_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(1);
                //mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_name_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(1, JJ2Version.TSF | JJ2Version.CC);
                //mapping.Add("UI", "character_name_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(1);

                m.Add("UI", "character_art_jazz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "character_art_lori", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.Add("UI", "character_art_spaz", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.NextSet();

                //mapping.Add("UI", "font_medium_2", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "font_small_2", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "logo_large", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.TSF | JJ2Version.CC, "UI", "tsf_title", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "menu_snow", JJ2DefaultPalette.Menu);
                //mapping.Add("UI", "logo_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "cc_title", JJ2DefaultPalette.Menu, skipNormalMap: true);
                //mapping.Add(JJ2Version.CC, "UI", "cc_title_small", JJ2DefaultPalette.Menu, skipNormalMap: true);
                m.DiscardItems(8);

                if (isFull) {
                    m.NextSet(2);
                    m.Add("Monkey", "Banana");
                    m.Add("Monkey", "BananaSplat");
                    m.Add("Monkey", "Jump");
                    m.Add("Monkey", "WalkStart");
                    m.Add("Monkey", "WalkEnd");
                    m.Add("Monkey", "Attack");
                    m.Add("Monkey", "Walk");

                    m.NextSet();
                    m.Add("Moth", "Green");
                    m.Add("Moth", "Gray");
                    m.Add("Moth", "Purple");
                    m.Add("Moth", "Pink");
                } else {
                    m.NextSet();
                }

                m.NextSet(3); // set 71 (1.24) / set 67 (1.23)
                m.Add("Pickup", "1up");
                m.Add("Pickup", "food_apple");
                m.Add("Pickup", "food_banana");
                m.Add("Object", "container_barrel");
                m.Add("Common", "poof_brown");
                m.Add("Object", "container_box_crush");
                m.Add("Object", "container_barrel_shrapnel_1");
                m.Add("Object", "container_barrel_shrapnel_2");
                m.Add("Object", "container_barrel_shrapnel_3");
                m.Add("Object", "container_barrel_shrapnel_4");
                m.Add("Object", "powerup_shield_bubble");
                m.Add("Pickup", "food_burger");
                m.Add("Pickup", "food_cake");
                m.Add("Pickup", "food_candy");
                m.Add("Object", "Savepoint");
                m.Add("Pickup", "food_cheese");
                m.Add("Pickup", "food_cherry");
                m.Add("Pickup", "food_chicken");
                m.Add("Pickup", "food_chips");
                m.Add("Pickup", "food_chocolate");
                m.Add("Pickup", "food_cola");
                m.Add("Pickup", "carrot");
                m.Add("Pickup", "Gem", allowRealtimePalette: true, addBorder: 1);
                m.Add("Pickup", "food_cucumber");
                m.Add("Pickup", "food_cupcake");
                m.Add("Pickup", "food_donut");
                m.Add("Pickup", "food_eggplant");
                m.Add("Unknown", "green_blast_thing");
                m.Add("Object", "ExitSign");
                m.Add("Pickup", "fast_fire_jazz");
                m.Add("Pickup", "fast_fire_spaz");
                m.Add("Object", "powerup_shield_fire");
                m.Add("Pickup", "food_fries");
                m.Add("Pickup", "fast_feet");
                m.Add("Object", "GemSuper", allowRealtimePalette: true);

                //mapping.Add("Pickup", "Gem2", keepIndexed: true);
                m.DiscardItems(1);

                m.Add("Pickup", "airboard");
                m.Add("Pickup", "coin_gold");
                m.Add("Pickup", "food_grapes");
                m.Add("Pickup", "food_ham");
                m.Add("Pickup", "carrot_fly");
                m.Add("UI", "heart", skipNormalMap: true);
                m.Add("Pickup", "freeze_enemies");
                m.Add("Pickup", "food_ice_cream");
                m.Add("Common", "ice_break_shrapnel_1");
                m.Add("Common", "ice_break_shrapnel_2");
                m.Add("Common", "ice_break_shrapnel_3");
                m.Add("Common", "ice_break_shrapnel_4");
                m.Add("Pickup", "food_lemon");
                m.Add("Pickup", "food_lettuce");
                m.Add("Pickup", "food_lime");
                m.Add("Object", "powerup_shield_lightning");
                m.Add("Object", "TriggerCrate");
                m.Add("Pickup", "food_milk");
                m.Add("Object", "crate_ammo_bouncer");
                m.Add("Object", "crate_ammo_freezer");
                m.Add("Object", "crate_ammo_seeker");
                m.Add("Object", "crate_ammo_rf");
                m.Add("Object", "crate_ammo_toaster");
                m.Add("Object", "crate_ammo_tnt");
                m.Add("Object", "powerup_upgrade_blaster_jazz");
                m.Add("Object", "powerup_upgrade_bouncer");
                m.Add("Object", "powerup_upgrade_freezer");
                m.Add("Object", "powerup_upgrade_seeker");
                m.Add("Object", "powerup_upgrade_rf");
                m.Add("Object", "powerup_upgrade_toaster");
                m.Add("Object", "powerup_upgrade_pepper");
                m.Add("Object", "powerup_upgrade_electro");
                m.Add("Object", "powerup_transform_birdy");
                m.Add("Object", "powerup_transform_birdy_yellow");
                m.Add("Object", "powerup_swap_characters");
                m.Add("Pickup", "food_orange");
                m.Add("Pickup", "carrot_invincibility");
                m.Add("Pickup", "food_peach");
                m.Add("Pickup", "food_pear");
                m.Add("Pickup", "food_soda");
                m.Add("Pickup", "food_pie");
                m.Add("Pickup", "food_pizza");
                m.Add("Pickup", "potion");
                m.Add("Pickup", "food_pretzel");
                m.Add("Pickup", "food_sandwich");
                m.Add("Pickup", "food_strawberry");
                m.Add("Pickup", "carrot_full");
                m.Add("Object", "powerup_upgrade_blaster_spaz");
                m.Add("Pickup", "coin_silver");
                m.Add("Unknown", "green_blast_thing_2");
                m.Add("Common", "generator");
                m.Add("Pickup", "stopwatch");
                m.Add("Pickup", "food_taco");
                m.Add("Pickup", "food_thing");
                m.Add("Object", "tnt");
                m.Add("Pickup", "food_hotdog");
                m.Add("Pickup", "food_watermelon");
                m.Add("Object", "container_crate_shrapnel_1");
                m.Add("Object", "container_crate_shrapnel_2");

                if (isFull) {
                    m.NextSet();
                    m.Add("Pinball", "Bumper500");
                    m.Add("Pinball", "Bumper500Hit");
                    m.Add("Pinball", "BumperCarrot");
                    m.Add("Pinball", "BumperCarrotHit");

                    m.Add("Pinball", "PaddleLeft", addBorder: 1);
                    //mapping.Add("Pinball", "PaddleRight", JJ2DefaultPalette.ByIndex);
                    m.DiscardItems(1);

                    m.NextSet();
                    m.Add("Platform", "lab");
                    m.Add("Platform", "lab_chain");

                    m.NextSet();
                    m.Add("Pole", "Psych");

                    m.NextSet();
                    m.Add("Queen", "scream");
                    m.Add("Queen", "ledge");
                    m.Add("Queen", "ledge_recover");
                    m.Add("Queen", "idle");
                    m.Add("Queen", "brick");
                    m.Add("Queen", "fall");
                    m.Add("Queen", "stomp");
                    m.Add("Queen", "backstep");

                    m.NextSet();
                    m.Add("Rapier", "attack");
                    m.Add("Rapier", "attack_swing");
                    m.Add("Rapier", "idle");
                    m.Add("Rapier", "attack_start");
                    m.Add("Rapier", "attack_end");

                    m.NextSet();
                    m.Add("Raven", "Attack");
                    m.Add("Raven", "Idle");
                    m.Add("Raven", "Turn");

                    m.NextSet();
                    m.Add("Robot", "spike_ball");
                    m.Add("Robot", "attack_start");
                    m.Add("Robot", "attack");
                    m.Add("Robot", "copter");
                    m.Add("Robot", "idle");
                    m.Add("Robot", "attack_end");
                    m.Add("Robot", "shrapnel_1");
                    m.Add("Robot", "shrapnel_2");
                    m.Add("Robot", "shrapnel_3");
                    m.Add("Robot", "shrapnel_4");
                    m.Add("Robot", "shrapnel_5");
                    m.Add("Robot", "shrapnel_6");
                    m.Add("Robot", "shrapnel_7");
                    m.Add("Robot", "shrapnel_8");
                    m.Add("Robot", "shrapnel_9");
                    m.Add("Robot", "run");
                    m.Add("Robot", "copter_start");
                    m.Add("Robot", "copter_end");

                    m.NextSet();
                    m.Add("Object", "rolling_rock");

                    m.NextSet(); // set 80 (1.24) / set 76 (1.23)
                    m.Add("TurtleRocket", "downright");
                    m.Add("TurtleRocket", "upright");
                    m.Add("TurtleRocket", "smoke");
                    m.Add("TurtleRocket", "upright_to_downright");

                    m.NextSet(3);
                    m.Add("Skeleton", "Bone");
                    m.Add("Skeleton", "Skull");
                    m.Add("Skeleton", "Walk");
                } else {
                    m.NextSet();
                }

                m.NextSet();
                m.Add("Pole", "DiamondusTree");

                if (isFull) {
                    m.NextSet();
                    m.Add("Common", "Snow", JJ2DefaultPalette.Snow);

                    m.NextSet();
                    m.Add("Bolly", "rocket");
                    m.Add("Bolly", "mace_chain");
                    m.Add("Bolly", "bottom");
                    m.Add("Bolly", "top");
                    m.Add("Bolly", "puff");
                    m.Add("Bolly", "mace");
                    m.Add("Bolly", "turret");
                    m.Add("Bolly", "crosshairs");
                    m.NextSet();
                    m.Add("Platform", "sonic");
                    m.Add("Platform", "sonic_chain");
                    m.NextSet();
                    m.Add("Sparks", "idle");
                }

                m.NextSet();
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unknown_disoriented");
                m.Add("Spaz", "airboard");
                m.Add("Spaz", "airboard_turn");
                m.Add("Spaz", "buttstomp_end");
                m.Add("Spaz", "corpse");
                m.Add("Spaz", "die");
                m.Add("Spaz", "crouch_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "crouch_shoot_2");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Spaz", "crouch");
                m.Add("Spaz", "crouch_shoot");
                m.Add("Spaz", "crouch_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_door_enter");
                m.Add("Spaz", "vine_walk");
                m.Add("Spaz", "eol");
                m.Add("Spaz", "fall");
                m.Add("Spaz", "buttstomp");
                m.Add("Spaz", "fall_end");
                m.Add("Spaz", "shoot");
                m.Add("Spaz", "shoot_ver");
                m.Add("Spaz", "shoot_end");
                m.Add("Spaz", "transform_frog_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_ledge_climb");
                m.Add("Spaz", "vine_shoot_start");
                m.Add("Spaz", "vine_shoot_up_end");
                m.Add("Spaz", "vine_shoot_up");
                m.Add("Spaz", "vine_idle");
                m.Add("Spaz", "vine_idle_flavor");
                m.Add("Spaz", "vine_shoot_end");
                m.Add("Spaz", "vine_shoot");
                m.Add("Spaz", "copter");
                m.Add("Spaz", "copter_shoot_start");
                m.Add("Spaz", "copter_shoot");
                m.Add("Spaz", "pole_h");
                m.Add("Spaz", "hurt");
                m.Add("Spaz", "idle_flavor_1");
                m.Add("Spaz", "idle_flavor_2");
                m.Add("Spaz", "idle_flavor_3_placeholder");
                m.Add("Spaz", "idle_flavor_4");
                m.Add("Spaz", "idle_flavor_5");
                m.Add("Spaz", "vine_shoot_up_start");
                m.Add("Spaz", "fall_shoot");
                m.Add("Spaz", "jump_unknown_1");
                m.Add("Spaz", "jump_unknown_2");
                m.Add("Spaz", "jump");
                m.Add("Spaz", "ledge");
                m.Add("Spaz", "lift");
                m.Add("Spaz", "lift_jump_light");
                m.Add("Spaz", "lift_jump_heavy");
                m.Add("Spaz", "lookup_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_upright");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_ver_up");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_upleft_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_downleft_reverse");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_ver_down");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_diag_downright");
                m.Add("Spaz", "dizzy_walk");
                m.Add("Spaz", "push");
                m.Add("Spaz", "shoot_start");
                m.Add("Spaz", "revup_start");
                m.Add("Spaz", "revup");
                m.Add("Spaz", "revup_end");
                m.Add("Spaz", "fall_diag");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unknown_mid_frame");
                m.Add("Spaz", "jump_diag");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_ver_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_jump_shoot_ver");
                m.Add("Spaz", "ball");
                m.Add("Spaz", "run");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_run_aim_diag");
                m.Add("Spaz", "dash_start");
                m.Add("Spaz", "dash");
                m.Add("Spaz", "dash_stop");
                m.Add("Spaz", "walk_stop");
                m.Add("Spaz", "run_stop");
                m.Add("Spaz", "Spring");
                m.Add("Spaz", "idle");
                m.Add("Spaz", "sidekick");
                m.Add("Spaz", "sidekick_end");
                m.Add("Spaz", "sidekick_start");
                m.Add("Spaz", "dizzy");
                m.Add("Spaz", "swim_diag_downright");
                m.Add("Spaz", "swim_right");
                m.Add("Spaz", "swim_diag_right_to_downright");
                m.Add("Spaz", "swim_diag_right_to_upright");
                m.Add("Spaz", "swim_diag_upright");
                m.Add("Spaz", "swing");
                m.Add("Spaz", "warp_in");
                m.Add("Spaz", "warp_out_freefall");
                m.Add("Spaz", "freefall");
                m.Add("Spaz", "warp_in_freefall");
                m.Add("Spaz", "warp_out");
                m.Add("Spaz", "pole_v");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_end");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_fall");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_hurt");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_idle");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_jump");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_crouch_end_2");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_lookup_start");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_run");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_unarmed_stare");
                m.Add(JJ2Version.BaseGame | JJ2Version.HH, "Spaz", "unused_lookup_start_2");
                m.NextSet(); // set 90 (1.24) / set 86 (1.23)
                m.Add("Spaz", "idle_flavor_3_start");
                m.Add("Spaz", "idle_flavor_3");
                m.Add("Spaz", "idle_flavor_3_bird");
                m.Add("Spaz", "idle_flavor_5_spaceship");

                if (isFull) {
                    m.NextSet();
                    m.Add("Unimplemented", "bonus_spaz_idle_flavor");
                    m.Add("Unimplemented", "bonus_spaz_jump");
                    m.Add("Unimplemented", "bonus_spaz_ball");
                    m.Add("Unimplemented", "bonus_spaz_run");
                    m.Add("Unimplemented", "bonus_spaz_dash");
                    m.Add("Unimplemented", "bonus_spaz_rotate");
                    m.Add("Unimplemented", "bonus_spaz_idle");

                    m.NextSet(2);
                    m.Add("Object", "3d_spike");
                    m.Add("Object", "3d_spike_chain");

                    m.NextSet();
                    //mapping.Add("Object", "3d_spike_2");
                    //mapping.Add("Object", "3d_spike_2_chain");
                    m.DiscardItems(2);

                    m.NextSet();
                    m.Add("Platform", "spike");
                    m.Add("Platform", "spike_chain");
                } else {
                    m.NextSet();
                }

                m.NextSet();
                m.Add("Spring", "spring_blue_ver");
                m.Add("Spring", "spring_blue_hor");
                m.Add("Spring", "spring_blue_ver_reverse");
                m.Add("Spring", "spring_green_ver_reverse");
                m.Add("Spring", "spring_red_ver_reverse");
                m.Add("Spring", "spring_green_ver");
                m.Add("Spring", "spring_green_hor");
                m.Add("Spring", "spring_red_ver");
                m.Add("Spring", "spring_red_hor");

                m.NextSet();
                m.Add("Common", "SteamNote");

                if (isFull) {
                    m.NextSet();
                }

                m.NextSet();
                m.Add("Sucker", "walk_top");
                m.Add("Sucker", "inflated_deflate");
                m.Add("Sucker", "walk_ver_down");
                m.Add("Sucker", "fall");
                m.Add("Sucker", "inflated");
                m.Add("Sucker", "poof");
                m.Add("Sucker", "walk");
                m.Add("Sucker", "walk_ver_up");

                if (isFull) {
                    m.NextSet(); // set 100 (1.24) / set 96 (1.23)
                    m.Add("TurtleTube", "Idle");

                    m.NextSet();
                    m.Add("TurtleToughBoss", "attack_start");
                    m.Add("TurtleToughBoss", "attack_end");
                    m.Add("TurtleToughBoss", "shell");
                    m.Add("TurtleToughBoss", "mace");
                    m.Add("TurtleToughBoss", "idle");
                    m.Add("TurtleToughBoss", "walk");

                    m.NextSet();
                    m.Add("TurtleTough", "Walk");
                }

                m.NextSet();
                m.Add("Turtle", "attack");
                m.Add("Turtle", "idle_flavor");
                m.Add("Turtle", "turn_start");
                m.Add("Turtle", "turn_end");
                m.Add("Turtle", "shell_reverse");
                m.Add("Turtle", "shell");
                m.Add("Turtle", "idle");
                m.Add("Turtle", "walk");

                if (isFull) {
                    m.NextSet();
                    m.Add("Tweedle", "magnet_start");
                    m.Add("Tweedle", "spin");
                    m.Add("Tweedle", "magnet_end");
                    m.Add("Tweedle", "shoot_jazz");
                    m.Add("Tweedle", "shoot_spaz");
                    m.Add("Tweedle", "hurt");
                    m.Add("Tweedle", "idle");
                    m.Add("Tweedle", "magnet");
                    m.Add("Tweedle", "walk");

                    m.NextSet();
                    m.Add("Uterus", "closed_start");
                    m.Add("Crab", "fall");
                    m.Add("Uterus", "closed_idle");
                    m.Add("Uterus", "idle");
                    m.Add("Crab", "fall_end");
                    m.Add("Uterus", "closed_end");
                    m.Add("Uterus", "shield");
                    m.Add("Crab", "walk");

                    m.NextSet();
                    m.DiscardItems(1); // Red dot

                    m.Add("Object", "vine");
                    m.NextSet();
                    m.Add("Object", "Bonus10");
                    m.NextSet();
                    m.Add("Object", "Bonus100");
                }

                m.NextSet();
                m.Add("Object", "Bonus20");

                if (isFull) {
                    m.NextSet(); // set 110 (1.24) / set 106 (1.23)
                    m.Add("Object", "Bonus50");
                }

                m.NextSet(2);
                m.Add("Witch", "attack");
                m.Add("Witch", "die");
                m.Add("Witch", "idle");
                m.Add("Witch", "bullet_magic");

                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_throw_fireball");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_appear");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_vanish");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_bullet_fireball");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Bilsy", "xmas_idle");
                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter_attack");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_bomb");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter_idle");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_copter");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Lizard", "xmas_walk");
                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH);
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_attack");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle_flavor");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_start");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_turn_end");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_shell_reverse");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_shell");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_idle");
                m.Add(JJ2Version.TSF | JJ2Version.CC | JJ2Version.HH, "Turtle", "xmas_walk");
                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Doggy", "xmas_attack");
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Doggy", "xmas_walk");
                m.NextSet(1, JJ2Version.TSF | JJ2Version.CC);
                m.Add(JJ2Version.TSF | JJ2Version.CC, "Sparks", "ghost_idle");
            }

            return m;
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


        private JJ2Version version;
        private int currentItem, currentSet;
        private Dictionary<Pair<int, int>, Entry> entries = new Dictionary<Pair<int, int>, Entry>();

        public AnimSetMapping(JJ2Version version)
        {
            this.version = version;
        }

        private void DiscardItems(int advanceBy, JJ2Version appliesTo = JJ2Version.All)
        {
            if ((version & appliesTo) != 0) {
                for (int i = 0; i < advanceBy; i++) {
                    entries.Add(Pair.Create(currentSet, currentItem), new Entry {
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

        private void Add(JJ2Version appliesTo, string category, string name, Color[] palette = null, bool skipNormalMap = false, int addBorder = 0, bool allowRealtimePalette = false) {
            if ((version & appliesTo) != 0) {
                entries.Add(Pair.Create(currentSet, currentItem), new Entry {
                    Category = category,
                    Name = name,
                    Palette = palette,
                    SkipNormalMap = skipNormalMap,
                    AddBorder = addBorder,
                    AllowRealtimePalette = allowRealtimePalette
                });
                currentItem++;
            }
        }

        private void Add(string category, string name, Color[] palette = null, bool skipNormalMap = false, int addBorder = 0, bool allowRealtimePalette = false)
        {
            entries.Add(Pair.Create(currentSet, currentItem), new Entry {
                Category = category,
                Name = name,
                Palette = palette,
                SkipNormalMap = skipNormalMap,
                AddBorder = addBorder,
                AllowRealtimePalette = allowRealtimePalette
            });
            currentItem++;
        }

        public Entry Get(int set, int item)
        {
            Entry result;
            if (!entries.TryGetValue(Pair.Create(set, item), out result)) {
                result = new Entry();
            }
            return result;
        }
    }
}