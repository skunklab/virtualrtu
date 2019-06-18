using Microsoft.Extensions.Configuration;

namespace IoTEdge.VRtu.FieldGateway.Configuration
{
    public class GatewayConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new GatewayConfigurationProvider();
        }
    }
}
