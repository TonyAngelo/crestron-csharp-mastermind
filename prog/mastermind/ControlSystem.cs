using System;
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
        Mastermind Game;
        MastermindGUI<XpanelForSmartGraphics> GUI;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // instantiate the game and register the callback
                Game = new Mastermind();
                Game.MastermindEvent += Mastermind_Event;

                // Hardware instantiation
                if (this.SupportsEthernet)
                {
                    // instantiate the panel and set the description
                    myXpanel = new XpanelForSmartGraphics(0x05, this);
                    myXpanel.Description = "Games Xpanel";

                    // register, then subscribe to events 
                    if (myXpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        ErrorLog.Error("Error registering {0}, Error: {1}", myXpanel.Description, myXpanel.RegistrationFailureReason);
                    }
                    else
                    {
                        myXpanel.SigChange += PanelSigChange;
                        myXpanel.OnlineStatusChange += PanelOnlineStatusChange;
                    }

                    // mastermindGUI class takes two parameters, the xpanel and the joinID at which the buttons begin
                    GUI = new MastermindGUI<XpanelForSmartGraphics>(myXpanel, 1);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public void Mastermind_Event(object sender, MastermindEventArgs e)
        {
            //CrestronConsole.PrintLine("Mastermind_Event sender: {0}, args: {1}", sender, e.Update);
            if(e.Update == "NewGame")
            {
                GUI.NewGame(e.NumberOfColors, e.MaxGuesses, e.GuessSize);
            }
            else if(e.Update == "NewClues")
            {
                GUI.ShowNewClues(e.Clues, e.AnswerLevel);
            }
            else if (e.Update == "Loser")
            {
                GUI.EndGame("Out of turns, try again.", e.Solution);
            }
            else if (e.Update == "Winner")
            {
                GUI.EndGame("You solved the code!", e.Solution);
            }
        }

        private void PanelOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            //CrestronConsole.PrintLine("PanelOnlineStatusChange: {0}, Online: {1}", currentDevice.Description, args.DeviceOnLine);
            if (currentDevice == myXpanel)
            {
                if (args.DeviceOnLine)
                {
                    ErrorLog.Notice("{0} is Online!", currentDevice.Description);
                }
                else
                {
                    ErrorLog.Error("{0} is Offline!", currentDevice.Description);
                }
            }
        }

        private void PanelSigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            //CrestronConsole.PrintLine("PanelSigChange: {0}, Type: {1}", currentDevice.Description, args.Sig.Type);
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
                                if (args.Sig.Number == 1) // help
                                {
                                    // not implemented
                                }
                                else if (args.Sig.Number == 2) // new game
                                {
                                    Game.NewGame();
                                }
                                else if (args.Sig.Number == 3) // submit answer
                                {
                                    Game.EvalAnswer(GUI.Guess);
                                }
                                else if (args.Sig.Number > 3 && args.Sig.Number < 10) // color selection
                                {
                                    GUI.SetColorSelection(args.Sig.Number - 3);
                                }
                                else if (args.Sig.Number > 10 && args.Sig.Number < 51) // answer spot
                                {
                                    GUI.SetGuessSpot(args.Sig.Number - 10);
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

        public override void InitializeSystem()
        {
            try
            {
                //CrestronConsole.PrintLine("System initialized, starting new game...");
                Game.NewGame();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
    }
}