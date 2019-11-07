#if !SERVER

using System;
using System.Net;
using System.Runtime;
using Duality.Async;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Server;
using Jazz2.Storage;

namespace Jazz2.Game
{
    partial class App
    {
        private NetworkHandler net;

        public void ConnectToServer(IPEndPoint endPoint)
        {
            // ToDo
            const string token = "J²";

            if (net != null) {
                net.OnDisconnected -= OnNetworkDisconnected;
                net.Close();
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

            net = new NetworkHandler(token, clientIdentifier, userName);
            net.OnDisconnected += OnNetworkDisconnected;
            net.AddCallback<LoadLevel>(OnNetworkLoadLevel);
            net.Connect(endPoint);
        }

        /// <summary>
        /// Player was disconnected from server
        /// </summary>
        private void OnNetworkDisconnected()
        {
            Await.NextAfterUpdate().OnCompleted(() => {
                ShowMainMenu(false);
            });
        }

        /// <summary>
        /// Player was requested to load a new level
        /// </summary>
        private void OnNetworkLoadLevel(ref LoadLevel p)
        {
            string episodeName;
            string levelName = p.LevelName;
            int i = levelName.IndexOf('/');
            if (i != -1) {
                episodeName = levelName.Substring(0, i);
                levelName = levelName.Substring(i + 1);
            } else {
                return;
            }

            byte playerIndex = p.AssignedPlayerIndex;

            Await.NextAfterUpdate().OnCompleted(() => {
                LevelInitialization levelInit = new LevelInitialization(episodeName, levelName, GameDifficulty.Multiplayer);

                Scene.Current.Dispose();
                Scene.SwitchTo(new MultiplayerLevelHandler(this, net, levelInit, playerIndex));

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                UpdateRichPresence(levelInit);
            });
        }
    }
}

#endif