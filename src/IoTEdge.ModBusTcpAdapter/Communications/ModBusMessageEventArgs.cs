using IoTEdge.VirtualRtu.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public class ModBusMessageEventArgs : EventArgs
    {
        public ModBusMessageEventArgs(byte[] message)
        {
            Message = message;
            Header = MbapHeader.Decode(message);
            byte[] buffer = new byte[message.Length - 7];
            Buffer.BlockCopy(message, 7, buffer, 0, buffer.Length);
            HeadlessMessage = buffer;           
        }

        public byte[] Message { get; internal set; }

        public MbapHeader Header { get; internal set; }

        public byte[] HeadlessMessage { get; internal set; }
    }
}
