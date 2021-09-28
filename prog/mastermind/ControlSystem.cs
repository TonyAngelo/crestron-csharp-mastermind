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

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

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
                                    Game.NewGame(myXpanel);
                                }
                                else if (args.Sig.Number == 3) // submit answer
                                {
                                    Game.EvalAnswer(myXpanel);
                                }
                                else if (args.Sig.Number > 3 && args.Sig.Number < 10) // color selection
                                {
                                    Game.SetColorSelection(myXpanel, args.Sig.Number);
                                }
                                else if (args.Sig.Number > 10 && args.Sig.Number < 51) // answer spot
                                {
                                    Game.SetGuessSpot(myXpanel, args.Sig.Number);
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
                CrestronConsole.PrintLine("System initialized, starting new game...");
                Game.NewGame(myXpanel);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }
    }
}