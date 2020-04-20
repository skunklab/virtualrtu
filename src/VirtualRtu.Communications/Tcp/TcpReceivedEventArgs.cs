using System;

namespace VirtualRtu.Communications.Tcp
{
    public class TcpReceivedEventArgs : EventArgs
    {
        public TcpReceivedEventArgs(string id, byte[] message)
        {
            Id = id;
            Message = message;
        }

        public string Id { get; set; }

        public byte[] Message { get; set; }
    }
}