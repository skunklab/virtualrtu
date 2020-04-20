using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkunkLab.Channels;
using VirtualRtu.Communications.Channels;
using VirtualRtu.Communications.IoTHub;
using VirtualRtu.Communications.Logging;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.Pipelines;
using VirtualRtu.Configuration;

namespace VirtualRtu.Module
{
    public class ModuleService : IHostedService
    {
        private readonly ModuleTwinAdapter adapter;
        private readonly ModuleConfig config;
        private IChannel input;

        private readonly ILogger logger;
        private readonly MbapMapper mapper;
        private IChannel output;
        private Pipeline pipeline;

        public ModuleService(ModuleConfig config, ModuleTcpChannel channel, Logger logger = null)
        {
            this.config = config;
            output = channel;
            this.logger = logger;
            mapper = new MbapMapper(Guid.NewGuid().ToString());
            adapter = new ModuleTwinAdapter();
            adapter.OnConfigurationReceived += Adapter_OnConfigurationReceived;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
#if DEBUG
            await Task.Delay(5000);
#endif
            await adapter.StartAsync();

            //wait for 5 minutes to get the configuration; otherwise fault
            int attempts = 0;
            while (!File.Exists("./data/config.json"))
            {
                attempts++;
                await Task.Delay(30000); //wait 30 seconds for new configuration
                if (attempts >= 10)
                {
                    break;
                }
            }

            if (attempts < 10)
            {
                BuildPipeline();
            }
            else
            {
                throw new Exception("No configuration available.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger?.LogInformation("Module service stopping.");
            DisposePipeline();
            await Task.CompletedTask;
        }

        private void BuildPipeline()
        {
            DisposePipeline();

            input = new ModuleChannel(config, logger);
            if (output == null)
            {
                output = new ModuleTcpChannel(config, logger);
            }

            try
            {
                PipelineBuilder builder = new PipelineBuilder(logger);
                pipeline = builder.AddConfig(config)
                    .AddInputChannel(input)
                    .AddOutputChannel(output)
                    .AddOutputFilter(new OutputMapFilter(mapper))
                    .Build();
                //pipeline = builder.AddConfig(config)
                //    .AddInputChannel(input)
                //    .AddOutputChannel(output)
                //    .AddInputFilter(new InputMapFilter(mapper))
                //    .AddOutputFilter(new OutputMapFilter(mapper))
                //    .Build();

                logger?.LogInformation("Module client pipeline built.");

                pipeline.OnPipelineError += Pipeline_OnPipelineError;
                pipeline.Execute();
                logger?.LogInformation("Module pipeline running.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault building module pipeline.");
                throw ex;
            }
        }

        private void DisposePipeline()
        {
            if (pipeline == null)
            {
                return;
            }

            try
            {
                pipeline.Dispose();
            }
            catch
            {
            }

            pipeline = null;

            logger?.LogInformation("Module pipeline disposed.");
        }

        private void Pipeline_OnPipelineError(object sender, PipelineErrorEventArgs e)
        {
            logger?.LogError(e.Error, "Fault in module pipeline.");
            BuildPipeline();
        }

        private void Adapter_OnConfigurationReceived(object sender, ModuleTwinEventArgs e)
        {
            logger?.LogInformation("Module twin received update.");

            if (e.JsonConfigString != null)
            {
                try
                {
                    ModuleConfig moduleConfig = JsonConvert.DeserializeObject<ModuleConfig>(e.JsonConfigString);
                    File.WriteAllText("./data/config.json", e.JsonConfigString);
                    adapter.UpdateReportedProperties(e.Luss).GetAwaiter();
                    logger?.LogInformation("New module configuration updated.");
                    config.UpdateConfig(e.JsonConfigString);
                    logger?.LogDebug("Must rebuild the pipeline due to update.");
                    BuildPipeline();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault writing new module config.");
                }
            }
        }
    }
}