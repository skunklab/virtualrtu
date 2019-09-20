using SkunkLab.Channels;
using SkunkLab.Channels.Tcp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public class TcpConnection : Connection
    {

        public TcpConnection(string ipAdressString, int port)
        {
            endpoint = new IPEndPoint(IPAddress.Parse(ipAdressString), port);
            src = new CancellationTokenSource();
            base.channel = TcpChannel.Create(false, endpoint, 1024, 204800, src.Token);
            Id = base.channel.Id;
        }

        private IPEndPoint endpoint;
        private CancellationTokenSource src;
        public string Id { get; internal set; }
        public event EventHandler<ModBusMessageEventArgs> OnReceive;
        public event EventHandler<ChannelOpenEventArgs> OnOpen;
        public event EventHandler<ChannelErrorEventArgs> OnError;
        public event EventHandler<ChannelCloseEventArgs> OnClose;

        public override async Task OpenAsync()
        {
            try
            {
                base.channel.OnClose += Channel_OnClose;
                base.channel.OnError += Channel_OnError;
                base.channel.OnReceive += Channel_OnReceive;
                base.channel.OnOpen += Channel_OnOpen;

                await channel.OpenAsync();
            }
            catch(Exception ex)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(channel.Id, ex));
            }
        }

        public override async Task SendAsync(byte[] message)
        {
            try
            {
                await base.channel.SendAsync(message);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Channel {base.channel.Id} failed to send with - { ex.Message}");
                OnError?.Invoke(this, new ChannelErrorEventArgs(base.channel.Id, ex));
            }
        }

        private void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            OnOpen?.Invoke(this, e);
            Task task = channel.ReceiveAsync();
            Task.WhenAll(task);
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            OnReceive?.Invoke(this, new ModBusMessageEventArgs(e.Message));
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            Console.WriteLine($"Channel '{e.ChannelId}' ERROR - {e.Error.Message}");
            OnError?.Invoke(this, e);
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            Console.WriteLine($"Channel '{e.ChannelId}' has been closed.");
            OnClose?.Invoke(this, e);
        }

        public override async Task CloseAsync()
        {
            if (channel != null)
            {
                await channel.CloseAsync();
            }
        }
    }
}
