using Newtonsoft.Json;
using System;

namespace IoTEdge.VirtualRtu.WebMonitor.Models
{
    [Serializable]
    [JsonObject]
    public class DevAsset
    {
        public DevAsset(string vrtuId, string deviceId)
        {
            Text = deviceId;
            Href = $"#{vrtuId}-{deviceId}";
        }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
