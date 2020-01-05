using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace ServerBackend
{
    public delegate void ReceiveMessage(ClientDescription sender, byte[] buffer);
    public delegate byte[] GetBufferFunc();

    enum ServerStateMessage : byte
    {
        List = 0,
        Join = 1,
    }

    public class NetworkServer
    {
        public static IPAddress GetPublicIP()
        {
            try
            {
                string url = "http://checkip.dyndns.org";
                WebRequest req = WebRequest.Create(url);
                WebResponse resp = req.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                string response = sr.ReadToEnd().Trim();
                sr.Close();
                string[] a = response.Split(':');
                string a2 = a[1].Substring(1);
                string[] a3 = a2.Split('<');
                string a4 = a3[0];
                return IPAddress.Parse(a4);
            }
            catch
            {
                return null;
            }
        }

        public static IPAddress[] LocalIPAddresses(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return null;
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] arr = host.AddressList;
            int k = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].AddressFamily == addressFamily)
                    arr[k++] = arr[i];
            }
            Array.Resize(ref arr, k);
            return arr;
        }

        public delegate void GameCreated(Server.Game game);
        public event GameCreated gameCreated;

        const int MAX_GAMES = 100;

        public Dictionary<int, Server.Game> hostedGames = new Dictionary<int, Server.Game>();
        private TcpListener server;
        private List<ClientDescription> clients = new List<ClientDescription>();
        private BackgroundWorker listenerProcess;
        private float heartBeat = 5;

        public bool started = false;
        public string errorText = "";

        private Dictionary<ReceiveMessage, byte> ClientCallbackLookup = new Dictionary<ReceiveMessage, byte>();
        private Dictionary<byte, ReceiveMessage> ServerCallbacks = new Dictionary<byte, ReceiveMessage>();
        private Dictionary<ReceiveMessage, byte> ServerCallbackLookup = new Dictionary<ReceiveMessage, byte>();
        int gameID = 0;

        /// <summary>
        /// Maps a client callback to an identifier number.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="callback"></param>
        public void SetClientCallback(byte identifier, ReceiveMessage callback)
        {
            ClientCallbackLookup.Add(callback, identifier);
        }

        /// <summary>
        /// Sets a callback to execute when a message with the identifier reaches the server.
        /// </summary>
        /// <param name="identifier">Identifier number to react to</param>
        /// <param name="callback">The function to execute</param>
        public void SetServerCallback(byte identifier, ReceiveMessage callback)
        {
            ServerCallbackLookup.Add(callback, identifier);
            ServerCallbacks.Add(identifier, callback);
        }

        void UpdateServerState(ClientDescription sender, byte[] serverList) { throw new Exception("Servers should not be receing this type of message."); }
        void SetGame(ClientDescription sender, byte[] message)
        {
            sender.gameID = message[0];
            Server.Game g;
            if (hostedGames.TryGetValue(sender.gameID, out g))
            {
                if (!g.connectedClients.Contains(sender))
                    g.connectedClients.Add(sender);
            }
            UpdateGamesList();
            Send(sender, UpdateServerState, new byte[] { (byte)ServerStateMessage.Join });
        }

        public void UpdateGamesList()
        {
            Send(-1, UpdateServerState, GetServerList());
        }

        public void CreateGame(string name)
        {
            int startID = gameID;
            while (hostedGames.ContainsKey(gameID++))
            {
                if (startID == gameID)
                    throw new Exception("Maximum number of games reached.");
                gameID %= MAX_GAMES;
            }
            Server.Game newGame = new Server.Game(name);
            hostedGames[gameID] = newGame;
            UpdateGamesList();
            if (gameCreated != null) gameCreated.Invoke(newGame);
        }

        public NetworkServer(int port)
        {
            SetClientCallback(0, UpdateServerState);
            SetServerCallback(1, SetGame);
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                listenerProcess = new BackgroundWorker();
                listenerProcess.DoWork += serverListen;
                listenerProcess.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                errorText = ex.ToString();
            }
        }

        byte[] GetServerList()
        {
            MemoryStream str;
            BinaryWriter wr = new BinaryWriter(str = new MemoryStream());
            wr.Write((byte)ServerStateMessage.List);
            wr.Write(hostedGames.Count);
            foreach (KeyValuePair<int, ServerBackend.Server.Game> g in hostedGames)
            {
                wr.Write(g.Key);
                wr.Write(g.Value.name);
                wr.Write(g.Value.connectedClients.Count);
                wr.Write(g.Value.maxClients);
                wr.Write(g.Value.open);
            }
            wr.Flush();
            wr.Close();
            return str.GetBuffer();
        }

        private void serverListen(object sender, DoWorkEventArgs e)
        {
            server.Start();
            while (true)
            {
                List<ClientDescription> removals = new List<ClientDescription>();
                if (server.Pending())
                {
                    TcpClient newClient = server.AcceptTcpClient();
                    lock (clients)
                        clients.Add(new ClientDescription(newClient));
                    Send(-1, UpdateServerState, GetServerList());
                }
                lock (clients)
                {
                    foreach (ClientDescription serverClient in clients)
                    {
                        int available = serverClient.client.Available;
                        if (available > 0)
                        {
                            byte[] buffer = new byte[available];
                            serverClient.client.GetStream().Read(buffer, 0, available);
                            serverClient.ReadData(buffer);
                            foreach (ClientDescription targetClient in serverClient.gameID == -1 ? clients : hostedGames[serverClient.gameID].connectedClients)
                                try
                                {
                                    if (targetClient != serverClient)
                                    {
                                        NetworkStream stream = targetClient.client.GetStream();
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

                    foreach (ClientDescription removal in removals)
                    {
                        clients.Remove(removal);
                        hostedGames[removal.gameID].connectedClients.Remove(removal);
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }


        public void Update()
        {
            lock (clients)
            {
                foreach (ClientDescription c in clients)
                    while (c.receivedMessages.Count > 0)
                    {
                        byte[] message = c.receivedMessages.Dequeue();
                        if (message[0] == 0xFF) continue;

                        ReceiveMessage callback;
                        if (ServerCallbacks.TryGetValue(message[0], out callback))
                        {
                            byte[] _message = new byte[message.Length - 1];
                            Array.Copy(message, 1, _message, 0, _message.Length);
                            callback(c, _message);
                        }
                    }
            }
        }

        public void Send(int game, ReceiveMessage callback, byte[] buffer)
        {
            List<ClientDescription> removals = new List<ClientDescription>();
            lock (clients)
            {
                foreach (ClientDescription serverClient in (game == -1 ? clients : hostedGames[game].connectedClients))
                    try
                    {
                        NetworkStream stream = serverClient.client.GetStream();
                        stream.Write(BitConverter.GetBytes((short)buffer.Length), 0, 2);
                        stream.WriteByte(ClientCallbackLookup[callback]);
                        if (buffer.Length > 0) stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                    }
                    catch
                    {
                        removals.Add(serverClient);
                    }

                foreach (ClientDescription removal in removals)
                    clients.Remove(removal);
            }
        }
        public void Send(ClientDescription client, ReceiveMessage callback, byte[] buffer)
        {
            try
            {
                NetworkStream stream = client.client.GetStream();
                stream.Write(BitConverter.GetBytes((short)buffer.Length), 0, 2);
                stream.WriteByte(ClientCallbackLookup[callback]);
                if (buffer.Length > 0) stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
            catch
            {
                lock (clients)
                    clients.Remove(client);
            }
        }
    }
}