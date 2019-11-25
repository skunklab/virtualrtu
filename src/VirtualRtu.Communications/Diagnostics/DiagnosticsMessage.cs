using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace VirtualRtu.Communications.Diagnostics
{
    [Serializable]
    [JsonObject]
    public class DiagnosticsMessage
    {
        public DiagnosticsMessage()
        {
        }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DiagnosticsEventType Type { get; set; }
    }
}
