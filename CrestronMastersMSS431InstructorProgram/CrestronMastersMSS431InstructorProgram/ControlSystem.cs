using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;                   // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.UI;
using MastersHelperLibrary;
using ReflectionInterface;  // Need the interface using statement and added to references.
using System;
using System.Reflection;    // Need this for reflection

namespace CrestronMastersMSS431InstructorProgram
{
    public class ControlSystem : CrestronControlSystem
    {
        // Globals
        Config myConfig;
        Password myPassword;
        XpanelForSmartGraphics myTp;

        IpluginInterface myPlugin;  // Global container to access the reflected library.

        int SelectedIndex = 0;

        //enums
        // This seems like we now have to type a LOT more but this really improves your programming flow
        // better.   The Enums for your joins allow you to use a name everywhere for it.  When you need to change that join
        // you now only change it in one location and it's changed everywhere it is used.   Think of this like a supercharged
        // constant array to manage your touch panels better in code. Only disadvantage is we have to cast them to (uint) to use them.

        enum Buttons
        {
            Settings = 1,
            Lock = 2,
            EntrySave = 3,
            Cancel = 5,
            DeleteShow = 8,
            Delete = 9,
            SettingsSave = 10

        }
        enum SubPages
        {
            ShowAddNew = 4,
            ShowLocked = 6,
            ShowSettings = 7
        }
        enum Serials
        {
            HeaderLabel = 1,
            PasswordLabel = 2,
            SettingsClassName = 3,
            SettingsIpAddress = 4,
            SettingsPort = 5,
            SettingsPinCode = 6,
            NVXName = 7,
            NVXAddress = 8

        }


        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                myConfig = new Config("Masters2022.xml");   // Instantiate the class.
                                                            // all of your configuration information will now live inside
                                                            // the myConfig class.  if you have another class that needs access
                                                            // to that information,  simply pass this instance of the class to it.

                myPassword = new Password();                // Instantiate our password class

                CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }
        // shutdown anything running cleanly when we get a program stopping event.
        private void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
                VirtualConsole.Stop();
        }

        public override void InitializeSystem()
        {
            try
            {
                // This is a part of the MastersHelperLibrary dll that was included in the package.  If you want the source code
                // It is available in the github link you download this from.
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)  // are we on VC-4?
                    VirtualConsole.Start(49400);  // Start our virtual console so we can get debugging information
                                              // If you are going to run multiple programs you need to make this port different in each program
                                              // you Can not run multiple servers on the same port.


                myConfig.Load(); // Load our settings
                Debug("Configuration file loaded");

                //UI
                myTp = new XpanelForSmartGraphics(0x03, this);
                var SGDPath = string.Format(@"{0}\{1}", Directory.GetApplicationDirectory(), "ClassTP.sgd");
                myTp.LoadSmartObjects(SGDPath);

                // Subscribe to the events
                foreach (System.Collections.Generic.KeyValuePair<uint, SmartObject> mySO in myTp.SmartObjects)
                {
                    mySO.Value.SigChange += myTpSmartObjectSigChange;
                }
                myTp.SigChange += MyTpSigChange;
                myTp.OnlineStatusChange += MyTp_OnlineStatusChange;

                myTp.Register();
                myTp.StringInput[(uint)Serials.HeaderLabel].StringValue = myConfig.Setting.MastersClass;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }



        // #### Touch panel event handler methods
        private void MyTp_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            Debug("Touchpanel online state : " + args.DeviceOnLine.ToString());

            if (args.DeviceOnLine == true)
            {
                RefreshList();
            }
        }
        private void myTpSmartObjectSigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {

            Debug(String.Format("SO currentdevice.ID = {0} Args.Sig.Name={1}", currentDevice.ID, args.Sig.Name));

            // Code to process events from the Button List vertical  we only care about two analogs
            // they are called "Item Held" and "Item Clicked" they have all the information we need
            // to process selection and  modifying/saving information
            // Remember to figure out what is what open the SGD file in a text editor.
            if (args.SmartObjectArgs.ID == 1) // We use the Smart object ID for the specific SO
            {
                if (args.Sig.Name.Contains("Item Held")) // This contains an analog value of the item held
                {
                    // The smart object tells us when something was held. So we process that and switch to the
                    // pop up window to process it.  
                    ushort heldItem = myTp.SmartObjects[1].UShortOutput["Item Held"].UShortValue;
                    UpdateOrAddItemSelect(heldItem);

                    myTp.BooleanInput[(uint)SubPages.ShowAddNew].BoolValue = true;                          // Set join  high to show the subpage
                }
                if (args.Sig.Name.Contains("Item Clicked"))                                                 // this is an analog value
                {
                    ushort clickedItem = myTp.SmartObjects[1].UShortOutput["Item Clicked"].UShortValue;

                    // Clear last selected item by reading what was last set
                    if (myTp.SmartObjects[1].UShortInput["Select Item"].UShortValue > 0)
                    {
                        myTp.SmartObjects[1].UShortInput["Deselect Item"].UShortValue =
                            myTp.SmartObjects[1].UShortInput["Select Item"].UShortValue;
                    }

                    myTp.SmartObjects[1].UShortInput["Select Item"].UShortValue = clickedItem;

                    //  Display the info on the Touchpanel Info Pane
                    myTp.StringInput[10].StringValue = clickedItem.ToString();
                    myTp.StringInput[11].StringValue = myConfig.Setting.Endpoints[clickedItem - 1].Name;
                    myTp.StringInput[12].StringValue = myConfig.Setting.Endpoints[clickedItem - 1].Address;

                }
            }

            // Password Popup Keypad
            if (args.SmartObjectArgs.ID == 2 && args.Sig.BoolValue == true)                                 //also checking if it's a press
            {
                if (args.Sig.Name.Contains("Misc_1"))  // left re-nameable button
                {
                    myTp.StringInput[2].StringValue = ""; // clear the string
                }
                else if (args.Sig.Name.Contains("Misc_2")) // right re-nameable button
                {
                    var PasswordEntered = myPassword.ComputeHash(myTp.StringInput[2].StringValue);
                    // Check password or use back door hard-coded password
                    if (myPassword.CheckPassword(PasswordEntered, myConfig.Setting.UiPassword) || PasswordEntered.Contains("12345"))
                    {
                        myTp.BooleanInput[(uint)SubPages.ShowLocked].BoolValue = false; //Clear the subpage joins
                        myTp.StringInput[(uint)Serials.PasswordLabel].StringValue = ""; // clear the string
                    }
                    else
                    {
                        myTp.StringInput[(uint)Serials.PasswordLabel].StringValue = ""; // clear the string
                    }
                }
                else
                {
                    myTp.StringInput[(uint)Serials.PasswordLabel].StringValue += args.Sig.Name;  // add each character as they are pressed
                }
            }
            // Reflection List
            if (args.SmartObjectArgs.ID == 3 && args.Sig.BoolValue == true)  //SmartObject ID 3 looking for presses only
            {
                switch (args.Sig.Name)
                {
                    case "Tab Button 1 Press":
                        {
                            if (LoadPlugin("/user/GetJoke.dll") == true)
                            {
                                myTp.BooleanInput[11].BoolValue = true;  // Enable for the transport
                                myTp.SmartObjects[3].BooleanInput["Tab Button 1 Select"].BoolValue = true;
                                myTp.SmartObjects[3].BooleanInput["Tab Button 2 Select"].BoolValue = false;
                                myTp.SmartObjects[3].BooleanInput["Tab Button 3 Select"].BoolValue = false;
                            }
                            else
                                myTp.BooleanInput[11].BoolValue = false;
                            break;
                        }
                    case "Tab Button 2 Press":
                        {
                            if (LoadPlugin("/user/GetFortune.dll") == true)
                            {
                                myTp.BooleanInput[11].BoolValue = true;  // Enable for the transport
                                myTp.SmartObjects[3].BooleanInput["Tab Button 1 Select"].BoolValue = false;
                                myTp.SmartObjects[3].BooleanInput["Tab Button 2 Select"].BoolValue = true;
                                myTp.SmartObjects[3].BooleanInput["Tab Button 3 Select"].BoolValue = false;
                            }
                            else
                                myTp.BooleanInput[11].BoolValue = false;
                            break;
                        }
                }
            }

            // reflection buttons
            if (args.SmartObjectArgs.ID == 4 && args.Sig.BoolValue == true)
            {
                switch (args.Sig.Name)
                {
                    case "Tab Button 1 Press":
                        myPlugin.Previous();
                        break;
                    case "Tab Button 2 Press":
                        myPlugin.Random();
                        break;
                    case "Tab Button 3 Press":
                        myPlugin.Next();
                        break;

                }
            }
        }
        private void MyTpSigChange(Crestron.SimplSharpPro.DeviceSupport.BasicTriList currentDevice, SigEventArgs args)
        {
            Debug(String.Format("Join currentdevice.ID = {0} Args.Sig.Number={1}", currentDevice.ID, args.Sig.Number));
            if (args.Sig.BoolValue == true)  // It's a press event
            {
                switch ((Buttons)args.Sig.Number) // we are using our enum for the join numbers so we have to cast it
                {
                    case Buttons.Settings:
                        UpdateSettingsPage();
                        break;
                    case Buttons.SettingsSave:
                        SaveSettings();
                        break;

                    case Buttons.Lock:
                        myTp.BooleanInput[(uint)SubPages.ShowLocked].BoolValue = true;
                        break;
                    case Buttons.EntrySave:
                        UpdateOrAddItem();  // Save was pressed on the touchpanel popup. Let's save the changes.
                        myTp.BooleanInput[(uint)SubPages.ShowAddNew].BoolValue = false;
                        break;

                    case Buttons.Cancel:
                        // a trick to simplify closing subpages by using the same join to clear all 
                        // the joins on them.   It's impossible for two to be visible so reset them all.
                        myTp.BooleanInput[(uint)SubPages.ShowAddNew].BoolValue = false;
                        myTp.BooleanInput[(uint)SubPages.ShowLocked].BoolValue = false;
                        myTp.BooleanInput[(uint)SubPages.ShowSettings].BoolValue = false;
                        break;
                    case Buttons.Delete:
                        DeleteItem(SelectedIndex - 1);
                        break;

                        //myTp.UShortInput[4].UShortValue = Selection;
                }
            }
        }


        // Program Specific Methods

        // These two could be in their own classes, but it also should be made more generic and flexible.
        private void Debug(string s)
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)  // are we on VC-4?
            {
                VirtualConsole.Send(s);
            }
            else
            {
                CrestronConsole.PrintLine(s);
            }
        }
        private string HtmlColor(string s, string color, int size)
        {
            return string.Format("<FONT size=\"{2}\" face=\"Segoe UI\" color=\"#{0}\">{1}</FONT>", color, s, size);
        }


        private void RefreshList()
        {
            int i = 1;
            string signame;
            foreach (var items in myConfig.Setting.Endpoints)
            {
                // Smartobjects are called by name as listed in the sgd file
                // On the Button List dont forget to turn on "use indirect text"
                signame = "Item " + i.ToString() + " Text";
                myTp.SmartObjects[1].StringInput[signame].StringValue =
                    i.ToString() + " : " + myConfig.Setting.Endpoints[i - 1].Name;  // <- minus 1 so we dont overrun our list index
                i++;
                myTp.SmartObjects[1].UShortInput["Set Num of Items"].UShortValue = (ushort)i;  // set the list size
            }
            // Lets add a nice label to the last entry to let the user know how to add a new entry.
            myTp.SmartObjects[1].StringInput["Item " + i.ToString() + " Text"].StringValue =
                HtmlColor("** Press and Hold here to Add **", "ffff88", 18);
        }

        // Methods for adding, modifying and removing items from the list These have Touch panel specific logic
        // If you find you use this style a lot, adding them into the config class and passing the objects to be manipulated
        // would be a good idea.
        private void UpdateOrAddItemSelect(int index)
        {
            if (index > myConfig.Setting.Endpoints.Count)  //  we are adding not modifying
            {
                myTp.StringInput[7].StringValue = "";
                myTp.StringInput[8].StringValue = "";
                myTp.BooleanInput[8].BoolValue = false;
            }
            else
            {
                myTp.StringInput[7].StringValue = myConfig.Setting.Endpoints[index - 1].Name;
                myTp.StringInput[8].StringValue = myConfig.Setting.Endpoints[index - 1].Address;
                myTp.BooleanInput[8].BoolValue = true;  // turn on the Delete button.
            }
            SelectedIndex = index;  // update our global
        }
        private void UpdateOrAddItem()
        {
            // We use a global here because the actual changes happen after a completely different 
            // button push on the touchpanel.  This is a completely different event and because 
            // we have to remember what was selected, we stored it in a global.

            if (SelectedIndex > myConfig.Setting.Endpoints.Count)   //  we are adding not modifying
            {
                myConfig.Setting.Endpoints.Add(                     // The key here is ADD wants an object
                    new NVX
                    {                                               // we create that new object
                        Address = myTp.StringOutput[(uint)Serials.NVXAddress].StringValue, // and then set each element inside it.
                        Name = myTp.StringOutput[(uint)Serials.NVXName].StringValue
                    }
                    );
            }
            else                                                    // We are modifying
            {
                // Once again we subtract 1 because the data is stored at 0 index and crestron objects are 1 index
                myConfig.Setting.Endpoints[SelectedIndex - 1].Name = myTp.StringOutput[(uint)Serials.NVXName].StringValue;
                myConfig.Setting.Endpoints[SelectedIndex - 1].Address = myTp.StringOutput[(uint)Serials.NVXAddress].StringValue;
            }
            myConfig.Save();    // Save it to our config file
            RefreshList();      // And refresh the displayed list to show the new entry/edited entry
            myTp.BooleanInput[(uint)SubPages.ShowAddNew].BoolValue = false; //Clear the subpage joins
        }
        private void DeleteItem(int index)
        {
            myConfig.Setting.Endpoints.RemoveAt(index);
            myConfig.Save();    // Save it to our config file
            RefreshList();      // And refresh the displayed list to show the new entry/edited entry
            myTp.BooleanInput[(uint)SubPages.ShowAddNew].BoolValue = false; //Clear the subpage joins
        }

        private void SaveSettings()
        {
            // The touchpanel, even if It shows something in a text field is actually empty.
            // We need to check for this and DO NOT update any settings if the user did not type anything in
            // to the field to keep from erasing the setting fields.  Remember just because you see text on the touchpanel
            // screen does not mean it's actually there.


            string _class = myTp.StringOutput[(uint)Serials.SettingsClassName].StringValue;
            if (_class.Length > 0)
                myConfig.Setting.MastersClass = _class;

            string _ipaddress = myTp.StringOutput[(uint)Serials.SettingsIpAddress].StringValue;
            if (_ipaddress.Length > 0)
                myConfig.Setting.IPAddress = _ipaddress;

            string _port = myTp.StringOutput[(uint)Serials.SettingsPort].StringValue;
            if (_port.Length > 0)
                ushort.TryParse(_port, out myConfig.Setting.Port);

            string pincode = myTp.StringOutput[(uint)Serials.SettingsPinCode].StringValue.Trim(' ');
            if (pincode.Length > 0)
                myConfig.Setting.UiPassword = myPassword.ComputeHash(pincode);

            //Finally save the information
            myConfig.Save();
            myTp.BooleanInput[(uint)SubPages.ShowSettings].BoolValue = false;
            myTp.StringInput[1].StringValue = myConfig.Setting.MastersClass;
        }
        private void UpdateSettingsPage()
        {
            myTp.StringInput[(uint)Serials.SettingsClassName].StringValue = myConfig.Setting.MastersClass;
            myTp.StringInput[(uint)Serials.SettingsIpAddress].StringValue = myConfig.Setting.IPAddress;
            myTp.StringInput[(uint)Serials.SettingsPort].StringValue = myConfig.Setting.Port.ToString();
            myTp.StringInput[(uint)Serials.SettingsPinCode].StringValue = "*****";
            myTp.BooleanInput[(uint)SubPages.ShowSettings].BoolValue = true;  // Show the subpage
        }

        /*
         * 
         *  Methods to do our reflection are below here.
         *  I have added the two libraries suppled already, you will have to add your own later.  
         * 
         *  Note we do not "unload" the library.  It's because we cant.  You have to unload the whole application domain to unload a reflected assembly.
         *  In .NET Framework, there is no way to unload an individual assembly without unloading all of the application domains that contain it. 
         *  Even if the assembly goes out of scope, the actual assembly file will remain loaded until all application domains that contain it are unloaded.
         *  In our case we are overwriting the object container each time we reflect in the library.
         *  
         */
        private bool LoadPlugin(string path)
        {
            path = Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory() + path;
            Debug("Load Plugin called with " + path);
            CrestronConsole.PrintLine("Load Plugin called with " + path);
            Type[] myType;  // Put it here because of context

            //myPlugin.DeviceEvent -= MyPlugin_DeviceEvent; // Unsubscribe event handler
            try
            {
                var myDll = Assembly.LoadFrom(path);  // Load the Dll
                myType = myDll.GetTypes();                   // Load the types into the array for inspection
                Debug("Loaded dll");
                CrestronConsole.PrintLine("Loaded DLL");
                myPlugin = (IpluginInterface)Activator.CreateInstance(myType[0]);

                myPlugin.DeviceEvent += MyPlugin_DeviceEvent;
                return true;
            }
            catch (FileNotFoundException) // If file is not found we throw this.
            {
                Debug(" FAIL: Reflection Can not find the file at " + path);
                return false;  // In this case we return false telling the calling code we did not load
            }
            catch (Exception e)
            {
                Debug(e.Message.ToString());
                CrestronConsole.PrintLine("Failed with exception");
                return false;
            }

        }

        private void MyPlugin_DeviceEvent(object sender, DeviceEventArgs e)
        {
            myTp.StringInput[13].StringValue = HtmlColor(e.Name, "FFEE00", 22) + "\r" + HtmlColor(e.Message, "FFFFFF", 20);
        }
    }
}