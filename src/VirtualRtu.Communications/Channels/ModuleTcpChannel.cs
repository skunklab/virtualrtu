using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.Tcp;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Channels
{
    public class ModuleTcpChannel : IChannel
    {
        
        public ModuleTcpChannel(ModuleConfig config, ILogger logger = null)
        {
            this.config = config;
            this.logger = logger;
            this.config.OnChanged += Config_OnChanged;

            slaveChannels = new Dictionary<byte, SlaveChannel>();
        }

        public static IChannel CreateSingleton(ModuleConfig config, ILogger logger = null)
        {
            if(instance == null)
            {
                instance = new ModuleTcpChannel(config, logger);
            }

            return instance;

        }       

        private static ModuleTcpChannel instance;

        #region IChannel Properties
        public bool RequireBlocking { get; set; }
        public bool IsConnected { get; set; }
        public string Id { get; set; }
        public string TypeId { get; set; }
        public int Port { get; set; }
        public ChannelState State { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsAuthenticated { get; set; }
        #endregion

        #region private fields
        private bool disposed;
        private ModuleConfig config;
        private ILogger logger;        
        private Dictionary<byte, SlaveChannel> slaveChannels;        

        #endregion

        #region Public Events
        public event System.EventHandler<ChannelReceivedEventArgs> OnReceive;
        public event System.EventHandler<ChannelCloseEventArgs> OnClose;
        public event System.EventHandler<ChannelOpenEventArgs> OnOpen;
        public event System.EventHandler<ChannelErrorEventArgs> OnError;
        public event System.EventHandler<ChannelStateEventArgs> OnStateChange;
        #endregion

        #region interface methods
        public async Task OpenAsync()
        {
            if(slaveChannels.Count > 0)
            {
                var channels = slaveChannels.Values.ToArray();
                foreach (var slave in channels)
                    slave.Dispose();
            }

            foreach(Slave slave in config.Slaves)
            {
                SlaveChannel slaveChannel = new SlaveChannel(slave, logger);
                slaveChannel.OnReceive += SlaveChannel_OnReceive;
                slaveChannels.Add(slave.UnitId, slaveChannel);                
            }

            await Task.CompletedTask;
        }

        private void SlaveChannel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            OnReceive?.Invoke(this, e);
        }

        public async Task SendAsync(byte[] message)
        {
          
            MbapHeader header = MbapHeader.Decode(message);

            if(slaveChannels.ContainsKey(header.UnitId))
            {
                SlaveChannel slaveChannel = slaveChannels[header.UnitId];
                await slaveChannel.SendAsync(message);
            }
        }
        public async Task CloseAsync()
        {
            await Task.CompletedTask;
        }
        public async Task ReceiveAsync()
        {
            await Task.CompletedTask;
        }
        public async Task AddMessageAsync(byte[] message)
        {
            await SendAsync(message);
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
                SlaveChannel[] array = slaveChannels.Values.ToArray();
                foreach(var item in array)
                {
                    item.Dispose();
                }

                slaveChannels.Clear();
                slaveChannels = null;

                
            }
        }
        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private events
        
        private void Config_OnChanged(object sender, ConfigUpdateEventArgs e)
        {
            if(e.Updated)
            {
                //force restart
                OpenAsync().GetAwaiter();
            }
        }
        #endregion

    }
}
