#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Duality;
using Duality.IO;
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
        private int maxPlayers, minPlayers;

        private Thread threadGame, threadPublishToServerList;
        private ServerConnection server;
        private byte neededMajor, neededMinor, neededBuild;
        private string customHostname;
        private bool allowOnlyUniqueClients;
        private DateTime startedTime;

        private Action<NetIncomingMessage, bool>[] callbacks;

        private HashSet<string> bannedClientIds = new HashSet<string>();
        private HashSet<IPEndPoint> bannedEndPoints = new HashSet<IPEndPoint>();

        public int LastFrameTime => lastFrameTime;
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

        public void Run(string configPath, int port, string serverName, int minPlayers, int maxPlayers, bool isPrivate, bool enableUPnP, byte neededMajor, byte neededMinor, byte neededBuild)
        {
            this.port = port;
            this.serverName = serverName;
            this.minPlayers = minPlayers;
            this.maxPlayers = maxPlayers;

            this.neededMajor = neededMajor;
            this.neededMinor = neededMinor;
            this.neededBuild = neededBuild;

            ContentResolver.Current.Init();
            ContentResolver.Current.InitPostWindow();

            callbacks = new Action<NetIncomingMessage, bool>[byte.MaxValue + 1];
            players = new Dictionary<NetConnection, PlayerClient>();
            playersByIndex = new PlayerClient[byte.MaxValue + 1];
            playerConnections = new List<NetConnection>();

            LoadServerConfig(configPath);

            if (this.port <= 0) {
                this.port = 10666;
            }
            if (this.minPlayers <= 0) {
                this.minPlayers = 2;
            }
            if (this.maxPlayers <= 0) {
                this.maxPlayers = 64;
            }

            server = new ServerConnection(Token, this.port, this.maxPlayers, !isPrivate && enableUPnP);
            server.MessageReceived += OnMessageReceived;
            server.DiscoveryRequest += OnDiscoveryRequest;
            server.ClientConnected += OnClientConnected;
            server.ClientStatusChanged += OnClientStatusChanged;

            RegisterPacketCallbacks();

            Log.Write(LogType.Info, "Endpoints:");
            Log.PushIndent();

            foreach (var address in server.LocalIPAddresses) {
                Log.Write(LogType.Verbose, address + ":" + this.port + (NetUtility.IsAddressPrivate(address) ? " [Private]" : ""));
            }

            if (server.PublicIPAddresses != null) {
                foreach (var address in server.PublicIPAddresses) {
                    Log.Write(LogType.Verbose, address + ":" + this.port + (NetUtility.IsAddressPrivate(address) ? " [Private]" : "") + " [UPnP]");
                }
            }

            if (customHostname != null) {
                Log.Write(LogType.Verbose, customHostname + ":" + this.port + " [Custom]");
            }

            Log.PopIndent();

            Log.Write(LogType.Info, "Unique Identifier: " + server.UniqueIdentifier);
            Log.Write(LogType.Info, "Server Name: " + this.serverName);
            Log.Write(LogType.Info, "Players: 0/" + this.maxPlayers);

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
                            .Append(lastFrameTime)
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

                        using (WebClient http = new WebClient()) {
                            http.Encoding = Encoding.UTF8;
                            http.Headers[HttpRequestHeader.UserAgent] = "Jazz2 Resurrection (Server)";
                            http.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                            string content = http.UploadString(ServerListUrl, data);
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

        public bool LoadServerConfig(string path)
        {
            if (string.IsNullOrEmpty(path) || File.Exists(path)) {
                return false;
            }

            Log.Write(LogType.Info, "Loading server configuration \"" + path + "\"...");
            Log.PushIndent();

            try {
                ServerConfigJson json;
                using (Stream s = FileOp.Open(path, FileAccessMode.Read)) {
                    json = ContentResolver.Current.ParseJson<ServerConfigJson>(s);
                }

                if (!string.IsNullOrEmpty(json.ServerName)) {
                    this.serverName = json.ServerName;
                    Log.Write(LogType.Verbose, "Server name was set to \"" + this.serverName + "\".");
                }

                if (json.MinPlayers > 0) {
                    this.minPlayers = json.MinPlayers;
                    Log.Write(LogType.Verbose, "Min. number of players was set to " + this.minPlayers + ".");
                }

                if (json.MaxPlayers > 0) {
                    if (server == null) {
                        this.maxPlayers = json.MaxPlayers;
                        Log.Write(LogType.Verbose, "Max. number of players was set to " + this.maxPlayers + ".");
                    } else {
                        Log.Write(LogType.Error, "Cannot set max. number of players of running server.");
                    }
                }

                if (json.Port > 0) {
                    if (server == null) {
                        this.port = json.Port;
                        Log.Write(LogType.Verbose, "Server port was set to " + this.port + ".");
                    } else {
                        Log.Write(LogType.Error, "Cannot set server port of running server.");
                    }
                }

                if (json.Playlist != null && json.Playlist.Count > 0) {
                    List<PlaylistItem> playlist = new List<PlaylistItem>();
                    for (int i = 0; i < json.Playlist.Count; i++) {
                        string levelName = json.Playlist[i].LevelName;
                        if (string.IsNullOrEmpty(levelName)) {
                            continue;
                        }

                        MultiplayerLevelType levelType = json.Playlist[i].LevelType;

                        int goalCount;
                        switch (levelType) {
                            case MultiplayerLevelType.Battle:
                            case MultiplayerLevelType.TeamBattle:
                                goalCount = json.Playlist[i].TotalKills; break;
                            case MultiplayerLevelType.Race:
                                goalCount = json.Playlist[i].TotalLaps; break;
                            case MultiplayerLevelType.TreasureHunt:
                                goalCount = json.Playlist[i].TotalGems; break;

                            default: {
                                Log.Write(LogType.Warning, "Level type " + levelType + " is not supported yet. Skipping.");
                                continue;
                            }
                        }

                        playlist.Add(new PlaylistItem {
                            LevelName = levelName,
                            LevelType = levelType,
                            GoalCount = goalCount,
                            PlayerHealth = json.Playlist[i].PlayerHealth
                        });
                    }

                    activePlaylist = playlist;
                    activePlaylistRandom = json.PlaylistRandom;

                    Log.Write(LogType.Info, "Loaded playlist with " + playlist.Count + " levels");

                    if (server != null) {
                        ChangeLevelFromPlaylist(0);
                    }
                }

                Log.PopIndent();

                return true;
            } catch (Exception ex) {

                Log.Write(LogType.Error, "Cannot parse server configuration: " + ex);
                Log.PopIndent();

                return false;
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
                "Be", "Gi", "Gla", "Le", "Ti", "Xe"
            };
            string[] syllables = {
                "blarg", "fay", "izen", "mon", "rash", "ray", "shi", "zag"
            };
            string[] suffixes = {
                "kor", "li", "son", "ssen"
            };

            StringBuilder sb = new StringBuilder(32);

            bool hasPrefix = MathF.Rnd.NextBool();
            if (hasPrefix) {
                sb.Append(prefixes[MathF.Rnd.Next(prefixes.Length)]);
            }

            int syllableCount = 1 + MathF.Rnd.Next(3);
            for (int i = 0; i < syllableCount; i++) {
                string syllable = syllables[MathF.Rnd.Next(syllables.Length)];
                if (i == 0 && !hasPrefix) {
                    syllable = char.ToUpperInvariant(syllable[0]) + syllable.Substring(1);
                }

                sb.Append(syllable);
            }

            if (MathF.Rnd.NextBool()) {
                sb.Append(suffixes[MathF.Rnd.Next(suffixes.Length)]);
            }

            if (MathF.Rnd.NextBool()) {
                sb.Append((10 + MathF.Rnd.Next(90)).ToString("N0"));
            }

            return sb.ToString();
        }
    }
}

#endif