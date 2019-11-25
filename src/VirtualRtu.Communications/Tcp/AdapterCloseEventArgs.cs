using System;

namespace VirtualRtu.Communications.Tcp
{
    public class AdapterCloseEventArgs : EventArgs
    {
        public AdapterCloseEventArgs(string id)
        {
            Id = id;
        }

        public string Id { get; internal set; }
    }
}
