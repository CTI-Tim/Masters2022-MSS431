using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;             // For Generic Device Support
using Crestron.SimplSharpPro.UI;
using System.Collections.Generic;

/*
 * This project demonstrates a framework for simplifying UI handling in SIMPL#Pro programs.
 * Its goals are to:
 *   improve programmer efficiency
 *   improve code readability
 *   make S#Pro a more comfortable environment
 *   not add complexity and overhead
 * 
 * Two ways of doing the same thing are shown side by side in this project.
 * TP1 has a signal handler with the traditional switch/case statement.
 * TP2 sets up individual signal handlers using helper functions in TPBase.cs.
 * 
 * This framework is extremely basic and can be expanded in a number of directions:
 *   handling analog and serial signals
 *   handling Smart Graphics objects
 *   moving user interface code out of ControlSystem.cs
 */
 
namespace SharpUI {
    public class ControlSystem : CrestronControlSystem {
        // since there is only one ControlSystem object, this provides a convenient handle to refer to it
        // we can refer to ControlSystem.cs from anywhere in the program
        // static = there is only one for the entire ControlSystem class (not one for each instance)
        // public = we can refer to it from other classes in the program
        static public ControlSystem cs;

        // member variables in each ControlSystem instance - since they are not public, they are private by default
        XpanelForSmartGraphics tp1;
        TPBase tp2;

        // putting all the join numbers in one place helps to avoid conflicts and allows you to change them in one place if needed
        const uint btnPress = 42;
        const uint btnPressAndHold = 43;

        const uint interlockBase = 51;
        const uint interlockCount = 3;
        
        const uint txtDebug = 1;
        const uint txtName = 2;

        // keep track of the interlock value and any other runtime data for the program
        uint interlock = 0;
        DateTime pressTime;
        List<string> debugLines = new List<string>();

        public ControlSystem() : base() {
            // store a reference to the one and only ControlSystem in ControlSystem.cs
            cs = this;
            try {
                // create an XPanel
                tp1 = new XpanelForSmartGraphics(0x03, this);

                // create an XPanel wrapped in TPBase class and give it a name for debugging purposes
                tp2 = new TPBase(new XpanelForSmartGraphics(0x04, this), "TP2");
            } catch (Exception e) {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem() {
            try {
                // Register and attach the SigEventHandler
                if (tp1.Register() != eDeviceRegistrationUnRegistrationResponse.Success) {
                    ErrorLog.Error("Unable to register IPID 03");
                } else {
                    tp1.SigChange += new SigEventHandler(tp_SigChange);
                }

                // Register and attach the signal handlers (broken out into a separate function for readability)
                tp2.Register();
                tp2setup();
            } catch (Exception e) {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        // traditional SigChange handler with switch/case
        // logic has to be performed inside the case statements or moved to a separate function
        // either way, the switch/case quickly becomes unwieldy on any moderately sized project
        void tp_SigChange(BasicTriList device, 
            SigEventArgs args) {
            if (args.Sig.Type == eSigType.Bool) {
                if (args.Sig.BoolValue) {
                    switch (args.Sig.Number) {
                        case 42:
                            // dbg is defined below and is a quick way to show debugging info on the panel
                            dbg("button 42 pressed");
                            break;

                        case 43:
                            dbg("button 43 pressed");
                            break;

                        // any time code is copy/pasted with small changes like this is a "code smell"
                        // while it may work right initially, this code is difficult to change without breaking it
                        // if the number of buttons change, it would be easy to forget to update the feedback statements to match
                        // if the join numbers have to change, it would need to be changed in multiple places
                        // this can be simplified by using fallthrough statements and math on the join numbers, but is still fragile
                        case 51:
                            interlock = 0;
                            tp1.BooleanInput[51].BoolValue = true;
                            tp1.BooleanInput[52].BoolValue = false;
                            tp1.BooleanInput[53].BoolValue = false;
                            break;

                        case 52:
                            interlock = 1;
                            tp1.BooleanInput[51].BoolValue = false;
                            tp1.BooleanInput[52].BoolValue = true;
                            tp1.BooleanInput[53].BoolValue = false;
                            break;

                        case 53:
                            interlock = 2;
                            tp1.BooleanInput[51].BoolValue = false;
                            tp1.BooleanInput[52].BoolValue = false;
                            tp1.BooleanInput[53].BoolValue = true;
                            break;

                        default:
                            // having a default case can be very helpful for troubleshooting when you push a button and nothing happens!
                            dbg("Unknown join pressed: " + args.Sig.Number);
                            break;
                    }
                } else {
                    switch (args.Sig.Number) {
                        // the release event is handled down here, separate from the press event logic above
                        case 43:
                            dbg("button 43 released");
                            break;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////
        // register button handlers for TP2 using the new framework
        // better practice would be to move all of this to a separate class and file
        void tp2setup() {
            // anonymous handler for simple button presses
            // (o, ea) => dbg("foo") creates an anonymous lambda that takes two parameters called o and ea
            tp2.handleBtn(42, (o, ea) => dbg("button 42 pressed"));

            // register for press and release events (default is only press events)
            // press and release are passed as named parameters, which is a great way to deal with bool parameters
            tp2.handleBtn(43, pressAndHold, press: true, release: true);

            // register for button range using the starting join and a count
            // event handler will receive the button's index in the array (so button 52 is index=1)
            tp2.handleRange(interlockBase, interlockCount, pressInterlock);
        }

        void pressAndHold(object o, BtnEventArgs ea) {
            // we asked for press and release events, so we need to check which one this is
            if (ea.press) {
                dbg("button 43 pressed");
                pressTime = DateTime.Now;
            } else {
                TimeSpan elapsed = DateTime.Now - pressTime;
                dbg("button 43 released after " + elapsed.TotalSeconds.ToString("F1") + " seconds");
            }
        }

        void pressInterlock(object o, BtnEventArgs ea) {
            // we registered for a range of buttons, so ea.index tells us which button was pressed, 0 through count
            interlock = ea.index; 

            // feedback can be set in a loop to allow it to easily be expanded by redefining interlockCount
            for (uint i=0; i<interlockCount; i++) {
                // TPBase.fb is a helper method for setting tp.BooleanInput[join].BoolValue = value
                tp2.fb(interlockBase + i, interlock == i);
            }
        }

        // show debug text on the panel
        public void dbg(string msg) {
            // add a new line and throw out an old line if needed
            debugLines.Add(msg);
            if (debugLines.Count > 7) {
                debugLines.RemoveAt(0);
            }

            // join the lines with a <br> between each
            string text = String.Join("<br>", debugLines);
            tp1.StringInput[1].StringValue = text;

            // TPBase.txt is a helper method for setting tp.StringInput[join].StringValue = value
            tp2.txt(txtDebug, text);
        }

    }
}

