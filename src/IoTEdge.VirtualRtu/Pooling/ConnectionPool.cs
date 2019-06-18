using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.Threading;

namespace IoTEdge.VirtualRtu.Pooling
{
    public class ConnectionPool
    {
        private static ConnectionPool instance;

        public static ConnectionPool Create(string endpoint, string securityToken, int poolSize)
        {
            if(instance == null)
            {
                instance = new ConnectionPool(endpoint, securityToken, poolSize);
            }

            return instance;
        }

        public static ConnectionPool Create()
        {
            return instance;
        }

        protected ConnectionPool(string endpoint, string securityToken, int poolSize)
        {
            clients = new Dictionary<string, PiraeusMqttClient>();
            channels = new Dictionary<string, IChannel>();
            sources = new Dictionary<string, CancellationTokenSource>();
            container = new HashSet<string>();
            this.endpoint = endpoint;
            this.securityToken = securityToken;
            this.Size = poolSize;
        }

        private Dictionary<string, PiraeusMqttClient> clients;
        private Dictionary<string, IChannel> channels;
        private Dictionary<string, CancellationTokenSource> sources;
        private HashSet<string> container;
        private string endpoint;
        private string securityToken;

        public void Init()
        {
            for(int i=0;i<Size;i++)
            {
                AddClient();
            }
        }

        public int Size { get; set; }

        public void Put(PiraeusMqttClient client)
        {
            string channelId = client.Channel.Id;

            if(sources.ContainsKey(channelId))
            {
                try
                {
                    sources[channelId].Cancel();
                }
                catch { }                
            }

            if(sources.ContainsKey(channelId))
            {
                sources.Remove(channelId);
            }

           
            if(clients.ContainsKey(channelId))
            {
                clients.Remove(channelId);
            }

            if (channels.ContainsKey(channelId))
            {
                try
                {
                    IChannel channel = channels[channelId];
                    channel.Dispose();
                }
                catch { }
            }

            if (channels.ContainsKey(channelId))
            {
                channels.Remove(channelId);
            }

            if(channels.Count < Size)
            {
                AddClient();
            }
        }

        public PiraeusMqttClient Take()
        {
            Dictionary<string, PiraeusMqttClient>.Enumerator en = clients.GetEnumerator();

            while(en.MoveNext())
            {
                if(!container.Contains(en.Current.Key))
                {
                    container.Add(en.Current.Key);
                    return en.Current.Value;
                }
            }

            AddClient();
            return Take();
        }

        



        private PiraeusMqttClient AddClient()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            //string uriString = String.Format("wss://{0}/ws/api/connect", hostname);

            Uri uri = new Uri(endpoint);
            IChannel channel = ChannelFactory.Create(uri, securityToken, "mqtt", new WebSocketConfig(), cts.Token);


            
            //IChannel channel = new WebSocketClientChannel(new Uri(endpoint), "mqtt", new WebSocketConfig(), cts.Token);
            //IChannel channel = ChannelFactory.Create(new Uri(endpoint), securityToken, "mqtt", new WebSocketConfig(), cts.Token);
            PiraeusMqttClient client = new PiraeusMqttClient(new MqttConfig(90), channel);

            try
            {

                ConnectAckCode code = client.ConnectAsync(Guid.NewGuid().ToString(), "JWT", securityToken, 90).GetAwaiter().GetResult();
                if (code != ConnectAckCode.ConnectionAccepted)
                {
                    Console.WriteLine("VRTU Client failed to connect in connection pool.");
                }
                else
                {
                    clients.Add(channel.Id, client);
                    sources.Add(channel.Id, cts);
                    channels.Add(channel.Id, channel);
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connnect pool failed to add client with - {ex.Message}");
            }

            return client;
        }
    }
}
