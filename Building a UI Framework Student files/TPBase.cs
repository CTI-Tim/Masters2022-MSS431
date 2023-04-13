using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using System;
using System.Collections.Generic;

/*
 * this class is the entire UI framework
 * BasicTriListWithSmartObject is a common ancestor for Core3 panels including TSWs, XPanels, and iPads
 * Unfortunately it does not allow direct inheritance, otherwise TPBase could logically inherit from it
 * Instead we will wrap an existing TP with this class in order to make it more functional
 */

namespace SharpUI {
    public class TPBase {
        BasicTriListWithSmartObject tp;

        // store a name for this touchpanel for clarity in debug messages
        string name;

        // store the list of button handlers, indexed by their join numbers
        Dictionary<uint, BtnHandler> handlers = new Dictionary<uint, BtnHandler>();

        public TPBase(BasicTriListWithSmartObject tp, string name) {
            this.tp = tp;
            this.name = name;
        }

        // register and attach our signal handler
        public void Register() {
            if (tp.Register() != eDeviceRegistrationUnRegistrationResponse.Success) {
                ErrorLog.Error("Unable to register IPID " + tp.ID.ToString("X2"));
            } else {
                tp.SigChange += new SigEventHandler(tp_SigChange);
            }
        }

        public List<string> listSources;

        // these are the methods used to set up handlers for individual joins or ranges of joins
        // note that press and release parameters are optional and have default values (most button events only care about the press event)
        public void handleBtn(uint join, EventHandler<BtnEventArgs> handler, bool press=true, bool release=false) {
            // BtnHandler is just a struct to keep all of the associated information together for a button handler
            BtnHandler bh = new BtnHandler() { handler = handler, press = press, release = release };
            addHandler(join, bh);
        }

        // create handlers for multiple buttons in a range
        public void handleRange(uint start, uint count, EventHandler<BtnEventArgs> handler) {
            // the index in the range, from 0-count, is what gets passed to the button event handler
            for (uint index = 0; index < count; index++) {
                uint join = start + index;
                BtnHandler bh = new BtnHandler() { handler = handler, press = true, index = index };
                addHandler(join, bh);
            }
        }

        // this is an internal handler which is used by handleBtn and handleRange above
        // this allows us to centralize logic that is common to both functions without duplicating it
        void addHandler(uint join, BtnHandler handler) {
            if (handlers.ContainsKey(join)) {
                // registering two handlers for one join number is an error, and probably means something is wrong with your join numbers
                // if your workflow requires multiple handlers for each join, you would need to change the
                // Dictionary<uint, BtnHandler> data structure to store multiple handlers for each join number
                ErrorLog.Error(name + ": Multiple handlers registered for TP join " + join);
            }
            handlers[join] = handler;
        }

        // public utility functions

        // set a digital join
        public void fb(uint join, bool value) {
            tp.BooleanInput[join].BoolValue = value;
        }

        // set a serial join
        public void txt(uint join, string value) {
            tp.StringInput[join].StringValue = value;
        }

        // get a serial join
        public string txtOut(uint join) {
            return tp.StringOutput[join].StringValue;
        }

        // signal handler registered for the TP
        private void tp_SigChange(BasicTriList device, SigEventArgs args) {
            try {
                if (args.Sig.Type == eSigType.Bool) {
                    // is there a handler registered for this digital join?
                    if (handlers.ContainsKey(args.Sig.Number)) {
                        BtnHandler ev = handlers[args.Sig.Number];

                        // does this handler want to be called?
                        if ((args.Sig.BoolValue && ev.press) ||  // is this a press event and the handler wants to be called for presses?  OR
                            (!args.Sig.BoolValue && ev.release)) { // is this a release event and the handler wants to be called for releases
                            // build a BtnEventArgs structure to hold relevant data about the event
                            BtnEventArgs ea = new BtnEventArgs() { join = args.Sig.Number, index = ev.index, press = args.Sig.BoolValue };

                            // call the registered handler function
                            ev.handler(tp, ea);
                        }
                    } else {
                        dbg("Unhandled digital press " + args.Sig.Number);
                    }
                }
            } catch (Exception e) {
                dbg("Error handling signal change: " + e.ToString());
            }
        }


        private void dbg(string msg) {
            ControlSystem.cs.dbg(name + ": " + msg);
        }
    }

    // BtnHandler keeps all the information together about an event handler
    // this struct is not public because it is only used inside this file
    struct BtnHandler {
        // index is only set if this was registered by handleRange() for a button array
        public uint index;

        // does this handler want to be called for press and/or release events?
        public bool press, release;

        // store a reference to the handler function
        // EventHandler is a delegate type for a function taking (object o, EventArgs ea) as a signature
        // EventHandler<BtnEventArgs> is a delegate type for a function taking (object o, BtnEventArgs ea) as a signature
        public EventHandler<BtnEventArgs> handler;
    }

    // BtnEventArgs keeps all the information together about a button event
    // this struct is public because it is referenced outside this file
    public struct BtnEventArgs {
        public uint join, index;
        public bool press;
    }
}
