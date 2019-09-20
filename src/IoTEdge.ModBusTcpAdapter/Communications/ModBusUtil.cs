using IoTEdge.VirtualRtu.Configuration;
using System;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    internal class ModBusUtil
    {
        private static byte[] CreateMessage(byte[] message, ushort tx, byte unitId, byte? unitIdAlias)
        {
            MbapHeader header = MbapHeader.Decode(message);

            byte[] body = new byte[message.Length - 7];
            Buffer.BlockCopy(message, 7, body, 0, body.Length);

            header.UnitId = unitIdAlias.HasValue ? unitIdAlias.Value : unitId;
            header.TransactionId = tx;
            byte[] headerBuffer = header.Encode();
            byte[] buffer = new byte[headerBuffer.Length + body.Length];
            Buffer.BlockCopy(headerBuffer, 0, buffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(body, 0, buffer, headerBuffer.Length, body.Length);
            return buffer;
        }
        public static byte[] ConvertToRtu(byte[] message, ushort tx, byte unitId, byte? unitIdAlias)
        {
            return CreateMessage(message, tx, unitId, unitIdAlias);
            //MbapHeader header = MbapHeader.Decode(message);
            //if (header.UnitId != unitId)
            //{
            //    Console.WriteLine("ModBus TCP message has unit id mismatch converting to RTU input.");
            //    return null;
            //}

            //if (!unitIdAlias.HasValue)
            //{
            //    return message;
            //}
            //else
            //{
            //    byte[] body = new byte[message.Length - 7];
            //    Buffer.BlockCopy(message, 7, body, 0, body.Length);
            //    header.UnitId = unitIdAlias.Value;
            //    byte[] headerBuffer = header.Encode();
            //    byte[] buffer = new byte[headerBuffer.Length + body.Length];
            //    Buffer.BlockCopy(headerBuffer, 0, buffer, 0, headerBuffer.Length);
            //    Buffer.BlockCopy(body, 0, buffer, headerBuffer.Length, body.Length);
            //    return buffer;
            //}
        }

        public static byte[] ConvertFromRtu(byte[] message, ushort tx, byte unitId, byte? unitIdAlias)
        {
            return CreateMessage(message, tx, unitId, null);
            //MbapHeader header = MbapHeader.Decode(message);

            //if (unitIdAlias.HasValue)
            //{
            //    if(header.UnitId != unitIdAlias.Value)
            //    {
            //        Console.WriteLine("ModBus TCP message has unit id mismatch converting from RTU output.");
            //        return null;
            //    }
            //    else
            //    {
            //        byte[] body = new byte[message.Length - 7];
            //        Buffer.BlockCopy(message, 7, body, 0, body.Length);
            //        header.UnitId = unitId;
            //        byte[] headerBuffer = header.Encode();
            //        byte[] buffer = new byte[headerBuffer.Length + body.Length];
            //        Buffer.BlockCopy(headerBuffer, 0, buffer, 0, headerBuffer.Length);
            //        Buffer.BlockCopy(body, 0, buffer, headerBuffer.Length, body.Length);
            //        return buffer;
            //    }
            //}
            //else
            //{
            //    if (header.UnitId != unitId)
            //    {
            //        Console.WriteLine("ModBus TCP message has unit id mismatch.");
            //        return null;
            //    }
            //    else
            //    {
            //        return message;
            //    }

            //}
        }
    }
}
