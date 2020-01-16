using System;

namespace VirtualRtu.WebMonitor.Hubs
{
    public class MonitorEventArgs : EventArgs
    {
        public MonitorEventArgs(string resourceUriString, string contentType, byte[] message)
        {
            ResoureUriString = resourceUriString;
            ContentType = contentType;
            Message = message;
        }

        public string ResoureUriString { get; internal set; }

        public string ContentType { get; internal set; }

        public byte[] Message { get; internal set; }
    }
}
