using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace Server
{
    public static class ConnectHandler
    {
        public static List<Lobby> lobbies;

        public static void MakeLobby(TcpClient client) 
        {
            Player player = new Player() { client = client };

            Lobby lobby = new Lobby();

            byte[] lobbyName = new byte[40];

            client.Client.Receive(lobbyName, SocketFlags.None);

            List<byte> test = new List<byte>();
            for (int i = 0; i < lobbyName.Length; i++)
            {
                if (lobbyName[i] != 0)
                {
                    test.Add(lobbyName[i]);
                }
            }

            string name = Encoding.UTF8.GetString(test.ToArray());

            lobby.lobbyName = name;

            Console.WriteLine($"Lobby name set: {lobby.lobbyName}");

            lobbies.Add(lobby);
            lobby.players.Add(player);


        }
        
        public static void PutPlayerInLobby(TcpClient client) 
        {
            Console.WriteLine("Lobby");
            Player player = new Player() { client = client };

            string lobbynamies = string.Empty;

            foreach (Lobby lobby in lobbies)
            {
                lobbynamies += lobby.lobbyName + ";";
            }

            byte[] bytes = BitConverter.GetBytes(lobbynamies.Length);
            client.Client.Send(bytes, SocketFlags.None);
            
            bytes = new byte[lobbynamies.Length];

            client.Client.Receive(new byte[1]);

            bytes = Encoding.UTF8.GetBytes(lobbynamies);
            client.Client.Send(bytes, SocketFlags.None);

            bytes = new byte[5];
            client.Client.Receive(bytes);

            int playerchoice = BitConverter.ToInt32(bytes);

            lobbies[playerchoice].players.Add(player);

            lobbies[playerchoice].JoinMessage();
        }

    }
}
