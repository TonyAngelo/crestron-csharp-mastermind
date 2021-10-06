using System;
using System.Linq;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;
using mastermind.Games;

namespace mastermind
{
    public class ControlSystem : CrestronControlSystem
    {
        // xpanel
        XpanelForSmartGraphics myXpanel;

        // game
        Mastermind Game = new Mastermind();

        public uint SelectedColor = 0;
        public int[] Guess = { 0, 0, 0, 0 };
        public uint AnswerLevel;
        public bool GameOver;
        public uint GuessSize;
        private int NumberOfColors;
        private int MaxGuesses;


        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                Game.MastermindEvent += Mastermind_Event;

                // Hardware instantiation
                if (this.SupportsEthernet)
                {
                    myXpanel = new XpanelForSmartGraphics(0x05, this);
                    myXpanel.Description = "Games Xpanel";

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

        public void Mastermind_Event(object sender, MastermindEventArgs e)
        {
            CrestronConsole.PrintLine("Mastermind_Event sender: {0}, args: {1}", sender, e.Update);
            if(e.Update == "NewGame")
            {
                GuessSize = e.GuessSize;
                NumberOfColors = e.NumberOfColors;
                MaxGuesses = e.MaxGuesses;
                NewGame(myXpanel);
            }
            else if(e.Update == "NewClues")
            {
                AnswerLevel = e.AnswerLevel;
                ShowNewClues(myXpanel, e.Clues);
            }
            else if (e.Update == "Loser")
            {
                myXpanel.StringInput[1].StringValue = "Out of turns, try again.";
                EndGame(myXpanel, e.Solution);
            }
            else if (e.Update == "Winner")
            {
                myXpanel.StringInput[1].StringValue = "You got it right!";
                EndGame(myXpanel, e.Solution);
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
                                    Game.NewGame();
                                }
                                else if (args.Sig.Number == 3) // submit answer
                                {
                                    for (uint i = 0; i < GuessSize; i++)
                                    {
                                        if(Guess[i] == 0)
                                        {
                                            break;
                                        }
                                        else if (i + 1 == GuessSize)
                                        {
                                            Game.EvalAnswer(Guess);
                                        }
                                    }
                                }
                                else if (args.Sig.Number > 3 && args.Sig.Number < 10) // color selection
                                {
                                    SetColorSelection(myXpanel, args.Sig.Number - 3);
                                }
                                else if (args.Sig.Number > 10 && args.Sig.Number < 51) // answer spot
                                {
                                    SetGuessSpot(myXpanel, args.Sig.Number);
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

        public void SetColorSelection(XpanelForSmartGraphics xPanel, uint color)
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
                    // set guess spot to color
                    Guess[spot - (11 + ((AnswerLevel - 1) * GuessSize))] = Convert.ToUInt16(SelectedColor);
                    // set the touch panel to the color
                    xPanel.UShortInput[spot].UShortValue = Convert.ToUInt16(SelectedColor);
                }
            }
        }

        private void ShowNewClues(XpanelForSmartGraphics xPanel, int[] clues)
        {
            for (uint i = 0; i < GuessSize; i++)
            {
                // enable the next level of guess spots
                xPanel.BooleanInput[(51 + ((AnswerLevel - 1) * GuessSize)) + i].BoolValue = true;
                // enable clue feedback buttons
                xPanel.BooleanInput[(87 + ((AnswerLevel - 1) * GuessSize)) + i].BoolValue = true;
                // show the clues
                xPanel.UShortInput[47 + ((AnswerLevel - 1) * GuessSize) + i].UShortValue = (ushort)clues[i];
            }
            // if this is the last guess
            if(AnswerLevel == MaxGuesses)
            {
                xPanel.StringInput[1].StringValue = String.Format("Last attempt, make it count!");
            }
            else
            {
                xPanel.StringInput[1].StringValue = String.Format("Try again, turn {0}", AnswerLevel);
            }
        }

        private void ShowAnswer(XpanelForSmartGraphics xPanel, int[] solution)
        {
            // show the answer
            for (uint i = 0; i < GuessSize + 1; i++)
            {
                xPanel.UShortInput[i + 91].UShortValue = (ushort)solution[i];
            }
        }

        private void EndGame(XpanelForSmartGraphics xPanel, int[] solution)
        {
            // set game over var
            GameOver = true;
            // mark game as over
            xPanel.UShortInput[2].UShortValue = 1;
            // show answer
            ShowAnswer(xPanel, solution);
            // deselect color
            SetColorSelection(xPanel, 0);
        }

        public void NewGame(XpanelForSmartGraphics xPanel)
        {
            // set local variables
            AnswerLevel = 1;
            GameOver = false;

            // setup panel for new game
            xPanel.UShortInput[2].UShortValue = 0;
            
            // reset feedback on color options
            SetColorSelection(xPanel, 0);

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

        public override void InitializeSystem()
        {
            try
            {
                CrestronConsole.PrintLine("System initialized, starting new game...");
                Game.NewGame();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
    }
}