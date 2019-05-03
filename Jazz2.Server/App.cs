#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2
{
    public static class App
    {
        public static string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0) {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (!string.IsNullOrEmpty(titleAttribute.Title)) {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                Version v = Assembly.GetEntryAssembly().GetName().Version;
                return v.Major.ToString(CultureInfo.InvariantCulture) + "." + v.Minor.ToString(CultureInfo.InvariantCulture) + (v.Build != 0 ? "." + v.Build.ToString(CultureInfo.InvariantCulture) : "");
            }
        }
    }
}

namespace Jazz2.Server
{
    internal static partial class App
    {
        private const string token = "J²";

        private static string name;
        private static int port, maxPlayers;

        private static Thread threadGame;
        private static ServerConnection server;
        private static byte neededMajor, neededMinor, neededBuild;

        private static Dictionary<byte, Action<NetIncomingMessage, bool>> callbacks;
        private static Dictionary<NetConnection, Player> players;
        private static List<NetConnection> playerConnections;

        private static int lastGameLoadMs;

        private static string currentLevel = "unknown/battle2";
        private static byte lastPlayerIndex;

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

            if (!TryRemoveArg(ref args, "/port:", out port)) {
                port = 10666;
            }

            if (!TryRemoveArg(ref args, "/name:", out name) || string.IsNullOrWhiteSpace(name)) {
                name = "Unnamed server";
            }

            if (!TryRemoveArg(ref args, "/players:", out maxPlayers)) {
                maxPlayers = 64;
            }

            // Initialization
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            neededMajor = (byte)v.Major;
            neededMinor = (byte)v.Minor;
            neededBuild = (byte)v.Build;

            Log.Write(LogType.Info, "Starting server...");
            Log.PushIndent();
            Log.Write(LogType.Info, "Port: " + port);
            Log.Write(LogType.Info, "Server Name: " + name);
            Log.Write(LogType.Info, "Max. Players: " + maxPlayers);

            callbacks = new Dictionary<byte, Action<NetIncomingMessage, bool>>();
            players = new Dictionary<NetConnection, Player>();
            playerConnections = new List<NetConnection>();

            server = new ServerConnection(token, port, maxPlayers);
            server.MessageReceived += OnMessageReceived;
            server.DiscoveryRequest += OnDiscoveryRequest;
            server.ClientConnected += OnClientConnected;
            server.ClientStatusChanged += OnClientStatusChanged;

            RegisterCallback<LevelReady>(OnLevelReady);
            RegisterCallback<UpdateSelf>(OnUpdateSelf);

            Log.PopIndent();

            // Create game loop
            threadGame = new Thread(OnGameLoop);
            threadGame.IsBackground = true;
            threadGame.Start();

            Log.Write(LogType.Info, "Ready!");
            Console.WriteLine();

            // Processing of console commands
            ProcessConsoleCommands();

            // Shutdown
            Console.WriteLine();
            Log.Write(LogType.Info, "Closing...");

            server.ClientStatusChanged -= OnClientStatusChanged;
            server.ClientConnected -= OnClientConnected;
            server.MessageReceived -= OnMessageReceived;
            server.DiscoveryRequest -= OnDiscoveryRequest;

            //ClearCallbacks();

            Thread threadGame_ = threadGame;
            threadGame = null;
            threadGame_.Join();

            server.Close();
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
                        Log.Write(LogType.Info, "Server Load: " + lastGameLoadMs + " ms");
                        Log.Write(LogType.Info, "Players: " + players.Count + "/" + maxPlayers);
                        Log.Write(LogType.Info, "Current Level: " + currentLevel);
                        break;
                    }

                    case "set": {
                        string key = GetPartFromInput(ref input);
                        string value = GetPartFromInput(ref input);
                        switch (key) {
                            case "level": {
                                currentLevel = value;

                                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                                    Send(new LoadLevel {
                                        LevelName = currentLevel,
                                        AssignedPlayerIndex = pair.Value.Index
                                    }, 64, pair.Key, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                                }

                                playerConnections.Clear();

                                Log.Write(LogType.Warning, "OK!");
                                break;
                            }

                            default: {
                                Log.Write(LogType.Warning, "Error!");
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