using System;
using System.Net;

namespace Client
{
    class Program
    {
        public static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Perform a graceful disconnect
            args.Cancel = true;
            Connect.Disconnect();
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Enter ip: ");

            string ip = Console.ReadLine();

            Connect.serverIP = IPAddress.Parse(ip);

            Console.CancelKeyPress += InterruptHandler;

            Menu.StartMenu();

            

        }
    }
}
