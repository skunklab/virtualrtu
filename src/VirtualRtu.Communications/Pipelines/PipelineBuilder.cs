using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Pipelines
{
    public class PipelineBuilder : IPipelineBuilder
    {
        private VConfig config;
        private IChannel input;
        private readonly List<IFilter> inputFilters;


        private readonly ILogger logger;
        private IChannel output;
        private readonly List<IFilter> outputFilters;

        public PipelineBuilder(ILogger logger)
        {
            this.logger = logger;
            inputFilters = new List<IFilter>();
            outputFilters = new List<IFilter>();
        }

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
            return PipelineFactory.Create(builder.config, builder.input, builder.output, builder.inputFilters,
                builder.outputFilters, builder.logger);
        }
    }
}