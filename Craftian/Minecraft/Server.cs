using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Craftian.Minecraft
{
    public class Server
    {
        public string Address { get; private set; }
        public string Name { get; private set; }
        public string Motd { get; private set; }
        public int Ping { get; private set; }
        public int PlayerCount { get; private set; }

        public Server(string address, string name, string motd, int ping, int playerCount)
        {
            Address = address;
            Name = name;
            Motd = motd;
            Ping = ping;
            PlayerCount = playerCount;
        }
    }
}
