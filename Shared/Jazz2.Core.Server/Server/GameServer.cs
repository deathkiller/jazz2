#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Duality;
using Jazz2.Game;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Server
{
    public partial class GameServer : IDisposable
    {
        private const string Token = "J²";
        private const string ServerListUrl = "http://deat.tk/jazz2/servers";

        private object sync = new object();

        private string serverName;
        private int port;
        private int maxPlayers;

        private Thread threadGame, threadPublishToServerList;
        private ServerConnection server;
        private byte neededMajor, neededMinor, neededBuild;
        private string customHostname;
        private bool allowOnlyUniqueClients;
        private DateTime startedTime;

        private Action<NetIncomingMessage, bool>[] callbacks;

        private HashSet<string> bannedClientIds = new HashSet<string>();
        private HashSet<IPEndPoint> bannedEndPoints = new HashSet<IPEndPoint>();

        public int LoadMs => lastGameLoadMs;
        public int PlayerCount => players.Count;
        public int MaxPlayers => maxPlayers;
        public string CurrentLevel => currentLevel;
        public MultiplayerLevelType CurrentLevelType => currentLevelType;
        public Dictionary<NetConnection, PlayerClient> Players => players;
        public DateTime StartedTime => startedTime;

        public object Synchronization => sync;

        public string Name
        {
            get
            {
                return serverName;
            }
            set
            {
                serverName = value;
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

        public void Run(int port, string serverName, int maxPlayers, bool isPrivate, bool enableUPnP, byte neededMajor, byte neededMinor, byte neededBuild)
        {
            this.port = port;
            this.serverName = serverName;
            this.maxPlayers = maxPlayers;

            this.neededMajor = neededMajor;
            this.neededMinor = neededMinor;
            this.neededBuild = neededBuild;

            ContentResolver.Current.Init();
            ContentResolver.Current.InitPostWindow();

            callbacks = new Action<NetIncomingMessage, bool>[byte.MaxValue + 1];
            players = new Dictionary<NetConnection, PlayerClient>();
            playersByIndex = new PlayerClient[256];
            playerConnections = new List<NetConnection>();

            server = new ServerConnection(Token, port, maxPlayers, !isPrivate && enableUPnP);
            server.MessageReceived += OnMessageReceived;
            server.DiscoveryRequest += OnDiscoveryRequest;
            server.ClientConnected += OnClientConnected;
            server.ClientStatusChanged += OnClientStatusChanged;

            RegisterPacketCallbacks();

            Log.Write(LogType.Info, "Endpoints:");
            Log.PushIndent();

            foreach (var address in server.LocalIPAddresses) {
                Log.Write(LogType.Verbose, address + ":" + port + (NetUtility.IsAddressPrivate(address) ? " [Private]" : ""));
            }

            if (server.PublicIPAddresses != null) {
                foreach (var address in server.PublicIPAddresses) {
                    Log.Write(LogType.Verbose, address + ":" + port + (NetUtility.IsAddressPrivate(address) ? " [Private]" : "") + " [UPnP]");
                }
            }

            if (customHostname != null) {
                Log.Write(LogType.Verbose, customHostname + ":" + port + " [Custom]");
            }

            Log.PopIndent();

            Log.Write(LogType.Info, "Unique Identifier: " + server.UniqueIdentifier);
            Log.Write(LogType.Info, "Server Name: " + serverName);
            Log.Write(LogType.Info, "Players: 0/" + maxPlayers);

            // Create game loop
            threadGame = new Thread(OnGameLoopThread);
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
                    StringBuilder sb = new StringBuilder(256);
                    sb.Append("0\n"); // Flags

                    bool isFirst = true;
                    foreach (IPAddress address in server.LocalIPAddresses) {
                        if (NetUtility.IsAddressPrivate(address)) {
                            continue;
                        }

                        if (isFirst) {
                            isFirst = false;
                        } else {
                            sb.Append("|");
                        }

                        sb.Append(address)
                            .Append(":")
                            .Append(port);
                    }
                    if (server.PublicIPAddresses != null) {
                        foreach (IPAddress address in server.PublicIPAddresses) {
                            if (NetUtility.IsAddressPrivate(address)) {
                                continue;
                            }

                            if (isFirst) {
                                isFirst = false;
                            } else {
                                sb.Append("|");
                            }

                            sb.Append(address)
                                .Append(":")
                                .Append(port);
                        }
                    }
                    if (customHostname != null) {
                        if (isFirst) {
                            isFirst = false;
                        } else {
                            sb.Append("|");
                        }

                        sb.Append(customHostname)
                            .Append(":")
                            .Append(port);
                    }

                    if (isFirst) {
                        // No public IP address found
                        Log.Write(LogType.Warning, "Server cannot be published to server list - no public IP address found!");

                        threadPublishToServerList = null;
                        break;
                    } else {
                        string currentVersion = Game.App.AssemblyVersion;

                        long uptimeSecs = (long)(TimeZoneInfo.ConvertTimeToUtc(startedTime) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                        string levelFriendlyName = (levelHandler?.LevelFriendlyName ?? currentLevel);

                        sb.Append('\n')
                            .Append(server.UniqueIdentifier)
                            .Append('\n')
                            .Append(currentVersion)
                            .Append('\n')
                            .Append(players.Count)
                            .Append('\n')
                            .Append(maxPlayers)
                            .Append('\n')
                            .Append(uptimeSecs)
                            .Append('\n')
                            .Append(lastGameLoadMs)
                            .Append('\n')
                            .Append((int)currentLevelType)
                            .Append('\n')
                            .Append(currentLevel)
                            .Append('\n')
                            .Append(levelFriendlyName)
                            .Append('\n')
                            .Append(serverName);

                        string data = "publish=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()))
                                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                        using (WebClient client = new WebClient()) {
                            client.Encoding = Encoding.UTF8;
                            client.Headers["User-Agent"] = "Jazz2 Resurrection (Server)";
                            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                            string content = client.UploadString(ServerListUrl, data);
                            // No need to parse full JSON response
                            if (content.Contains("\"r\":false")) {
                                if (content.Contains("\"e\":1")) {
                                    Log.Write(LogType.Warning, "Cannot publish server with private IP addresses!");
                                } else if (content.Contains("\"e\":2")) {
                                    Log.Write(LogType.Error, "Access to server list is denied! Try it later.");
                                } else {
                                    Log.Write(LogType.Warning, "Server cannot be published to server list!");
                                }

                                threadPublishToServerList = null;
                                break;
                            } else if (!isPublished) {
                                Log.Write(LogType.Error, "Server was successfully published again!");
                            }

                            isPublished = true;
                        }
                    }
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

        public bool BanPlayer(byte playerIndex)
        {
            lock (sync) {
                foreach (var player in players) {
                    if (player.Value.Index == playerIndex) {
                        bannedClientIds.Add(ClientIdentifierToString(player.Value.ClientIdentifier));

                        IPEndPoint endPoint = player.Value.Connection?.RemoteEndPoint;
                        if (endPoint != null) {
                            bannedEndPoints.Add(endPoint);
                        }
                        return true;
                    }
                }
            }

            return false;
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
                        SendToActivePlayers(new PlayerTakeDamage {
                            Index = playerIndex,
                            HealthAfter = 0
                        }, 7, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
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
                    SendToActivePlayers(new PlayerTakeDamage {
                        Index = player.Value.Index,
                        HealthAfter = 0
                    }, 7, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }
            }
        }

        public void ShowMessageToPlayer(byte playerIndex, string text)
        {
            SendToPlayerByIndex(new ShowMessage {
                Text = text
            }, 64, playerIndex, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        public void ShowMessageToAllPlayers(string text)
        {
            SendToActivePlayers(new ShowMessage {
                Text = text
            }, 64, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
        }

        public PlayerClient FindPlayerByUserName(string userName)
        {
            foreach (var player in players) {
                if (player.Value.UserName == userName) {
                    return player.Value;
                }
            }

            return null;
        }

        private static string ClientIdentifierToString(byte[] clientIdentifier)
        {
            if (clientIdentifier == null) {
                return "";
            }

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < clientIdentifier.Length; i++) {
                sb.Append(clientIdentifier[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static string GenerateRandomUserName()
        {
            string[] prefixes = {
                "be", "gi", "gla", "le", "ti", "xe"
            };
            string[] syllables = {
                "blarg", "fay", "izen", "mon", "rash", "ray", "shi", "zag"
            };
            string[] suffixes = {
                "kor", "li", "son", "ssen"
            };

            StringBuilder sb = new StringBuilder(32);

            if (MathF.Rnd.NextBool()) {
                sb.Append(prefixes[MathF.Rnd.Next(prefixes.Length)]);
            }

            int syllableCount = 2 + MathF.Rnd.Next(3);
            for (int i = 0; i < syllableCount; i++) {
                string syllable = syllables[MathF.Rnd.Next(syllables.Length)];
                if (i == 0) {
                    syllable = char.ToUpperInvariant(syllable[0]) + syllable.Substring(1);
                }

                sb.Append(syllable);
            }

            if (MathF.Rnd.NextBool()) {
                sb.Append(suffixes[MathF.Rnd.Next(suffixes.Length)]);
            }

            if (MathF.Rnd.NextBool()) {
                sb.Append(MathF.Rnd.Next(100).ToString("N2"));
            }

            return sb.ToString();
        }
    }
}

#endif