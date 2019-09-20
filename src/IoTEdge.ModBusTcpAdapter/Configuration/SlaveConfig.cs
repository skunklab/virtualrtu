using Newtonsoft.Json;
using System;

namespace IoTEdge.ModBusTcpAdapter.Configuration
{
    [Serializable]
    [JsonObject]      
    public class SlaveConfig
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("unitId")]
        public byte UnitId { get; set; }

        [JsonProperty("unitIdAlias")]
        public byte? UnitIdAlias { get; set; }


    }

   
}
