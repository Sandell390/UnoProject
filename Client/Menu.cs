using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    static class Menu
    {
        public static void StartMenu() 
        {
            Console.WriteLine("YOOOOOOOO Hvad så, Velkommen til Uno MP");

            Console.WriteLine("1. Search for Lobby");
            Console.WriteLine("2. Host Lobby");
            Console.WriteLine("3. Exit Game, noob");

            //GET GOOD
            int choice = int.Parse(Console.ReadLine());

            switch (choice)
            {
                case 1:
                    Connect.JoinLobby();
                    break;
                case 2:
                    Connect.HostLobby();
                    break;
                default:
                    break;
            }
        }

        public static void WaitingToGameStart() 
        {
            string message = string.Empty;

            while (message != "start")
            {
                message = Connect.Waiting();

                if (message != "" && message != "start") 
                {
                    Console.WriteLine(message);
                    message = string.Empty;
                }
                
            }
            Console.WriteLine(message);
            Console.ReadLine();
            //Console.Clear();
        }


    }
}
