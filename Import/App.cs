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
using Jazz2.Compatibility;
using Jazz2.Migrations;
using static Jazz2.Game.ContentResolver;
using static Jazz2.Game.LevelHandler;

namespace Import
{
    internal class App
    {
        private enum LogType
        {
            Debug,
            Info,
            Warning,
            Error
        }

        private static readonly object sync = new object();

        private static void Main(string[] args)
        {
            Utils.TryEnableUnicode();

            Console.Title = Jazz2.App.AssemblyTitle;

            if (args.Length < 1) {
                ShowHelp();

                DemoDownloader.Start();
                Console.ReadLine();

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
            bool migrate = false;
            bool clean = false;
            bool check = false;
            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "/skip-anims": processAnims = false; break;
                    case "/skip-levels": processLevels = false; break;
                    case "/skip-music": processMusic = false; break;
                    case "/skip-tilesets": processTilesets = false; break;
                    case "/skip-all": processAnims = processLevels = processMusic = processTilesets = false; break;

                    case "/all": all = true; break;
                    case "/no-wait": noWait = true; break;

                    case "/migrate": migrate = true; break;
                    case "/clean": clean = true; break;
                    case "/check": check = true; break;

                    default:
                        if (File.Exists(args[i]) && Path.GetExtension(args[i]) == ".png") {
                            AdaptImageToPalette(args[i]);
                            return;
                        }

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
                    WriteLog(LogType.Error, "You must specify path to Jazz Jackrabbit™ 2 game.");
                    return;
                }
            } else {
                WriteLog(LogType.Info, "Game path: " + sourcePath);
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
                ConvertJJ2Music(sourcePath, targetPath, all ? null : usedMusic);
            }
            if (processTilesets) {
                ConvertJJ2Tilesets(sourcePath, targetPath, all ? null : usedTilesets);
            }
            if (clean) {
                Clean(targetPath);
            }
            if (check || (processAnims && processLevels && processMusic && processTilesets)) {
                CheckFiles(targetPath);
            }

            bool isAnyMissing = false;
            if (!Directory.Exists(Path.Combine(targetPath, "Content", "Metadata"))) {
                WriteLog(LogType.Error, "Directory \"Metadata\" is missing!");
                isAnyMissing = true;
            } else if (migrate) {
                MigrateMetadata(Path.Combine(targetPath, "Content", "Metadata"));
            }

            if (!Directory.Exists(Path.Combine(targetPath, "Content", "Shaders"))) {
                WriteLog(LogType.Error, "Directory \"Shaders\" is missing!");
                isAnyMissing = true;
            }
            if (isAnyMissing) {
                WriteLog(LogType.Error, "It should be distributed with Jazz² Resurrection.");
            }

            if (!noWait) {
                WriteLog(LogType.Info, "Done! (Press any key to exit)");
                Console.ReadLine();
            }
        }

        private static void WriteLog(LogType type, string text)
        {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            ConsoleColor color, color2;
            switch (type) {
                case LogType.Debug:
#if DEBUG
                    color = ConsoleColor.Green;
                    color2 = ConsoleColor.DarkGreen;
                    break;
#else
                    return;
#endif
                case LogType.Info:
                    color = ConsoleColor.White;
                    color2 = ConsoleColor.Gray;
                    break;
                case LogType.Warning:
                    color = ConsoleColor.Yellow;
                    color2 = ConsoleColor.DarkYellow;
                    break;
                case LogType.Error:
                    color = ConsoleColor.Red;
                    color2 = ConsoleColor.DarkRed;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            lock (sync) {
                Console.ForegroundColor = color;
                Console.Write(" │  ");
                Console.ForegroundColor = color2;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }

        private static void ShowHelp()
        {
            string appName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            string appVersion = " v" + Jazz2.App.AssemblyVersion;

            int width = Console.BufferWidth;

            Console.Write(new string('_', width));
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', width));
            Console.Write(("  " + Jazz2.App.AssemblyTitle).PadRight(width - appVersion.Length - 2) + appVersion + "  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(new string('_', width));
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("  Run this application with following parameters:");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(new string('_', width));
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', width));
            Console.Write(("  " + appName + " \"Path to Jazz Jackrabbit 2\"").PadRight(width));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string('_', width));
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("  Application will automatically import all animations, sounds, music,");
            Console.WriteLine("  tilesets, levels and episodes. It could take several minutes.");
            Console.WriteLine("  Christmas Chronicles, The Secret Files, Holiday Hare '98 and JJ2+ extension");
            Console.WriteLine("  is supported. Specific version could be required according to included");
            Console.WriteLine("  Metadata files.");
            Console.WriteLine();
            Console.WriteLine("  To remove unused files afterwards, use following command:");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(new string('_', width));
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', width));
            Console.Write(("  " + appName + " /clean /skip-all").PadRight(width));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string('_', width));
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("  There are several other options:");
            Console.WriteLine();
            Console.WriteLine("   /skip-anims     Don't convert animations and sounds (*.j2a files).");
            Console.WriteLine("   /skip-levels    Don't convert level files (*.j2l files).");
            Console.WriteLine("   /skip-music     Don't convert music files (*.j2b files).");
            Console.WriteLine("   /skip-tilesets  Don't convert tileset files (*.j2t files).");
            Console.WriteLine("   /skip-all       Don't convert anything.");
            Console.WriteLine("   /all            Convert all (even unused) music and tileset files.");
            Console.WriteLine("                   Otherwise only files referenced in levels will be converted.");
            Console.WriteLine("   /no-wait        Don't show (Press any key to exit) message when it's done.");
            Console.WriteLine("   /migrate        Migrate metadata files to newer version.");
            Console.WriteLine("   /clean          Remove unused music, tilesets, animations and sounds.");
            Console.WriteLine("   /check          Check that all needed assets are present.");
        }

        public static void ConvertJJ2Anims(string sourcePath, string targetPath)
        {
            WriteLog(LogType.Info, "Importing assets...");

            targetPath = Path.Combine(targetPath, "Content", "Animations");
            Directory.CreateDirectory(targetPath);

            string animsPath = Path.Combine(sourcePath, "Anims.j2a");
            if (FileResolveCaseInsensitive(ref animsPath)) {
                JJ2Anims.Convert(animsPath, targetPath, false);
            } else {
                // Try to convert Shareware Demo
                animsPath = Path.Combine(sourcePath, "AnimsSw.j2a");
                if (FileResolveCaseInsensitive(ref animsPath)) {
                    JJ2Anims.Convert(animsPath, targetPath, false);
                }
            }

            string plusPath = Path.Combine(sourcePath, "Plus.j2a");
            if (FileResolveCaseInsensitive(ref plusPath)) {
                JJ2Anims.Convert(plusPath, targetPath, true);
            }
        }

        public static void ConvertJJ2Levels(string sourcePath, string targetPath, HashSet<string> usedTilesets, HashSet<string> usedMusic)
        {
            WriteLog(LogType.Info, "Importing levels...");

            string xmasEpisodePath = Path.Combine(sourcePath, "xmas99.j2e");
            string xmasEpisodeToken = (FileResolveCaseInsensitive(ref xmasEpisodePath) ? "xmas99" : "xmas98");

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

                    WriteLog(LogType.Debug, "Converted episode \"" + e.Token + "\" (" + e.Name + ")");
                } catch (Exception ex) {
                    WriteLog(LogType.Error, "Episode \"" + Path.GetFileName(file) + "\" is not supported!");
                    Console.WriteLine(ex.ToString());
                }
            });

            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "*.j2l"), file => {
                try {
                    string asPath = Path.ChangeExtension(file, ".j2as");
                    bool isPlusEnhanced = FileResolveCaseInsensitive(ref asPath);

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

                    string versionString;
                    switch (l.Version) {
                        case JJ2Version.BaseGame:
                            versionString = "Base";
                            break;
                        case JJ2Version.TSF:
                            versionString = "TSF";
                            break;
                        default:
                            versionString = "Unknown";
                            break;
                    }

                    Directory.CreateDirectory(targetPathInner);
                    l.Convert(targetPathInner, levelTokenConversion);

                    if (l.UnsupportedEvents.Count > 0) {
                        WriteLog(LogType.Warning, "Converted level \"" + levelToken + "\" [" + versionString + (isPlusEnhanced ? "+" : "") + "] with " + l.UnsupportedEvents.Sum(i => i.Value) + " warnings");
                    } else {
                        WriteLog(LogType.Debug, "Converted level \"" + levelToken + "\" [" + versionString + (isPlusEnhanced ? "+" : "") + "]");
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
                    WriteLog(LogType.Error, "Level \"" + Path.GetFileName(file) + "\" is not supported!");
                    Console.WriteLine(ex.ToString());
                }
            });

            WriteLog(LogType.Info, "Summary of unsupported events:");
            foreach (var e in unsupportedEvents.OrderByDescending(i => i.Value)) {
                WriteLog(LogType.Info, "  " + e.Key.ToString().PadRight(32, ' ') + e.Value.ToString().PadLeft(4, ' '));
            }
        }

        public static void ConvertJJ2Music(string sourcePath, string targetPath, HashSet<string> usedMusic)
        {
            WriteLog(LogType.Info, "Importing music...");

            string[] exts = {".j2b", ".xm", ".it", ".s3m"};

            Directory.CreateDirectory(Path.Combine(targetPath, "Content", "Music"));

            for (int i = 0; i < exts.Length; i++) {
                foreach (string file in Directory.EnumerateFiles(sourcePath, "*" + exts[i], SearchOption.TopDirectoryOnly)) {
                    string token = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (usedMusic != null && !usedMusic.Contains(token)) {
                        WriteLog(LogType.Warning, "File \"" + Path.GetFileName(file) + "\" not used! Skipped.");
                        continue;
                    }

                    string targetFile = Path.Combine(targetPath, "Content", "Music", Path.GetFileName(file).ToLowerInvariant());
                    if (File.Exists(targetFile)) {
                        WriteLog(LogType.Debug, "File \"" + Path.GetFileName(file) + "\" already exists! Skipped.");
                        continue;
                    }

                    File.Copy(file, targetFile);
                }
            }
        }

        public static void ConvertJJ2Tilesets(string sourcePath, string targetPath, HashSet<string> usedTilesets)
        {
            WriteLog(LogType.Info, "Importing tilesets...");

            Directory.CreateDirectory(Path.Combine(targetPath, "Content", "Tilesets"));

            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "*.j2t"), file => {
                string token = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (usedTilesets != null && !usedTilesets.Contains(token)) {
                    WriteLog(LogType.Warning, "File \"" + Path.GetFileName(file) + "\" not used! Skipped.");
                    return;
                }

                try {
                    JJ2Tileset t = JJ2Tileset.Open(file, true);
                    string output = Path.Combine(targetPath, "Content", "Tilesets", token);
                    Directory.CreateDirectory(output);
                    t.Convert(output);
                } catch (Exception ex) {
                    WriteLog(LogType.Error, "Tileset \"" + Path.GetFileName(file) + "\" is not supported!");
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        private static void MigrateMetadata(string targetPath)
        {
            WriteLog(LogType.Info, "Migrating metadata...");

            Parallel.ForEach(Directory.EnumerateDirectories(targetPath), directory => {
                foreach (string file in Directory.EnumerateFiles(directory, "*.res")) {
                    try {
                        bool result = MetadataV1ToV2.Convert(file);
                        if (result) {
                            WriteLog(LogType.Info, "Metadata \"" + Path.GetFileName(file) + "\" was migrated to v2!");
                        } else {
                            WriteLog(LogType.Debug, "Metadata \"" + Path.GetFileName(file) + "\" is already up to date!");
                        }
                    } catch (Exception ex) {
                        WriteLog(LogType.Error, "Metadata \"" + Path.GetFileName(file) + "\" is not supported!");
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
        }

        private static void Clean(string targetPath)
        {
            JsonParser jsonParser = new JsonParser();

            HashSet<string> usedMusic = new HashSet<string>();
            HashSet<string> usedTilesets = new HashSet<string>();
            HashSet<string> usedAnimations = new HashSet<string>();

            // Clean music and tilesets
            if (Directory.Exists(Path.Combine(targetPath, "Content", "Episodes"))) {
                WriteLog(LogType.Info, "Cleaning \"Music\" and \"Tileset\" directories...");

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
                                WriteLog(LogType.Debug, "Music \"" + Path.GetFileName(file) + "\" removed...");
                            } catch {
                                WriteLog(LogType.Warning, "Music \"" + Path.GetFileName(file) + "\" could not be removed...");
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
                                WriteLog(LogType.Debug, "Tileset \"" + Path.GetFileName(directory) + "\" removed...");
                            } catch {
                                WriteLog(LogType.Warning, "Tileset \"" + Path.GetFileName(directory) + "\" could not be removed...");
                            }

                        }
                    }
                }
            }

            // Clean animations and sounds
            if (Directory.Exists(Path.Combine(targetPath, "Content", "Metadata"))) {
                WriteLog(LogType.Info, "Cleaning \"Animations\" directory...");

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
                            try {
                                File.Delete(animation);
                                WriteLog(LogType.Debug, "Animation \"" + Path.GetFileName(animation) + "\" removed...");
                            } catch {
                                WriteLog(LogType.Warning, "Animation \"" + Path.GetFileName(animation) + "\" could not be removed...");
                            }
                        }
                    }

                    foreach (string directory in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Animations"))) {
                        bool hasFiles = Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories).Any();
                        if (!hasFiles) {
                            try {
                                Directory.Delete(directory);
                                WriteLog(LogType.Debug, "Empty directory \"" + Path.GetFileName(directory) + "\" removed...");
                            } catch {
                                WriteLog(LogType.Warning, "Empty directory \"" + Path.GetFileName(directory) + "\" could not be removed...");
                            }
                        }
                    }
                }
            }
        }

        private static void CheckFiles(string targetPath)
        {
            JsonParser jsonParser = new JsonParser();

            // Check music and tilesets
            WriteLog(LogType.Info, "Checking \"Music\" and \"Tileset\" directories for missing files...");

            foreach (string unreferenced in new[] { "boss1.j2b", "boss2.j2b", "bonus2.j2b", "bonus3.j2b", "menu.j2b" }) {
                if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Music", unreferenced))) {
                    WriteLog(LogType.Warning, "\"" + Path.Combine("Music", unreferenced) + "\" is missing!");
                }
            }

            if (Directory.Exists(Path.Combine(targetPath, "Content", "Episodes"))) {
                foreach (string episode in Directory.EnumerateDirectories(Path.Combine(targetPath, "Content", "Episodes"))) {
                    foreach (string level in Directory.EnumerateDirectories(episode)) {
                        string path = Path.Combine(level, ".res");
                        using (Stream s = File.Open(path, FileMode.Open)) {
                            LevelConfigJson json = jsonParser.Parse<LevelConfigJson>(s);

                            if (!string.IsNullOrEmpty(json.Description.DefaultMusic)) {
                                if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Music", json.Description.DefaultMusic))) {
                                    WriteLog(LogType.Warning, "\"" + Path.Combine("Music", json.Description.DefaultMusic) + "\" is missing!");
                                }
                            }

                            if (!string.IsNullOrEmpty(json.Description.DefaultTileset)) {
                                if (!Directory.Exists(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset)) ||
                                    !FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "tiles.png")) ||
                                    !FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "mask.png")) ||
                                    !FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, "normal.png")) ||
                                    !FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Tilesets", json.Description.DefaultTileset, ".palette"))) {
                                    WriteLog(LogType.Warning, "\"" + Path.Combine("Tilesets", json.Description.DefaultTileset) + "\" is missing!");
                                }
                            }
                        }
                    }
                }
            }

            // Check animations and sounds
            WriteLog(LogType.Info, "Checking \"Animations\" directory for missing files...");

            foreach (string unreferenced in new[] { "_custom/noise.png", "UI/font_medium.png", "UI/font_medium.png.res", "UI/font_medium.png.config", "UI/font_small.png", "UI/font_small.png.res", "UI/font_small.png.config" }) {
                if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", unreferenced))) {
                    WriteLog(LogType.Warning, "\"" + Path.Combine("Animations", unreferenced.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
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
                                WriteLog(LogType.Error, "\"" + Path.GetFileName(Path.GetDirectoryName(path)) + Path.DirectorySeparatorChar + Path.GetFileName(path) + "\" is corrupted! " + ex.Message);
                                continue;
                            }

                            if (json.Animations != null) {
                                foreach (var animation in json.Animations) {
                                    if (animation.Value == null || animation.Value.Path == null) {
                                        continue;
                                    }
                                    if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", animation.Value.Path))) {
                                        WriteLog(LogType.Warning, "\"" + Path.Combine("Animations", animation.Value.Path.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
                                    }
                                    if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", animation.Value.Path + ".res"))) {
                                        WriteLog(LogType.Warning, "\"" + Path.Combine("Animations", animation.Value.Path.Replace('/', Path.DirectorySeparatorChar)) + ".res" + "\" is missing!");
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
                                        if (!FileExistsCaseSensitive(Path.Combine(targetPath, "Content", "Animations", soundPath))) {
                                            WriteLog(LogType.Warning, "\"" + Path.Combine("Animations", soundPath.Replace('/', Path.DirectorySeparatorChar)) + "\" is missing!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AdaptImageToPalette(string path)
        {
            WriteLog(LogType.Info, "Adapting image to \"Sprite\" palette...");

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

                            if (r.Next(100) < 10 && Math.Abs(color.A - secondMatch.A) < 20) {
                                bestMatch = secondMatch;
                            }

                            b.SetPixel(x, y, bestMatch);

                            diffMax = Math.Max(diffMax, bestMatchDiff);
                            diffTotal += bestMatchDiff;
                        }
                    }

                    b.Save(Path.ChangeExtension(path, ".new" + Path.GetExtension(path)), ImageFormat.Png);
                }
            }

            WriteLog(LogType.Info, "Image adapted! - Max. diff: " + diffMax + " - Total diff: " + diffTotal);

            Console.ReadLine();
        }

        private static bool FileExistsCaseSensitive(string path)
        {
            path = path.Replace('/', Path.DirectorySeparatorChar);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                // Check case-sensitive on Windows
                if (File.Exists(path)) {
                    string directory = Path.GetDirectoryName(path);
                    string fileName = Path.GetFileName(path);
                    string found = Directory.EnumerateFiles(directory, fileName).First();
                    if (found == null || found == path) {

                        directory = directory.TrimEnd(Path.DirectorySeparatorChar);

                        while (true) {
                            int index = directory.LastIndexOf(Path.DirectorySeparatorChar);
                            if (index >= 0) {
                                string directoryName = directory.Substring(index + 1);
                                string parent = directory.Substring(0, index);

                                bool isDrive = (parent.Length == 2 && char.IsLetter(parent[0]) && parent[1] == ':');
                                if (isDrive) {
                                    // Parent directory is probably drive specifier (C:)
                                    // Append backslash...
                                    parent += Path.DirectorySeparatorChar;
                                }

                                found = Directory.EnumerateDirectories(parent, directoryName).First();
                                if (found != null && found != directory) {
                                    return false;
                                }

                                if (isDrive) {
                                    // Parent directory is probably drive specifier (C:)
                                    // Check is done...
                                    break;
                                }

                                directory = parent;
                            } else {
                                // No directory separator found
                                break;
                            }
                        }

                        return true;
                    }
                }

                return false;
            } else {
                return File.Exists(path);
            }
        }

        public static bool FileResolveCaseInsensitive(ref string path)
        {
            if (File.Exists(path)) {
                return true;
            }

            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            string found = Directory.EnumerateFiles(directory).FirstOrDefault(current => string.Compare(Path.GetFileName(current), fileName, true) == 0);
            if (found == null) {
                return false;
            } else {
                path = found;
                return true;
            }
        }
    }
}