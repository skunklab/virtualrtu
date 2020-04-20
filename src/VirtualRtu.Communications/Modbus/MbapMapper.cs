using System;
using System.Runtime.Caching;
using VirtualRtu.Communications.Caching;

namespace VirtualRtu.Communications.Modbus
{
    public class MbapMapper
    {
        private readonly LocalCache cache;

        public MbapMapper(string name)
        {
            cache = new LocalCache(name);
        }

        public byte[] MapIn(byte[] message)
        {
            return MapIn(message, null);
        }

        public byte[] MapIn(byte[] message, byte? alias)
        {
            MbapHeader header = MbapHeader.Decode(message);
            ModbusTransaction tx = ModbusTransaction.Create();

            ushort actualTx = header.TransactionId;
            ushort proxyTx = tx.Id;
            Tuple<ushort, byte> tuple = new Tuple<ushort, byte>(actualTx, header.UnitId);
            byte unitId = header.UnitId == (alias ?? header.UnitId) ? header.UnitId : alias.Value;
            header.UnitId = unitId;
            header.TransactionId = proxyTx;

            cache.Add(GetProxyMap(unitId, proxyTx), tuple, 20.0);

            byte[] buffer = new byte[message.Length];
            byte[] src = header.Encode();
            Buffer.BlockCopy(src, 0, buffer, 0, src.Length);
            Buffer.BlockCopy(message, src.Length, buffer, src.Length, message.Length - src.Length);


            return buffer;
        }


        public byte[] MapOut(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);

            string key = GetProxyMap(header.UnitId, header.TransactionId);
            if (cache.Contains(key))
            {
                Tuple<ushort, byte> tuple = (Tuple<ushort, byte>) cache[key];
                header.TransactionId = tuple.Item1;
                header.UnitId = tuple.Item2;
                cache.Remove(key);
                byte[] buffer = new byte[message.Length];
                byte[] src = header.Encode();
                Buffer.BlockCopy(src, 0, buffer, 0, src.Length);
                Buffer.BlockCopy(message, src.Length, buffer, src.Length, message.Length - src.Length);
                return buffer;
            }

            return null;
        }

        private string GetProxyMap(byte unitId, ushort proxy)
        {
            return $"{unitId}-{proxy}";
        }

        private CacheItemPolicy GetCachePolicy(double expirySeconds)
        {
            return new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expirySeconds)
            };
        }
    }
}