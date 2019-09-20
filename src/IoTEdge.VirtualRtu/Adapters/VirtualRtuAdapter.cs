using IoTEdge.ModBus.Telemetry;
using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.Pooling;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.Adapters
{
    public class VirtualRtuAdapter : IDisposable
    {
        public VirtualRtuAdapter(RtuMap map, IChannel channel, string hostname, string instrumentationKey)
        {
            this.instrumentationKey = instrumentationKey;
            this.Id = Guid.NewGuid().ToString();
            this.map = map;
            this.channel = channel;
            this.cache = System.Runtime.Caching.MemoryCache.Default;
            monitoringPiSystem = String.Format($"http://{hostname}/monitor/vrtu/{map.Name}");
            loggingPiSystem = String.Format($"http://{hostname}/log/vrtu/{map.Name}");
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
        private string monitoringPiSystem;
        private string loggingPiSystem;
        private MonitorClient mclient;
        private string instrumentationKey;
        private System.Runtime.Caching.MemoryCache cache;

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
        private void OpenPiraeusClient()
        {
            src = new CancellationTokenSource();
            client = ClientManager.GetClient(src.Token);
            string securityToken = ClientManager.GetSecurityToken();

            ConnectAckCode code = client.ConnectAsync(Guid.NewGuid().ToString(), "JWT", securityToken, 120).GetAwaiter().GetResult();

            if (code != ConnectAckCode.ConnectionAccepted)
            {

                string errorMsg = ($"Connection to Piraeus failed with - {code}");
                Console.WriteLine(errorMsg);
                src.Cancel();

                client = null;
                Exception ex = new Exception("Connection to piraeus failed.");
                throw ex;
            }
            else
            {
                client.OnChannelError += Client_OnChannelError;
                string msg = $"VRTU - {map.Name} connected to Piraeus.";                
                mclient = new MonitorClient(this.monitoringPiSystem, this.loggingPiSystem, client, this.instrumentationKey);
                mclient?.TraceEvent(SeverityLevel.Information, msg);
                Console.WriteLine(msg);
            }
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
                        string msg = String.Format($"PI-System not found for unit id - {unitId.Value}");
                        mclient?.TraceEvent(SeverityLevel.Error, msg);
                        OnError?.Invoke(this, new AdapterErrorEventArgs(Id, new Exception(msg)));                  
                    }
                    else
                    {
                        if(client == null)
                        {
                            try
                            {
                                OpenPiraeusClient();
                            }
                            catch { }
                        }
                        if (!subscribed)
                        {                           
                            client.SubscribeAsync(piSystem.RtuOutputEvent.ToLowerInvariant(), QualityOfServiceLevelType.AtMostOnce, ReceiveOutput).GetAwaiter();
                            subscribed = true;
                            mclient?.TraceEvent(SeverityLevel.Information, ($"VRTU {map.Name} subscribed to {piSystem.RtuOutputEvent.ToLowerInvariant()}"));
                        }

                        if(!cache.Contains(header.TransactionId.ToString()))
                        {
                            cache.Add(header.TransactionId.ToString(), header.UnitId, DateTimeOffset.Now.AddSeconds(30.0));
                        }

                        client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, piSystem.RtuInputEvent.ToLowerInvariant(), contentType, e.Message).GetAwaiter();
                        mclient?.SendInAsync(map.Name, header.UnitId, header.TransactionId).GetAwaiter();
                    }
                }
                else
                {
                    OnError?.Invoke(this, new AdapterErrorEventArgs(Id, new Exception("MBAP UNIT ID mismatch.")));
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
                MbapHeader header = MbapHeader.Decode(message);
                if(cache.Contains(header.TransactionId.ToString()))
                {
                    cache.Remove(header.TransactionId.ToString());
                    channel.SendAsync(message).GetAwaiter();
                    MonitorReceiveOutputAsync(header).GetAwaiter();
                }
                else
                {
                    Console.WriteLine($"Transaction ID {header.TransactionId} not found, dropping message.");
                }
                
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


        private async Task MonitorReceiveOutputAsync(MbapHeader header)
        {
            await mclient?.SendOutAsync(map.Name, header.UnitId, header.TransactionId);
        }

        
    }
}
