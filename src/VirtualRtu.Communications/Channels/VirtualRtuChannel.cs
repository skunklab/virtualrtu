using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Security.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Communications.Caching;
using VirtualRtu.Communications.Diagnostics;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.Channels
{
    /// <summary>
    /// Channel used to connect VRTU to cloud.
    /// </summary>
    public class VirtualRtuChannel : IChannel
    {
        #region ctor
        public VirtualRtuChannel(VrtuConfig config, ILogger logger = null)
        {
            this.config = config;
            map = RtuMap.LoadAsync(config.StorageConnectionString, config.Container, config.Filename).GetAwaiter().GetResult();
            //mapper = new MbapMapper(Guid.NewGuid().ToString());
            this.logger = logger;
            subscriptions = new HashSet<byte>();
            name = Guid.NewGuid().ToString(); 
            cache = new LocalCache(name);
            cache.OnExpired += Cache_OnExpired;

            securityToken = GetSecurityToken(config);
            endpointUrl = new Uri($"wss://{config.Hostname}/ws/api/connect");            
            
        }       
        
        #endregion

        #region Public Events
        public event System.EventHandler<ChannelReceivedEventArgs> OnReceive;
        public event System.EventHandler<ChannelCloseEventArgs> OnClose;
        public event System.EventHandler<ChannelOpenEventArgs> OnOpen;
        public event System.EventHandler<ChannelErrorEventArgs> OnError;
        public event System.EventHandler<ChannelStateEventArgs> OnStateChange;
        #endregion

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

        #region Private Fields
        private string name;
        private int retryCount;
        private bool disposed;
        private PiraeusMqttClient client;
        private IChannel channel;
        private RtuMap map;
        private string securityToken;
        private Uri endpointUrl;
        private HashSet<byte> subscriptions;
        //private MbapMapper mapper;
        private ILogger logger;
        private ExponentialBackoff retryPolicy;
        private LocalCache cache;
        private DiagnosticsChannel diag;
        private VrtuConfig config;
        #endregion

        #region Public Interface Methods
        public async Task OpenAsync()
        {
            await ExecuteRetryPolicy();
            subscriptions.Clear();

            if(channel != null)
            {
                try
                {
                    channel.Dispose();
                    channel = null;
                    client = null;
                    logger?.LogDebug("Disposed internal channel.");
                }catch(Exception ex)
                {
                    logger?.LogError(ex, "Fault disposing internal channel.");
                }
            }

            try
            {
                channel = new WebSocketClientChannel(endpointUrl, securityToken, "mqtt", new WebSocketConfig(), CancellationToken.None);
                client = new PiraeusMqttClient(new MqttConfig(180), channel);                
                client.OnChannelError += Client_OnChannelError;
                client.OnChannelStateChange += Client_OnChannelStateChange;
                
                string sessionId = Guid.NewGuid().ToString();
                ConnectAckCode code = await client.ConnectAsync(sessionId, "JWT", securityToken, 180);
                if(code != ConnectAckCode.ConnectionAccepted)
                {
                    logger?.LogWarning($"Vrtu channel connect return code = '{code}'.");
                    OnError.Invoke(this, new ChannelErrorEventArgs(channel.Id, new Exception($"VRtu channel failed to open with code = {code}")));
                }
                else
                {
                    logger?.LogInformation("Vrtu channel connected.");
                    try
                    {
                        diag = new DiagnosticsChannel(config, client, logger);
                        diag.StartAsync().GetAwaiter();
                    }
                    catch(Exception ex)
                    {
                        diag = null;
                        logger?.LogError(ex, "Diagnostics on Vrtu channel faulted.");
                    }                    
                }
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault opening MQTT client channel.");
            }
        }
        public async Task SendAsync(byte[] message)
        {
            if(client == null || !client.IsConnected)
            {
                logger?.LogWarning("MQTT client is not available to forward message.");
                return;
            }

            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                if (map.HasItem(header.UnitId))
                {
                    if (!subscriptions.Contains(header.UnitId))
                    {
                        string resource = map.GetItem(header.UnitId).RtuOutputEvent;
                        await client.SubscribeAsync(resource, QualityOfServiceLevelType.AtMostOnce, ModbusMessageReceived);
                        logger?.LogInformation($"MQTT client channel subscribed {resource} with Unit ID = {header.UnitId}");
                        subscriptions.Add(header.UnitId);
                    }

                    cache.Add(GetCacheKey(header), new Tuple<ushort, byte[]>(header.TransactionId, message), 20.0);                   
                    string pisystem = map.GetItem(header.UnitId).RtuInputEvent;
                    await client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, pisystem, "application/octet-stream", message);
                    logger?.LogDebug($"VRTU published to {pisystem}");
                    await diag?.PublishInput(header);
                }
                else
                {
                    logger?.LogWarning($"Unit Id = {header.UnitId} in Modbus message not found in RTU map.");
                }
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault sending MQTT client channel.");
            }
        }        
        public async Task ReceiveAsync()
        {
            await Task.CompletedTask;
        }
        public async Task AddMessageAsync(byte[] message)
        {
            await Task.CompletedTask;
        }
        public async Task CloseAsync()
        {
            if (client == null || !client.IsConnected)
            {
                logger?.LogWarning("MQTT client is not available to close.");
                return;
            }

            try
            {
                await client.DisconnectAsync();
                client = null;
                logger?.LogInformation("MQTT client channel disconnected/closed.");
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault closing MQTT client channel.");
            }
        }
        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;

                try
                {
                    if (client != null)
                    {
                        client.Channel.Dispose();
                        client = null;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault disposing vrtu channel.");
                }


            }
        }
        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Internal MQTT Client Events
        private void Client_OnChannelStateChange(object sender, ChannelStateEventArgs args)
        {
            logger?.LogDebug($"Vrtu channel state = {args.State}");
        }

        private void Client_OnChannelError(object sender, ChannelErrorEventArgs args)
        {
            OnError?.Invoke(this, new ChannelErrorEventArgs(channel.Id, args.Error));
        }

        #endregion

        #region Private Methods
        private void Cache_OnExpired(object sender, CacheItemExpiredEventArgs e)
        {
            Tuple<ushort, byte[]> tuple = (Tuple<ushort, byte[]>)e.Value;
            byte[] errorMsg = ModbusErrorMessage.Create(tuple.Item2, ErrorCode.DeviceFailedToRespond);
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(channel.Id, errorMsg));

        }

        private void ModbusMessageReceived(string resource, string contentType, byte[] message)
        {
            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                string key = GetCacheKey(header);
                if (cache.Contains(key))
                {
                    cache.Remove(key);
                    OnReceive?.Invoke(this, new ChannelReceivedEventArgs(channel.Id, message));
                    logger?.LogDebug("Output channel received message.");
                    diag?.PublishOutput(header).GetAwaiter();
                }
                else
                {
                    logger?.LogWarning("Vrtu channel cannot match received message.");
                }
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault receiving message on vrtu channel.");
            }
        }
        private string GetCacheKey(MbapHeader header)
        {
            return $"{header.UnitId}-{header.TransactionId}";
        }
        private async Task ExecuteRetryPolicy()
        {
            if (retryPolicy == null || !retryPolicy.ShouldRetry(retryCount, null, out TimeSpan interval))
            {
                retryCount = 0;
                retryPolicy = new ExponentialBackoff(5, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(10.0));
            }
            else
            {
                retryCount++;
                await Task.Delay(interval);
            }
        }
        private string GetSecurityToken(VrtuConfig vconfig)
        {
            List<Claim> claimset = new List<Claim>();
            claimset.Add(new Claim($"http://{vconfig.Hostname}/name", vconfig.VirtualRtuId.ToLowerInvariant()));
            return CreateJwt($"http://{vconfig.Hostname.ToLowerInvariant()}/", $"http://{vconfig.Hostname.ToLowerInvariant()}/", claimset, vconfig.SymmetricKey, vconfig.LifetimeMinutes.Value);
        }
        private string CreateJwt(string audience, string issuer, IEnumerable<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }
        #endregion

    }
}
