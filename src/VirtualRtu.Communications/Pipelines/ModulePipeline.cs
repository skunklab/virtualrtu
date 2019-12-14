using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Pipelines
{
    public class ModulePipeline : Pipeline
    {
        #region ctor
        public ModulePipeline()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ModulePipeline(ModuleConfig config, IChannel input, IChannel output, List<IFilter> inputFilters, List<IFilter> outputFilters, ILogger logger = null)
        {
            Id = Guid.NewGuid().ToString();
            this.config = config;
            InputChannel = input;
            OutputChannel = output;
            InputFilters = inputFilters;
            OutputFilters = outputFilters;
            this.logger = logger;
        }
        #endregion
        #region public properties
        public override string Id { get; set; }
        public override IChannel InputChannel { get; set; }
        public override IChannel OutputChannel { get; set; }
        public override List<IFilter> InputFilters { get; set; }
        public override List<IFilter> OutputFilters { get; set; }
        #endregion

        #region private fields
        private ModuleConfig config;
        private ILogger logger;
        private bool inputDisposed;
        private bool disposed;
        private ExponentialBackoff inputPolicy;
        private ExponentialBackoff outputPolicy;
        private int inputCount;
        private int outputCount;
        #endregion

        #region public methods
        public override void Execute()
        {
            InputChannel.OnOpen += Input_OnOpen;
            InputChannel.OnReceive += Input_OnReceive;
            InputChannel.OnError += Input_OnError;
            InputChannel.OnClose += Input_OnClose;

            OutputChannel.OnClose += Output_OnClose;
            OutputChannel.OnError += Output_OnError;
            OutputChannel.OnReceive += Output_OnReceive;
            OutputChannel.OnOpen += Output_OnOpen;

            if (!InputChannel.IsConnected)
            {
                InputChannel.OpenAsync().GetAwaiter();
            }

            if (!OutputChannel.IsConnected)
            {
                OutputChannel.OpenAsync().GetAwaiter();
                //OutputChannel.ReceiveAsync().GetAwaiter();
            }
        }
        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
                try
                {
                    InputFilters.Clear();
                    OutputFilters.Clear();
                    InputChannel.Dispose();
                    OutputChannel.Dispose();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Fault disposing vrtu pipeline.");
                }

                InputFilters = null;
                OutputFilters = null;
                InputChannel = null;
                OutputChannel = null;
            }
        }
        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Input Events
        private void Input_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            logger?.LogInformation("Input channel open.");
            InputChannel.ReceiveAsync().GetAwaiter();
            logger?.LogDebug("Input channel receiving.");
        }
        private void Input_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            byte[] message = e.Message;
            byte[] msg = message;

            try
            {
                MbapHeader header = MbapHeader.Decode(message);
                if (header == null)
                    return;

                Slave slave = config.Slaves.Where((s) => s.UnitId == header.UnitId).FirstOrDefault();
                byte? alias = slave?.Alias.Value;

                foreach (var filter in InputFilters)
                {
                    msg = filter.Execute(message, alias);
                    msg ??= message;
                    logger?.LogDebug("Filter executed.");
                }

                OutputChannel.SendAsync(msg).GetAwaiter();
                logger?.LogDebug("Message sent to output channel.");
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault input channel receive.");
            }
        }
        private void Input_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError(e.Error, "Fault in input channel.");            
        }
        private void Input_OnClose(object sender, ChannelCloseEventArgs e)
        {
            try
            {
                logger?.LogWarning("Input channel closed.");
                logger?.LogInformation("Restarting input channel.");
                ExecuteInputRetryPolicy();
                InputChannel.OpenAsync().GetAwaiter();
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Fault restarting module input channel.");
                throw ex;
            }
        }
        #endregion

        #region Output Events
        private void Output_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            OutputChannel.ReceiveAsync().GetAwaiter();
        }

        private void Output_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            byte[] message = e.Message;

            if (message.Length < 7)
                return; 

            byte[] msg = null;
            foreach (var filter in OutputFilters)
            {
                msg = filter.Execute(message);
                msg ??= message;
            }

            InputChannel.SendAsync(msg);
        }

        private void Output_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError(e.Error, "Fault in module output channel.");
            OutputChannel.CloseAsync().GetAwaiter();
        }

        private void Output_OnClose(object sender, ChannelCloseEventArgs e)
        {
            try
            {
                logger?.LogWarning("Output channel closed.");
                logger?.LogInformation("Restarting output channel.");
                ExecuteOutputRetryPolicy();
                OutputChannel.OpenAsync().GetAwaiter();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault restarting module output channel.");
                throw ex;
            }
        }

        #endregion

        #region private methods
        private void ExecuteInputRetryPolicy()

        {
            if (inputPolicy == null || !inputPolicy.ShouldRetry(inputCount, null, out TimeSpan interval))
            {
                inputCount = 0;
                inputPolicy = new ExponentialBackoff(5, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(10.0));
            }
            else
            {
                inputCount++;
                Task.Delay(interval).Wait();
            }
        }

        private void ExecuteOutputRetryPolicy()

        {
            if (outputPolicy == null || !outputPolicy.ShouldRetry(inputCount, null, out TimeSpan interval))
            {
                outputCount = 0;
                outputPolicy = new ExponentialBackoff(5, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(2.0));
            }
            else
            {
                outputCount++;
                Task.Delay(interval).Wait();
            }
        }

        #endregion
    }
}
