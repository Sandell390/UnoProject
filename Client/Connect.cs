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
        static TcpClient client = new TcpClient();
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
        public static async Task<string> Waiting() 
        {
            string message = string.Empty;

            byte[] buffer = new byte[30];
            await client.Client.ReceiveAsync(buffer, SocketFlags.None);

            message = Encoding.UTF8.GetString(buffer);

            return message;
        
        }

    }
}
