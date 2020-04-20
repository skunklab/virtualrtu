using System;
using System.Collections.Generic;

namespace VirtualRtu.Configuration.Vrtu
{
    public class DiagnosticsMessage : ModbusTcpMessage
    {
        private List<IModbusFilter> filters;

        public DiagnosticsMessage()
        {
            filters = new List<IModbusFilter>();
        }

        public static DiagnosticsMessage Decode(byte[] message)
        {
            int index = 0;
            DiagnosticsMessage msg = new DiagnosticsMessage();
            msg.MessageType = (ModbusMessageType) message[7];
            msg.TransactionId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.ProtocolId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.Length = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.UnitId = Convert.ToByte(message[index++]);
            msg.Function = Convert.ToByte(message[index++]);

            return msg;
        }
    }
}