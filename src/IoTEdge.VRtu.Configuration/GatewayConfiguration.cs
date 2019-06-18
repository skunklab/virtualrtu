using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTEdge.VRtu.Configuration
{
    public class GatewayConfiguration
    {
        public GatewayConfiguration()
        {

        }


        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("securityToken")]
        public string SecurityToken { get; set; }

        public string RtuInputPiSystem { get; set; }

        public string RtuOutputPiSsytem { get; set; }

        public int UnitId { get; set; }

        [JsonProperty("modBusContainer")]
        public string ModBusContainer { get; set; }

        [JsonProperty("modBusPort")]
        public int ModBusPort { get; set; }

        [JsonProperty("modBusPath")]
        public string ModBusPath { get; set; }
    }
}
