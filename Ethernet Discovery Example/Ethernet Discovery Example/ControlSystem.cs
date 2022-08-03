using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support

namespace Ethernet_Discovery_Example
{
    public class ControlSystem : CrestronControlSystem
    {

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;


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
                DetectAndSetTouchpanel();

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        public void DetectAndSetTouchpanel()
        {
            var ethernetQuery = EthernetAutodiscovery.Query(EthernetAdapterType.EthernetLANAdapter);

            if (ethernetQuery == EthernetAutodiscovery.eAutoDiscoveryErrors.AutoDiscoveryOperationSuccess)
            {
                foreach (var Item in EthernetAutodiscovery.DiscoveredElementsList)
                {
                    CrestronConsole.PrintLine("Found {0} at {1}", Item.DeviceIdString, Item.IPAddress);
                    if (Item.DeviceIdString.Contains("TSW-560"))
                    {
                        CrestronConsole.PrintLine("## TSW panel found at {0}", Item.IPAddress);
                        // The code below is not supported on Touch panels. you cannot clear or set the IP
                        // Table this way on a touch panel or any IP device that supports more than one
                        // IP Table entry.  To configure from the program those devices IP table entries
                        // Requires SSH into the device and issue the console commands.

                        // EthernetAutodiscovery.ClearIPTableEntryOnSpecifiedDevice(Item);
                        // EthernetAutodiscovery.SetIPTableEntryOnSpecifiedDevice(Item, 0x10, 500);
                        // EthernetAutodiscovery.SetHostNameOnSpecifiedDevice(Item, "Room 101 Touch panel");
                    }
                    if (Item.DeviceIdString.Contains("DM-RMC-100-STR"))
                    {
                        CrestronConsole.PrintLine("## DM-RMC-100-STR found at {0}", Item.IPAddress);
                        //EthernetAutodiscovery.SetHostNameOnSpecifiedDevice(Item, "MY-RMC-100-STR");
                    }
                }
                EthernetAutodiscovery.StopLightAndPoll();
            }
        }

        public void SendCommandToTSW(string command, string username, string password)
        {

        }
    }
}