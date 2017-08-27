using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Import.Downloaders;
using Jazz2.Compatibility;
using static Jazz2.Game.ContentResolver;
using static Jazz2.Game.LevelHandler;

namespace Import
{
    internal class App
    {
        private static void Main(string[] args)
        {
            Utils.TryEnableUnicode();

            int cursorTop = Console.CursorTop;

            Console.Title = Jazz2.App.AssemblyTitle;

            if (!Utils.IsOutputRedirected) {
                ConsoleImage.RenderFromManifestResource("ConsoleImage.udl");
            }

            if (args.Length < 1) {
                OnShowHelp(cursorTop);
                return;
            }

            string targetPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string sourcePath = null;
            bool processAnims = true;
            bool processLevels = true;
            bool processMusic = true;
            bool processTilesets = true;
            bool all = false;
            bool noWait = false;
            bool check = false;
            bool keep = false;
            bool verbose = false;
            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "/skip-anims": processAnims = false; break;
                    case "/skip-levels": processLevels = false; break;
                    case "/skip-music": processMusic = false; break;
                    case "/skip-tilesets": processTilesets = false; break;
                    case "/skip-all": processAnims = processLevels = processMusic = processTilesets = false; break;

                    case "/all": all = true; break;
                    case "/no-wait": noWait = true; break;
                    case "/verbose": verbose = true; break;

                    case "/check": check = true; break;
                    case "/keep": keep = true; break;

                    default:
#if DEBUG
                        if (File.Exists(args[i]) && Path.GetExtension(args[i]) == ".png") {
                            AdaptImageToPalette(args[i], args);
                            return;
                        }
#endif

                        if (!Directory.Exists(args[i]) && File.Exists(args[i])) {
                            args[i] = Path.GetDirectoryName(args[i]);
                        }
                        if (Directory.Exists(args[i]) && File.Exists(Path.Combine(args[i], "Jazz2.exe"))) {
                            sourcePath = args[i];
                        }
                        break;
                }
            }

            if (sourcePath == null) {
                if (processAnims || processLevels || processMusic || processTilesets) {
                    Log.Write(LogType.Error, "You must specify path to Jazz Jackrabbit™ 2 game.");
                    return;
                }
            } else {
                Log.Write(LogType.Info, "Importing path \"" + sourcePath + "\"...");
                Log.PushIndent();
            }

            if (processAnims) {
                ConvertJJ2Anims(sourcePath, targetPath);
            }

            HashSet<string> usedMusic = new HashSet<string>();
            HashSet<string> usedTilesets = new HashSet<string>();
            if (processLevels) {
                ConvertJJ2Levels(sourcePath, targetPath, usedTilesets, usedMusic);

                usedMusic.Add("boss1");
                usedMusic.Add("boss2");
                usedMusic.Add("bonus2");
                usedMusic.Add("bonus3");
                usedMusic.Add("menu");
            } else {
                all = true;
            }
            if (processMusic) {
                ConvertJJ2Music(sourcePath, targetPath, all ? null : usedMusic, verbose);
            }
            if (processTilesets) {
                ConvertJJ2Tilesets(sourcePath, targetPath, all ? null : usedTilesets, verbose);
            }

            if (sourcePath != null) {
                Log.PopIndent();
            }

            OnPostImport(targetPath, verbose, !noWait, keep, check || (processAnims && processLevels && processMusic && processTilesets));
        }

        private static void OnShowHelp(int cursorTop)
        {
            string exeName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            int width = Console.BufferWidth;

            // Show version number in the right corner
            if (!Utils.IsOutputRedirected) {
                string appVersion = "v" + Jazz2.App.AssemblyVersion;

                int currentCursorTop = Console.CursorTop;
                Console.SetCursorPosition(width - appVersion.Length - 2, cursorTop + 1);
                Console.WriteLine(appVersion);
                Console.CursorTop = currentCursorTop;
            }

            // Show help
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  Run this application with following parameters:");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(new string('_', width));
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', width));
            Console.Write(("  ." + Path.DirectorySeparatorChar + exeName + " \"Path to Jazz Jackrabbit 2\"").PadRight(width));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(new string('_', width));
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("  The application will automatically import all animations, sounds, music,");
            Console.WriteLine("  tilesets, levels and episodes. It could take several minutes.");
            Console.WriteLine("  Holiday Hare '98, Christmas Chronicles and The Secret Files is supported.");

            Console.WriteLine();
            Console.WriteLine("  There are several other options:");
            Console.WriteLine();
            //Console.WriteLine("   /skip-anims     Don't convert animations and sounds (*.j2a files).");
            //Console.WriteLine("   /skip-levels    Don't convert level files (*.j2l files).");
            //Console.WriteLine("   /skip-music     Don't convert music files (*.j2b files).");
            //Console.WriteLine("   /skip-tilesets  Don't convert tileset files (*.j2t files).");
            //Console.WriteLine("   /skip-all       Don't convert anything.");
            Console.WriteLine("   /skip-anims | /skip-levels | /skip-music | /skip-tilesets | /skip-all");
            Console.WriteLine();
            Console.WriteLine("   /all            Convert all (even unused) music and tileset files.");
            Console.WriteLine("                   Otherwise only files referenced in levels will be converted.");
            Console.WriteLine("   /keep           Keep unused music, tilesets, animations and sounds.");
            Console.WriteLine("   /check          Check that all needed assets are present.");
            //Console.WriteLine("   /no-wait        Don't show (Press any key to exit) message when it's done.");

            // Show "Shareware Demo" notice
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  If you don't have any suitable version, Shareware Demo can be automatically");
            Console.WriteLine("  imported, but functionality will be slightly degraded.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Press Enter to download and import Shareware Demo now. Otherwise, press");
            Console.WriteLine("  CTRL+C or close the window.");

            Console.ReadLine();

            string targetPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (File.Exists(Path.Combine(targetPath, "Content", "Animations", "Jazz", "Idle.png"))) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  It seems that there is already other version imported. It's not");
                Console.WriteLine("  recommended to import Shareware Demo over full version of the game.");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("  Press Enter to continue. Otherwise, press CTRL+C or close the window.");

                Console.ReadLine();
            }

            // Clear console window and show logo
            if (!Utils.IsOutputRedirected) {
                Console.Clear();
                ConsoleImage.RenderFromManifestResource("ConsoleImage.udl");
            }

            // Download and import...
            DemoDownloader.Run(targetPath);

            OnPostImport(targetPath, false, true, false, false);
        }

        private static void OnPostImport(string targetPath, bool verbose, bool wait, bool keep, bool check)
        {
            if (!keep) {
                Clean(targetPath, verbose);
            }
            if (check) {
                CheckMissingFiles(targetPath);
            }

            bool isAnyMissing = false;
            if (!Directory.Exists(Path.Combine(targetPath, "Content", "Metadata"))) {
                Log.Write(LogType.Error, "Directory \"Metadata\" is missing!");
                isAnyMissing = true;
            }

            if (!Directory.Exists(Path.Combine(targetPath, "Content", "Shaders"))) {
                Log.Write(LogType.Error, "Directory \"Shaders\" is missing!");
                isAnyMissing = true;
            }
            if (isAnyMissing) {
                Log.Write(LogType.Error, "It should be distributed with Jazz² Resurrection.");
            }

            if (wait) {
                Log.Write(LogType.Info, "Done! (Press any key to exit)");
                Console.ReadLine();
            }
        }

        public static void ConvertJJ2Anims(string sourcePath, string targetPath)
        {
            Log.Write(LogType.Info, "Importing assets...");
            Log.PushIndent();

            string animationsPath = Path.Combine(targetPath, "Content", "Animations");
            Directory.CreateDirectory(animationsPath);

            string animsPath = Path.Combine(sourcePath, "Anims.j2a");
            if (Utils.FileResolveCaseInsensitive(ref animsPath)) {
                JJ2Anims.Convert(animsPath, animationsPath, false);
            } else {
                // Try to convert Shareware Demo
                animsPath = Path.Combine(sourcePath, "AnimsSw.j2a");
                if (Utils.FileResolveCaseInsensitive(ref animsPath)) {
                    JJ2Anims.Convert(animsPath, animationsPath, false);
                }
            }

            string plusPath = Path.Combine(sourcePath, "Plus.j2a");
            if (Utils.FileResolveCaseInsensitive(ref plusPath)) {
                JJ2Anims.Convert(plusPath, animationsPath, true);
            } else {
                JJ2PlusDownloader.Run(targetPath);
            }

            Log.PopIndent();
        }

        public static void ConvertJJ2Levels(string sourcePath, string targetPath, HashSet<string> usedTilesets, HashSet<string> usedMusic)
        {
            Log.Write(LogType.Info, "Importing episodes...");
            Log.PushIndent();

            string xmasEpisodePath = Path.Combine(sourcePath, "xmas99.j2e");
            string xmasEpisodeToken = (Utils.FileResolveCaseInsensitive(ref xmasEpisodePath) ? "xmas99" : "xmas98");

            Dictionary<string, Tuple<string, string>> knownLevels = new Dictionary<string, Tuple<string, string>> {
                ["castle1"] = Tuple.Create("prince", "01"),
                ["castle1n"] = Tuple.Create("prince", "02"),
                ["carrot1"] = Tuple.Create("prince", "03"),
                ["carrot1n"] = Tuple.Create("prince", "04"),
                ["labrat1"] = Tuple.Create("prince", "05"),
                ["labrat2"] = Tuple.Create("prince", "06"),
                ["labrat3"] = Tuple.Create("prince", "bonus"),

                ["colon1"] = Tuple.Create("rescue", "01"),
                ["colon2"] = Tuple.Create("rescue", "02"),
                ["psych1"] = Tuple.Create("rescue", "03"),
                ["psych2"] = Tuple.Create("rescue", "04"),
                ["beach"] = Tuple.Create("rescue", "05"),
                ["beach2"] = Tuple.Create("rescue", "06"),
                ["psych3"] = Tuple.Create("rescue", "bonus"),

                ["diam1"] = Tuple.Create("flash", "01"),
                ["diam3"] = Tuple.Create("flash", "02"),
                ["tube1"] = Tuple.Create("flash", "03"),
                ["tube2"] = Tuple.Create("flash", "04"),
                ["medivo1"] = Tuple.Create("flash", "05"),
                ["medivo2"] = Tuple.Create("flash", "06"),
                ["garglair"] = Tuple.Create("flash", "bonus"),
                ["tube3"] = Tuple.Create("flash", "bonus"),

                ["jung1"] = Tuple.Create("monk", "01"),
                ["jung2"] = Tuple.Create("monk", "02"),
                ["hell"] = Tuple.Create("monk", "03"),
                ["hell2"] = Tuple.Create("monk", "04"),
                ["damn"] = Tuple.Create("monk", "05"),
                ["damn2"] = Tuple.Create("monk", "06"),

                ["share1"] = Tuple.Create("share", "01"),
                ["share2"] = Tuple.Create("share", "02"),
                ["share3"] = Tuple.Create("share", "03"),

                ["xmas1"] = Tuple.Create(xmasEpisodeToken, "01"),
                ["xmas2"] = Tuple.Create(xmasEpisodeToken, "02"),
                ["xmas3"] = Tuple.Create(xmasEpisodeToken, "03"),

                ["easter1"] = Tuple.Create("secretf", "01"),
                ["easter2"] = Tuple.Create("secretf", "02"),
                ["easter3"] = Tuple.Create("secretf", "03"),
                ["haunted1"] = Tuple.Create("secretf", "04"),
                ["haunted2"] = Tuple.Create("secretf", "05"),
                ["haunted3"] = Tuple.Create("secretf", "06"),
                ["town1"] = Tuple.Create("secretf", "07"),
                ["town2"] = Tuple.Create("secretf", "08"),
                ["town3"] = Tuple.Create("secretf", "09"),

                // Special names
                ["endepis"] = Tuple.Create((string)null, ":end"),
                ["ending"] = Tuple.Create((string)null, ":credits")
            };

            Func<string, JJ2Level.LevelToken> levelTokenConversion = levelToken => {
                levelToken = levelToken.ToLower(CultureInfo.InvariantCulture).Replace(" ", "_").Replace("\"", "").Replace("'", "");

                Tuple<string, string> knownLevel;
                if (knownLevels.TryGetValue(levelToken, out knownLevel)) {
                    if (string.IsNullOrEmpty(knownLevel.Item2)) {
                        return new JJ2Level.LevelToken {
                            Episode = (string.IsNullOrEmpty(knownLevel.Item1) ? null : knownLevel.Item1),
                            Level = levelToken
                        };
                    }
                    return new JJ2Level.LevelToken {
                        Episode = knownLevel.Item1,
                        Level = (knownLevel.Item2[0] == ':' ? knownLevel.Item2 : (knownLevel.Item2 + "_" + levelToken))
                    };
                }
                return new JJ2Level.LevelToken {
                    Level = levelToken
                };
            };

            Func<JJ2Episode, string> episodeNameConversion = episode => {
                if (episode.Token == "share" && episode.Name == "#Shareware@Levels") {
                    return "Shareware Demo";
                } else if (episode.Token == "xmas98" && episode.Name == "#Xmas 98@Levels") {
                    return "Holiday Hare '98";
                } else if (episode.Token == "xmas99" && episode.Name == "#Xmas 99@Levels") {
                    return "The Christmas Chronicles";
                } else if (episode.Token == "secretf" && episode.Name == "#Secret@Files") {
                    return "The Secret Files";
                } else {
                    // @ is new line, # is random color
                    return episode.Name.Replace("#", "").Replace("@", " ");
                }
            };

            // Previous/Next Episode mapping
            Func<JJ2Episode, Tuple<string, string>> episodePrevNext = episode => {
                if (episode.Token == "prince") {
                    return Tuple.Create((string)null, "rescue");
                } else if (episode.Token == "rescue") {
                    return Tuple.Create("prince", "flash");
                } else if (episode.Token == "flash") {
                    return Tuple.Create("rescue", "monk");
                } else if (episode.Token == "monk") {
                    return Tuple.Create("flash", (string)null);
                } else {
                    return Tuple.Create((string)null, (string)null);
                }
            };

            Dictionary<EventConverter.JJ2Event, int> unsupportedEvents = new Dictionary<EventConverter.JJ2Event, int>();

            Directory.CreateDirectory(Path.Combine(targetPath, "Content", "Episodes"));

            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "*.j2e"), file => {
                try {
                    JJ2Episode e = JJ2Episode.Open(file);
                    if (e.Token == "home") {
                        return;
                    }

                    string output = Path.Combine(targetPath, "Content", "Episodes", e.Token);
                    Directory.CreateDirectory(output);
                    e.Convert(output, levelTokenConversion, episodeNameConversion, episodePrevNext);

                    Log.Write(LogType.Info, "Episode \"" + e.Token + "\" (" + e.Name + ") converted.");
                } catch (Exception ex) {
                    Log.Write(LogType.Error, "Episode \"" + Path.GetFileName(file) + "\" not supported! " + ex);
                }
            });

            Log.PopIndent();
            Log.Write(LogType.Info, "Importing levels...");
            Log.PushIndent();

            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "*.j2l"), file => {
                try {
                    string asPath = Path.ChangeExtension(file, ".j2as");
                    bool isPlusEnhanced = Utils.FileResolveCaseInsensitive(ref asPath);

                    JJ2Level l = JJ2Level.Open(file, false);
                    string levelToken = l.CurrentLevelToken.ToLower().Replace(" ", "_").Replace("\"", "").Replace("'", "");

                    Tuple<string, string> knownLevel;
                    string targetPathInner = Path.Combine(targetPath, "Content", "Episodes");
                    if (knownLevels.TryGetValue(levelToken, out knownLevel)) {
                        if (string.IsNullOrEmpty(knownLevel.Item2)) {
                            targetPathInner = Path.Combine(targetPathInner, knownLevel.Item1, levelToken);
                        } else {
                            targetPathInner = Path.Combine(targetPathInner, knownLevel.Item1, knownLevel.Item2 + "_" + levelToken);
                        }
                    } else {
                        targetPathInner = Path.Combine(targetPathInner, "unknown", levelToken);
                    }

                    string versionPart;
                    switch (l.Version) {
                        case JJ2Version.BaseGame:
                            versionPart = "";
                            break;
                        case JJ2Version.TSF:
                            versionPart = " [TSF]";
                            break;
                        default:
                            versionPart = " [Unknown]";
                            break;
                    }

                    Directory.CreateDirectory(targetPathInner);
                    l.Convert(targetPathInner, levelTokenConversion);

                    if (l.UnsupportedEvents.Count > 0) {
                        Log.Write(LogType.Warning, "Level \"" + levelToken + "\"" + versionPart + " converted" + (isPlusEnhanced ? " without .j2as" : "") + " with " + l.UnsupportedEvents.Sum(i => i.Value) + " warnings.");
                    } else {
                        Log.Write(LogType.Info, "Level \"" + levelToken + "\"" + versionPart + " converted" + (isPlusEnhanced ? " without .j2as" : "") + ".");
                    }

                    if (!string.IsNullOrEmpty(l.Music)) {
                        usedMusic.Add(Path.GetFileNameWithoutExtension(l.Music).ToLowerInvariant());
                    }
                    if (!string.IsNullOrEmpty(l.Tileset)) {
                        usedTilesets.Add(l.Tileset.ToLowerInvariant());
                    }

                    lock (unsupportedEvents) {
                        foreach (var e in l.UnsupportedEvents) {
                            int count;
                            unsupportedEvents.TryGetValue(e.Key, out count);
                            unsupportedEvents[e.Key] = (count + e.Value);
                        }
                    }
                } catch (Exception ex) {
                    Log.Write(LogType.Error, "Level \"" + Path.GetFileName(file) + "\" not supported! " + ex);
                }
            });

            Log.Write(LogType.Verbose, "Summary of unsupported events:");
            Log.PushIndent();

            foreach (var e in unsupportedEvents.OrderByDescending(i => i.Value)) {
                Log.Write(LogType.Verbose, " " + e.Key.ToString().PadRight(32, ' ') + e.Value.ToString().PadLeft(4, ' '));
            }

            Log.PopIndent();

            Log.PopIndent();
        }

        public static void ConvertJJ2Music(string sourcePath, string targetPath, HashSet<string> usedMusic, bool verbose)
        {
            Log.Write(LogType.Info, "Importing music...");
            Log.PushIndent();

            string[] exts = { ".j2b", ".xm", ".it", ".s3m" };

            Directory.CreateDirectory(Path.Combine(targetPath, "Content", "Music"));

            for (int i = 0; i < exts.Length; i++) {
                foreach (string file in Directory.EnumerateFiles(sourcePath, "*" + exts[i], SearchOption.TopDirectoryOnly)) {
                    string token = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (usedMusic != null && !usedMusic.Contains(token)) {
                        if (verbose) {
                            Log.Write(LogType.Info, "File \"" + Path.GetFileName(file) + "\" not used! Skipped.");
                        }
                        continue;
                    }

                    string targetFile = Path.Combine(targetPath, "Content", "Music", Path.GetFileName(file).ToLowerInvariant());
                    if (File.Exists(targetFile)) {
                        if (verbose) {
                            Log.Write(LogType.Verbose, "File \"" + Path.GetFileName(file) + "\" already exists! Skipped.");
                        }
                        continue;
                    }

                    File.Copy(file, targetFile);
                }
            }

            Log.PopIndent();
        }

        public static void ConvertJJ2Tilesets(string sourcePath, string targetPath, HashSet<string> usedTilesets, bool verbose)
        {
            Log.Write(LogType.Info, "Importing tilesets...");
            Log.PushIndent();

            Directory.CreateDirectory(Path.Combine(targetPath, "Content", "Tilesets"));

            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "*.j2t"), file => {
                string token = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (usedTilesets != null && !usedTilesets.Contains(token)) {
                    if (verbose) {
                        Log.Write(LogType.Info, "File \"" + Path.GetFileName(file) + "\" not used! Skipped.");
                    }
                    return;
                }

                try {
                    JJ2Tileset t = JJ2Tileset.Open(file, true);
                    string output = Path.Combine(targetPath, "Content", "Tilesets", token);
                    Directory.CreateDirectory(output);
                    t.Convert(output);
                } catch (Exception ex) {
                    Log.Write(LogType.Error, "Tileset \"" + Path.GetFileName(file) + "\" not supported!");
                    Console.WriteLine(ex.ToString());
                }
            });

            Log.PopIndent();
        }

        private static void Clean(string targetPath, bool verbose)
        {
            JsonParser jsonParser = new JsonParser();

            HashSet<string> usedMusic = new HashSet<string>();
            HashSet<string> usedTilesets = new HashSet<string>();
            HashSet<string> usedAnimations = new HashSet<string>();

            // Clean music and tilesets
            if (Directory.Exists(Path.Combine(targetPath, "Content", "Episodes"))) {
                Log.Write(LogType.Info, "Cleaning \"Music\" and \"Tileset\" directories...");
                Log.PushIndent();

                int removedCount = 0;

                // Paths in the set have to be lower-case
                usedMusic.Add("boss1");
                usedMusic.Add("boss2");
                usedMusic.Add("bonus2");
                usedMusic.Add("bonus3");
                usedMusic.Add("menu");

                foreach (string episode in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Episodes"))) {
                    foreach (string level in Directory.EnumerateDirectories(episode)) {
                        string path = Path.Combine(level, ".res");
                        using (Stream s = File.Open(path, FileMode.Open)) {
                            LevelConfigJson json = jsonParser.Parse<LevelConfigJson>(s);

                            if (!string.IsNullOrEmpty(json.Description.DefaultMusic)) {
                                usedMusic.Add(Path.GetFileNameWithoutExtension(json.Description.DefaultMusic).ToLowerInvariant());
                            }

                            if (!string.IsNullOrEmpty(json.Description.DefaultTileset)) {
                                usedTilesets.Add(Path.GetFileName(json.Description.DefaultTileset).ToLowerInvariant());
                            }
                        }
                    }
                }

                if (Directory.Exists(Path.Combine(targetPath, "Content", "Music"))) {
                    string[] music = Directory.GetFiles(Path.Combine(targetPath, "Content", "Music"));
                    foreach (string file in music) {
                        if (!usedMusic.Contains(Path.GetFileNameWithoutExtension(file).ToLowerInvariant())) {
                            try {
                                File.Delete(file);
                                if (verbose) {
                                    Log.Write(LogType.Verbose, "Music \"" + Path.GetFileName(file) + "\" removed.");
                                }
                                removedCount++;
                            } catch {
                                Log.Write(LogType.Warning, "Music \"" + Path.GetFileName(file) + "\" cannot be removed.");
                            }

                        }
                    }
                }

                if (Directory.Exists(Path.Combine(targetPath, "Content", "Tilesets"))) {
                    string[] tilesets = Directory.GetDirectories(Path.Combine(targetPath, "Content", "Tilesets"));
                    foreach (string directory in tilesets) {
                        if (!usedTilesets.Contains(Path.GetFileName(directory).ToLowerInvariant())) {
                            try {
                                Directory.Delete(directory, true);
                                if (verbose) {
                                    Log.Write(LogType.Verbose, "Tileset \"" + Path.GetFileName(directory) + "\" removed.");
                                }
                                removedCount++;
                            } catch {
                                Log.Write(LogType.Warning, "Tileset \"" + Path.GetFileName(directory) + "\" cannot be removed.");
                            }

                        }
                    }
                }

                if (!verbose) {
                    Log.Write(LogType.Info, "Removed " + removedCount + " files.");
                }
                Log.PopIndent();
            }

            // Clean animations and sounds
            if (Directory.Exists(Path.Combine(targetPath, "Content", "Metadata"))) {
                Log.Write(LogType.Info, "Cleaning \"Animations\" directory...");
                Log.PushIndent();

                int removedCount = 0;

                // Paths in the set have to be lower-case
                usedAnimations.Add("_custom/noise.png");
                usedAnimations.Add("ui/font_small.png");
                usedAnimations.Add("ui/font_medium.png");

                foreach (string metadata in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Metadata"))) {
                    foreach (string path in Directory.EnumerateFiles(metadata, "*.res")) {
                        using (Stream s = File.Open(path, FileMode.Open)) {
                            MetadataJson json = jsonParser.Parse<MetadataJson>(s);

                            if (json.Animations != null) {
                                foreach (var animation in json.Animations) {
                                    if (animation.Value == null || animation.Value.Path == null) {
                                        continue;
                                    }
                                    usedAnimations.Add(animation.Value.Path.ToLowerInvariant());
                                }
                            }

                            if (json.Sounds != null) {
                                foreach (var sound in json.Sounds) {
                                    if (sound.Value == null || sound.Value.Paths == null) {
                                        continue;
                                    }
                                    foreach (var soundPath in sound.Value.Paths) {
                                        if (soundPath == null) {
                                            continue;
                                        }
                                        usedAnimations.Add(soundPath.ToLowerInvariant());
                                    }
                                }
                            }
                        }
                    }
                }

                string prefix = Path.Combine(targetPath, "Content", "Animations");
                if (Directory.Exists(prefix)) {
                    foreach (string animation in Directory.EnumerateFiles(prefix, "*", SearchOption.AllDirectories)) {
                        string animationFile = animation.Substring(prefix.Length + 1).ToLowerInvariant().Replace('\\', '/').Replace(".png.res", ".png").Replace(".n.png", ".png").Replace(".png.config", ".png");
                        if (!usedAnimations.Contains(animationFile)) {
                            string pathWithoutPrefix = animation.Substring(prefix.Length);
                            try {
                                File.Delete(animation);
                                if (verbose) {
                                    Log.Write(LogType.Verbose, "Animation \"" + pathWithoutPrefix + "\" removed.");
                                }
                                removedCount++;
                            } catch {
                                Log.Write(LogType.Warning, "Animation \"" + pathWithoutPrefix + "\" cannot be removed.");
                            }
                        }
                    }

                    foreach (string directory in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Animations"))) {
                        bool hasFiles = Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories).Any();
                        if (!hasFiles) {
                            string pathWithoutPrefix = directory.Substring(prefix.Length);
                            try {
                                Directory.Delete(directory);
                                if (verbose) {
                                    Log.Write(LogType.Verbose, "Empty directory \"" + pathWithoutPrefix + "\" removed.");
                                }
                                removedCount++;
                            } catch {
                                Log.Write(LogType.Warning, "Empty directory \"" + pathWithoutPrefix + "\" cannot be removed.");
                            }
                        }
                    }
                }

                if (!verbose) {
                    Log.Write(LogType.Info, "Removed " + removedCount + " files.");
                }
                Log.PopIndent();
            }
        }

        private static void CheckMissingFiles(string targetPath)
        {
            JsonParser jsonParser = new JsonParser();

            // Check music and tilesets
            Log.Write(LogType.Info, "Checking \"Music\" and \"Tileset\" directories for missing files...");
            Log.PushIndent();

            foreach (string unreferenced in new[] { "boss1.j2b", "boss2.j2b", "bonus2.j2b", "bonus3.j2b", "menu.j2b" }) {
                if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Music", unreferenced))) {
                    Log.Write(LogType.Warning, "\"" + Path.Combine("Music", unreferenced) + "\" is missing!");
                }
            }

            if (Directory.Exists(Path.Combine(targetPath, "Content", "Episodes"))) {
                foreach (string episode in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Episodes"))) {
                    foreach (string level in Directory.EnumerateDirectories(episode)) {
                        string path = Path.Combine(level, ".res");
                        using (Stream s = File.Open(path, FileMode.Open)) {
                            LevelConfigJson json = jsonParser.Parse<LevelConfigJson>(s);

                            if (!string.IsNullOrEmpty(json.Description.DefaultMusic)) {
                                if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Music", json.Description.DefaultMusic))) {
                                    Log.Write(LogType.Warning, "\"" + Path.Combine("Music", json.Description.DefaultMusic) + "\" is missing!");
                                }
                            }

                            if (!string.IsNullOrEmpty(json.Description.DefaultTileset)) {
                                if (!Directory.Exists(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset)) ||
                                    !Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "tiles.png")) ||
                                    !Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "mask.png")) ||
                                    !Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "normal.png")) ||
                                    !Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, ".palette"))) {

                                    Log.Write(LogType.Warning, "\"" + Path.Combine("Tilesets", json.Description.DefaultTileset) + "\" is missing!");
                                }
                            }
                        }
                    }
                }
            }

            Log.PopIndent();

            // Check animations and sounds
            Log.Write(LogType.Info, "Checking \"Animations\" directory for missing files...");
            Log.PushIndent();

            foreach (string unreferenced in new[] { "_custom/noise.png", "UI/font_medium.png", "UI/font_medium.png.res", "UI/font_medium.png.config", "UI/font_small.png", "UI/font_small.png.res", "UI/font_small.png.config" }) {
                if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", unreferenced))) {
                    Log.Write(LogType.Warning, "\"" + Path.Combine("Animations", unreferenced.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
                }
            }

            if (Directory.Exists(Path.Combine(targetPath, "Content", "Metadata"))) {
                foreach (string metadata in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Metadata"))) {
                    foreach (string path in Directory.EnumerateFiles(metadata, "*.res")) {
                        using (Stream s = File.Open(path, FileMode.Open)) {
                            MetadataJson json;
                            try {
                                json = jsonParser.Parse<MetadataJson>(s);
                            } catch (Exception ex) {
                                Log.Write(LogType.Error, "\"" + Path.GetFileName(Path.GetDirectoryName(path)) + Path.DirectorySeparatorChar + Path.GetFileName(path) + "\" is corrupted! " + ex.Message);
                                continue;
                            }

                            if (json.Animations != null) {
                                foreach (var animation in json.Animations) {
                                    if (animation.Value == null || animation.Value.Path == null) {
                                        continue;
                                    }
                                    if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", animation.Value.Path))) {
                                        Log.Write(LogType.Warning, "\"" + Path.Combine("Animations", animation.Value.Path.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
                                    }
                                    if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", animation.Value.Path + ".res"))) {
                                        Log.Write(LogType.Warning, "\"" + Path.Combine("Animations", animation.Value.Path.Replace('/', Path.DirectorySeparatorChar)) + ".res" + "\" is missing!");
                                    }
                                }
                            }

                            if (json.Sounds != null) {
                                foreach (var sound in json.Sounds) {
                                    if (sound.Value == null || sound.Value.Paths == null) {
                                        continue;
                                    }
                                    foreach (var soundPath in sound.Value.Paths) {
                                        if (soundPath == null) {
                                            continue;
                                        }
                                        if (!Utils.FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", soundPath))) {
                                            Log.Write(LogType.Warning, "\"" + Path.Combine("Animations", soundPath.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Log.PopIndent();
        }

        // :: Debug-only methods ::
#if DEBUG
        private static void MigrateMetadata(string targetPath)
        {
            Log.Write(LogType.Info, "Migrating metadata...");
            Log.PushIndent();

            Parallel.ForEach(Directory.EnumerateDirectories(targetPath), directory => {
                foreach (string file in Directory.EnumerateFiles(directory, "*.res")) {
                    try {
                        bool result = Jazz2.Migrations.MetadataV1ToV2.Convert(file);
                        if (result) {
                            Log.Write(LogType.Info, "Metadata \"" + Path.GetFileName(file) + "\" was migrated to v2!");
                        } else {
                            Log.Write(LogType.Verbose, "Metadata \"" + Path.GetFileName(file) + "\" is already up to date!");
                        }
                    } catch (Exception ex) {
                        Log.Write(LogType.Error, "Metadata \"" + Path.GetFileName(file) + "\" is not supported! " + ex);
                    }
                }
            });

            Log.PopIndent();
        }

        private static void AdaptImageToPalette(string path, string[] args)
        {
            int noise = 0;
            for (int i = 0; i < args.Length; i++) {
                if (args[i].StartsWith("/noise:")) {
                    int.TryParse(args[i].Substring(7), out noise);
                }
            }

            Log.Write(LogType.Info, $"Adapting image to \"Sprite\" palette with {noise}% noise...");

            Random r = new Random();
            int diffMax = 0, diffTotal = 0;

            using (FileStream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (Bitmap b = new Bitmap(s)) {
                    for (int x = 0; x < b.Width; x++) {
                        for (int y = 0; y < b.Height; y++) {
                            Color color = b.GetPixel(x, y);

                            Color bestMatch = Color.Transparent;
                            Color secondMatch = Color.Transparent;
                            int bestMatchDiff = int.MaxValue;
                            for (int i = 0; i < JJ2DefaultPalette.Sprite.Length; i++) {
                                Color current = JJ2DefaultPalette.Sprite[i];
                                int currentDiff = Math.Abs(color.R - current.R) + Math.Abs(color.G - current.G) + Math.Abs(color.B - current.B) + Math.Abs(color.A - current.A);
                                if (currentDiff < bestMatchDiff) {
                                    secondMatch = bestMatch;
                                    bestMatch = current;
                                    bestMatchDiff = currentDiff;
                                }
                            }

                            if (r.Next(100) < noise && Math.Abs(color.A - secondMatch.A) < 20) {
                                bestMatch = secondMatch;
                            }

                            bestMatch = Color.FromArgb(color.A, bestMatch);
                            b.SetPixel(x, y, bestMatch);

                            diffMax = Math.Max(diffMax, bestMatchDiff);
                            diffTotal += bestMatchDiff;
                        }
                    }

                    b.Save(Path.ChangeExtension(path, ".new" + Path.GetExtension(path)), ImageFormat.Png);
                }
            }

            Log.Write(LogType.Info, "Image adapted!   |   Max. diff: " + diffMax + "   |   Total diff: " + diffTotal);

            Console.ReadLine();
        }
#endif
    }
}