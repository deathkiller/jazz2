using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Jazz2.Discord
{
    public class DiscordRpcClient : IDisposable
    {
        private const int StateDisconnected = 0;
        private const int StateConnecting = 1;
        private const int StateConnected = 2;

        private string applicationId;
        private int processId;

        private ManagedNamedPipeClient namedPipe;
        private Queue<PipeFrame> sendQueue = new Queue<PipeFrame>();
        private AutoResetEvent queueUpdatedEvent = new AutoResetEvent(false);
        private Thread thread;

        private int state;
        private bool aborting;
        private bool shutdown;
        private int nonce;

        private string userName;

        public string UserName => userName;

        public DiscordRpcClient(string applicationId)
        {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }

            this.applicationId = applicationId;
            this.processId = Process.GetCurrentProcess().Id;

            this.namedPipe = new ManagedNamedPipeClient();
            this.namedPipe.FrameReceived += OnFrameReceived;
        }

        public bool Connect()
        {
            if (thread != null) {
                return false;
            }

            if (state != StateDisconnected) {
                return false;
            }

            if (aborting) {
                return false;
            }

            sendQueue.Clear();

            thread = new Thread(OnBackgroundThread);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();

            return true;
        }

        public void Dispose()
        {
            if (thread == null || aborting) {
                return;
            }

            aborting = true;
            queueUpdatedEvent.Set();
        }


        public void SetRichPresence(RichPresence richPresence)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"cmd\":\"SET_ACTIVITY\",\"nonce\":");
            sb.Append(GetNextNonce());
            sb.Append(",\"args\":{\"pid\":");
            sb.Append(processId);
            sb.Append(",\"activity\":{");

            if (!string.IsNullOrEmpty(richPresence.State)) {
                sb.Append("\"state\":\"");
                sb.Append(richPresence.State);
                sb.Append("\",");
            }

            if (!string.IsNullOrEmpty(richPresence.Details)) {
                sb.Append("\"details\":\"");
                sb.Append(richPresence.Details);
                sb.Append("\",");
            }

            sb.Append("\"assets\":{");

            bool isFirst = true;
            if (!string.IsNullOrEmpty(richPresence.LargeImage)) {
                isFirst = false;

                sb.Append("\"large_image\":\"");
                sb.Append(richPresence.LargeImage);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(richPresence.LargeImageTooltip)) {
                if (isFirst) {
                    isFirst = false;
                } else {
                    sb.Append(",");
                }

                sb.Append("\"large_text\":\"");
                sb.Append(richPresence.LargeImageTooltip);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(richPresence.SmallImage)) {
                if (isFirst) {
                    isFirst = false;
                } else {
                    sb.Append(",");
                }

                sb.Append("\"small_image\":\"");
                sb.Append(richPresence.SmallImage);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(richPresence.SmallImageTooltip)) {
                if (isFirst) {
                    isFirst = false;
                } else {
                    sb.Append(",");
                }

                sb.Append("\"small_text\":\"");
                sb.Append(richPresence.SmallImageTooltip);
                sb.Append("\"");
            }

            sb.Append("}}}}");

            lock (sendQueue) {
                sendQueue.Enqueue(new PipeFrame(1 /*Frame*/, sb.ToString()));
            }

            queueUpdatedEvent.Set();
        }

        private void OnBackgroundThread()
        {
            int tries = 1;

            while (!aborting && !shutdown) {
                try {
                    if (namedPipe == null) {
                        aborting = true;
                        return;
                    }

                    if (namedPipe.Connect(-1)) {

                        if (!namedPipe.WriteFrame(new PipeFrame(0 /*Handshake*/, "{\"v\":1,\"client_id\":\"" + applicationId + "\"}"))) {
                            break;
                        }

                        tries = 1;
                        state = StateConnecting;

                        PipeFrame frame;
                        bool continueReading = true;
                        while (continueReading && !aborting && !shutdown && namedPipe.IsConnected) {
                            if (namedPipe.ReadFrame(out frame)) {
                                switch (frame.Opcode) {
                                    default:
                                    case 0: // Handshake - invalid frame
                                    case 2: // Close
                                        continueReading = false;
                                        break;

                                    case 3: // Ping
                                        frame.Opcode = 4; // Pong
                                        namedPipe.WriteFrame(frame);
                                        break;

                                    case 4: // Pong
                                        break;

                                    case 1: // Frame
                                        if (shutdown || frame.Data == null) {
                                            break;
                                        }

                                        if (state == StateConnecting && frame.Message.Contains("\"cmd\":\"DISPATCH\"") && frame.Message.Contains("\"evt\":\"READY\"")) {
                                            state = StateConnected;

                                            int idx = frame.Message.IndexOf("\"username\":\"");
                                            if (idx != -1) {
                                                idx += 12;

                                                int idx2 = frame.Message.IndexOf("\"", idx);
                                                if (idx2 != -1) {
                                                    userName = frame.Message.Substring(idx, idx2 - idx);
                                                }
                                            }
                                        }

                                        // Other types of messages are not relevant for now
                                        break;
                                }
                            }

                            if (!aborting && namedPipe.IsConnected) {
                                ProcessCommandQueue();

                                queueUpdatedEvent.WaitOne(300);
                            }
                        }
                    } else if (tries < 20) {
                        tries++;
                    }

                    if (!aborting && !shutdown) {
                        Thread.Sleep(tries * 30000);
                    }
                } catch (Exception e) {
                    // Nothing to do...
                } finally {
                    if (namedPipe.IsConnected) {
                        namedPipe.Close();
                    }

                    state = StateDisconnected;
                }
            }

            if (namedPipe != null) {
                namedPipe.Dispose();
            }
        }

        private void OnFrameReceived()
        {
            queueUpdatedEvent.Set();
        }

        private int GetNextNonce()
        {
            nonce++;
            return nonce;
        }

        private void ProcessCommandQueue()
        {
            if (state != StateConnected) {
                return;
            }

            bool continueWriting = true;
            while (continueWriting && namedPipe.IsConnected) {
                PipeFrame frame;

                lock (sendQueue) {
                    continueWriting = (sendQueue.Count > 0);
                    if (!continueWriting) {
                        break;
                    }

                    frame = sendQueue.Peek();
                }

                if (shutdown) {
                    continueWriting = false;
                }

                if (aborting) {
                    lock (sendQueue) {
                        sendQueue.Dequeue();
                    }
                } else if (namedPipe.WriteFrame(frame)) {
                    lock (sendQueue) {
                        sendQueue.Dequeue();
                    }
                } else {
                    break;
                }
            }
        }
    }
}