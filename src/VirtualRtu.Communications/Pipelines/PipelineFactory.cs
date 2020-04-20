using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Pipelines
{
    public abstract class PipelineFactory
    {
        public static Pipeline Create(VrtuConfig config, IChannel input, IChannel output, List<IFilter> inputFiters,
            List<IFilter> outputFilters, ILogger logger = null)
        {
            return new VirtualRtuPipeline(config, input, output, inputFiters, outputFilters, logger);
        }

        public static Pipeline Create(ModuleConfig config, IChannel input, IChannel output, List<IFilter> inputFiters,
            List<IFilter> outputFilters, ILogger logger = null)
        {
            return new ModulePipeline(config, input, output, inputFiters, outputFilters, logger);
        }

        public static Pipeline Create(VConfig config, IChannel input, IChannel output, List<IFilter> inputFiters,
            List<IFilter> outputFilters, ILogger logger = null)
        {
            if (config is VrtuConfig)
            {
                return Create((VrtuConfig) config, input, output, inputFiters, outputFilters, logger);
            }

            if (config is ModuleConfig)
            {
                return Create((ModuleConfig) config, input, output, inputFiters, outputFilters, logger);
            }

            throw new InvalidCastException("VConfig invalid type.");
        }
    }
}