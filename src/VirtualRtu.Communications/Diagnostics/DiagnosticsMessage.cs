using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VirtualRtu.Communications.Diagnostics
{
    [Serializable]
    [JsonObject]
    public class DiagnosticsMessage
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DiagnosticsEventType Type { get; set; }
    }
}