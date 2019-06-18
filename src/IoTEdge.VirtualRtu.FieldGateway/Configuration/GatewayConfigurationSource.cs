using Microsoft.Extensions.Configuration;

namespace IoTEdge.VirtualRtu.FieldGateway.Configuration
{
    public class GatewayConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new GatewayConfigurationProvider();
        }
    }
}
