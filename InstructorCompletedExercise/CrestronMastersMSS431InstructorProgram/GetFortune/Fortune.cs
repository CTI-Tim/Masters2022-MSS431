// Did you add this next library into your references before you added your using statement for our interface?
using ReflectionInterface;
using Crestron.SimplSharp;  // Need this as we are using crestron file path information.
using System;
using System.Collections.Generic;
using System.IO;

namespace GetFortune
{
    public class Fortune : IpluginInterface  //We impliment the interface here
    {

        private int index = 0;
        private List<string> Quotes = new List<string>();

        public event EventHandler<DeviceEventArgs> DeviceEvent;

        // Default Constructor
        public Fortune()
        {
            /*
            * In this example we are loading the file directly from the User folder instead of embedding the text file into the assembly
            * as shown in the Jokes plugin
            * 
            */

            string path = CheckFilename("Quotes.txt");  // Yes I know  Fortunes loading Quotes?  Consistent naming matters!

            if (File.Exists(path))      // Check to see if it exists. 
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        string[] field = line.Split(';');
                        Quotes.Add(field[0].Trim() + " - " + field[1].Trim()); // Quote plus author
                    }
                }
            }
        }

        //HINT: This is probably the code you want to copy to make your own Library to reflect in.

        public void Random()
        {
            Random rnd = new Random();
            var joke = Quotes[rnd.Next(Quotes.Count)];                                            // Return a random joke.
            OnRaiseEvent(new DeviceEventArgs(joke));                                            // Call the Event Handler
        }
        public void Next()
        {
            index++;
            if (index >= Quotes.Count)
                index = 0;                                                                      // Wrap around behavior instead of stopping at lowest index.
            var joke = Quotes[index];
            OnRaiseEvent(new DeviceEventArgs(joke));
        }
        public void Previous()
        {
            index--;
            if (index < 0)
                index = Quotes.Count;
            var joke = Quotes[index];
            OnRaiseEvent(new DeviceEventArgs(joke));
        }



        // Private methods
        protected virtual void OnRaiseEvent(DeviceEventArgs e)
        {
            EventHandler<DeviceEventArgs> raiseEvent = DeviceEvent;                             // Make a copy of the event

            if (raiseEvent != null)                                                             // Verify we have subscribers
            {
                e.Name = "Fortune Library";
                raiseEvent(this, e);                                                            // trigger the event
            }
        }


        private string CheckFilename(string Filename)
        {
            /*
             *  Things are different on VC-4 versus a 4 series processor.  Specifically the file locations.  On the server we need to get the
             *  whole path.    On a processor we need to append the programID tag to emulate behavior that Simpl and simpl+ uses
             */

            if (Filename.Length < 5)  // an invalid filename was specified a.txt is the least acceptable here
                Filename = InitialParametersClass.ProgramIDTag + ".txt";  // Default to the program tag.txt


            var RootDir = Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory();  // get the root directory
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)  // are we on VC-4?
            {
                return RootDir + "/user/" + Filename;
            }
            else
            {
                // If we are on a processor to emulate S+  fileopen() behavior we are going to append the ProgramNameTag to the path

                //var Path = "/user/" + InitialParametersClass.ProgramIDTag + "/";
                return "/user/" + Filename;
            }
        }
    }
}
