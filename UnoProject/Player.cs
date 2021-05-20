using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Server
{
    public class Player
    {
        public TcpClient client;

        public string name;

        public List<Card> playerCards { get; set;}

        public State playerState { get; set; }

        public enum State
        {
            ACTIVE,
            UNO,
            DONE
        }
        public void setUNO()
        {
            playerState = State.UNO;
        }
        public void CheckUno()
        {

            if (playerCards.Count == 1 && playerState == State.ACTIVE)
            {
                Console.WriteLine("Type 'u' for call UNO");
            }

            if (playerCards.Count > 1)
            {
                playerState = State.ACTIVE;
            }
        }

        public Player(TcpClient _client, string _name) 
        {
            client = _client;
            name = _name;
            playerCards = new List<Card>();
        }

        public void addCard(Card card)
        {
            //Adds cards to the player
            playerCards.Add(card);
        }

        public void removeCard(Card card)
        {
            //remove cards from the player
            playerCards.Remove(card);
        }


    }
}
