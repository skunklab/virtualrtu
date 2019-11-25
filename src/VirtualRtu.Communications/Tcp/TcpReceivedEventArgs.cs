using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Communications.Tcp
{
    public class TcpReceivedEventArgs : EventArgs
    {
        public TcpReceivedEventArgs(string id, byte[] message)
        {
            this.Id = id;
            this.Message = message;
        }

        public string Id { get; set; }

        public byte[] Message { get; set; }
    }
}
