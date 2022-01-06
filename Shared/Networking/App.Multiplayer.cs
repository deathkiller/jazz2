#if MULTIPLAYER && !SERVER

using System;
using System.Net;
using System.Runtime;
using Duality.Async;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.UI.Menu;
using Jazz2.Networking.Packets.Server;
using Jazz2.Storage;

namespace Jazz2.Game
{
    partial class App
    {
        private GameClient client;

        public void ConnectToServer(IPEndPoint endPoint)
        {
            // ToDo
            const string Token = "J²";

            if (client != null) {
                client.OnDisconnected -= OnPacketDisconnected;
                client.Close();
            }

            byte[] clientIdentifier = Preferences.Get<byte[]>("ClientIdentifier");
            if (clientIdentifier == null) {
                // Generate new client identifier
                Guid guid = Guid.NewGuid();
                clientIdentifier = guid.ToByteArray();
                Preferences.Set<byte[]>("ClientIdentifier", clientIdentifier);
                Preferences.Commit();
            }

            string userName = Preferences.Get<string>("UserName");
            if (userName == null) {
                userName = TryGetDefaultUserName();
                if (!string.IsNullOrEmpty(userName)) {
                    Preferences.Set<string>("UserName", userName);
                    Preferences.Commit();
                }
            }

            ControlScheme.IsSuspended = true;

            client = new GameClient(Token, clientIdentifier, userName);
            client.OnDisconnected += OnPacketDisconnected;
            client.AddCallback<LoadLevel>(OnPacketLoadLevel);
            client.Connect(endPoint);
        }

        /// <summary>
        /// Player was disconnected from server
        /// </summary>
        private void OnPacketDisconnected(string reason)
        {
            Await.NextAfterUpdate().OnCompleted(() => {
                string message = (string.IsNullOrEmpty(reason) ? "error/reconnecting" : "error/" + reason);
                ShowMainMenu(false).SwitchToSection(new SimpleMessageSection("error/disconnected".T(), message.T()));
                ControlScheme.IsSuspended = false;
            });
        }

        /// <summary>
        /// Player was requested to load a new level
        /// </summary>
        private void OnPacketLoadLevel(ref LoadLevel p)
        {
            int i = p.LevelName.IndexOf('/');
            if (i == -1) {
                return;
            }

            string serverName = p.ServerName;

            string episodeName = p.LevelName.Substring(0, i);
            string levelName = p.LevelName.Substring(i + 1);

            byte playerIndex = p.AssignedPlayerIndex;
            MultiplayerLevelType levelType = p.LevelType;

            Await.NextAfterUpdate().OnCompleted(() => {
                LevelInitialization levelInit = new LevelInitialization(episodeName, levelName, GameDifficulty.Multiplayer, true, false);

                Scene.Current.Dispose();
                Scene.SwitchTo(new MultiplayerLevelHandler(this, client, levelInit, levelType, playerIndex));

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                ControlScheme.IsSuspended = false;

                UpdateRichPresence(levelInit, serverName);
            });
        }
    }
}

#endif