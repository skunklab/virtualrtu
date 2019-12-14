using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Tcp
{
    public class SlaveChannel : IDisposable
    {
        private IChannel channel;
        public SlaveChannel(Slave slave, ILogger logger = null)
        {
            this.slave = slave;
            this.logger = logger;
            SlaveId = slave.UnitId;
            mapper = new MbapMapper(Guid.NewGuid().ToString());
            delayPolicy = new ExponentialDelayPolicy(30000, 2, true);
            retryPolicy = new BasicRetryPolicy(delayPolicy, 5);
            CreateChannelAsync().GetAwaiter();
        }

        public event System.EventHandler<ChannelReceivedEventArgs> OnReceive;
        private MbapMapper mapper;
        private Slave slave;
        private ILogger logger;
        private byte[] lastMessage;
        private int retryCount;
        private ExponentialDelayPolicy delayPolicy;
        private BasicRetryPolicy retryPolicy;
        private bool disposed;

        public byte SlaveId { get; set; }

        public async Task SendAsync(byte[] message)
        {
            byte[] msg = mapper.MapIn(message, slave.Alias);
            lastMessage = msg;
            await channel.SendAsync(msg);
        }

        private async void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            logger?.LogDebug($"Slave channel '{e.ChannelId} with Id = '{slave.UnitId}' opened.");

            if (lastMessage != null)
            {
                logger?.LogDebug($"Slave channel '{e.ChannelId}' with Id = '{slave.UnitId}' resending last message.");
                await channel.SendAsync(lastMessage);
            }

            logger?.LogDebug($"Slave channel '{e.ChannelId}' with Id = '{slave.UnitId}' receiving.");
            await channel.ReceiveAsync();
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            logger?.LogDebug($"Slave channel '{e.ChannelId}' with Id = '{slave.UnitId}' received message.");
            byte[] msg = mapper.MapOut(e.Message);
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(e.ChannelId, msg));
            lastMessage = null;
        }

        private async void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError($"Slave channel '{e.ChannelId}' with Id = '{slave.UnitId}' error - '{e.Error.Message}'");
            await channel.CloseAsync();
            
        }
        private async void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            logger?.LogInformation($"Slave channel '{e.ChannelId}' with Id = '{slave.UnitId}' closing.");
            channel.Dispose();
            channel = null;
            await CreateChannelAsync();
        }

        private async Task CreateChannelAsync()
        {
            if(channel != null)
            {
                channel.Dispose();
                channel = null;
            }

            if(retryCount > 0)
            {
                if (retryPolicy.ShouldRetry(retryCount++))
                {
                    retryPolicy.Delay();
                }
                else
                {
                    retryCount = 0;
                }
            }
            else
            {
                retryCount++;
            }

            channel = ChannelFactory.Create(false, new IPEndPoint(IPAddress.Parse(slave.IPAddress), slave.Port), 1024, 102400, CancellationToken.None);
            channel.OnOpen += Channel_OnOpen;
            channel.OnReceive += Channel_OnReceive;
            channel.OnError += Channel_OnError;
            channel.OnClose += Channel_OnClose;

            logger?.LogDebug($"Slave channel '{channel.Id}' with Id = '{slave.UnitId}' created.");
            await channel.OpenAsync();
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
              
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

                channel = null;
            }
        }
        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
    }
}
