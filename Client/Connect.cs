using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Client
{
    static class Connect
    {
        static TcpClient _client = new TcpClient();

        #region OG
        public static void JoinLobby() 
        {
            Connect:
            try
            {
                ConnectToServer("s");

                
                byte[] bytes = new byte[10];
                client.Client.Receive(bytes);

                int buffer = BitConverter.ToInt32(bytes,0);
                client.Client.Send(new byte[1]);

                bytes = new byte[buffer];
                client.Client.Receive(bytes, 0, bytes.Length,SocketFlags.None);

                string names = Encoding.UTF8.GetString(bytes);

                List<string> listOfLobbies = new List<string>();

                int previousPoint = 0;

                for (int i = 0; i < names.Length; i++)
                {
                    previousPoint = names.IndexOf(";");

                    listOfLobbies.Add(names.Substring(0, previousPoint));

                    names = names.Replace(names, names.Remove(0, previousPoint + 1));
                }

                Console.WriteLine("Choice a lobby: ");

                for (int i = 0; i < listOfLobbies.Count; i++)
                {
                    Console.WriteLine($"[{i}] {listOfLobbies[i]}");
                }

                int choice = int.Parse(Console.ReadLine());

                bytes = BitConverter.GetBytes(choice);
                client.Client.Send(bytes);



            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Can not connect to the server... retrying ");

                goto Connect;
            }
        }

        public static void HostLobby()
        {
            Connect:
            try
            {
                ConnectToServer("h");

                string lobbyName = string.Empty;
                do
                {
                    Console.WriteLine("Write Lobby Name (Max 40 character)");
                    lobbyName = Console.ReadLine();

                } while (lobbyName.Length > 40);

                byte[] lobbyNameBytes = Encoding.UTF8.GetBytes(lobbyName);

                client.Client.Send(lobbyNameBytes);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Can not connect to the server... retrying ");
                
                goto Connect;
            }
        }
        static void ConnectToServer(string connectType) 
        {
            client.Connect(IPAddress.Loopback, 1234);

            byte[] test = new byte[1];

            test = Encoding.UTF8.GetBytes(connectType);

            client.Client.Send(test);

            NetworkStream ns = client.GetStream();
            byte[] bytes = new byte[10];

            ns.Read(bytes, 0, bytes.Length);

            ns.Flush();

            string message = Encoding.UTF8.GetString(bytes);

            Console.WriteLine(message + " To server");
        }
        public static string Waiting() 
        {
            string message = string.Empty;

            byte[] buffer = new byte[30];
            client.Client.Receive(buffer, 0, buffer.Length, SocketFlags.Peek);

            if (buffer[0] == 0) 
            {
                return "";
            }

            message = Encoding.UTF8.GetString(buffer);

            return message;
        
        }
        #endregion

        public static bool Running { get; private set; }
        private static bool _clientRequestedDisconnect = false;

        // Messaging
        private static NetworkStream _msgStream = null;
        private static Dictionary<string, Func<string, Task>> _commandHandlers = new Dictionary<string, Func<string, Task>>();

        // Cleans up any leftover network resources
        private static void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Connects to the games server
        public static void ConnectToServer()
        {
            // Connect to the server
            try
            {
                _client.Connect(IPAddress.Loopback, 1234);   // Resolves DNS for us
            }
            catch (SocketException se)
            {
                Console.WriteLine("[ERROR] {0}", se.Message);
            }

            // check that we've connected
            if (_client.Connected)
            {
                // Connected!
                Console.WriteLine("Connected to the server at {0}.", _client.Client.RemoteEndPoint);
                Running = true;

                // Get the message stream
                _msgStream = _client.GetStream();

                // Hook up some packet command handlers
                _commandHandlers["bye"] = _handleBye;
                _commandHandlers["lobbies"] = ;
                _commandHandlers["winner"] = ;
                _commandHandlers["joinedP"] = ;
                _commandHandlers["failedStart"] = ;
                _commandHandlers["cards"] = ;
                _commandHandlers["ups"] = ;
                _commandHandlers["failed"] = ;
                _commandHandlers["done"] = ;
                _commandHandlers["switchColor"] = ;
            }
            else
            {
                // Nope...
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}:{1}.", ServerAddress, Port);
            }
        }

        // Requests a disconnect, will send a "bye," message to the server
        // This should only be called by the user
        public void Disconnect()
        {
            Console.WriteLine("Disconnecting from the server...");
            Running = false;
            _clientRequestedDisconnect = true;
            _sendPacket(new Packet("bye")).GetAwaiter().GetResult();
        }

        // Main loop for the Games Client
        public void Run()
        {
            bool wasRunning = Running;

            // Listen for messages
            List<Task> tasks = new List<Task>();
            while (Running)
            {
                // Check for new packets
                tasks.Add(_handleIncomingPackets());

                // Use less CPU
                Thread.Sleep(10);

                // Make sure that we didn't have a graceless disconnect
                if (_isDisconnected(_client) && !_clientRequestedDisconnect)
                {
                    Running = false;
                    Console.WriteLine("The server has disconnected from us ungracefully.\n:[");
                }
            }

            // Just incase we have anymore packets, give them one second to be processed
            Task.WaitAll(tasks.ToArray(), 1000);

            // Cleanup
            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }

        // Sends packets to the server asynchronously
        private async Task _sendPacket(Packet packet)
        {
            try
            {                // convert JSON to buffer and its length to a 16 bit unsigned integer buffer
                byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
                byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                // Join the buffers
                byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                lengthBuffer.CopyTo(packetBuffer, 0);
                jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);

                // Send the packet
                await _msgStream.WriteAsync(packetBuffer, 0, packetBuffer.Length);

                //Console.WriteLine("[SENT]\n{0}", packet);
            }
            catch (Exception) { }
        }

        // Checks for new incoming messages and handles them
        // This method will handle one Packet at a time, even if more than one is in the memory stream
        private async Task _handleIncomingPackets()
        {
            try
            {
                // Check for new incomding messages
                if (_client.Available > 0)
                {
                    // There must be some incoming data, the first two bytes are the size of the Packet
                    byte[] lengthBuffer = new byte[2];
                    await _msgStream.ReadAsync(lengthBuffer, 0, 2);
                    ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

                    // Now read that many bytes from what's left in the stream, it must be the Packet
                    byte[] jsonBuffer = new byte[packetByteSize];
                    await _msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

                    // Convert it into a packet datatype
                    string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                    Packet packet = Packet.FromJson(jsonString);

                    // Dispatch it
                    try
                    {
                        await _commandHandlers[packet.Command](packet.Message);
                    }
                    catch (KeyNotFoundException) { }

                    //Console.WriteLine("[RECEIVED]\n{0}", packet);
                }
            }
            catch (Exception) { }
        }

        #region Command Handlers
        private static Task _handleBye(string message)
        {
            // Print the message
            Console.WriteLine("The server is disconnecting us with this message:");
            Console.WriteLine(message);

            // Will start the disconnection process in Run()
            Running = false;
            return Task.FromResult(0);  // Task.CompletedTask exists in .NET v4.6
        }
        private static Task _handleLobbies(List<SendLobby> lobbies) 
        {
            int count = 0;
            foreach (SendLobby lobby in lobbies)
            {
                Console.Write($"[{count}] ");

                if (lobby.started) 
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.Write($"{lobby.name} | ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Players: {lobby.NumberOfPlayers}");

                count++;
            }


        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        private static Task _()
        {

        }
        #endregion // Command Handlers

        #region TcpClient Helper Methods
        // Checks if a client has disconnected ungracefully
        // Adapted from: http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        private static bool _isDisconnected(TcpClient client)
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
        #endregion // TcpClient Helper Methods




        #region Program Execution
        public static TcpGamesClient gamesClient;

        public static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Perform a graceful disconnect
            args.Cancel = true;
            gamesClient?.Disconnect();
        }

        public static void Main(string[] args)
        {
            // Setup the Games Client
            string host = "localhost";//args[0].Trim();
            int port = 6000;//int.Parse(args[1].Trim());
            gamesClient = new TcpGamesClient(host, port);

            // Add a handler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            // Try to connecct & interact with the server
            gamesClient.Connect();
            gamesClient.Run();

        }
        #endregion // Program Execution
    }
}
