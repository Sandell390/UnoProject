using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public static class PlayerAction
    {
        public static List<Card> playerCards { get; set; }

        public static string playerAction(string uno, out int card)
        {
            card = 0;
            Console.WriteLine("Your Cards:");
            //Show cards to player
            ShowPlayerCards();

            Console.WriteLine("Options: ");
            Console.WriteLine("Type 'p' for picking up a card");
            Console.WriteLine("Type 'd' for end round");

            CheckUno(uno);
            //Let the player choose card


            Console.WriteLine("Choose a card or an option: ");
            string playerChoice = Console.ReadLine();

            string command = "";

            if (playerChoice == "p") 
            {
                command = "cardPick";
            }
            else if (playerChoice == "d")
            {
                command = "turn";
            }
            else if (int.TryParse(playerChoice, out card))
            {
                command = "downCard";
            }
            else if (playerChoice == "u")
            {
                command = "uno";
            }


            return command;
        }

        public static void CheckUno(string uno)
        {

            if (uno == "uno")
            {
                Console.WriteLine("Type 'u' for call UNO");
            }
        }
        static void ShowPlayerCards()
        {

            foreach (var card in playerCards)
            {
                card.showCard();
            }
            Console.WriteLine();
            for (int i = 0; i < playerCards.Count; i++)
            {

                if (i >= 10) //If the card has 2 charaters 
                {
                    Console.Write($"  {i + 1} ");
                }
                else if (playerCards[i].extraSpace) //If the player have over 10 cards
                {
                    Console.Write($"   {i + 1}  ");
                }
                else //If the player have under 10 cards
                {
                    Console.Write($"  {i + 1}  ");
                }

            }
            Console.WriteLine();
        }

        public static int switchColor()
        {
            Console.WriteLine("Which color would you like?");

            Console.Write("1.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Red");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("2.");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Blue");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("3.");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Green");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("4.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Yellow");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("");

            int playerChoice = Convert.ToInt32(Console.ReadLine()) - 1;

            return playerChoice;
        }
    }
}
