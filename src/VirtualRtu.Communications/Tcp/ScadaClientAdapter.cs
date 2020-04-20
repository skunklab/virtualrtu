using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using SkunkLab.Protocols.Mqtt;
using VirtualRtu.Communications.Diagnostics;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.WebSockets;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.Tcp
{
    public class ScadaClientAdapter : IDisposable
    {
        private const string CONTENT_TYPE = "application/octet-stream";
        private readonly IChannel channel;
        private readonly VrtuConfig config;
        private WebSocketConnection connection;
        private DiagnosticsConnection diagnostics;
        private bool disposed;

        private readonly ILogger logger;
        private RtuMap map;
        private readonly MbapMapper mapper;
        private bool restarting;
        private bool shutdown;
        private readonly HashSet<byte> subscribed;

        public ScadaClientAdapter(VrtuConfig config, IChannel channel, ILogger logger)
        {
            Id = Guid.NewGuid().ToString();

            this.config = config;
            this.channel = channel;
            this.logger = logger;
            subscribed = new HashSet<byte>();
            mapper = new MbapMapper("vrtu");
            OpenWebSocketAsync().GetAwaiter();
        }

        public string Id { get; internal set; }

        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }


        public event System.EventHandler<AdapterErrorEventArgs> OnError;
        public event System.EventHandler<AdapterCloseEventArgs> OnClose;

        public async Task RunAsync()
        {
            try
            {
                map = await RtuMap.LoadAsync(config.StorageConnectionString, config.Container, config.Filename);

                if (map == null)
                {
                    logger?.LogWarning("RTU map is null.");
                    throw new InvalidOperationException("RTU map was not found.");
                }

                logger?.LogInformation("SCADA client adapter loaded RTU map.");

                channel.OnOpen += Channel_OnOpen;
                channel.OnReceive += Channel_OnReceive;
                channel.OnError += Channel_OnError;
                channel.OnClose += Channel_OnClose;
                await channel.OpenAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.Message);
                OnError?.Invoke(this, new AdapterErrorEventArgs(Id, ex));
            }
        }


        private async Task OpenWebSocketAsync()
        {
            connection = null;

            connection = new WebSocketConnection(config, logger);
            connection.OnError += WebSocket_OnError;
            connection.OnClose += WebSocket_OnClose;
            ConnectAckCode code = await connection.OpenAsync();
            restarting = false;
            logger?.LogDebug($"VRTU web socket client opened with code '{code}'");

            if (code != ConnectAckCode.ConnectionAccepted)
            {
                OnError?.Invoke(this,
                    new AdapterErrorEventArgs(Id,
                        new Exception($"SCADA adapter failed to open web socket with code = {code}")));
            }
        }

        private async void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            logger?.LogInformation("SCADA client channel is open.");
            await channel.ReceiveAsync();
        }

        private async void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            logger?.LogDebug("SCADA client channel starting receive.");

            try
            {
                MbapHeader header = MbapHeader.Decode(e.Message);
                RtuPiSystem piSystem = map.GetItem(header.UnitId);

                if (piSystem == null)
                {
                    logger?.LogWarning("SCADA client receive cannot find RTU pi-system.");
                    throw new InvalidOperationException("RTU pi-system was not found.");
                }

                if (!subscribed.Contains(header.UnitId))
                {
                    //subscribe to pi-system for unit id
                    await connection.AddSubscriptionAsync(piSystem.RtuOutputEvent.ToLowerInvariant(), ReceiveOutput);
                    subscribed.Add(header.UnitId);
                }

                byte[] msg = mapper.MapIn(e.Message);
                await connection.SendAsync(piSystem.RtuInputEvent.ToLowerInvariant(), CONTENT_TYPE, msg);
                MbapHeader mheader = MbapHeader.Decode(msg);


                //await connection.Monitor.SendInAsync(ModuleType.VRTU.ToString(), e.Message, mheader.TransactionId);
            }
            catch (Exception ex)
            {
                logger?.LogError($"SCADA client receive error - {ex.Message}");
                OnError?.Invoke(this, new AdapterErrorEventArgs(Id, ex));
            }
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            shutdown = true;
            logger?.LogInformation("SCADA client channel is closed.");
            try
            {
                connection.CloseAsync().GetAwaiter();
                logger?.LogDebug("Closing VRTU Web socket connection.");
            }
            catch
            {
            }

            connection = null;
            OnClose?.Invoke(this, new AdapterCloseEventArgs(Id));
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            shutdown = true;
            logger?.LogInformation($"SCADA client channel generated error - {e.Error.Message}");
            try
            {
                connection.CloseAsync().GetAwaiter();
                logger?.LogDebug("Closing VRTU Web socket connection.");
            }
            catch
            {
            }

            connection = null;
            OnError?.Invoke(this, new AdapterErrorEventArgs(Id, e.Error));
        }

        private async void ReceiveOutput(string topic, string contentType, byte[] message)
        {
            logger?.LogDebug("SCADA client channel receiving output.");

            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                byte[] msg = mapper.MapOut(message);
                await channel.SendAsync(msg);

                MbapHeader actual = MbapHeader.Decode(msg);
                await diagnostics.PublishOutput(actual, header.TransactionId);
                logger?.LogDebug("SCADA client channel was sent output message.");
            }
            catch (Exception ex)
            {
                logger?.LogError($"SCADA client receive output error - {ex.Message}");
                OnError?.Invoke(this, new AdapterErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
                shutdown = true;

                if (connection != null)
                {
                    try
                    {
                        if (connection.IsConnected)
                        {
                            connection.CloseAsync().GetAwaiter();
                        }

                        connection = null;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Fault disposing web socket in SCADA client adapter - {ex.Message}");
                    }
                }

                if (channel != null)
                {
                    try
                    {
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Fault disposing channel in SCADA client adapter - {ex.Message}");
                    }
                }
            }
        }

        #region Web Socket events

        private void WebSocket_OnClose(object sender, ChannelCloseEventArgs e)
        {
            logger?.LogWarning("VRTU Web socket is closed.");
            if (!shutdown && !restarting)
            {
                //restart Web socket 
                restarting = true;
                OpenWebSocketAsync().GetAwaiter();
            }
        }

        private void WebSocket_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError($"VRTU received error from web socket - {e.Error.Message}");
            if (!shutdown && !restarting)
            {
                //restart Web socket
                restarting = true;
                OpenWebSocketAsync().GetAwaiter();
            }
        }

        #endregion
    }
}