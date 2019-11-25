using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Configuration;
using System.Linq;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.Caching;
using VirtualRtu.Communications.Logging;

namespace VirtualRtu.Communications.Channels
{
    public class ModuleTcpChannel : IChannel
    {
        //public ModuleTcpChannel(ModuleConfig config, Logger logger)
        //{
        //    this.config = config;
        //    this.logger = logger;
        //    this.cache = new LocalCache(Guid.NewGuid().ToString());
        //    this.config.OnChanged += Config_OnChanged;

        //    slaves = new Dictionary<string, Slave>();
        //    channels = new Dictionary<string, IChannel>();
        //    faultedSlaves = new Dictionary<byte, Slave>();
        //    slaveMap = new Dictionary<byte, byte?>();

        //    faultedChannelTimer = new System.Timers.Timer(interval);
        //    faultedChannelTimer.Elapsed += FaultedChannelTimer_Elapsed;
        //    faultedChannelTimer.Enabled = false;
        //}
        public ModuleTcpChannel(ModuleConfig config, ILogger logger = null)
        {
            this.config = config;
            this.logger = logger;
            this.cache = new LocalCache(Guid.NewGuid().ToString());
            this.config.OnChanged += Config_OnChanged;

            slaves = new Dictionary<string, Slave>();
            channels = new Dictionary<string, IChannel>();
            faultedSlaves = new Dictionary<byte, Slave>();
            slaveMap = new Dictionary<byte, byte?>();

            faultedChannelTimer = new System.Timers.Timer(interval);
            faultedChannelTimer.Elapsed += FaultedChannelTimer_Elapsed;
            faultedChannelTimer.Enabled = false;
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
        private double interval = 1000;
        private ModuleConfig config;
        private ILogger logger;
        private Dictionary<string, IChannel> channels;
        private Dictionary<string, Slave> slaves;
        private Dictionary<byte, byte?> slaveMap;
        private Dictionary<byte, Slave> faultedSlaves;
        private LocalCache cache;
        private System.Timers.Timer faultedChannelTimer;

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
            CleanupChannels();

            foreach(var slave in config.Slaves)
            { 
                IChannel channel = ChannelFactory.Create(false, new IPEndPoint(IPAddress.Parse(slave.IPAddress), slave.Port), 1024, 102400, CancellationToken.None);
                channel.OnReceive += Channel_OnReceive;
                channel.OnError += Channel_OnError;
                channel.OnClose += Channel_OnClose;
                channels.Add(channel.Id, channel);
                slaves.Add(channel.Id, slave);
                slaveMap.Add(slave.UnitId, slave.Alias);

                try
                {                    
                    await channel.OpenAsync();
                }
                catch(Exception ex)
                {
                    logger?.LogError(ex, "Fault starting TCP clients");
                    channels.Remove(channel.Id);
                    slaves.Remove(channel.Id);
                    faultedSlaves.Add(slave.UnitId, slave);  //marked for restart                    
                }
            }
        }
        public async Task SendAsync(byte[] message)
        {
            //find tcp channel associated with a unique Unit Id and send to it.
            //make sure if aliasing is used that the header contains the alias.
            MbapHeader header = MbapHeader.Decode(message);
            IChannel channel = GetSlaveChannel(header.UnitId);
            if(channel != null)
            {
                byte[] msg = MapInput(message);
                await channel.SendAsync(msg);
                //await channel.SendAsync(message);
                logger?.LogDebug($"Message sent to channel '{channel.Id}'");
            }
            else
            {
                logger?.LogWarning("No channel found to forward message.");
            }
        }
        public async Task CloseAsync()
        {
            IChannel[] array = channels.Values.ToArray();
            foreach (var channel in array)
            {
                try
                {
                    await channel.CloseAsync();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault closing tpc module channel.");
                }
            }
        }
        public async Task ReceiveAsync()
        {
            IChannel[] array = channels.Values.ToArray();
            foreach(var item in array)
            {
                await item.ReceiveAsync();
            }
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
                slaves.Clear();
                slaveMap.Clear();
                faultedSlaves.Clear();

                IChannel[] array = channels.Values.ToArray();
                foreach (var channel in array)
                {
                    try
                    {
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Fault disposing tcp module channel.");
                    }
                }

                channels.Clear();
                slaves = null;
                slaveMap = null;
                faultedSlaves = null;
                channels = null;
            }
        }
        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private events

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            //one of the tcp channels is closing
            //dispose the channel and restart it
            logger?.LogWarning($"TCP channel '{e.ChannelId}' closed.");

            if (channels.ContainsKey(e.ChannelId))
            {
                IChannel channel = channels[e.ChannelId];
                try
                {
                    channel.Dispose();
                    logger?.LogDebug($"TCP channel '{e.ChannelId}' disposed.");
                }
                catch(Exception ex)
                {
                    logger?.LogError(ex, $"Fault disposing channel '{e.ChannelId}'.");
                }

                channel = null;

                if (slaves.ContainsKey(e.ChannelId))
                {
                    Slave slave = slaves[e.ChannelId];
                    slaves.Remove(e.ChannelId);
                    faultedSlaves.Add(slave.UnitId, slave); //timer will restart
                }
            }
        }
        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            //one of the tcp channel error'd
            //close the channel
            logger?.LogError(e.Error, $"TCP channel '{e.ChannelId}' fault.");
        }
        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            //receiving on one of the tcp channels.
            //map out (de-alias) the message and forward it
            byte[] message = MapOutput(e.Message);
            if (message != null)
            {
                OnReceive?.Invoke(this, new ChannelReceivedEventArgs(e.ChannelId, message));
            }
            else
            {
                logger?.LogWarning("Message received from tcp channel cannot be mapped to output.");
            }
        }
        private async void FaultedChannelTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //try to restart as many channels as possible
            List<byte> runningSlaves = new List<byte>();
            foreach (var kvp in faultedSlaves)
            {
                try
                {
                    await RestartSlaveAsync(kvp.Value);
                    runningSlaves.Add(kvp.Key);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault restart slave.");
                }
            }

            //remove all the restarted channels
            foreach (var item in runningSlaves)
            {
                faultedSlaves.Remove(item);
            }

            //set indicator whether timer needs to run
            faultedChannelTimer.Enabled = faultedSlaves.Count > 0;

            if(faultedChannelTimer.Enabled)
            {
                ResetTimerInterval(); //increase the timer interval
            }
            else
            {
                interval = 2000;
            }
        }
        private void Config_OnChanged(object sender, ConfigUpdateEventArgs e)
        {
            if(e.Updated)
            {
                //force restart
                OpenAsync().GetAwaiter();
            }
        }
        #endregion

        #region private methods

        private void CleanupChannels()
        {
            faultedChannelTimer.Enabled = false;
            slaves.Clear();
            slaveMap.Clear();
            faultedSlaves.Clear();

            if (channels.Count > 0)
            {
                //dispose all the channels
                IChannel[] array = channels.Values.ToArray();
                foreach (var item in array)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch { }
                }

                channels.Clear();
            }
        }
        private void ResetTimerInterval()
        {
            if(interval > TimeSpan.FromSeconds(640).TotalMilliseconds ||
                faultedChannelTimer.Interval * 2.0 >= TimeSpan.FromSeconds(640).TotalMilliseconds)
            {
                interval = TimeSpan.FromSeconds(2000).TotalMilliseconds;
            }
            else 
            {
                interval = faultedChannelTimer.Interval * 2.0;                
            }

            faultedChannelTimer.Interval = interval;
        }
        private async Task RestartSlaveAsync(Slave slave)
        {
            IChannel channel = ChannelFactory.Create(false, new IPEndPoint(IPAddress.Parse(slave.IPAddress), slave.Port), 1024, 102400, CancellationToken.None);
            channel.OnReceive += Channel_OnReceive;
            channel.OnError += Channel_OnError;
            channel.OnClose += Channel_OnClose;
            channels.Add(channel.Id, channel);
            slaves.Add(channel.Id, slave);

            if (!slaveMap.ContainsKey(slave.UnitId))
            {
                slaveMap.Add(slave.UnitId, slave.Alias);
            }

            await channel.OpenAsync();
        }
        private byte[] MapInput(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);
            if (slaveMap.ContainsKey(header.UnitId) && slaveMap[header.UnitId] != null)
            {
                cache.Add(header.TransactionId.ToString(), header.UnitId, 20.0);

                byte? alias = slaveMap[header.UnitId];
                byte[] body = new byte[message.Length - 7];
                Buffer.BlockCopy(message, 7, body, 0, body.Length);
                header.UnitId = alias.Value;
                byte[] headerBuffer = header.Encode();
                byte[] buffer = new byte[headerBuffer.Length + body.Length];
                Buffer.BlockCopy(headerBuffer, 0, buffer, 0, headerBuffer.Length);
                Buffer.BlockCopy(body, 0, buffer, headerBuffer.Length, body.Length);

                return buffer;
            }
            else if (slaveMap.ContainsKey(header.UnitId))
            {
                cache.Add(header.TransactionId.ToString(), header.UnitId, 20.0);
                return message;
            }
            else
            {
                logger?.LogWarning("Unit ID is not available for mapping input.  Returning null message to tpc module channel.");
                return null;
            }
        }
        private byte[] MapOutput(byte[] message)
        {
            MbapHeader header = MbapHeader.Decode(message);
            if (cache.Contains(header.TransactionId.ToString()))
            {
                byte unitId = (byte)cache.Get(header.TransactionId.ToString());
                cache.Remove(header.TransactionId.ToString());

                if (unitId != header.UnitId)
                {
                    byte[] body = new byte[message.Length - 7];
                    Buffer.BlockCopy(message, 7, body, 0, body.Length);
                    header.UnitId = unitId;
                    byte[] headerBuffer = header.Encode();
                    byte[] buffer = new byte[headerBuffer.Length + body.Length];
                    Buffer.BlockCopy(headerBuffer, 0, buffer, 0, headerBuffer.Length);
                    Buffer.BlockCopy(body, 0, buffer, headerBuffer.Length, body.Length);
                    return buffer;
                }
                else
                {
                    return message;
                }
            }
            else
            {
                logger?.LogWarning("Unit ID is not available for mapping output.  Returning null message to tpc module channel.");
                return null;
            }

        }
        private IChannel GetSlaveChannel(byte unitId)
        {
            string id = slaves.Where((s) => s.Value.UnitId == unitId).FirstOrDefault().Key;

            if (string.IsNullOrEmpty(id))
                return null;
            else
                return channels[id];
        }
        #endregion
    }
}
