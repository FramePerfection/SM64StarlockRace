using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;

namespace ServerBackend
{
    public class NetworkClient
    {
        static Dictionary<int, Client.Game> DecodeServerList(byte[] buffer, int position)
        {
            BinaryReader rd = new BinaryReader(new MemoryStream(buffer));
            rd.BaseStream.Position = position;
            Dictionary<int, Client.Game> output = new Dictionary<int, Client.Game>();
            int numGames = rd.ReadInt32();
            for (int i = 0; i < numGames; i++)
                output[rd.ReadInt32()] = new Client.Game(rd.ReadString(), rd.ReadInt32(), rd.ReadInt32(), rd.ReadBoolean());
            return output;
        }

        public event EventHandler ServerListChanged, JoinSuccess;

        private TcpClient client;
        private NetworkStream stream;
        private BackgroundWorker listenerProcess;
        private float heartBeat = 5;
        private byte[] receiveBuffer = new byte[2];
        private int receivePosition = -3;
        public Dictionary<int, Client.Game> hostedGames = new Dictionary<int, Client.Game>();
        int game = -1;
        public int connectedClients { get { return game == -1 ? 0 : hostedGames[game].numClients; } }

        public string ErrorText = "";

        private Dictionary<byte, ReceiveMessage> ClientCallbacks = new Dictionary<byte, ReceiveMessage>();
        private Dictionary<ReceiveMessage, byte> ClientCallbackLookup = new Dictionary<ReceiveMessage, byte>();
        private Dictionary<byte, ReceiveMessage> ServerCallbacks = new Dictionary<byte, ReceiveMessage>();
        private Dictionary<ReceiveMessage, byte> ServerCallbackLookup = new Dictionary<ReceiveMessage, byte>();
        public void SetClientCallback(byte identifier, ReceiveMessage callback)
        {
            ClientCallbackLookup.Add(callback, identifier);
            ClientCallbacks.Add(identifier, callback);
        }
        public void SetServerCallback(byte identifier, ReceiveMessage callback)
        {
            ServerCallbackLookup.Add(callback, identifier);
            ServerCallbacks.Add(identifier, callback);
        }
        private Queue<byte[]> receivedMessages = new Queue<byte[]>();

        public static void SetGame(ClientDescription sender, byte[] message) { throw new Exception("Clients should not be receiving this type of message."); }
        public NetworkClient(string host, int port)
        {
            try
            {
                client = new TcpClient();
                client.NoDelay = true;
                client.Connect(host, port);
                stream = client.GetStream();
                SetClientCallback(0, UpdateServerState);
                SetServerCallback(1, SetGame);
                listenerProcess = new BackgroundWorker();
                listenerProcess.DoWork += clientListen;
                listenerProcess.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ErrorText = ex.ToString();
            }
        }

        public void Disconnect()
        {
            client.Client.Disconnect(false);
        }

        public void SelectGame(byte game)
        {
            this.game = game;
            Send(ServerBackend.NetworkClient.SetGame, new byte[] {  game });
        }

        public void update(float fTime)
        {
            while (receivedMessages.Count > 0)
            {
                byte[] message = receivedMessages.Dequeue();
                if (message[0] == 0xFF) continue;

                ReceiveMessage callback;
                if (ClientCallbacks.TryGetValue(message[0], out callback))
                {
                    byte[] _message = new byte[message.Length - 1];
                    Array.Copy(message, 1, _message, 0, message.Length - 1);
                    callback(null, _message);
                }
            }
            if (client != null)
            {
                heartBeat -= fTime;
                if (heartBeat <= 0)
                {
                    try
                    {
                        stream.Write(BitConverter.GetBytes((short)0), 0, 2);
                        stream.WriteByte(0xFF);
                        stream.Flush();
                    }
                    catch (Exception ex)
                    {
                        ErrorText = ex.ToString();
                    }
                    heartBeat += 5;
                }
            }
        }

        public void Send(ReceiveMessage callback, byte[] buffer)
        {
            if (client != null)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(BitConverter.GetBytes((short)buffer.Length), 0, 2);
                    stream.WriteByte(ServerCallbackLookup[callback]);
                    if (buffer.Length > 0) stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    ErrorText = ex.ToString();
                }
            }
        }

        private void UpdateServerState(ClientDescription sender, byte[] buffer)
        {
            if (buffer[0] == (byte)ServerStateMessage.Join && JoinSuccess != null)
                JoinSuccess(this, null);
            else if (buffer[0] == (byte)ServerStateMessage.List)
            {
                hostedGames = DecodeServerList(buffer, 1);
                if (ServerListChanged != null)
                    ServerListChanged(this, null);
            }
        }

        private void clientListen(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!client.Connected) return;
                int available = client.Available;
                if (available > 0)
                {
                    byte[] buffer = new byte[available];
                    client.GetStream().Read(buffer, 0, available);
                    readData(buffer);
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        private void readData(byte[] newBytes)
        {
            int readPosition = 0;
        newMessage:
            while (receivePosition < -1 && readPosition < newBytes.Length)
            {
                receiveBuffer[receivePosition + 3] = newBytes[readPosition++];
                receivePosition++;
            }
            if (receivePosition == -1)
            {
                receivePosition = 0;
                receiveBuffer = new byte[BitConverter.ToInt16(receiveBuffer, 0) + 1];
            }
            if (receivePosition >= 0)
            {
                while (receivePosition >= 0 && readPosition < newBytes.Length)
                {
                    receiveBuffer[receivePosition++] = newBytes[readPosition++];
                    if (receivePosition == receiveBuffer.Length)
                    {
                        receivePosition = -3;
                        receivedMessages.Enqueue(receiveBuffer);
                        receiveBuffer = new byte[2];
                        goto newMessage;
                    }
                }
            }
        }
    }

}