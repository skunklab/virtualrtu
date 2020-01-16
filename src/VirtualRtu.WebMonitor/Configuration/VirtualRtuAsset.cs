using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VirtualRtu.WebMonitor.Configuration
{
    [Serializable]
    [JsonObject]
    public class VirtualRtuAsset
    {
        public VirtualRtuAsset()
        {
            Devices = new List<DeviceAsset>();
        }

        public int this[string deviceId]
        {
            get
            {
                return Devices.FindIndex((item) => item.Id == deviceId.ToLowerInvariant());
            }
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("modules")]
        public List<DeviceAsset> Devices { get; set; }
    }
}
