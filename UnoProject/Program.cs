using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            TcpListener listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();

            ConnectHandler.lobbies = new List<Lobby>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Connected");

                    using NetworkStream ns = tcpClient.GetStream();

                    byte[] bytes = new byte[1];

                    ns.Read(bytes,0,bytes.Length);

                    string message = Encoding.UTF8.GetString(bytes);

                    Console.WriteLine(message);

                    bytes = Encoding.UTF8.GetBytes("Connected");
                    await tcpClient.Client.SendAsync(bytes,SocketFlags.None);

                    if (message[0] == 'h') 
                    {
                        ConnectHandler.MakeLobby(tcpClient);
                    }
                    else if (message[0] == 's')
                    {
                        ConnectHandler.PutPlayerInLobby(tcpClient);
                    }

                }
                Console.WriteLine("Disconnected");
            });

            Console.ReadLine();

        }
    }
}
