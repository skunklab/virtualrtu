using Microsoft.Extensions.Logging;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.WebSockets
{
    public class WebSocketConnection
    {
        public WebSocketConnection(ModuleConfig config, ILogger logger = null)
        {
            this.hostname = config.Hostname;
            this.securityToken = config.SecurityToken;
            this.logger = logger;            
        }

        public WebSocketConnection(VrtuConfig config, ILogger logger = null)
        {
            this.hostname = config.Hostname;
            this.securityToken = GetSecurityToken(config);
            this.logger = logger;
        }

        private string hostname;
        private string securityToken;
        private ILogger logger;
        private CancellationTokenSource cts;
        private IChannel channel;
        private PiraeusMqttClient client;
        public event System.EventHandler<ChannelCloseEventArgs> OnClose;
        public event System.EventHandler<ChannelErrorEventArgs> OnError;

        public bool IsConnected
        {
            get
            {
                return client != null && client.Channel != null && client.Channel.IsConnected;
            }
        }

        public async Task<ConnectAckCode> OpenAsync()
        {
            string sessionId = Guid.NewGuid().ToString();
            Uri uri = new Uri(String.Format($"wss://{hostname}/ws/api/connect"));
            cts = new CancellationTokenSource();
            channel = ChannelFactory.Create(uri, securityToken, "mqtt", new WebSocketConfig(), cts.Token);
            channel.OnClose += Channel_OnClose;
            ConnectAckCode code = ConnectAckCode.ServerUnavailable;

            try
            {
                client = new PiraeusMqttClient(new MqttConfig(180), channel);
                client.OnChannelError += Client_OnChannelError;
                code = await client.ConnectAsync(sessionId, "JWT", securityToken, 90);
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Web socket connection error.");
            }

            return code;
        }

        public async Task CloseAsync()
        {
            if (client == null)
            {
                try
                {
                    await client.DisconnectAsync();
                }
                catch { }
            }
        }

        public async Task SendAsync(string pisystem, string contentType, byte[] message)
        {
            await client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, pisystem, contentType, message);
        }

        public async Task AddSubscriptionAsync(string piSystem, Action<string, string, byte[]> action)
        {
            if (IsConnected)
            {
                await client.SubscribeAsync(piSystem.ToLowerInvariant(), QualityOfServiceLevelType.AtMostOnce, action);
                logger?.LogDebug($"Web socket client subscribed to {piSystem}");                
            }
            else
            {
                logger?.LogWarning($"Web socket client is not connected and cannot subscribe to {piSystem}.");
            }
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            OnClose?.Invoke(this, e);
        }

        private void Client_OnChannelError(object sender, ChannelErrorEventArgs args)
        {
            OnError?.Invoke(this, args);
        }

        private string GetSecurityToken(VrtuConfig vconfig)
        {
            List<Claim> claimset = new List<Claim>();
            claimset.Add(new Claim($"http://{vconfig.Hostname}/name", vconfig.VirtualRtuId.ToLowerInvariant()));
            return CreateJwt($"http://{vconfig.Hostname.ToLowerInvariant()}/", $"http://{vconfig.Hostname.ToLowerInvariant()}/", claimset, vconfig.SymmetricKey, vconfig.LifetimeMinutes.Value);
        }
        private string CreateJwt(string audience, string issuer, IEnumerable<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            SkunkLab.Security.Tokens.JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }
    }
}
