using Crestron.SimplSharp;
using MastersHelperLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CrestronMastersMSS431InstructorProgram
{
    public class Config
    {
        private string _filename = "";
        public Configuration Setting;

        public Config(string Filename)
        {
            Setting = new Configuration();                                          // Create an instance of our Configuration class that will
                                                                                    // be available as ClassName.Setting when we instantiate the
                                                                                    // config class

            _filename = CheckFilename(Filename);                                     // Is the filename valid and add in a path if one is not
        }

        public void Save()
        {
            try
            {
                Debug("Saving to " + _filename);
                CrestronConsole.PrintLine("File Opening {0} for saving", _filename);
                XmlSerializer seralizer = new XmlSerializer(typeof(Configuration));
                TextWriter filestream = new StreamWriter(_filename);
                seralizer.Serialize(filestream, this.Setting);
                filestream.Close();
                Debug("File Save Closed");
            }
            catch (Exception ex)
            {
                string a = ex.ToString();
                string[] b = a.Split('\x0d');
                foreach (string b2 in b)
                    Debug(b2);
            }
        }

        public void Load()
        {
            XmlSerializer seralizer = new XmlSerializer(typeof(Configuration));      // Create a serializer that has the structure of our class

            if (File.Exists(_filename))                                              // Check if the file even exists before we try and open it
            {
                Debug("File Exists loading");
                using (FileStream stream = File.OpenRead(_filename))                 // Open our file
                {

                    this.Setting = (Configuration)seralizer.Deserialize(stream);     // Read the XML from the file and dump it into the class
                }
            }
            else
            {
                Debug("File Does not Exist");


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
                Debug(" On VC-4 application is at |" + RootDir + "|");
                return RootDir + "/user/" + Filename;
            }
            else
            {
                // If we are on a processor to emulate S+ behavior we are going to append the ProgramNameTag to the path to create that folder.

                var Path = "/user/" + InitialParametersClass.ProgramIDTag + "/";  // Lowercase!  Case is important now and User will create a new TLD

                if (!Directory.Exists(Path))            // If the directory does not exist, make it.  With great power comes great responsibility.
                    Directory.CreateDirectory(Path);

                return Path + Filename;
            }
        }
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
    }


    public class Configuration
    {
        public string MastersClass = "";
        public string IPAddress = "";
        public ushort Port = 0;
        public string UiPassword = "";

        //TODO:  Lab1
        public bool Checkbox1 = false;
        public bool Checkbox2 = false;
        public bool Checkbox3 = false;
        

        // Create a list
        public List<NVX> Endpoints;

        public Configuration()
        {
            Endpoints = new List<NVX>(); // need to instantiate the list when the class is instantiated
        }

    }

  
    public class NVX
    {
        public string Address = "";
        public string Name = "";
    }

    /*
     * Here are some important details about XML serialization in C# that you should be aware of. First of all, every class
     * that we want to serialize should define the default (parameterless) constructor. 
     * In our case, we have not defined any constructors. Therefore, the parameterless constructor is included by default.
     * Also, only the public members of a class will be serialized; private members will be omitted from the XML document.
     * Any properties must have a public getter method. If they also have a setter method, this must be public too.
     * If your Setter is not Public then it cannot be updated when loaded.
     * Be careful using any properties with code to calculate other properties, the calculated ones will be saved and attempt to
     * be loaded when the file is loaded.
     * 
     * You CAN serialize a class that contains more classes.  For example you want a list of objects that contain information
     * about your infinetEX dimmers.  
     */
}
