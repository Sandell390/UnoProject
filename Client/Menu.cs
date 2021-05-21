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
            Console.Clear();
            Console.WriteLine("YOOOOOOOO Hvad så, Velkommen til Uno MP");

            Console.WriteLine("Skriv dit username");
            string name = Console.ReadLine();

            Console.WriteLine("1. Search for Lobby");
            Console.WriteLine("2. Host Lobby");
            Console.WriteLine("3. Exit Game, noob");

            //GET GOOD
            int choice = int.Parse(Console.ReadLine());

            switch (choice)
            {
                case 1:
                    Connect.ConnectToServer("join", name);
                    break;
                case 2:
                    Connect.ConnectToServer("host", name);
                    break;
                default:
                    break;
            }
        }


    }
}
