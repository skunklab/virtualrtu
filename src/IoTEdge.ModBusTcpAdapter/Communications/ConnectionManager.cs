using IoTEdge.ModBusTcpAdapter.Configuration;
using IoTEdge.VirtualRtu.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public class ConnectionManager : IConnection
    {
        public ConnectionManager(IAdapterConfig config)
        {
            random = new Random();
            client = new RestClient(config.FieldGatewayContainerName, config.FieldGatewayPort, config.FieldgatewayPath);
            connections = new Dictionary<string, TcpConnection>();
            maps = new Dictionary<byte, string>();
            configurations = new Dictionary<byte, SlaveConfig>();
            cache = new MemoryCache(null);
            
            List<Task> taskList = new List<Task>();

            foreach (var item in config.Slaves)
            {
                taskList.Add(Task.Factory.StartNew(async () => await CreateConnection(item).OpenAsync()));
            }

            Task.WhenAll(taskList);
        }

        private Dictionary<string, TcpConnection> connections;
        private Dictionary<byte, string> maps;
        private Dictionary<byte, SlaveConfig> configurations;
        private RestClient client;
        private MemoryCache cache;
        private object lockObj;
        private Random random;
        
        public async Task SendAsync(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);
            byte[] msg = GetRtuInputMessage(message);
            TcpConnection conn = GetConnection(header.UnitId);
            if(conn != null)
            {
                await conn.SendAsync(msg);
            }
            else
            {
                Console.WriteLine($"No connection found for Unit ID '{header.UnitId}'");
            }
        }

        private void Connection_OnOpen(object sender, SkunkLab.Channels.ChannelOpenEventArgs e)
        {
            Console.WriteLine($"TCP connnection {e.ChannelId} is open.");
        }

        private void Connection_OnReceive(object sender, ModBusMessageEventArgs e)
        {
            try
            {
                MbapHeader header = MbapHeader.Decode(e.Message);
                byte[] msg = GetRtuOutputMessage(e.Message);
                client.SendAsync(msg).GetAwaiter();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Return REST call failed with - {ex.Message}");
            }
        }

        private void Connection_OnError(object sender, SkunkLab.Channels.ChannelErrorEventArgs e)
        {
            Console.WriteLine($"TCP connection error - {e.Error.Message}");
            //try to close the connection
            try
            {
                if(connections.ContainsKey(e.ChannelId))
                {
                    TcpConnection conn = connections[e.ChannelId];
                    conn.CloseAsync().GetAwaiter();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"TCP connection OnError close fault - {ex.Message}");                     
            }
        }

        private void Connection_OnClose(object sender, SkunkLab.Channels.ChannelCloseEventArgs e)
        {
            Console.WriteLine($"TCP connection {e.ChannelId} is closed.");
            if(connections.ContainsKey(e.ChannelId))
            {
                IEnumerable<byte> keys = maps.Where(kvp => kvp.Value.Contains(e.ChannelId)).Select(kvp => kvp.Key);
                if(keys != null || keys.Count() == 1)
                {
                    byte key = keys.First();
                    ResetConnection(key);
                }
            }
        }
        
        private TcpConnection GetConnecton(MbapHeader header)
        {
            if (maps.ContainsKey(header.UnitId))
            {
                string key = maps[header.UnitId];
                if (connections.ContainsKey(key))
                {
                    return connections[key];
                }
            }

            return null;
        }

        private SlaveConfig GetSlaveConfig(byte unitId)
        {
            if (configurations.ContainsKey(unitId))
            {
                return configurations[unitId];
            }

            return null;
        }
                
        private TcpConnection CreateConnection(SlaveConfig config)
        {
            TcpConnection connection = new TcpConnection(config.Address, config.Port);
            configurations.Add(config.UnitId, config);
            maps.Add(config.UnitId, connection.Id);
            connections.Add(connection.Id, connection);
            connection.OnClose += Connection_OnClose;
            connection.OnError += Connection_OnError;
            connection.OnReceive += Connection_OnReceive;
            connection.OnOpen += Connection_OnOpen;
            return connection;
        }       

        private TcpConnection GetConnection(byte unitId)
        {
            if (maps.ContainsKey(unitId))
            {
                string id = maps[unitId];
                if (connections.ContainsKey(id))
                {
                    return connections[id];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

        }

        private void ResetConnection(byte unitId)
        {
            if(maps.ContainsKey(unitId) && configurations.ContainsKey(unitId))
            {
                string id = maps[unitId];
                SlaveConfig config = configurations[unitId];

                if(connections.ContainsKey(id))
                {                    
                    TcpConnection conn = connections[id];
                    connections.Remove(id);
                    maps.Remove(unitId);
                    configurations.Remove(unitId);

                    conn.Dispose();
                    conn = null;

                    Task task = Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(random.Next(2000, 15000));
                        await CreateConnection(config).OpenAsync();
                    });

                    Task.WaitAll(task);
                }
            }
        }

        private byte[] GetRtuInputMessage(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);
            var config = GetSlaveConfig(header.UnitId);
            byte[] msg = null;
            lockObj = new Object();

            lock (lockObj)
            {
                Transaction tx = Transaction.Create();
                ushort id = tx.Id;
                msg = ModBusUtil.ConvertToRtu(message, id, header.UnitId, config.UnitIdAlias);
                cache.Add(id.ToString(), new Tuple<byte, ushort>(header.UnitId, header.TransactionId), new CacheItemPolicy() { AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.AddSeconds(10)) });
            }

            return msg;
        }

        private byte[] GetRtuOutputMessage(byte[] message)
        {
            byte[] msg = null;
            MbapHeader header = MbapHeader.Decode(message);

            lockObj = new Object();

            lock (lockObj)
            {
                if (cache.Contains(header.TransactionId.ToString()))
                {
                    Tuple<byte, ushort> tuple = (Tuple<byte, ushort>)cache.Get(header.TransactionId.ToString());
                    msg = ModBusUtil.ConvertFromRtu(message, tuple.Item2, tuple.Item1, null);
                    cache.Remove(header.TransactionId.ToString());
                }
            }

            return msg;

        }
    }
}
