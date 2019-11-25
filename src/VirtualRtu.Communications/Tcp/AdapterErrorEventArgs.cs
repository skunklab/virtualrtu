using System;

namespace VirtualRtu.Communications.Tcp
{
    public class AdapterErrorEventArgs : EventArgs
    {
        public AdapterErrorEventArgs(string id, Exception error)
        {
            Id = id;
            Error = error;
        }

        public string Id { get; internal set; }

        public Exception Error { get; internal set; }
    }
}
