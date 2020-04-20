using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace VirtualRtu.WebMonitor.Models
{
    [Serializable]
    [JsonObject]
    public class DevAsset : IComparable<DevAsset>
    {
        public DevAsset(string vrtuId, string deviceId)
        {
            Text = deviceId;
            Href = $"#{vrtuId}-{deviceId}";
        }

        [JsonProperty("text")] public string Text { get; set; }

        [JsonProperty("href")] public string Href { get; set; }

        public int CompareTo([AllowNull] DevAsset other)
        {
            if (other == null)
            {
                return 1;
            }

            return Text.CompareTo(other.Text);
        }
    }
}