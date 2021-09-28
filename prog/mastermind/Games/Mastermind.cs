using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.UI;

namespace mastermind.Games
{
    public class Mastermind
    {
        // game state variables
        private uint AnswerLevel = 0;
        private uint SelectedColor = 0;
        private int[] Solution = { 0, 0, 0, 0 };
        private bool GameOver = false;
        private uint GuessSize = 4;
        private int NumberOfColors = 6;
        private int MaxGuesses = 10;
        public bool Debug = false;
        public string VictoryMessage = "You got it right!";
        public string DefeatMessage = "Out of turns, try again.";

        public void SetColorSelection(XpanelForSmartGraphics xPanel, uint color)
        {
            if (GameOver == false)
            {
                // set global variable
                //SelectedColor = Convert.ToInt16(color - 3) ;
                SelectedColor = color - 3;
                // turn on feedback for selected color and turn off feedback for the rest
                for (uint i = 1; i < NumberOfColors + 1; i++)
                {
                    if (SelectedColor == i)
                    {
                        xPanel.BooleanInput[i + 3].BoolValue = true;
                    }
                    else
                    {
                        xPanel.BooleanInput[i + 3].BoolValue = false;
                    }
                }
            }
        }

        public void SetGuessSpot(XpanelForSmartGraphics xPanel, uint spot)
        {
            if (GameOver == false)
            {
                // check if this is a spot on the current answer level
                if (spot > 10 + ((AnswerLevel - 1) * GuessSize) && spot < 11 + (AnswerLevel * GuessSize))
                {
                    xPanel.UShortInput[spot].UShortValue = Convert.ToUInt16(SelectedColor);
                }
            }
        }

        private void ShowAnswer(XpanelForSmartGraphics xPanel)
        {
            // show the answer
            for (uint i = 91; i < 95; i++)
            {
                xPanel.UShortInput[i].UShortValue = (ushort)Solution[i - 91];
            }
        }

        private void WinGame(XpanelForSmartGraphics xPanel)
        {
            // send game won feedback
            xPanel.StringInput[1].StringValue = VictoryMessage;
            // end the game
            EndGame(xPanel);
        }

        private void LoseGame(XpanelForSmartGraphics xPanel)
        {
            // send game lost feedback
            xPanel.StringInput[1].StringValue = DefeatMessage;
            // end the game
            EndGame(xPanel);
        }

        private void EndGame(XpanelForSmartGraphics xPanel)
        {
            // mark game as over
            GameOver = true;
            xPanel.UShortInput[2].UShortValue = 1;
            // show answer
            ShowAnswer(xPanel);
            // deselect color
            SetColorSelection(xPanel, 0);
        }

        public void NewGame(XpanelForSmartGraphics xPanel)
        {
            // reset game over flag
            GameOver = false;
            xPanel.UShortInput[2].UShortValue = 0;
            // get solution
            Random random = new Random();
            for (int i = 0; i < GuessSize; i++)
            {
                Solution[i] = random.Next(1, NumberOfColors + 1);
            }

            if (Debug)
                CrestronConsole.PrintLine("New Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);

            // reset feedback on color options
            SetColorSelection(xPanel, 0);

            // reset answerlevel 
            AnswerLevel = 1;

            // reset guess and clue and solution feedback spots
            for (uint i = 11; i < 95; i++)
            {
                xPanel.UShortInput[i].UShortValue = 0;
            }

            // enable the first four guess sports
            for (uint i = 51; i < 55; i++)
            {
                xPanel.BooleanInput[i].BoolValue = true;
            }

            // disable the rest of the guess spots and all the clue spots
            for (uint i = 55; i < 131; i++)
            {
                xPanel.BooleanInput[i].BoolValue = false;
            }

            // update the panel
            xPanel.StringInput[1].StringValue = String.Format("New Game, turn {0}", AnswerLevel);
        }

        public void EvalAnswer(XpanelForSmartGraphics xPanel)
        {
            int[] Clues = { 0, 0, 0, 0 };
            bool[] SolutionSpotUsed = { false, false, false, false };
            bool[] GuessSpotUsed = { false, false, false, false };
            uint JoinIDOffset = (AnswerLevel * GuessSize) + 7;

            if (GameOver == false)
            {
                if (Debug)
                {
                    CrestronConsole.PrintLine("Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);
                    CrestronConsole.PrintLine("Guess: {0}, {1}, {2}, {3}", xPanel.UShortInput[JoinIDOffset].UShortValue,
                                                                       xPanel.UShortInput[JoinIDOffset + 1].UShortValue,
                                                                       xPanel.UShortInput[JoinIDOffset + 2].UShortValue,
                                                                       xPanel.UShortInput[JoinIDOffset + 3].UShortValue);
                }

                // check for exact matches
                for (uint i = 0; i < GuessSize; i++)
                {
                    if (xPanel.UShortInput[i + JoinIDOffset].UShortValue == Solution[i])
                    {
                        Clues[i] = 2;
                        SolutionSpotUsed[i] = true;
                        GuessSpotUsed[i] = true;
                    }
                }

                // check for color matches
                for (uint i = 0; i < GuessSize; i++)
                {
                    for (uint x = 0; x < GuessSize; x++)
                    {
                        if (SolutionSpotUsed[i] == false && GuessSpotUsed[x] == false)
                        {
                            if (xPanel.UShortInput[x + JoinIDOffset].UShortValue == Solution[i])
                            {
                                Clues[i] = 1;
                                SolutionSpotUsed[i] = true;
                                GuessSpotUsed[x] = true;
                            }
                        }
                    }
                }

                if (Clues.Sum() == (GuessSize * 2)) // correct answer
                {
                    if (Debug)
                        CrestronConsole.PrintLine("Correct Answer!");

                    // end the game
                    WinGame(xPanel);
                }
                else // incorrect answer
                {
                    if (AnswerLevel == MaxGuesses) // no more guesses, reveal answer
                    {
                        if (Debug)
                            CrestronConsole.PrintLine("No more guesses, game over.");

                        // end the game
                        LoseGame(xPanel);
                    }
                    else // update game with clues and the next set of guess spots
                    {
                        if (Debug)
                            CrestronConsole.PrintLine("Incorrect Answer.");

                        // sort the clue array
                        Array.Sort(Clues);
                        Array.Reverse(Clues);

                        if (Debug)
                            CrestronConsole.PrintLine("Clues sorted: {0}, {1}, {2}, {3}", Clues[0], Clues[1], Clues[2], Clues[3]);

                        for (uint i = 0; i < GuessSize; i++)
                        {
                            // enable the next level of guess spots
                            xPanel.BooleanInput[(51 + (AnswerLevel * GuessSize)) + i].BoolValue = true;
                            // enable clue feedback buttons
                            xPanel.BooleanInput[(87 + (AnswerLevel * GuessSize)) + i].BoolValue = true;
                            // show the clues
                            xPanel.UShortInput[47 + (AnswerLevel * GuessSize) + i].UShortValue = (ushort)Clues[i];
                        }

                        // update the answer level
                        AnswerLevel = (ushort)(AnswerLevel + 1);
                        if (Debug)
                            CrestronConsole.PrintLine("AnswerLevel: {0}", AnswerLevel);

                        // update the panel
                        xPanel.StringInput[1].StringValue = String.Format("Take another guess, turn {0}", AnswerLevel);
                    }
                }
            }
        }
    }
}
