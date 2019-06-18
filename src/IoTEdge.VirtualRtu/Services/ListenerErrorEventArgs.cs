using System;

namespace IoTEdge.VirtualRtu.Services
{
    public class ListenerErrorEventArgs : EventArgs
    {

        public ListenerErrorEventArgs(Exception error)
        {
            Error = error;
        }

        public Exception Error { get; internal set; }
    }
}
