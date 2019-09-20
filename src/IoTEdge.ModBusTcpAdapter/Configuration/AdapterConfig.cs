using Newtonsoft.Json;
using System;

namespace IoTEdge.ModBusTcpAdapter.Configuration
{
    [Serializable]
    [JsonObject]
    public class AdapterConfig : IAdapterConfig
    {
        public AdapterConfig()
        {
        }

        [JsonProperty("slaves")]
        public SlaveConfig[] Slaves { get; set; }

        [JsonProperty("fieldgatewayContainerName")]
        public string FieldGatewayContainerName { get; set; }

        [JsonProperty("fieldgatewayPort")]
        public int FieldGatewayPort { get; set; }

        [JsonProperty("fieldgatewayPath")]
        public string FieldgatewayPath { get; set; }


    }
}
