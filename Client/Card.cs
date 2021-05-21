using System;
using System.Collections.Generic;
using System.Text;

namespace Client
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
                case cardType.BLANK:
                    number = -7;
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
            NULL,
            BLANK
        }

        public void showCard()
        {
            //Show the card's properties (Switch)
            switch (ColorState)
            {
                case colorState.RED:
                    Console.ForegroundColor = ConsoleColor.Red;
                    checkType();
                    break;
                case colorState.BLUE:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    checkType();
                    break;
                case colorState.GREEN:
                    Console.ForegroundColor = ConsoleColor.Green;
                    checkType();
                    break;
                case colorState.YELLOW:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    checkType();
                    break;
                case colorState.NULL:
                    Console.ForegroundColor = ConsoleColor.White;
                    checkType();
                    break;
                default:
                    Console.WriteLine("ERROR CODE 1: CANT SEE THE COLOR OF THE CARD");
                    break;
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        void checkType()
        {
            if (CardType == cardType.NUMBER)
            {
                Console.Write($" |{number}| ");
            }
            else
            {
                Console.Write($" |{getTypeToShow()}| ");
            }
        }

        string getTypeToShow()
        {
            switch (CardType)
            {
                case cardType.PLUS2:
                    extraSpace = true;
                    return "+2";

                case cardType.PLUS4:
                    extraSpace = true;
                    return "+4";

                case cardType.SWICTH_COLOR:
                    return "C";

                case cardType.REVERSE:
                    return "R";

                case cardType.SKIP:
                    return "S";
                case cardType.BLANK:
                    return "U";
                default:
                    break;
            }
            return "ERROR CODE 2: CANT SEE THE TYPE OF THE CARD";
        }
    }
}
