using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using System.Collections.Generic;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Pipelines
{
    public class PipelineBuilder : IPipelineBuilder
    {
        public PipelineBuilder(ILogger logger)
        {
            this.logger = logger;
            this.inputFilters = new List<IFilter>();
            this.outputFilters = new List<IFilter>();
        }



        private ILogger logger;
        private IChannel output;
        private IChannel input;
        private List<IFilter> inputFilters;
        private List<IFilter> outputFilters;
        private VConfig config;

        public IPipelineBuilder AddConfig(VConfig config)
        {
            this.config = config;
            return this;
        }
        public IPipelineBuilder AddInputChannel(IChannel channel)
        {
            input = channel;
            return this;

        }

        public IPipelineBuilder AddOutputChannel(IChannel channel)
        {
            output = channel;
            return this;
        }

        public IPipelineBuilder AddInputFilter(IFilter filter)
        {
            inputFilters.Add(filter);
            return this;
        }

        public IPipelineBuilder AddOutputFilter(IFilter filter)
        {
            outputFilters.Add(filter);
            return this;
        }


        public Pipeline Build()
        {
            return PipelineFactory.Create(config, input, output, inputFilters, outputFilters, logger);
        }

        public static implicit operator Pipeline(PipelineBuilder builder)
        {
            return PipelineFactory.Create(builder.config, builder.input, builder.output, builder.inputFilters, builder.outputFilters, builder.logger);           
        }
    }
}
