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
        /*
        #region MIT

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
        #endregion
        */
        // Listens for new incoming connections
        private static TcpListener _listener;


        private static List<Player> players = new List<Player>();
        private static List<Lobby> lobbies = new List<Lobby>();


        private static List<Thread> _gameThreads = new List<Thread>();

        public static bool Running { get; private set; }

        // Shutsdown the server if its running
        public static void Shutdown()
        {
            if (Running)
            {
                Running = false;
                Console.WriteLine("Shutting down the Game(s) Server...");
            }
        }

        // The main loop for the games server
        public static void Run()
        {
            _listener = new TcpListener(IPAddress.Any,1234);
            Console.WriteLine("Starting the server.");
            Console.WriteLine("Press Ctrl-C to shutdown the server at any time.");

            // Start running the server
            _listener.Start();
            Running = true;
            List<Task> newConnectionTasks = new List<Task>();

            Console.WriteLine("Waiting for incommming connections...");

            while (Running)
            {
                // Handle any new clients
                if (_listener.Pending())
                    newConnectionTasks.Add(_handleNewConnection());

                // Take a small nap
                Thread.Sleep(10);
            }

            // In the chance a client connected but we exited the loop, give them 1 second to finish
            Task.WaitAll(newConnectionTasks.ToArray(), 1000);

            // Shutdown all of the threads, regardless if they are done or not
            foreach (Thread thread in _gameThreads)
                thread.Abort();

            // Cleanup our resources
            _listener.Stop();

            // Info
            Console.WriteLine("The server has been shut down.");
        }

        private static void HostLobby(Player host) 
        {
            Console.WriteLine($"{host.name} are making a lobby");

            Packet lobbyName = null;

            while (lobbyName == null)
            {
                lobbyName = ReceivePacket(host.client).GetAwaiter().GetResult();
                Thread.Sleep(20);
            }

            if (lobbyName.Command == "lobbyName") 
            {
                
                Lobby newLobby = new Lobby(lobbyName.Message, host);
                lobbies.Add(newLobby);
                newLobby.players.Add(host);
                newLobby.Host = host;
                Thread gameThread = new Thread(new ThreadStart(newLobby.Run));
                gameThread.Start();
                _gameThreads.Add(gameThread);

                Console.WriteLine(newLobby.lobbyName + " has been created");
            }

        }

        private static async Task PutPlayerInPlayer(Player newPlayer) 
        {
            Console.WriteLine($"{newPlayer.name} is joining a lobby");

            List<SendLobby> sendLobbies = new List<SendLobby>();

            foreach (Lobby lobby in lobbies)
            {
                sendLobbies.Add(new SendLobby() { name = lobby.lobbyName, NumberOfPlayers = lobby.players.Count, started = lobby.isGameStarted});
            }

            await SendPacket(newPlayer.client, new Packet("lobbies", "", sendLobbies));
            

            Packet choiceOfLobby = null;
            while (choiceOfLobby == null)
            {
                choiceOfLobby = ReceivePacket(newPlayer.client).GetAwaiter().GetResult();
                Thread.Sleep(10);
            }

            if (choiceOfLobby.Command == "lobbyName") 
            {
                int choice = int.Parse(choiceOfLobby.Message);

                lobbies[choice].AddPlayerToLobby(newPlayer);
            }
        }

        // Awaits for a new connection and then adds them to the waiting lobby
        private static async Task _handleNewConnection()
        {
            // Get the new client using a Future
            TcpClient newClient = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("New connection from {0}.", newClient.Client.RemoteEndPoint);

            // Store them and put them in the waiting lobby
            Packet clientType = null;

            while (clientType == null)
            {
                clientType = ReceivePacket(newClient).GetAwaiter().GetResult();
                Console.WriteLine("Waiting on client");
                Thread.Sleep(50);
            }

            Player player = new Player(newClient, clientType.Message);

            if (clientType.Command == "host") 
            {
                //Host lobby
                HostLobby(player);
            }
            else if (clientType.Command == "join")
            {
                //Send lobbies and let the player choice a lobby
                await Task.Run(async () => await PutPlayerInPlayer(player)); 
            }
        }

        // Will attempt to gracefully disconnect a TcpClient
        // This should be use for clients that may be in a game, or the waiting lobby
        public static void DisconnectClient(TcpClient client, string message = "")
        {

            // If there wasn't a message set, use the default "Goodbye."
            if (message == "")
                message = "Goodbye.";

            // Send the "bye," message
            Task byePacket = SendPacket(client, new Packet("winner", message));


            // Give the client some time to send and proccess the graceful disconnect
            Thread.Sleep(100);

            // Cleanup resources on our end
            byePacket.GetAwaiter().GetResult();
            _cleanupClient(client);
        }

        #region Packet Transmission Methods
        // Sends a packet to a client asynchronously
        public static async Task SendPacket(TcpClient client, Packet packet)
        {
            try
            {
                // convert JSON to buffer and its length to a 16 bit unsigned integer buffer
                byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
                byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                // Join the buffers
                byte[] msgBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                lengthBuffer.CopyTo(msgBuffer, 0);
                jsonBuffer.CopyTo(msgBuffer, lengthBuffer.Length);

                // Send the packet
                await client.GetStream().WriteAsync(msgBuffer, 0, msgBuffer.Length);

                Console.WriteLine("[SENT]\n{0}", packet);
            }
            catch (Exception e)
            {
                // There was an issue is sending
                Console.WriteLine("There was an issue receiving a packet.");
                Console.WriteLine("Reason: {0}", e.Message);
            }
        }

        // Will get a single packet from a TcpClient
        // Will return null if there isn't any data available or some other
        // issue getting data from the client
        public static async Task<Packet> ReceivePacket(TcpClient client)
        {
            Packet packet = null;
            try
            {
                // First check there is data available
                if (client.Available == 0)
                    return null;

                NetworkStream msgStream = client.GetStream();

                // There must be some incoming data, the first two bytes are the size of the Packet
                byte[] lengthBuffer = new byte[2];
                await msgStream.ReadAsync(lengthBuffer, 0, 2);
                ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

                // Now read that many bytes from what's left in the stream, it must be the Packet
                byte[] jsonBuffer = new byte[packetByteSize];
                await msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

                // Convert it into a packet datatype
                string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                packet = Packet.FromJson(jsonString);

                Console.WriteLine("[RECEIVED]\n{0}", packet);
            }
            catch (Exception e)
            {
                // There was an issue in receiving
                Console.WriteLine("There was an issue sending a packet to {0}.", client.Client.RemoteEndPoint);
                Console.WriteLine("Reason: {0}", e.Message);
            }

            return packet;
        }
        #endregion // Packet Transmission Methods

        #region TcpClient Helper Methods
        // Checks if a client has disconnected ungracefully
        // Adapted from: http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        public static bool IsDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException)
            {
                // We got a socket error, assume it's disconnected
                return true;
            }
        }

        // cleans up resources for a TcpClient and closes it
        public static void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close();     // Close network stream
            client.Close();                 // Close client
        }
        #endregion // TcpClient Helper Methods
    }
}
