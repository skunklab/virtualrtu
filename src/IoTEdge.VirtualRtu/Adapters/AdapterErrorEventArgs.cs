using System;

namespace IoTEdge.VirtualRtu.Adapters
{
    public class AdapterErrorEventArgs : EventArgs
    {
        public AdapterErrorEventArgs(string id, Exception error)
        {
            AdapterId = id;
            Error = error;
        }

        public string AdapterId { get; internal set; }
        public Exception Error { get; internal set; }
    }
}
