using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public abstract class VConfig
    {
        [JsonProperty("hostname")] public string Hostname { get; set; }

        [JsonProperty("virtualRtuId")] public string VirtualRtuId { get; set; }

        [JsonProperty("instrumentationKey")] public string InstrumentationKey { get; set; }

        [JsonProperty("logLevel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LoggingLevel { get; set; }

        public virtual event EventHandler<ConfigUpdateEventArgs> OnChanged;
    }
}