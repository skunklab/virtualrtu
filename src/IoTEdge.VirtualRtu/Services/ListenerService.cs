using IoTEdge.VirtualRtu.Adapters;
using IoTEdge.VirtualRtu.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.Services
{
    public class ListenerService
    {

        public ListenerService(VirtualRtuConfiguration config)
        {
            this.config = config;            
            adapters = new Dictionary<string, VirtualRtuAdapter>();
            sources = new Dictionary<string, CancellationTokenSource>();
        }

        private VirtualRtuConfiguration config;

        private Dictionary<string, VirtualRtuAdapter> adapters;
        private Dictionary<string, CancellationTokenSource> sources;
        public event EventHandler<ListenerErrorEventArgs> OnError;
        private TcpListener listener;
        private TelemetryClient telemetryClient;       
        private PerformanceCollectorModule pmodule;

        public async Task RunAsync()
        {

            listener = new TcpListener(new IPEndPoint(GetIPAddress(System.Net.Dns.GetHostName()), 502));
            listener.ExclusiveAddressUse = false;            
            listener.Start();

            while(true)
            {
                try
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync();                   
                    tcpClient.LingerState = new LingerOption(true, 0);
                    tcpClient.NoDelay = true;
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    CancellationTokenSource cts = new CancellationTokenSource();
                    
                    RtuMap map = await RtuMap.LoadAsync(config.StorageConnectionString, config.ContainerName, config.Filename);
                    
                    if(map == null)
                    {
                        tcpClient.Close();
                        continue;
                    }


                    if(telemetryClient == null && !string.IsNullOrEmpty(config.AppInsightsKey))
                    {
                        TelemetryConfiguration tc = new TelemetryConfiguration(config.AppInsightsKey);
                        telemetryClient = new TelemetryClient(tc);
                        string machName = String.Format($"VRTU-{System.Environment.MachineName}");
                        telemetryClient.Context.GlobalProperties[machName] = map.Name;
                        TelemetryConfiguration.Active.DisableTelemetry = true;                        
                        pmodule = new PerformanceCollectorModule();
                        pmodule.Initialize(tc);
                    }

                    IChannel channel = ChannelFactory.Create(false, tcpClient, 1024, 1024 * 100, cts.Token);
                    VirtualRtuAdapter adapter = new VirtualRtuAdapter(map, channel, config.PiraeusHostname, config.AppInsightsKey);                    
                    
                    adapter.OnError += Adapter_OnError;
                    adapter.OnClose += Adapter_OnClose;
                    try
                    {
                        await adapter.StartAsync();
                        adapters.Add(adapter.Id, adapter);
                        sources.Add(adapter.Id, cts);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Adapter failed to start with error - {ex.Message}");
                        try
                        {
                            RemoveCancelToken(adapter.Id, false);
                            RemoveAdapter(adapter.Id);
                        }
                        catch { }
                    }
                }
                catch(Exception ex)
                {                   
                    OnError?.Invoke(this, new ListenerErrorEventArgs(ex));
                }
            }
        }

        
        private IPAddress GetIPAddress(string hostname)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                if (hostInfo.AddressList[index].AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostInfo.AddressList[index];
                }
            }

            return null;
        }
        private void Adapter_OnClose(object sender, AdapterCloseEventArgs e)
        {           
            RemoveCancelToken(e.AdapterId, false);
            RemoveAdapter(e.AdapterId);
        }

        private void Adapter_OnError(object sender, AdapterErrorEventArgs e)
        {
            RemoveCancelToken(e.AdapterId, true);
            RemoveAdapter(e.AdapterId);            
        }

        public void Shutdown()
        {
            try
            {
                Dictionary<string, CancellationTokenSource>.Enumerator sourceEn = sources.GetEnumerator();
                while (sourceEn.MoveNext())
                {
                    try
                    {
                        CancellationTokenSource cts = sourceEn.Current.Value;
                        cts.Cancel();
                    }
                    catch { }
                }

                sources.Clear();

                Dictionary<string, VirtualRtuAdapter>.Enumerator adapterEn = adapters.GetEnumerator();
                while (adapterEn.MoveNext())
                {
                    try
                    {
                        VirtualRtuAdapter adapter = adapterEn.Current.Value;
                        adapter.Dispose();
                    }
                    catch { }
                }

                adapters.Clear();
            }
            catch { }

            listener.Stop();
        }


        private void RemoveAdapter(string id)
        {
            try
            {
                if (adapters.ContainsKey(id))
                {
                    adapters[id].Dispose();
                    adapters.Remove(id);
                }
            }
            catch { }
        }

        private void RemoveCancelToken(string id, bool useCancel)
        {
            try
            {
                if (sources.ContainsKey(id))
                {
                    CancellationTokenSource cts = sources[id];
                    sources.Remove(id);

                    if(useCancel)
                        cts.Cancel();
                }
            }
            catch { }
        }
    }
}
