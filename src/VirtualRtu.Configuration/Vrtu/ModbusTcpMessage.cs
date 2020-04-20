using System;

namespace VirtualRtu.Configuration.Vrtu
{
    public abstract class ModbusTcpMessage
    {
        public virtual ushort TransactionId { get; set; }

        public virtual ushort ProtocolId { get; set; }

        public virtual ushort Length { get; set; }

        public virtual byte UnitId { get; set; }

        public virtual byte Function { get; set; }

        public virtual ModbusMessageType MessageType { get; set; }

        public static ModbusTcpMessage Create(byte[] message)
        {
            if (message[7] <= 4 || message[7] == 15 || message[7] == 16)
            {
                return BasicModbusMessage.Decode(message);
            }

            if (message[7] > 4 && message[7] < 7)
            {
                return WriteSingleMessage.Decode(message);
            }

            if (message[7] == 8 || message[7] == 11 || message[7] == 12)
            {
                return DiagnosticsMessage.Decode(message);
            }

            throw new InvalidCastException("Invalid modbus message type.");
        }
    }
}