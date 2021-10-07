using System;
using Crestron.SimplSharpPro.DeviceSupport;             // For Generic Device Support

// __  __           _                      _           _ 
//|  \/  | __ _ ___| |_ ___ _ __ _ __ ___ (_)_ __   __| |
//| |\/| |/ _` / __| __/ _ \ '__| '_ ` _ \| | '_ \ / _` |
//| |  | | (_| \__ \ ||  __/ |  | | | | | | | | | | (_| |
//|_|  |_|\__,_|___/\__\___|_|  |_| |_| |_|_|_| |_|\__,_|
//                                                     

namespace mastermind.Games
{
    public class MastermindGUI<T> where T : BasicTriList
    {
        private T _panel;

        private uint StartID;
        private uint GuessSize;
        private int NumberOfColors;
        private int MaxGuesses;
        private uint SelectedColor;
        private bool GameOver;
        private uint AnswerLevel;
        public int[] Guess { get; private set; }

        public MastermindGUI(T panel, uint sID)
        {
            _panel = panel;
            StartID = sID;
            Guess = new[] { 0,0,0,0 };
        }

        public void SetColorSelection(uint color)
        {
            if (GameOver == false)
            {
                // set global variable
                SelectedColor = color;

                // turn on feedback for selected color and turn off feedback for the rest
                for (uint i = 1; i < NumberOfColors + 1; i++)
                {
                    if (SelectedColor == i)
                    {
                        _panel.BooleanInput[i + StartID + 2].BoolValue = true;
                    }
                    else
                    {
                        _panel.BooleanInput[i + StartID + 2].BoolValue = false;
                    }
                }
            }
        }

        public int SetGuessSpot(uint spot)
        {
            if (GameOver == false && SelectedColor > 0)
            {
                // check if this is a spot on the current answer level
                if (spot > ((AnswerLevel - 1) * GuessSize) && spot < 1 + (AnswerLevel * GuessSize))
                {
                    // set guess spot to color
                    Guess[spot - (1 + ((AnswerLevel - 1) * GuessSize))] = Convert.ToUInt16(SelectedColor);
                    // set the touch panel to the color
                    _panel.UShortInput[spot + StartID + 9].UShortValue = Convert.ToUInt16(SelectedColor);
                }
            }
            return 0;
        }

        public void ShowNewClues(int[] clues, uint level)
        {
            AnswerLevel = level;
            Array.Clear(Guess, 0, Guess.Length);

            for (uint i = 0; i < GuessSize; i++)
            {
                // enable the next level of guess spots
                _panel.BooleanInput[(StartID + 50 + ((AnswerLevel - 1) * GuessSize)) + i].BoolValue = true;
                // enable clue feedback buttons
                _panel.BooleanInput[(StartID + 86 + ((AnswerLevel - 1) * GuessSize)) + i].BoolValue = true;
                // show the clues
                _panel.UShortInput[StartID + 46 + ((AnswerLevel - 1) * GuessSize) + i].UShortValue = (ushort)clues[i];
            }
            // if this is the last guess
            if (level == MaxGuesses)
            {
                _panel.StringInput[StartID].StringValue = String.Format("Last attempt, make it count!");
            }
            else
            {
                _panel.StringInput[StartID].StringValue = String.Format("Try again, turn {0}", AnswerLevel);
            }
        }

        public void EndGame(string msg, int[] solution)
        {
            GameOver = true;
            // send the message
            _panel.StringInput[StartID].StringValue = msg;
            // mark game as over
            _panel.UShortInput[StartID + 1].UShortValue = 1;
            // show the answer
            for (uint i = 0; i < GuessSize + 1; i++)
            {
                _panel.UShortInput[i + StartID + 90].UShortValue = (ushort)solution[i];
            }
            // deselect color
            SetColorSelection(0);
        }

        public void NewGame(int numColors, int maxGuesses, uint guessSize)
        {
            GameOver = false;
            AnswerLevel = 1;
            GuessSize = guessSize;
            NumberOfColors = numColors;
            MaxGuesses = maxGuesses;
            Array.Clear(Guess, 0, Guess.Length);

            // setup panel for new game
            _panel.UShortInput[StartID + 1].UShortValue = 0;

            // reset feedback on color options
            SetColorSelection(0);

            // reset guess and clue and solution feedback spots
            for (uint i = StartID + 10; i < StartID + 94; i++)
            {
                _panel.UShortInput[i].UShortValue = 0;
            }

            // enable the first four guess sports
            for (uint i = StartID + 50; i < StartID + 54; i++)
            {
                _panel.BooleanInput[i].BoolValue = true;
            }

            // disable the rest of the guess spots and all the clue spots
            for (uint i = StartID + 54; i < StartID + 130; i++)
            {
                _panel.BooleanInput[i].BoolValue = false;
            }

            // update the panel
            _panel.StringInput[StartID].StringValue = String.Format("Can you guess the code?");
        }
    }
}
