using System;
using System.Linq;
using Crestron.SimplSharp;

 // __  __           _                      _           _ 
 //|  \/  | __ _ ___| |_ ___ _ __ _ __ ___ (_)_ __   __| |
 //| |\/| |/ _` / __| __/ _ \ '__| '_ ` _ \| | '_ \ / _` |
 //| |  | | (_| \__ \ ||  __/ |  | | | | | | | | | | (_| |
 //|_|  |_|\__,_|___/\__\___|_|  |_| |_| |_|_|_| |_|\__,_|
 //                                                     

namespace mastermind.Games
{
    public class MastermindEventArgs : EventArgs
    {
        public uint AnswerLevel { get; set; }
        public int[] Solution { get; set; }
        public string Update { get; set; }
        public int[] Clues { get; set; }
        public uint GuessSize { get; set; }
        public int NumberOfColors { get; set; }
        public int MaxGuesses { get; set; }

    }
    
    public class Mastermind
    {
        // game state variables
        private uint AnswerLevel = 0;
        private int[] Solution = { 0, 0, 0, 0 };
        private int[] Clues = { 0, 0, 0, 0 };
        private bool GameOver = false;
        private uint GuessSize = 4;
        private int NumberOfColors = 6;
        private int MaxGuesses = 10;

        public delegate void MastermindEventHandler(object source, MastermindEventArgs args);

        public event MastermindEventHandler MastermindEvent;

        public void NewGame()
        {
            // reset game over flag
            GameOver = false;
            
            // get solution
            Random random = new Random();
            for (int i = 0; i < GuessSize; i++)
            {
                Solution[i] = random.Next(1, NumberOfColors + 1);
            }

            CrestronConsole.PrintLine("New Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);

            // reset answerlevel 
            AnswerLevel = 1;

            // send new game message
            OnMastermindEvent(new MastermindEventArgs() { Update = "NewGame", GuessSize = GuessSize, NumberOfColors = NumberOfColors, MaxGuesses = MaxGuesses });
        }

        public void EvalAnswer(int[] guess)
        {
            // make sure the game is over
            if (GameOver == false)
            {
                // init the clue finding vars
                bool[] SolutionSpotUsed = { false, false, false, false };
                bool[] GuessSpotUsed = { false, false, false, false };

                CrestronConsole.PrintLine("Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);
                CrestronConsole.PrintLine("Guess: {0}, {1}, {2}, {3}", guess[0], guess[1], guess[2], guess[3]);

                // clear the clues
                Array.Clear(Clues, 0, Clues.Length);

                // check for exact matches
                for (uint i = 0; i < GuessSize; i++)
                {
                    if (guess[i] == Solution[i])
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
                            if (guess[x] == Solution[i])
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
                    CrestronConsole.PrintLine("Correct Answer!");

                    // end the game
                    OnMastermindEvent(new MastermindEventArgs() { Update = "Winner", Solution = Solution });
                }
                else // incorrect answer
                {
                    if (AnswerLevel == MaxGuesses) // no more guesses, reveal answer
                    {
                        CrestronConsole.PrintLine("No more guesses, game over.");

                        // end the game
                        OnMastermindEvent(new MastermindEventArgs() { Update = "Loser", Solution = Solution });
                    }
                    else // update game with clues and the next set of guess spots
                    {
                        //CrestronConsole.PrintLine("Incorrect Answer.");

                        // sort the clue array
                        Array.Sort(Clues);
                        Array.Reverse(Clues);

                        CrestronConsole.PrintLine("Clues sorted: {0}, {1}, {2}, {3}", Clues[0], Clues[1], Clues[2], Clues[3]);

                        // update the answer level
                        AnswerLevel = (ushort)(AnswerLevel + 1);
                        //CrestronConsole.PrintLine("AnswerLevel: {0}", AnswerLevel);

                        // update the panel
                        OnMastermindEvent(new MastermindEventArgs() { Update = "NewClues", Clues = Clues, AnswerLevel = AnswerLevel});
                    }
                }
            }
        }

        protected virtual void OnMastermindEvent(MastermindEventArgs args)
        {
            if(MastermindEvent != null)
            {
                MastermindEvent(this, args);
            }
        }
    }
}
