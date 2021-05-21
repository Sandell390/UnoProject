using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    static class Connect
    {
        static TcpClient _client = new TcpClient();
        static bool hosting = false;
        public static IPAddress serverIP { get; set; }

        #region OG
        public static void HostLobby()
        {
            Connect:
            try
            {

                string lobbyName = string.Empty;
                do
                {
                    Console.WriteLine("Write Lobby Name (Max 40 character)");
                    lobbyName = Console.ReadLine();

                } while (lobbyName.Length > 40);

                _sendPacket(new Packet("lobbyName", lobbyName)).GetAwaiter().GetResult();

                hosting = true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Can not connect to the server... retrying ");
                Thread.Sleep(100);
                
                goto Connect;
            }
        }


        static void startLobby() 
        {
            while (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q && hosting)
            {
                _sendPacket(new Packet("start", "")).GetAwaiter().GetResult();
                hosting = false;
            }
        }
        #endregion

        public static bool Running { get; private set; }
        private static bool _clientRequestedDisconnect = false;

        // Messaging
        private static NetworkStream _msgStream = null;
        private static Dictionary<string, Func<string, List<SendLobby>, List<Card>, Task>> _commandHandlers = new Dictionary<string, Func<string, List<SendLobby>, List<Card>, Task>>();

        // Cleans up any leftover network resources
        private static void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Connects to the games server
        public static void ConnectToServer(string connectType, string name)
        {
            // Connect to the server
            try
            {
                _client.Connect(serverIP, 1234);   // Resolves DNS for us
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
                _commandHandlers["lobbies"] = _handleLobbies;
                _commandHandlers["winner"] = _handleWinner;
                _commandHandlers["cards"] = _handleCards;
                _commandHandlers["message"] = _handleMessage;
                _commandHandlers["failedStart"] = _handleFailedStart;
                //_commandHandlers["done"] = ;
                _commandHandlers["switchColor"] = _handleSwitchColor;

                Thread.Sleep(300);
                Task.Run(async () => await _sendPacket(new Packet(connectType, name)));

                if(connectType == "host")
                    HostLobby();

                Run();
            }
            else
            {
                // Nope...
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server");
            }
        }

        // Requests a disconnect, will send a "bye," message to the server
        // This should only be called by the user
        public static void Disconnect()
        {
            Console.WriteLine("Disconnecting from the server...");
            Running = false;
            _clientRequestedDisconnect = true;
            _sendPacket(new Packet("bye")).GetAwaiter().GetResult();
        }

        // Main loop for the Games Client
        public static void Run()
        {
            bool wasRunning = Running;

            // Listen for messages
            List<Task> tasks = new List<Task>();
            while (Running)
            {
                if(hosting)
                    startLobby();


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
        private async static Task _sendPacket(Packet packet)
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
            catch (Exception e) 
            {
                Console.WriteLine("Cant send packet");
                Console.WriteLine(e);
            }
        }

        // Checks for new incoming messages and handles them
        // This method will handle one Packet at a time, even if more than one is in the memory stream
        private async static Task _handleIncomingPackets()
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

                    //Console.WriteLine("[RECEIVED]\n{0}", packet);
                    // Dispatch it
                    try
                    {
                        await _commandHandlers[packet.Command](packet.Message, packet.Lobbies, packet.Cards);
                    }
                    catch (KeyNotFoundException) { }

                    
                }
            }
            catch (Exception) { }
        }

        #region Command Handlers
        private static Task _handleBye(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            // Print the message
            Console.WriteLine("The server is disconnecting us with this message:");
            Console.WriteLine(message);

            // Will start the disconnection process in Run()
            Running = false;
            return Task.FromResult(0);  // Task.CompletedTask exists in .NET v4.6
        }
        private static async Task _handleLobbies(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default) 
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

            Console.WriteLine();

            int chocie = 0;
            bool goodLobby = false;
            do
            {
                chocie = int.Parse(Console.ReadLine());

                if (!lobbies[chocie].started)
                {
                    goodLobby = true;
                }
                else
                {
                    Console.WriteLine("You cant join that lobby, try again");
                }

            } while (!goodLobby);
            while (!goodLobby)
            {
                
            }

            Packet resp = new Packet("lobbyName", chocie.ToString());

            await _sendPacket(resp);
        }
        private static Task _handleMessage(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            Console.Write(message);
            return Task.FromResult(0);
        }
        
        private async static Task _handleCards(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            Console.Clear();

            int index = message.IndexOf(';');
            string uno = message.Substring(index + 1, (message.Length - index) - 1);
            message = message.Remove(index, uno.Length + 1);

            Console.WriteLine("Played card: ");

            cards[0].showCard();

            Console.WriteLine();
            Console.WriteLine($"{message}'s cards: ");
            if (cards[1].CardType == Card.cardType.BLANK) 
            {
                for (int i = 1; i < cards.Count; i++)
                {
                    cards[i].showCard();
                }

                Console.WriteLine();
                Console.WriteLine("Wait");
            }
            else
            {
                PlayerAction.playerCards = new List<Card>();
                

                int card = 0;
                for (int i = 1; i < cards.Count; i++)
                {
                    PlayerAction.playerCards.Add(cards[i]);
                }
                string choice = PlayerAction.playerAction(uno, out card);

                Packet resp = new Packet(choice, card.ToString());
                await _sendPacket(resp);
            }
        }
        
        private static Task _handleFailedStart(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            Console.WriteLine(message);
            hosting = true;
            return Task.FromResult(0);
        }
       
        private async static Task _handleSwitchColor(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            int colorPick = PlayerAction.switchColor();
            Packet resp = new Packet("color", colorPick.ToString());
            await _sendPacket(resp);
        }
        
        private static Task _handleWinner(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            Console.Clear();
            Console.WriteLine(message);
            Running = false;
            return Task.FromResult(0);
        }
        /*
        private static Task _(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {

        }
        private static Task _(string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {

        }
        */
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
    }
}
