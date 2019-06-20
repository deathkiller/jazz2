#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Server
{
    public partial class GameServer : IDisposable
    {
        private const string Token = "J²";
        private const string ServerListUrl = "http://deat.tk/jazz2/servers";

        private object sync = new object();

        private string name;
        private int port;
        private int maxPlayers;

        private Thread threadGame, threadPublishToServerList;
        private ServerConnection server;
        private byte neededMajor, neededMinor, neededBuild;
        private string customHostname;
        private bool allowOnlyUniqueClients = true;
        private DateTime startedTime;

        private Dictionary<byte, Action<NetIncomingMessage, bool>> callbacks;

        public int LoadMs => lastGameLoadMs;
        public int PlayerCount => players.Count;
        public int MaxPlayers => maxPlayers;
        public string CurrentLevel => currentLevel;
        public Dictionary<NetConnection, Player> Players => players;
        public DateTime StartedTime => startedTime;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public bool AllowOnlyUniqueClients
        {
            get
            {
                return allowOnlyUniqueClients;
            }
            set
            {
                allowOnlyUniqueClients = value;
            }
        }

        public byte SpawnedPlayerHealth
        {
            get
            {
                return playerHealth;
            }
            set
            {
                playerHealth = value;
            }
        }

        public void Run(int port, string name, int maxPlayers, bool isPrivate, bool enableUPnP, byte neededMajor, byte neededMinor, byte neededBuild)
        {
            this.port = port;
            this.name = name;
            this.maxPlayers = maxPlayers;

            this.neededMajor = neededMajor;
            this.neededMinor = neededMinor;
            this.neededBuild = neededBuild;

            callbacks = new Dictionary<byte, Action<NetIncomingMessage, bool>>();
            players = new Dictionary<NetConnection, Player>();
            playerConnections = new List<NetConnection>();

            remotableActors = new Dictionary<int, RemotableActor>();

            server = new ServerConnection(Token, port, maxPlayers, !isPrivate && enableUPnP);
            server.MessageReceived += OnMessageReceived;
            server.DiscoveryRequest += OnDiscoveryRequest;
            server.ClientConnected += OnClientConnected;
            server.ClientStatusChanged += OnClientStatusChanged;

            RegisterPacketCallbacks();

            Log.Write(LogType.Info, "Endpoint: " + server.LocalIPAddress + ":" + port + (server.PublicIPAddress != null ? " / " + server.PublicIPAddress + ":" + port : "") + (customHostname != null ? " / " + customHostname + ":" + port : ""));
            Log.Write(LogType.Info, "Server Name: " + name);
            Log.Write(LogType.Info, "Players: 0/" + maxPlayers);

            // Create game loop
            threadGame = new Thread(OnGameLoop);
            threadGame.IsBackground = true;
            threadGame.Start();

            startedTime = DateTime.Now;

            // Publish to server list
            if (!isPrivate) {
                Log.Write(LogType.Info, "Publishing to server list...");

                threadPublishToServerList = new Thread(OnPublishToServerList);
                threadPublishToServerList.IsBackground = true;
                threadPublishToServerList.Start();
            }
        }

        public void Dispose()
        {
            if (server == null) {
                return;
            }

            server.ClientStatusChanged -= OnClientStatusChanged;
            server.ClientConnected -= OnClientConnected;
            server.MessageReceived -= OnMessageReceived;
            server.DiscoveryRequest -= OnDiscoveryRequest;

            //ClearCallbacks();

            Thread threadGame_ = threadGame;
            threadGame = null;
            threadGame_.Join();

            server.Close();
            server = null;
        }

        public void OverrideHostname(string hostname)
        {
            customHostname = hostname;
        }

        private void OnPublishToServerList()
        {
            bool isPublished = true;

            // Wait 15 seconds before the first publishing on server list
            Thread.Sleep(15000);

            while (threadPublishToServerList != null) {
                try {
                    string currentVersion = Game.App.AssemblyVersion;

                    string endpoint = (customHostname ?? (server.PublicIPAddress ?? server.LocalIPAddress).ToString()) + ":" + port;

                    long uptimeSecs = (long)(TimeZoneInfo.ConvertTimeToUtc(startedTime) -  new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

                    string dataString = "0\n" + endpoint + "\n" + currentVersion + "\n" + players.Count + "\n" + maxPlayers + "\n" + uptimeSecs + "\n" + lastGameLoadMs + "\n" + (int)currentLevelType + "\n" + currentLevel + "\n" + currentLevelFriendlyName + "\n" + name;
                    string data = "publish=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(dataString))
                                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers["User-Agent"] = Game.App.AssemblyTitle;
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    string content = client.UploadString(ServerListUrl, data);
                    if (content.Contains("\"r\":false")) {
                        if (content.Contains("\"e\":1")) {
                            Log.Write(LogType.Warning, "Cannot publish server with private IP address (" + endpoint + ")!");
                        } else if (content.Contains("\"e\":2")) {
                            Log.Write(LogType.Error, "Access to server list is denied! Try it later.");
                        } else {
                            Log.Write(LogType.Warning, "Server cannot be published to server list!");
                        }
                        return;
                    } else if (!isPublished) {
                        Log.Write(LogType.Error, "Server was successfully published again!");
                    }

                    isPublished = true;
                } catch (Exception ex) {
                    // Try it again later
                    if (isPublished) {
                        isPublished = false;
                        Log.Write(LogType.Error, "Server list is unreachable! " + ex.Message);
                    }
                }

                Thread.Sleep(300000); // 5 minutes
            }
        }

        public bool KickPlayer(byte playerIndex)
        {
            lock (sync) {
                foreach (var player in players) {
                    if (player.Value.Index == playerIndex) {
                        player.Key.Disconnect("kicked");
                        return true;
                    }
                }
            }

            return false;
        }

        public void KickAllPlayers()
        {
            lock (sync) {
                foreach (var player in players) {
                    player.Key.Disconnect("kicked");
                }
            }
        }

        public bool KillPlayer(byte playerIndex)
        {
            lock (sync) {
                foreach (var player in players) {
                    if (player.Value.Index == playerIndex) {
                        Send(new DecreasePlayerHealth {
                            Index = playerIndex,
                            Amount = byte.MaxValue
                        }, 3, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                        return true;
                    }
                }
            }

            return false;
        }

        public void KillAllPlayers()
        {
            lock (sync) {
                foreach (var player in players) {
                    Send(new DecreasePlayerHealth {
                        Index = player.Value.Index,
                        Amount = byte.MaxValue
                    }, 3, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                }
            }
        }

        public static string PlayerNameToConsole(Player player)
        {
            if (ConsoleUtils.SupportsUnicode) {
                string line = "";
                int playerIndex = player.Index;
                do {
                    int digit = playerIndex % 10;
                    playerIndex /= 10;
                    line = (char)((int)'₀' + digit) + line;
                } while (playerIndex > 0);
                return "℘" + line;
            } else {
                return "#" + player.Index;
            }
        }
    }
}

#endif