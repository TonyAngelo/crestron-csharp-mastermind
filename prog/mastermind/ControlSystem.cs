using System;
using System.Linq;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;

namespace mastermind
{
    public class ControlSystem : CrestronControlSystem
    {
        // xpanel
        XpanelForSmartGraphics myXpanel;

        // game state variables
        uint AnswerLevel = 0;
        ushort SelectedColor = 0;
        int[] Solution = { 0, 0, 0, 0 };
        bool GameOver = false;
        uint GuessSize = 4;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                //Subscribe to the controller events (System, Program, and Ethernet)
                //CrestronEnvironment.SystemEventHandler += new SystemEventHandler(_ControllerSystemEventHandler);
                //CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(_ControllerProgramEventHandler);
                //CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(_ControllerEthernetEventHandler);

                // Hardware instantiation
                if (this.SupportsEthernet)
                {
                    myXpanel = new XpanelForSmartGraphics(0x05, this);
                    myXpanel.Description = "Mastermind Xpanel";

                    // subscribe to events, then register
                    if (myXpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        ErrorLog.Error("Error registering {0}, err = {1}", myXpanel.Description, myXpanel.RegistrationFailureReason);
                    }
                    else
                    {
                        myXpanel.SigChange += MyXpanel_SigChange;
                        myXpanel.OnlineStatusChange += MyXpanel_OnlineStatusChange;
                    }

                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        private void MyXpanel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            CrestronConsole.PrintLine("OnlineStatusChange Args: {0}", args.ToString());

            if (currentDevice == myXpanel)
            {
                if (args.DeviceOnLine)
                {
                    ErrorLog.Notice("myXpanel is Online!");
                }
                else
                {
                    ErrorLog.Error("myXpanel is Offline!");
                }
            }
        }

        private void MyXpanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            //CrestronConsole.PrintLine("SigChange Event: {0}", args.Event.ToString());
            //CrestronConsole.PrintLine("SigChange Sig: {0}", args.Sig.ToString());
            if (currentDevice == myXpanel)
            {
                switch (args.Sig.Type)
                {
                    case eSigType.NA:
                        break;
                    case eSigType.Bool:
                        {
                            if (args.Sig.BoolValue == true)
                            {
                                if (args.Sig.Number == 2) // new game
                                {
                                    NewGame();
                                }
                                else if (args.Sig.Number == 3) // submit answer
                                {
                                    if (GameOver == false)
                                    {
                                        EvalAnswer();
                                    }
                                }
                                else if (args.Sig.Number > 3 && args.Sig.Number < 10) // color selection
                                {
                                    if (GameOver == false)
                                    {
                                        SetColorSelection((ushort)(args.Sig.Number - 3));
                                    }
                                }
                                else if (args.Sig.Number > 10 && args.Sig.Number < 51) // answer spot
                                {
                                    if (GameOver == false)
                                    {
                                        // check if this is a spot on the current answer level
                                        if (args.Sig.Number > 10 + ((AnswerLevel - 1) * GuessSize) && args.Sig.Number < 11 + (AnswerLevel * GuessSize))
                                        {
                                            myXpanel.UShortInput[args.Sig.Number].UShortValue = SelectedColor;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case eSigType.UShort:
                        break;
                    case eSigType.String:
                        break;
                    default:
                        break;
                }
            }
        }

        public void SetColorSelection(ushort color)
        {
            CrestronConsole.PrintLine("SetColorSelection({0})", color);
            // set global variable
            SelectedColor = color;
            // turn on feedback for selected color and turn off feedback for the rest
            for (uint i = 1; i < 7; i++)
            {
                if(SelectedColor == i)
                {
                    myXpanel.BooleanInput[i + 3].BoolValue = true;
                }
                else
                {
                    myXpanel.BooleanInput[i + 3].BoolValue = false;
                }
            }
        }

        public void ShowAnswer()
        {
            // show the answer
            for (uint i = 91; i < 95; i++)
            {
                myXpanel.UShortInput[i].UShortValue = (ushort)Solution[i - 91];
            }
        }

        public void EndGame()
        {
            // mark game as over
            GameOver = true;
            // show answer
            ShowAnswer();
            // deselect color
            SetColorSelection(0);
        }

        public void NewGame()
        {
            CrestronConsole.PrintLine("NewGame()");

            // reset game over flag
            GameOver = false;

            // get solution
            Random random = new Random();
            for (int i = 0; i < GuessSize; i++)
            {
                Solution[i] = random.Next(1, 7);
            }
            CrestronConsole.PrintLine("Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);

            // reset feedback on color options
            SetColorSelection(0);

            // reset answerlevel 
            AnswerLevel = 1;

            // reset guess and clue and solution feedback spots
            for (uint i = 11; i < 95; i++)
            {
                myXpanel.UShortInput[i].UShortValue = 0;
            }

            // enable the first four guess sports
            for (uint i = 51; i < 55; i++)
            {
                myXpanel.BooleanInput[i].BoolValue = true;
            }

            // disable the rest of the guess spots and all the clue spots
            for (uint i = 55; i < 131; i++)
            {
                myXpanel.BooleanInput[i].BoolValue = false;
            }

            //for (uint i = 91; i < 131; i++)
            //{
            //    myXpanel.BooleanInput[i].BoolValue = false;
            //}
        }

        public void EvalAnswer()
        {
            int[] Clues = { 0, 0, 0, 0 };
            bool[] SolutionSpotUsed = { false, false, false, false };
            bool[] GuessSpotUsed = { false, false, false, false };
            uint JoinIDOffset = (AnswerLevel * GuessSize) + 7;
            
            CrestronConsole.PrintLine("Solution: {0}, {1}, {2}, {3}", Solution[0], Solution[1], Solution[2], Solution[3]);
            CrestronConsole.PrintLine("Guess: {0}, {1}, {2}, {3}", myXpanel.UShortInput[JoinIDOffset].UShortValue, 
                                                                   myXpanel.UShortInput[JoinIDOffset + 1].UShortValue,
                                                                   myXpanel.UShortInput[JoinIDOffset + 2].UShortValue,
                                                                   myXpanel.UShortInput[JoinIDOffset + 3].UShortValue);

            // check for exact matches
            for (uint i = 0; i < GuessSize; i++)
            {
                //CrestronConsole.PrintLine("Join ID {0} with guess of {1} is being evaluated", i + JoinIDOffset, myXpanel.UShortInput[i + JoinIDOffset].UShortValue);
                
                if (myXpanel.UShortInput[i + JoinIDOffset].UShortValue == Solution[i])
                {
                    Clues[i] = 2;
                    SolutionSpotUsed[i] = true;
                    GuessSpotUsed[i] = true;
                    CrestronConsole.PrintLine("Exact Match for Join ID {0}", i + JoinIDOffset);
                }
            }
            CrestronConsole.PrintLine("State after exact match loop; Clues: {0}, SpotUsed: {1}", Clues, SolutionSpotUsed);

            // check for color matches
            for (uint i = 0; i < GuessSize; i++)
            {
                //CrestronConsole.PrintLine("Join ID {0} with guess of {1} is being evaluated", i + JoinIDOffset, myXpanel.UShortInput[i + JoinIDOffset].UShortValue);

                for (uint x = 0; x < GuessSize; x++)
                {
                    //CrestronConsole.PrintLine("Join ID {0} with guess of {1} is being evaluated", i + JoinIDOffset, myXpanel.UShortInput[i + JoinIDOffset].UShortValue);

                    if (SolutionSpotUsed[i] == false && GuessSpotUsed[x] == false)
                    {
                        if (myXpanel.UShortInput[x + JoinIDOffset].UShortValue == Solution[i])
                        {
                            Clues[i] = 1;
                            SolutionSpotUsed[i] = true;
                            GuessSpotUsed[x] = true;
                            CrestronConsole.PrintLine("Color Match for Join ID {0}", i + JoinIDOffset);
                        }
                    }
                }
            }

            if (Clues.Sum() == 8) // correct answer
            {
                CrestronConsole.PrintLine("Correct Answer!");
                // end the game
                EndGame();
            }
            else // incorrect answer
            {
                if (AnswerLevel == 10) // no more guesses, reveal answer
                {
                    CrestronConsole.PrintLine("No more guesses, game over.");
                    // end the game
                    EndGame();
                }
                else // update game with clues and the next set of guess spots
                {
                    uint JoinID;

                    CrestronConsole.PrintLine("Incorrect Answer.");
                    //CrestronConsole.PrintLine("Clues: {0}, {1}, {2}, {3}", Clues[0], Clues[1], Clues[2], Clues[3]);

                    // sort the clue array
                    Array.Sort(Clues);
                    Array.Reverse(Clues);

                    CrestronConsole.PrintLine("Clues sorted: {0}, {1}, {2}, {3}", Clues[0], Clues[1], Clues[2], Clues[3]);

                    // enable the next level of guess spots
                    for (uint i = 0; i < GuessSize; i++)
                    {
                        JoinID = (51 + (AnswerLevel * GuessSize)) + i;
                        myXpanel.BooleanInput[JoinID].BoolValue = true;
                        //CrestronConsole.PrintLine("Enable Join: {0}", JoinID);
                    }

                    // enable clue feedback buttons
                    for (uint i = 0; i < GuessSize; i++)
                    {
                        JoinID = (87 + (AnswerLevel * GuessSize)) + i;
                        myXpanel.BooleanInput[JoinID].BoolValue = true;
                        //CrestronConsole.PrintLine("Clue Enable JoinID {0}", JoinID);
                    }

                    // show the clues
                    for (uint i = 0; i < GuessSize; i++)
                    {
                        JoinID = 47 + (AnswerLevel * GuessSize) + i;
                        myXpanel.UShortInput[JoinID].UShortValue = (ushort)Clues[i];
                        //CrestronConsole.PrintLine("Clue Feedback JoinID {0}", JoinID);
                    }

                    // update the answer level
                    AnswerLevel = (ushort)(AnswerLevel + 1);
                    CrestronConsole.PrintLine("AnswerLevel: {0}", AnswerLevel);
                }
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                CrestronConsole.PrintLine("System initialized, starting new game...");
                NewGame();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
    }
}