using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.Pooling;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.Adapters
{
    public class VirtualRtuAdapter : IDisposable
    {
        public VirtualRtuAdapter(RtuMap map, IChannel channel)
        {
            this.Id = Guid.NewGuid().ToString();
            this.map = map;
            this.channel = channel;

            src = new CancellationTokenSource();
            client = ClientManager.GetClient(src.Token);
            string securityToken = ClientManager.GetSecurityToken();

            ConnectAckCode code = client.ConnectAsync(Guid.NewGuid().ToString(), "JWT", securityToken, 120).GetAwaiter().GetResult();

            if(code != ConnectAckCode.ConnectionAccepted)
            {
                Console.WriteLine($"Connection to Piraeus failed with - {code}");
                src.Cancel();
                    
                client = null;
                throw new Exception("Connection to piraeus failed.");
            }
            else
            {
                client.OnChannelError += Client_OnChannelError;
                Console.WriteLine("VRTU connection to Piraeus.");
            }
        }

        private void Client_OnChannelError(object sender, ChannelErrorEventArgs args)
        {
            this.OnError?.Invoke(this, new AdapterErrorEventArgs(channel.Id, args.Error));
        }

        public event System.EventHandler<AdapterErrorEventArgs> OnError;
        public event System.EventHandler<AdapterCloseEventArgs> OnClose;
        private IChannel channel;
        private CancellationTokenSource src;
        private RtuMap map;
        private byte? unitId = null;
        private PiraeusMqttClient client;
        private string contentType = "application/octet-stream";
        private bool disposed;
        private bool subscribed = false;

        public string Id { get; internal set; }

        public async Task StartAsync()
        {
            channel.OnReceive += Channel_OnReceive;
            channel.OnError += Channel_OnError;
            channel.OnClose += Channel_OnClose;
            await channel.OpenAsync();
            StartReceive();

        }
        private void StartReceive()
        {
            Task task = channel.ReceiveAsync();
            Task.WhenAll(task);
        }
        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            try
            {
                MbapHeader header = MbapHeader.Decode(e.Message);

                if (!unitId.HasValue)
                {
                    unitId = header.UnitId;
                }

                if (unitId.HasValue && header.UnitId == unitId.Value)
                {
                    RtuPiSystem piSystem = map.GetItem((ushort)unitId.Value);

                    if (piSystem == null)
                    {
                        Console.WriteLine("PI-System not found.");
                        throw new Exception($"PI-System not found for unit id - {unitId.Value}");
                    }
                    else
                    {
                        if (!subscribed)
                        {
                            client.SubscribeAsync(piSystem.RtuOutputEvent, QualityOfServiceLevelType.AtMostOnce, ReceiveOutput).GetAwaiter();
                            subscribed = true;
                            Console.WriteLine("VRTU client subscribed.");
                        }

                        client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, piSystem.RtuInputEvent, contentType, e.Message).GetAwaiter();
                        Console.WriteLine("VRTU client publishing");
                    }
                }
                else
                {
                    throw new Exception("Unit Id missing from SCADA client message.");
                }

            }
            catch(Exception ex)
            {
                OnError?.Invoke(this, new AdapterErrorEventArgs(Id, ex));
            }
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            OnClose?.Invoke(this, new AdapterCloseEventArgs(Id));
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            OnError?.Invoke(this, new AdapterErrorEventArgs(Id, e.Error));
        }

        private void ReceiveOutput(string topic, string contentType, byte[] message)
        {
            try
            {
                channel.SendAsync(message).GetAwaiter();
                Console.WriteLine("VRTU scada channel sending to client.");
            }
            catch(Exception ex)
            {
                OnError?.Invoke(this, new AdapterErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;

                if (client != null)
                {
                    try
                    {
                        client.CloseAsync().GetAwaiter();
                    }
                    catch { }
                    client = null;
                    channel.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
    }
}
