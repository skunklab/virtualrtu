using System;

namespace VirtualRtu.Configuration.Vrtu
{
    public class WriteSingleMessage : ModbusTcpMessage
    {
        public virtual ushort Address { get; set; }

        public static WriteSingleMessage Decode(byte[] message)
        {
            int index = 0;
            WriteSingleMessage msg = new WriteSingleMessage();
            msg.MessageType = (ModbusMessageType) message[7];
            msg.TransactionId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.ProtocolId = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.Length = (ushort) ((message[index++] << 0x08) | message[index++]);
            msg.UnitId = Convert.ToByte(message[index++]);
            msg.Function = Convert.ToByte(message[index++]);
            msg.Address = (ushort) ((message[index++] << 0x08) | message[index++]);

            return msg;
        }
    }
}