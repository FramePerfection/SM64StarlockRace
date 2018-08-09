using System;
using System.Collections.Generic;
using System.Text;

namespace ServerBackend.Server
{
    public class Game
    {
        public string name = "";
        public List<ClientDescription> connectedClients = new List<ClientDescription>();
        public int maxClients = 25;
        public bool open = true;
        public Game(string name) { this.name = name; }
    }
}

namespace ServerBackend.Client
{
    public class Game
    {
        public string name = "";
        public int numClients = 0, maxClients;
        public bool open = true;
        public Game(string name, int numClients, int maxClients, bool open)
        {
            this.name = name;
            this.numClients = numClients;
            this.maxClients = maxClients;
            this.open = open;
        }
        public override string ToString()
        {
            return name + " (" + numClients + "/" + maxClients + ")" + (open ? "" : " (active)");
        }
    }
}
