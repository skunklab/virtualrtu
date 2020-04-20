using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Tcp
{
    public class TcpManager
    {
        private readonly ModuleConfig config;
        private readonly Dictionary<byte, Tuple<TcpConnection, byte?>> connections;
        private readonly ILogger logger;
        private readonly MbapMapper mapper;
        private Dictionary<string, Slave> slaves;

        public TcpManager(ModuleConfig config, ILogger logger = null)
        {
            this.config = config;
            this.config.OnChanged += Config_OnChanged;
            this.logger = logger;
            slaves = new Dictionary<string, Slave>();
            connections = new Dictionary<byte, Tuple<TcpConnection, byte?>>();
            mapper = new MbapMapper("rtumapper");
            RunAsync().GetAwaiter();
        }

        private void Config_OnChanged(object sender, ConfigUpdateEventArgs e)
        {
            RunAsync().GetAwaiter();
        }

        public event EventHandler<TcpReceivedEventArgs> OnReceived;

        public async Task RunAsync()
        {
            if (config == null || string.IsNullOrEmpty(config.Hostname))
            {
                return;
            }

            ExponentialDelayPolicy policy = new ExponentialDelayPolicy(180);
            foreach (var slave in config.Slaves)
            {
                string id = Guid.NewGuid().ToString();
                TcpConnection connection = new TcpConnection(id, slave.IPAddress, slave.Port, policy, logger);
                connection.OnReceived += Connection_OnReceived;
                connections.Add(slave.UnitId, new Tuple<TcpConnection, byte?>(connection, slave.Alias));
                await connection.OpenAsync();
            }
        }


        public async Task SendAsync(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);
            if (connections.ContainsKey(header.UnitId))
            {
                TcpConnection connection = connections[header.UnitId].Item1;
                byte[] msg = mapper.MapIn(message, connections[header.UnitId].Item2);
                await connection.SendAsync(msg);
                logger?.LogDebug($"Modbus message sent to UnitId {header.UnitId} TCP channel.");
            }
            else
            {
                logger?.LogWarning($"No tcp connection found with Unit ID = {header.UnitId}");
            }
        }

        private void Connection_OnReceived(object sender, TcpReceivedEventArgs e)
        {
            byte[] message = mapper.MapOut(e.Message);
            OnReceived?.Invoke(this, new TcpReceivedEventArgs(e.Id, message));
        }
    }
}