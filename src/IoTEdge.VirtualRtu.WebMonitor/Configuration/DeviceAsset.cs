using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IoTEdge.VirtualRtu.WebMonitor.Configuration
{
    [Serializable]
    [JsonObject]
    public class DeviceAsset
    {
        public DeviceAsset()
        {
        }

        [JsonProperty("id")]
        public string Id { get; set; }

    }
}
