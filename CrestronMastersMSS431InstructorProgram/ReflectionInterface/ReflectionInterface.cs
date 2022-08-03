using System;

namespace ReflectionInterface
{
    /// <summary>
    /// This is the interface template we are going to use for our reflection
    /// This interface MUST be used in your program and in all libraries you intend to reflect into your
    /// program that are going to be based on this interface.  Using an Interface will significantly simplify
    /// your use of reflection.  Without an interface you must dig into the dll and discover everything.
    /// 
    /// This interface MUST be referenced in your program and your libraries that will be based on it
    /// </summary>
    public interface IpluginInterface
    {

        event EventHandler<DeviceEventArgs> DeviceEvent;

        void Random();
        void Next();
        void Previous();
    }
    // Also define any event classes here in the interface to ensure they are
    // consistent across all libraries you use the interface in
    public class DeviceEventArgs : EventArgs
    {
        public DeviceEventArgs(string message)
        {
            Message = message;
        }
        public string Name { get; set; }
        public string Message { get; set; }
    }
}
