using System;
using System.Collections.Generic;

namespace VirtualRtu.Configuration.Vrtu
{
    public class BasicModbusMessage : ModbusTcpMessage
    {
        private List<IModbusFilter> filters;

        public BasicModbusMessage()
        {
            filters = new List<IModbusFilter>();
        }

        public virtual ushort Address { get; set; }

        public virtual ushort Quantity { get; set; }

        public static BasicModbusMessage Decode(byte[] message)
        {
            int index = 0;
            BasicModbusMessage msg = new BasicModbusMessage();
            msg.MessageType = (ModbusMessageType) message[7];
            msg.TransactionId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.ProtocolId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.Length = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.UnitId = Convert.ToByte(message[index++]);
            msg.Function = Convert.ToByte(message[index++]);
            msg.Address = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.Quantity = (ushort) ((message[index++] << 0x08) | message[index++]);

            return msg;
        }
    }
}