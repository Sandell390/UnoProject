using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Client
{
    public class Packet
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("deck")]
        public List<Card> Cards { get; set; }

        [JsonProperty("lobbies")]
        public List<SendLobby> Lobbies { get; set; }

        // Makes a packet
        [JsonConstructor]
        public Packet(string command = "", string message = "", List<SendLobby> lobbies = default, List<Card> cards = default)
        {
            Command = command;
            Message = message;
            Lobbies = lobbies;
            Cards = cards;
        }
        /*
        public Packet(string command = "")
        {
            Command = command;
        }
        public Packet(string command = "", string message = "")
        {
            Command = command;
            Message = message;
        }
        
        public Packet(string command = "", string message = "", List<Card> cards = default)
        {
            Command = command;
            Message = message;
            Cards = cards;
        }
        */


        public override string ToString()
        {
            return string.Format(
                "[Packet:\n" +
                "  Command=`{0}`\n" +
                "  Message=`{1}`]",
                Command, Message);
        }

        // Serialize to Json
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        // Deserialize
        public static Packet FromJson(string jsonData)
        {
            return JsonConvert.DeserializeObject<Packet>(jsonData);
        }
    }
}
