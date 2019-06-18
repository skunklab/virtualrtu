using Newtonsoft.Json;
using System;

namespace IoTEdge.VRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class IssuedConfig
    {
        public IssuedConfig()
        {
        }

        public IssuedConfig(string hostname, string securityToken, RtuPiSystem piSystem)
        {
            Hostname = hostname;
            SecurityToken = securityToken;
            PiSystem = piSystem;
        }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }


        [JsonProperty("securityToken")]
        public string SecurityToken { get; set; }

        [JsonProperty("piSystem")]
        public RtuPiSystem PiSystem { get; set; }

    }
}
