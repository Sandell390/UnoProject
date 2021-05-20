using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace Server
{
    public class Lobby
    {
        public bool isGameStarted = false;
        public string lobbyName { get; set;}

        public Player Host { get; set; }
        public List<Player> players { get; set; }
        public List<Card> playedDeck { get; set; }
        public List<Card> currentDeck { get; set; }
        public List<Card> ActionDeck { get; set; }
        public Card currentCard { get; set; }
        public Player winner { get; set; }
        public int stackCardAmount { get; set; }
        public bool stackCard { get; set; }

        Random random;

        public Lobby(string _lobbyName, Player _host) 
        {
            players = new List<Player>();
            playedDeck = new List<Card>();
            currentDeck = new List<Card>();
            ActionDeck = new List<Card>();
            Host = _host;
            lobbyName = _lobbyName;
        }

        public void AddPlayerToLobby(Player newPlayer) 
        {
            foreach (Player player in players)
            {
                string msg = $"{newPlayer.name} has joined the game, Get ready to be destroyed";
                ConnectHandler.SendPacket(player.client, new Packet("joinedP", msg, new List<Card>())).GetAwaiter().GetResult();
            }

            players.Add(newPlayer);
        }

        public void Run() 
        {
            while (!isGameStarted)
            {
                _checkForDisconnects();   

                Packet startCommand = ConnectHandler.ReceivePacket(Host.client).GetAwaiter().GetResult();

                if (startCommand.Command == "start" && players.Count > 1) 
                {
                    isGameStarted = true;
                }
                else if (startCommand.Command == "start")
                {
                    string msg = "There is not enough players to start the game, YOU FUCK HEAD";
                    ConnectHandler.SendPacket(Host.client,new Packet("failedStart",msg)).GetAwaiter().GetResult();
                }
            }

            createDeck();

            shuffleDesk();

            for (int i = 0; i < players.Count; i++)
            {
                givPlayerCards(7, i);
            }

            while (isGameStarted)
            {
                NextRoud();
                if (winner != null)
                {
                    isGameStarted = false;
                }

            }


            //All player gets disconnected
            foreach (Player player in players)
            {
                ConnectHandler.DisconnectClient(player.client,$"The winner is {winner.name}, NEVER COME BACK");
            }
        }

        private void _checkForDisconnects()
        {
            // Check the viewers first
            foreach (Player player in players)
            {
                if (ConnectHandler.IsDisconnected(player.client))
                {
                    Console.WriteLine($"{player.name} has left the game");

                    // cleanup on our end
                    players.Remove(player);     // Remove from list
                    ConnectHandler._cleanupClient(player.client);
                }
            }
        }

        #region Game logic

        public void createDeck()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int j = 1; j < 10; j++)
                    {
                        currentDeck.Add(new Card((Card.colorState)i, Card.cardType.NUMBER, j)); //Number 1-9 Card
                    }

                    currentDeck.Add(new Card((Card.colorState)i, Card.cardType.SKIP)); //Skip card
                    currentDeck.Add(new Card((Card.colorState)i, Card.cardType.REVERSE)); //Reverse Card
                    currentDeck.Add(new Card((Card.colorState)i, Card.cardType.PLUS2)); //Plus 2 Card

                }
                currentDeck.Add(new Card((Card.colorState)i, Card.cardType.NUMBER, 0)); //Number 0 Card
                currentDeck.Add(new Card(Card.colorState.NULL, Card.cardType.SWICTH_COLOR)); //Switch Color Card
                currentDeck.Add(new Card(Card.colorState.NULL, Card.cardType.PLUS4)); //Plus 4 Card
            }

        }

        public void shuffleDesk()
        {

            var shuffledcards = currentDeck.OrderBy(a => Guid.NewGuid()).ToList(); //Shuffle the cards

            currentDeck = shuffledcards;

            playedDeck.Add(currentDeck[0]); //Place the first card on the table ^^
            currentDeck.Remove(currentDeck[0]);

            currentCard = playedDeck[0];

            if (currentCard.ColorState == Card.colorState.NULL)
            {
                currentCard.ColorState = (Card.colorState)random.Next(0, 3);
            }
        }

        public void givPlayerCards(int amount, int playerNumber) //Giving the player cards and switchs the decks around when current deck is low
        {
            //TODO: send card to player

            if (amount > currentDeck.Count)
            {
                currentDeck.AddRange(playedDeck.GetRange(1, playedDeck.Count - 1));
                playedDeck.RemoveRange(0, playedDeck.Count - 1);

                for (int i = 0; i < currentDeck.Count; i++)
                {
                    if (currentDeck[i].CardType == Card.cardType.PLUS4 || currentDeck[i].CardType == Card.cardType.SWICTH_COLOR)
                    {
                        currentDeck[i].ColorState = Card.colorState.NULL;
                    }
                }
            }
            for (int i = 0; i < amount; i++)
            {
                players[playerNumber].addCard(currentDeck[0]);
                currentDeck.RemoveAt(0);

            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("");
            Console.WriteLine($"Added {amount} cards to {players[playerNumber].name}");
            Console.ForegroundColor = ConsoleColor.White;
            Thread.Sleep(500);
        }

        public void NextRoud()
        {
            Console.Clear();

            bool completeRound = false;

            while (!completeRound && players[0].playerState != Player.State.DONE)
            {
                List<Card> sendDeck = new List<Card>();
                sendDeck.Add(playedDeck[playedDeck.Count - 1]);
                sendDeck.AddRange(players[0].playerCards);

                Task.Run(async () => await ConnectHandler.SendPacket(players[0].client, new Packet("cards", "", sendDeck)));

                //playedDeck[playedDeck.Count - 1].showCard();

                Packet ChoicePacket = null;
                while (ChoicePacket == null)
                {
                    ChoicePacket = ConnectHandler.ReceivePacket(players[0].client).GetAwaiter().GetResult();
                    Thread.Sleep(15);
                }

                //string playerChoiceString = players[0].playerAction(); //Returns the player chooses and want play

                //If the player can stack more cards then a system will detect it and will limit the cards

                if (ChoicePacket.Command == "cardPick") //Checks if player can picks up cards
                {
                    givPlayerCards(1, 0);
                }
                else if (ChoicePacket.Command == "uno")
                {
                    players[0].setUNO();
                }
                else if (ChoicePacket.Command == "turn" && ActionDeck.Count > 0) //Checks if player can end the round
                {
                    putPlayerLastInList();


                    for (int i = 0; i < ActionDeck.Count; i++)
                    {
                        cardAction(ActionDeck[i]);

                    }
                    ActionDeck.Clear();

                    completeRound = true;
                    stackCard = false;
                }
                else if (ChoicePacket.Command == "downCard" && int.TryParse(ChoicePacket.Message, out int playerChoiceInt)) //Checks if player have choose a number
                {
                    processingRound(playerChoiceInt);

                }
                else //Player have to play a card or an option
                {
                    Error("You have to play a card before you can end the round");
                    Thread.Sleep(1100);
                }
            }
        }

        void processingRound(int playerChoiceInt) //When the player have entered a number then it checks if the number/card is right 
        {
            playerChoiceInt -= 1;
            if (playerChoiceInt >= 0 && playerChoiceInt <= players[0].playerCards.Count - 1)
            {
                if (stackCard && players[0].playerCards[playerChoiceInt].number == currentCard.number)
                {
                    moveCards(playerChoiceInt);
                }
                else if (!stackCard && players[0].playerCards[playerChoiceInt].ColorState == currentCard.ColorState || players[0].playerCards[playerChoiceInt].ColorState == Card.colorState.NULL || players[0].playerCards[playerChoiceInt].number == currentCard.number)
                {
                    moveCards(playerChoiceInt);
                    stackCard = true;
                }
                else //If the player dont play the right card then an error shows to the player
                {
                    Error("You can't play that card, please try again");
                }
            }
            else //If the player dont do the right thing then an error shows to the player
            {
                Error("You can't play that card, please try again");
            }
        }
        void moveCards(int playerChoiceInt) //Moving the cards around to the lists 
        {



            if (players[0].playerState == Player.State.ACTIVE && players[0].playerCards.Count == 1)
            {
                Task.Run(async () => await ConnectHandler.SendPacket(players[0].client, new Packet("ups", "You forgot to call UNO, Have fun with your new card")));
                Thread.Sleep(1100);


                givPlayerCards(1, 0);
                putPlayerLastInList();
            }
            else
            {
                playedDeck.Add(players[0].playerCards[playerChoiceInt]);

                ActionDeck.Add(players[0].playerCards[playerChoiceInt]);

                currentCard = playedDeck[playedDeck.Count - 1];

                players[0].removeCard(players[0].playerCards[playerChoiceInt]);
            }

            if (players[0].playerCards.Count == 0 && players[0].playerState == Player.State.UNO)
            {
                players[0].playerState = Player.State.DONE;

                if (winner == null)
                {
                    winner = players[0];
                }

            }
        }
        void Error(string message)
        {
            Thread.Sleep(10);
            Task.Run(async () => await ConnectHandler.SendPacket(players[0].client, new Packet("failed", message)));
            Thread.Sleep(500);
        }
        void putPlayerLastInList() //Switchs the next player to index 0 
        {
            Task.Run(async () => await ConnectHandler.SendPacket(players[0].client, new Packet("done", "")));
            players.Add(players[0]);
            players.RemoveAt(0);
        }

        void cardAction(Card card)
        {
            switch (card.CardType)
            {
                case Card.cardType.PLUS2:
                    stackCardAmount += 2;
                    stack();
                    break;
                case Card.cardType.PLUS4:
                    stackCardAmount += 4;
                    currentCard.ColorState = (Card.colorState)switchColor();
                    stack();
                    break;
                case Card.cardType.SWICTH_COLOR:
                    currentCard.ColorState = (Card.colorState)switchColor();
                    break;
                case Card.cardType.REVERSE:
                    if (players.Count == 2) players.Reverse(0, players.Count);

                    else players.Reverse(0, players.Count - 1);
                    break;
                case Card.cardType.SKIP:
                    putPlayerLastInList();
                    break;
                default:
                    Console.WriteLine("Can not do any actions on the card");
                    break;
            }
        }

        int switchColor() 
        {

            Task.Run(async () => await ConnectHandler.SendPacket(players[0].client, new Packet("switchColor", "")));

            Packet ChoicePacket = null;
            while (ChoicePacket == null)
            {
                ChoicePacket = ConnectHandler.ReceivePacket(players[0].client).GetAwaiter().GetResult();
                Thread.Sleep(15);
            }

            return int.Parse(ChoicePacket.Message);
        }

        void stack()
        {
            bool playerStackCard = false;

            for (int i = 0; i < players[0].playerCards.Count; i++)  //Checks if the player has some 4plus or 2plus
            {
                if (players[0].playerCards[i].CardType == currentCard.CardType)
                {
                    playerStackCard = true;
                    stackCard = true;
                }
            }
            //Hej det er en test
            if (!playerStackCard) //If they dont have them then the amout of cards is giving to the player
            {
                givPlayerCards(stackCardAmount, 0);
                playerStackCard = false;
                stackCardAmount = 0;
            }
        }

        #endregion

    }
}
