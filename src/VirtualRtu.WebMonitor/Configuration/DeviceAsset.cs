using System;
using Newtonsoft.Json;

namespace VirtualRtu.WebMonitor.Configuration
{
    [Serializable]
    [JsonObject]
    public class DeviceAsset
    {
        [JsonProperty("id")] public string Id { get; set; }
    }
}