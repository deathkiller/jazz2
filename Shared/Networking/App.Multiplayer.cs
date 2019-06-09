using System;
using System.Net;
using System.Threading;
using Duality;
using Duality.Resources;
using Jazz2.Game.Multiplayer;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Server;
using Jazz2.Storage;

namespace Jazz2.Game
{
    public partial class App
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

            net = new NetworkHandler(token, clientIdentifier);
            net.OnDisconnected += OnNetworkDisconnected;
            net.RegisterCallback<LoadLevel>(OnNetworkLoadLevel);
            net.Connect(endPoint);
        }

        private void OnNetworkDisconnected()
        {
            DispatchToMainThread(delegate {
                ShowMainMenu(false);
            });
        }

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

            DispatchToMainThread(delegate {
                Scene.Current.Dispose();

                NetworkLevelHandler handler = new NetworkLevelHandler(this, net,
                    new LevelInitialization(episodeName, levelName, GameDifficulty.Multiplayer),
                    playerIndex);

                Scene.SwitchTo(handler);
            });
        }


        public void DispatchToMainThread(Action action)
        {
            DualityApp.DisposeLater(new ActionDisposable(action));
        }

        private class ActionDisposable : IDisposable
        {
            private Action action;

            public ActionDisposable(Action action)
            {
                this.action = action;
            }

            void IDisposable.Dispose()
            {
                Interlocked.Exchange(ref action, null)();
            }
        }
    }
}