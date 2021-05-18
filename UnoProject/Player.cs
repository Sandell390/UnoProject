using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Server
{
    public class Player
    {
        public TcpClient client;

        public List<Card> playerDeck;
    }
}
