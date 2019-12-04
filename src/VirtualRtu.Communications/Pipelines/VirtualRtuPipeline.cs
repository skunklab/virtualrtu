using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.Pipelines
{
    public class VirtualRtuPipeline : Pipeline
    {
        #region ctor
        public VirtualRtuPipeline()
        {
            Id = Guid.NewGuid().ToString();
        }

        public VirtualRtuPipeline(VrtuConfig config, IChannel input, IChannel output, List<IFilter> inputFiters, List<IFilter> outputFilters, ILogger logger = null)
        {
            Id = Guid.NewGuid().ToString();
            this.config = config;
            this.logger = logger;
            InputChannel = input;
            OutputChannel = output;
            InputFilters = inputFiters;
            OutputFilters = outputFilters;
            map = RtuMap.LoadAsync(config.StorageConnectionString, config.Container, config.Filename).GetAwaiter().GetResult();
        }
        #endregion

        #region private fields
        private VrtuConfig config;
        private ILogger logger;
        private ExponentialBackoff outputPolicy;
        private int outputCount;
        private bool outputDisposed;
        private bool disposed;
        private RtuMap map;
        #endregion

        #region public properties
        public override event System.EventHandler<PipelineErrorEventArgs> OnPipelineError;
        public override string Id { get; set; }
        public override IChannel InputChannel { get; set; }
        public override IChannel OutputChannel { get; set; }      
        public override List<IFilter> InputFilters { get; set; }
        public override List<IFilter> OutputFilters { get; set; }
        #endregion

        #region public methods
        public override void Execute()
        {
            //wire up events
            InputChannel.OnOpen += Input_OnOpen;
            InputChannel.OnReceive += Input_OnReceive;
            InputChannel.OnError += Input_OnError;
            InputChannel.OnClose += Input_OnClose;

            OutputChannel.OnClose += Output_OnClose;
            OutputChannel.OnError += Output_OnError;
            OutputChannel.OnReceive += Output_OnReceive;
            OutputChannel.OnOpen += Output_OnOpen;

            try
            {
                InputChannel.OpenAsync().GetAwaiter();
                OutputChannel.OpenAsync().GetAwaiter();
            }
            catch(Exception ex)
            {
                OnPipelineError?.Invoke(this, new PipelineErrorEventArgs(Id, ex));
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
            InputChannel.ReceiveAsync().GetAwaiter();
        }
        private void Input_OnReceive(object sender, ChannelReceivedEventArgs e)
        {            

            MbapHeader header = MbapHeader.Decode(e.Message);
            if (header == null)
            {
                logger?.LogWarning("MBAP Header returned null");                
                return;  //assume keep alive
            }
            if(!map.HasItem(header.UnitId))
            {
                byte[] errorMsg = ModbusErrorMessage.Create(e.Message, ErrorCode.GatewayPathsNotAvailable);
                this.InputChannel.SendAsync(errorMsg).GetAwaiter();
                return;
            }
            
            if(!map.GetItem(header.UnitId).Authorize(e.Message))
            {
                byte[] errorMsg = ModbusErrorMessage.Create(e.Message, ErrorCode.IllegalAddress);
                this.InputChannel.SendAsync(errorMsg).GetAwaiter();
                return;
            }

            byte[] message = e.Message;
            byte[] msg = null;

            foreach (var filter in InputFilters)
            {
                msg = filter.Execute(message);
                msg ??= message;
            }

            OutputChannel.SendAsync(msg).GetAwaiter();
        }
        private void Input_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError(e.Error, "Fault in input channel.");
            logger?.LogWarning("Shutting down channels in pipeline.");

            //dispose both channels
            outputDisposed = true;

            try
            {
                InputChannel.Dispose();                
            }
            catch { }

            try
            {
                OutputChannel.Dispose();
            }
            catch { }

        }
        private void Input_OnClose(object sender, ChannelCloseEventArgs e)
        {            
            logger?.LogWarning("Input channel closed.");
            OnPipelineError?.Invoke(this, new PipelineErrorEventArgs(this.Id));
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
            


            byte[] msg = null;
            foreach(var filter in OutputFilters)
            {
                msg = filter.Execute(message);
                msg ??= message;
            }

            InputChannel.SendAsync(msg);
        }
        private void Output_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError(e.Error, "Fault in output channel.");

            if (!outputDisposed)
            {
                //restart the channel
                ExecuteOutputRetryPolicyAsync().Wait();
                OutputChannel.OpenAsync().GetAwaiter();
            }
        }
        private void Output_OnClose(object sender, ChannelCloseEventArgs e)
        {
            logger?.LogWarning("Output channel closed.");
        }
        #endregion

        #region private methods
        private async Task ExecuteOutputRetryPolicyAsync()

        {
            if (outputPolicy == null || !outputPolicy.ShouldRetry(outputCount, null, out TimeSpan interval))
            {
                outputCount = 0;
                outputPolicy = new ExponentialBackoff(5, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(10.0));
            }
            else
            {
                outputCount++;
                await Task.Delay(interval);
            }
        }
        #endregion






    }
}
