using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support

namespace Cresnet_Helper_Example
{
    public class ControlSystem : CrestronControlSystem
    {

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
                CrestronConsole.PrintLine("## Program Started");
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }


        public override void InitializeSystem()
        {
            try
            {
                FindAndSetKeypads(); // Launch our method
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// This method will use the CrestronCresnetHelper class to find all the keypads on our cresnet bus
        /// and set their ID's. NOTE: this will set them as they come in.
        /// to control the ID assignments to be consistent, more code would need to be added to keep track of what
        /// was assigned previously and do not reassign keypad cresnet id's later if a new device is added.
        /// 
        /// This is example code only and is not a complete solution.
        /// 
        /// Note: Any of the discovery methods are blocking code. If you need a responsive system during the discovery
        /// process, you need to leverage threading
        /// </summary>
        public void FindAndSetKeypads()
        {
            byte newId = 0x10;  // Set our starting ID number.
            var cresnetSearch = CrestronCresnetHelper.DiscoverAllDevices();     // This will detect devices with the same ID

            // If the above successfully runs it will dump everything discovered in the DiscoveredElementsList
            // We check for success below in our if statement

            if (cresnetSearch == CrestronCresnetHelper.eCresnetDiscoveryReturnValues.Success)
            {
                foreach (var device in CrestronCresnetHelper.DiscoveredElementsList) 
                {
                    if(device.DeviceModel.Contains("C2N-CBD")) // does the device match our keypad model?
                    {
                        CrestronConsole.PrintLine(" Found Keypad with TSID {0:X} at ID {1:X}",
                            device.TouchSettableId, device.CresnetId);

                        // We are going to set the devices CresnetID by using the TSID it reports back
                        CrestronCresnetHelper.SetCresnetIdByTouchSettableID(device.TouchSettableId, newId);
                        newId++; // Increment to the next ID
                    }

                }

            }
        }

    }
}