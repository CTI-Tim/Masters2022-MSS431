// Did you add this next library into your references before you added your using statement?
using ReflectionInterface;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GetJoke
{
    public class Joke : IpluginInterface
    {

        private int index = 0;
        private List<string> Jokes = new List<string>();

        public event EventHandler<DeviceEventArgs> DeviceEvent;

        // Default Constructor
        public Joke()
        {
            /*
            * We are using Reflection inside the library to open the csv file we embedded
            * into the library at compile time. this is a very easy way to dig inside and grab data
            * files we may want to be a part of the library.  Reflection has tons of uses.
            * Note: you must add a data file as a resource and then tell its build action to be an embedded resource 
            * for this to work.  
            */

            var assembly = Assembly.GetExecutingAssembly();
            string resource = assembly.GetManifestResourceNames().Single(str => str.EndsWith("cleanjokes.csv"));

            using (Stream stream = assembly.GetManifestResourceStream(resource))
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    Jokes.Add(reader.ReadLine());                                               // write the line to the list.
                }
            }
        }


        public void Random()
        {
            Random rnd = new Random();
            var joke = Jokes[rnd.Next(Jokes.Count)];                                            // Return a random joke.
            OnRaiseEvent(new DeviceEventArgs(joke));                                            // Call the Event Handler
        }
        public void Next()
        {
            index++;
            if (index >= Jokes.Count)
                index = 0;                                                                      // Wrap around behavior instead of stopping at lowest index.
            var joke = Jokes[index];
            OnRaiseEvent(new DeviceEventArgs(joke));
        }
        public void Previous()
        {
            index--;
            if (index < 0)
                index = Jokes.Count;
            var joke = Jokes[index];
            OnRaiseEvent(new DeviceEventArgs(joke));
        }



        // Private methods

        protected virtual void OnRaiseEvent(DeviceEventArgs e)
        {
            EventHandler<DeviceEventArgs> raiseEvent = DeviceEvent;                             // Make a copy of the event

            if (raiseEvent != null)                                                             // Verify we have subscribers
            {
                e.Name = "Jokes Library";
                raiseEvent(this, e);                                                            // trigger the event
            }
        }
    }
}
