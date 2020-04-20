using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using VirtualRtu.Communications.Channels;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.Pipelines;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Tcp
{
    public class ScadaClientListener
    {
        private readonly VrtuConfig config;
        private TcpListener listener;
        private readonly ILogger logger;
        private Dictionary<string, Pipeline> pipelines;

        public ScadaClientListener(VrtuConfig config, ILogger logger = null)
        {
            this.config = config;
            this.logger = logger;
            pipelines = new Dictionary<string, Pipeline>();
        }

        public async Task RunAsync()
        {
#if DEBUG
            listener = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 502));
#else
            listener = new TcpListener(new IPEndPoint(GetIPAddress(Dns.GetHostName()), 502));
#endif

            listener.ExclusiveAddressUse = false;
            listener.Start();
            logger?.LogInformation("SCADA client listener started.");

            while (true)
            {
                try
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                    tcpClient.LingerState = new LingerOption(true, 0);
                    tcpClient.NoDelay = true;
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    CancellationTokenSource cts = new CancellationTokenSource();
                    logger?.LogDebug("SCADA client connection acquired.");

                    IChannel inputChannel = ChannelFactory.Create(false, tcpClient, 1024, 1024 * 100, cts.Token);
                    logger?.LogDebug("SCADA client created TCP input channel");

                    IChannel outputChannel = new VirtualRtuChannel(config, logger);
                    logger?.LogDebug("SCADA client created VRTU output channel");

                    MbapMapper mapper = new MbapMapper(Guid.NewGuid().ToString());
                    PipelineBuilder builder = new PipelineBuilder(logger);
                    Pipeline pipeline = builder.AddConfig(config)
                        .AddInputChannel(inputChannel)
                        .AddOutputChannel(outputChannel)
                        .AddInputFilter(new InputMapFilter(mapper))
                        .AddOutputFilter(new OutputMapFilter(mapper))
                        .Build();
                    logger?.LogDebug("SCADA client pipeline built.");

                    pipeline.OnPipelineError += Pipeline_OnPipelineError;
                    pipelines.Add(pipeline.Id, pipeline);
                    pipeline.Execute();
                    logger?.LogDebug("SCADA client pipeline executed.");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault creating pipeline.");
                }
            }
        }

        public async Task Shutdown()
        {
            try
            {
                pipelines.Clear();
                pipelines = null;
                listener = null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Not so gracefull scada client listener shutdown.");
            }

            await Task.CompletedTask;
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

        private void Pipeline_OnPipelineError(object sender, PipelineErrorEventArgs e)
        {
            if (e.Error != null)
            {
                logger?.LogError(e.Error, "Pipe error.");
                logger?.LogWarning("Disposing pipeline.");
            }

            if (pipelines.ContainsKey(e.Id))
            {
                Pipeline pipeline = pipelines[e.Id];
                pipelines.Remove(e.Id);
                try
                {
                    pipeline.Dispose();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault disposing pipeline.");
                }
            }
            else
            {
                logger?.LogWarning("Pipeline not identified to dispose.");
            }
        }
    }
}