using Newtonsoft.Json;
using System;

namespace IoTEdge.FieldGateway.Function
{
    [Serializable]
    [JsonObject]
    public class EdgeGatewayConfiguration
    {
        public EdgeGatewayConfiguration()
        {
        }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("securityToken")]
        public string SecurityToken { get; set; }

        [JsonProperty("rtuInputPiSystem")]
        public string RtuInputPiSystem { get; set; }

        [JsonProperty("rtuOutputPiSsytem")]
        public string RtuOutputPiSsytem { get; set; }

        [JsonProperty("unitId")]
        public int UnitId { get; set; }

        [JsonProperty("modBusContainer")]
        public string ModBusContainer { get; set; }

        [JsonProperty("modBusPort")]
        public int ModBusPort { get; set; }

        [JsonProperty("modBusPath")]
        public string ModBusPath { get; set; }
    }
}
