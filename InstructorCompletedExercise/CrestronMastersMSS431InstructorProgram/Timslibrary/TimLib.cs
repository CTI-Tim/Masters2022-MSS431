using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReflectionInterface;

namespace Timslibrary
{
    public class TimLib : IpluginInterface  // Impliment or interface
    {
        public event EventHandler<DeviceEventArgs> DeviceEvent;

        private int counter = 0;

        public void Next()
        {
            counter++;
            if (counter > 100)
                counter = 100;
            OnRaiseEvent(new DeviceEventArgs(String.Format("Increment {0}", counter)));
        }

        public void Previous()
        {
            counter--;
            if (counter < 1)
                counter = 1;
            OnRaiseEvent(new DeviceEventArgs(String.Format("Decrement {0}", counter)));
        }

        public void Random()
        {
            Random rnd = new Random();
            OnRaiseEvent(new DeviceEventArgs(String.Format("Random {0}",rnd.Next(100))));
        }

        protected virtual void OnRaiseEvent(DeviceEventArgs e)
        {
            EventHandler<DeviceEventArgs> raiseEvent = DeviceEvent;                             // Make a copy of the event

            if (raiseEvent != null)                                                             // Verify we have subscribers
            {
                e.Name = "HomeWork Library";
                raiseEvent(this, e);                                                            // trigger the event
            }
        }
    }
}
