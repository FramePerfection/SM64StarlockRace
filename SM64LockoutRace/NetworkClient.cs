using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;

namespace SM64LockoutRace
{
    public class NetworkClient
    {
        private TcpClient client;
        private TcpListener server;
        private int clientCount = -1;
        private List<TcpClient> serverClients = new List<TcpClient>();
        private NetworkStream stream;
        private BackgroundWorker listenerProcess;
        private float heartBeat = 5;
        private byte[] receiveBuffer = new byte[2];
        private int receivePosition = -3;

        public bool started = false;
        public string ErrorText = "";

        public delegate void ReceiveMessage(byte[] buffer);
        public delegate byte[] GetBufferFunc();
        public GetBufferFunc GetWelcomeBuffer;
        public ReceiveMessage WelcomeMessage;

        public int numClients()
        {
            if (server != null)
                return serverClients.Count + 1;
            return clientCount;
        }

        public void SetMessageListener(byte identifier, ReceiveMessage callback)
        {
            CallbackLookup.Add(callback, identifier);
            MessageCallbacks.Add(identifier, callback);
        }
        private Dictionary<byte, ReceiveMessage> MessageCallbacks = new Dictionary<byte, ReceiveMessage>();
        private Dictionary<ReceiveMessage, byte> CallbackLookup = new Dictionary<ReceiveMessage, byte>();
        private Queue<byte[]> receivedMessages = new Queue<byte[]>();

        public NetworkClient(string host, int port)
        {
            try
            {
                client = new TcpClient();
                client.NoDelay = true;
                client.Connect(host, port);
                stream = client.GetStream();
                SetMessageListener(0, updateClientCount);
                listenerProcess = new BackgroundWorker();
                listenerProcess.DoWork += clientListen;
                listenerProcess.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ErrorText = ex.ToString();
            }
        }
        private void updateClientCount(byte[] buffer)
        {
            clientCount = BitConverter.ToInt16(buffer, 0) + 1;
        }

        public NetworkClient(int port)
        {
            try
            {
                server = new TcpListener(new System.Net.IPAddress(new byte[] {127, 0, 0, 1}), port);
                listenerProcess = new BackgroundWorker();
                listenerProcess.DoWork += serverListen;
                listenerProcess.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ErrorText = ex.ToString();
            }
        }

        private void serverListen(object sender, DoWorkEventArgs e)
        {
            server.Start();
            while (true)
            {
                List<TcpClient> removals = new List<TcpClient>();
                if (server.Pending() )
                {
                    if (!started || Main.instance.Game.Mode != 0)
                    {
                        TcpClient newClient = server.AcceptTcpClient();
                        serverClients.Add(newClient);
                        short count = (short)serverClients.Count;

                        if (started && GetWelcomeBuffer != null && WelcomeMessage != null)
                        {
                            try
                            {
                                byte[] welcomeBuffer = GetWelcomeBuffer();
                                NetworkStream stream = newClient.GetStream();
                                stream.Write(BitConverter.GetBytes((short)welcomeBuffer.Length), 0, 2);
                                stream.WriteByte(CallbackLookup[WelcomeMessage ]);
                                if (welcomeBuffer.Length > 0) stream.Write(welcomeBuffer, 0, welcomeBuffer.Length);
                                stream.Flush();
                            }
                            catch
                            {
                                removals.Add(newClient);
                            }
                        }

                        foreach (TcpClient targetClient in serverClients)
                            try
                            {
                                NetworkStream stream = targetClient.GetStream();
                                stream.Write(BitConverter.GetBytes((short)2), 0, 2);
                                stream.WriteByte(0);
                                stream.Write(BitConverter.GetBytes(count), 0, 2);
                                stream.Flush();
                            }
                            catch
                            {
                                removals.Add(targetClient);
                            }
                    }
                }

                foreach (TcpClient serverClient in serverClients)
                {
                    int available = serverClient.Available;
                    if (available > 0)
                    {
                        byte[] buffer = new byte[available];
                        serverClient.GetStream().Read(buffer, 0, available);
                        readData(buffer);

                        foreach (TcpClient targetClient in serverClients)
                            try
                            {
                                if (targetClient != serverClient)
                                {
                                    NetworkStream stream = targetClient.GetStream();
                                    stream.Write(buffer, 0, buffer.Length);
                                    stream.Flush();
                                }
                            }
                            catch
                            {
                                removals.Add(targetClient);
                            }
                    }
                }

                foreach (TcpClient removal in removals)
                    serverClients.Remove(removal);

                System.Threading.Thread.Sleep(10);
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
            if (receivePosition >= 0) {
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

        public void update(float fTime)
        {
            while (receivedMessages.Count > 0)
            {
                byte[] message = receivedMessages.Dequeue();
                if (message[0] == 0xFF) continue;

                ReceiveMessage callback = MessageCallbacks[message[0]];
                if (callback != null)
                {
                    byte[] _message = new byte[message.Length - 1];
                    Array.Copy(message, 1, _message, 0, message.Length - 1);
                    callback(_message);
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

        public void send(ReceiveMessage callback, byte[] buffer)
        {
            if (client != null)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(BitConverter.GetBytes((short)buffer.Length), 0, 2);
                    stream.WriteByte(CallbackLookup[callback]);
                    if (buffer.Length > 0) stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    ErrorText = ex.ToString();
                }
            }
            else
            {
                List<TcpClient> removals = new List<TcpClient>();
                foreach (TcpClient serverClient in serverClients)
                try
                {
                    NetworkStream stream = serverClient.GetStream();
                    stream.Write(BitConverter.GetBytes((short)buffer.Length), 0, 2);
                    stream.WriteByte(CallbackLookup[callback]);
                    if (buffer.Length > 0) stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
                catch
                {
                    removals.Add(serverClient);
                }

                foreach (TcpClient removal in removals)
                    serverClients.Remove(removal);
            }
        }
    }
}
