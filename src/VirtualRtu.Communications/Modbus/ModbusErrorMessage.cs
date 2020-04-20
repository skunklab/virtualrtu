using System;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.Modbus
{
    public class ModbusErrorMessage
    {
        public static byte[] Create(byte[] message, ErrorCode code)
        {
            MbapHeader header = MbapHeader.Decode(message);
            byte funcCode = message[7];
            byte msb = 8;
            byte funcByte = (byte) ((msb << 0x04) | funcCode);
            byte errorCode = (byte) code;
            header.Length = 2;
            byte[] headerBytes = header.Encode();
            byte[] remaining = {funcByte, errorCode};
            byte[] errorMsg = new byte[headerBytes.Length + remaining.Length];
            Buffer.BlockCopy(headerBytes, 0, errorMsg, 0, headerBytes.Length);
            Buffer.BlockCopy(remaining, 0, errorMsg, headerBytes.Length, remaining.Length);
            return errorMsg;
        }
    }
}