using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Lobby
    {
        public bool isGameStarted = false;
        public string lobbyName { get; set;}
        public List<Player> players { get; set; }
        public List<Card> playedDeck { get; set; }
        public List<Card> currentDeck { get; set; }
        public List<Card> ActionDeck { get; set; }
        public Card currentCard { get; set; }
        public Player winner { get; set; }
        public int stackCardAmount { get; set; }
        public bool stackCard { get; set; }

        Random random;

        public Lobby() 
        {
            players = new List<Player>();
            playedDeck = new List<Card>();
            currentDeck = new List<Card>();
            ActionDeck = new List<Card>();
        }


        public void JoinMessage() 
        {
            for (int i = 0; i < players.Count - 1; i++)
            {
                string joinMessage = "A player has joined the game";
                byte[] buffer = Encoding.UTF8.GetBytes(joinMessage);
                players[i].client.Client.Send(buffer);

            }
        }
    }
}
