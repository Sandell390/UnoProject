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
            Console.CancelKeyPress += InterruptHandler;

            ConnectHandler.Run();

            Console.ReadLine();

        }

        public static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            ConnectHandler.Shutdown();
        }

    }
}
