using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public abstract class VConfig
    {
        public VConfig()
        {
        }

        public virtual event EventHandler<ConfigUpdateEventArgs> OnChanged;

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("virtualRtuId")]
        public string VirtualRtuId { get; set; }

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey { get; set; }

        [JsonProperty("logLevel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LoggingLevel { get; set; }
    }
}
