using SkunkLab.Channels;
using VirtualRtu.Configuration;

namespace VirtualRtu.Communications.Pipelines
{
    public interface IPipelineBuilder
    {
        IPipelineBuilder AddConfig(VConfig config);
        IPipelineBuilder AddInputChannel(IChannel channel);
        IPipelineBuilder AddOutputChannel(IChannel channel);
        IPipelineBuilder AddInputFilter(IFilter filter);
        IPipelineBuilder AddOutputFilter(IFilter filter);
        Pipeline Build();
    }
}