using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Card
    {
        public colorState ColorState { get; set; }
        public cardType CardType { get; set; }
        public int number { get; set; }
        public bool extraSpace { get; set; }

        public Card(colorState color = colorState.NULL, cardType type = cardType.NULL, int _number = -1)
        {

            ColorState = color;
            CardType = type;
            number = _number;
            extraSpace = false;

            switch (CardType)
            {
                case cardType.PLUS2:
                    number = -2;
                    break;
                case cardType.SKIP:
                    number = -3;
                    break;
                case cardType.PLUS4:
                    number = -4;
                    break;
                case cardType.REVERSE:
                    number = -5;
                    break;
                case cardType.SWICTH_COLOR:
                    number = -6;
                    break;
            }
        }

        public enum colorState
        {
            RED,
            BLUE,
            GREEN,
            YELLOW,
            NULL
        }
        public enum cardType
        {
            NUMBER,
            PLUS2 = 2,
            PLUS4 = 4,
            SWICTH_COLOR,
            REVERSE,
            SKIP,
            NULL
        }
    }
}
