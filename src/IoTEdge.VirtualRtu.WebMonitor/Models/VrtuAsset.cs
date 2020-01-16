using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IoTEdge.VirtualRtu.WebMonitor.Models
{
    [Serializable]
    [JsonObject]
    public class VrtuAsset
    {
        public VrtuAsset(VirtualRtuAsset node)
        {
            Text = node.Id;
            Href = "#" + node.Id;
            Nodes = new List<DevAsset>();
            foreach(var item in node.Devices)
            {
                var asset = new DevAsset(node.Id, item.Id);
                Nodes.Add(asset);
            }

            Nodes.Sort();
        }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("nodes")]
        public List<DevAsset> Nodes{ get;set; }
    }
}
