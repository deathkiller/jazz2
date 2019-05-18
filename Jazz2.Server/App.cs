#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Reflection;
using Jazz2.Networking;
using Lidgren.Network;

namespace Jazz2.Server
{
    internal static partial class App
    {
        private static GameServer gameServer;

        private static void Main(string[] args)
        {
            ConsoleUtils.TryEnableUnicode();

#if DEBUG
            if (Console.BufferWidth < 90) {
                Console.BufferWidth = 90;
                Console.WindowWidth = 90;
            }
#endif

            if (!ConsoleUtils.IsOutputRedirected) {
                ConsoleImage.RenderFromManifestResource("ConsoleImage.udl");
            }

            // Process parameters
            int port;
            if (!TryRemoveArg(ref args, "/port:", out port)) {
                port = 10666;
            }

            string name;
            if (!TryRemoveArg(ref args, "/name:", out name) || string.IsNullOrWhiteSpace(name)) {
                name = "Unnamed server";
            }

            int maxPlayers;
            if (!TryRemoveArg(ref args, "/players:", out maxPlayers)) {
                maxPlayers = 64;
            }

            string levelName;
            if (!TryRemoveArg(ref args, "/level:", out levelName)) {
                levelName = "unknown/battle2";
            }

            bool isPrivate = TryRemoveArg(ref args, "/private");
            bool enableUPnP = TryRemoveArg(ref args, "/upnp");

            // Initialization
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            byte neededMajor = (byte)v.Major;
            byte neededMinor = (byte)v.Minor;
            byte neededBuild = (byte)v.Build;

            Log.Write(LogType.Info, "Starting server...");
            Log.PushIndent();

            // Start game server
            gameServer = new GameServer();
            gameServer.Run(port, name, maxPlayers, isPrivate, enableUPnP, neededMajor, neededMinor, neededBuild);

            Log.PopIndent();

            gameServer.ChangeLevel(levelName, MultiplayerLevelType.Battle);

            Log.Write(LogType.Info, "Ready!");
            Console.WriteLine();

            // Processing of console commands
            ProcessConsoleCommands();

            // Shutdown
            Console.WriteLine();
            Log.Write(LogType.Info, "Closing...");

            gameServer.Dispose();
        }

        private static void ProcessConsoleCommands()
        {
            while (true) {
                string input = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(input)) {
                    continue;
                }

                string command = GetPartFromInput(ref input);
                switch (command) {
                    case "quit":
                    case "exit":
                        return;

                    case "info": {
                        Log.Write(LogType.Info, "Server Load: " + gameServer.LoadMs + " ms");
                        Log.Write(LogType.Info, "Players: " + gameServer.PlayerCount + "/" + gameServer.MaxPlayers);
                        Log.Write(LogType.Info, "Current Level: " + gameServer.CurrentLevel);

                        Log.Write(LogType.Info, "Players:");
                        Log.PushIndent();
                        foreach (KeyValuePair<NetConnection, GameServer.Player> pair in gameServer.Players) {
                            Log.Write(LogType.Info, "#" + pair.Value.Index + " | " + pair.Key.RemoteEndPoint + " | " + pair.Value.State + " | " + pair.Value.Pos);
                        }
                        Log.PopIndent();

                        break;
                    }

                    case "set": {
                        string key = GetPartFromInput(ref input);
                        switch (key) {
                            case "name": {
                                if (!string.IsNullOrWhiteSpace(input)) {
                                    gameServer.Name = input;
                                    Log.Write(LogType.Info, "Server name was set to \"" + input + "\"!");
                                } else {
                                    Log.Write(LogType.Error, "Cannot set server name to \"" + input + "\"!");
                                }
                                break;
                            }

                            case "level": {
                                string value = GetPartFromInput(ref input);
                                if (gameServer.ChangeLevel(value, MultiplayerLevelType.Battle)) {
                                    Log.Write(LogType.Info, "OK!");
                                } else {
                                    Log.Write(LogType.Error, "Cannot load level \"" + value + "\"!");
                                }
                                break;
                            }

                            case "spawning": {
                                string value = GetPartFromInput(ref input);
                                gameServer.EnablePlayerSpawning(value == "true" || value == "yes" || value == "1");
                                Log.Write(LogType.Info, "OK!");
                                break;
                            }

                            default: {
                                Log.Write(LogType.Warning, "Unknown command: " + command + " " + key);
                                break;
                            }
                        }
                        break;
                    }

                    default:
                        Log.Write(LogType.Warning, "Unknown command: " + input);
                        break;
                }
            }
        }

        private static string GetPartFromInput(ref string input)
        {
            if (input == null) {
                return null;
            }

            string part;
            int idx = input.IndexOf(' ');
            if (idx == -1) {
                part = input;
                input = null;
            } else {
                part = input.Substring(0, idx);
                input = input.Substring(idx + 1);
            }
            return part;
        }

        public static bool TryRemoveArg(ref string[] args, string arg)
        {
            for (int i = 0; i < args.Length; i++) {
                if (string.Compare(args[i], arg, StringComparison.OrdinalIgnoreCase) == 0) {
                    List<string> list = new List<string>(args);
                    list.RemoveAt(i);
                    args = list.ToArray();
                    return true;
                }
            }

            return false;
        }

        public static bool TryRemoveArg(ref string[] args, string argPrefix, out string argSuffix)
        {
            for (int i = 0; i < args.Length; i++) {
                if (args[i].StartsWith(argPrefix, StringComparison.OrdinalIgnoreCase)) {
                    argSuffix = args[i].Substring(argPrefix.Length);

                    List<string> list = new List<string>(args);
                    list.RemoveAt(i);
                    args = list.ToArray();
                    return true;
                }
            }

            argSuffix = null;
            return false;
        }

        public static bool TryRemoveArg(ref string[] args, string argPrefix, out int argSuffix)
        {
            string suffix;
            if (TryRemoveArg(ref args, argPrefix, out suffix) && int.TryParse(suffix, out argSuffix)) {
                return true;
            }

            argSuffix = 0;
            return false;
        }

        public static void ReportProgress(string text, int progress = -1)
        {
            if (progress < 0) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("    ˙ ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(text);
            } else {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((progress + "%").PadLeft(5) + " ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(text);
            }
        }
    }
}

#else

public class App
{
    public static void Main()
    {
        throw new System.NotSupportedException("Multiplayer is not supported in this build!");
    }
}

#endif