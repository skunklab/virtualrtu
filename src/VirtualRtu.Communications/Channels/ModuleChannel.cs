using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Communications.Diagnostics;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Uris;

namespace VirtualRtu.Communications.Channels
{
    /// <summary>
    /// Channel used by an edge device to connect to the cloud.
    /// </summary>
    public class ModuleChannel : IChannel
    {
        public ModuleChannel(ModuleConfig config, ILogger logger = null)
        {
            this.config = config;
            this.logger = logger;
            hostname = config.Hostname;
            virtualRtuId = config.VirtualRtuId;
            deviceId = config.DeviceId;
            securityToken = config.SecurityToken;
            subscriptions = new HashSet<byte>();
            //inputPiSystem = UriGenerator.GetDeviceSubscribePiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId);            
            endpointUrl = new Uri($"wss://{config.Hostname}/ws/api/connect");
        }

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
        //private string inputPiSystem;
        private int retryCount;
        private bool disposed;
        private PiraeusMqttClient client;
        private string virtualRtuId;
        private string deviceId;
        private string hostname;
        private IChannel channel;
        private string securityToken;
        private Uri endpointUrl;
        private HashSet<byte> subscriptions;
        private ILogger logger;
        private ExponentialBackoff retryPolicy;
        private DiagnosticsChannel diag;
        private ModuleConfig config;
        #endregion

        #region Public interface Methods
        public async Task OpenAsync()
        {
            await ExecuteRetryPolicy();
            subscriptions.Clear();

            if (channel != null)
            {
                try
                {
                    channel.Dispose();
                    channel = null;
                    client = null;
                    logger?.LogDebug("Disposed internal channel.");
                }
                catch (Exception ex)
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
                if (code != ConnectAckCode.ConnectionAccepted)
                {
                    logger?.LogWarning($"Module client connect return code = '{code}'.");
                    OnError?.Invoke(this, new ChannelErrorEventArgs(channel.Id, new Exception($"Module channel failed to open with code = {code}")));
                }
                else
                {   
                    logger?.LogInformation("Module client connected.");
                    foreach(var slave in config.Slaves)
                    {
                        string inputPiSystem = UriGenerator.GetRtuPiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId, slave.UnitId, true);
                        await client.SubscribeAsync(inputPiSystem, QualityOfServiceLevelType.AtMostOnce, ModuleReceived);
                        logger?.LogDebug($"Module client subscribed to '{inputPiSystem}'");
                    }

                    try
                    {
                        diag = new DiagnosticsChannel(config, client, logger);                        
                        diag.StartAsync().GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        diag = null;
                        logger?.LogError(ex, "Diagnostics channel faulted.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault opening module channel.");
            }
        }
        public async Task SendAsync(byte[] message)
        {
            if(client == null || !client.IsConnected)
            {
                logger?.LogWarning("Module channel client is unavailable to send.");
                return;
            }

            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                string pisystem = UriGenerator.GetRtuPiSystem(hostname, virtualRtuId, deviceId, header.UnitId, false);
                await client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, pisystem, "application/json", message);
                await diag?.PublishOutput(header);
                logger?.LogDebug("Published message on module channel");
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault sending on module channel.");
            }
        }
        public async Task CloseAsync()
        {
            if (client == null || !client.IsConnected)
            {
                logger?.LogWarning("Module channel is not available to close.");
                return;
            }

            try
            {
                await client.DisconnectAsync();
                client = null;
                logger?.LogInformation("Module channel forced disconnected/closed.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault closing module channel.");
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
        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
                try
                {
                    if(client != null)
                    {
                        client.Channel.Dispose();
                        client = null;
                    }
                }
                catch(Exception ex)
                {
                    logger?.LogError(ex, "Fault disposing module channel.");
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
            logger?.LogDebug($"Module channel state = {args.State}");
        }

        private void Client_OnChannelError(object sender, ChannelErrorEventArgs args)
        {
            OnError?.Invoke(this, new ChannelErrorEventArgs(channel.Id, args.Error));
        }

        #endregion

        private void ModuleReceived(string resource, string contentType, byte[] message)
        {
            //received a message from subscription
            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                diag?.PublishInput(header).GetAwaiter();
                logger?.LogDebug("Diagnostics sent input.");
                OnReceive?.Invoke(this, new ChannelReceivedEventArgs(channel.Id, message));
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Unable to decode MBAP header module channel input.");
            }
        }

        #region Private methods
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

        #endregion


    }
}
