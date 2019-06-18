using System;

namespace IoTEdge.VirtualRtu.Adapters
{
    public class AdapterCloseEventArgs : EventArgs
    {
        public AdapterCloseEventArgs(string id)
        {
            AdapterId = id;
        }

        public string AdapterId { get; internal set; }
    }
}
