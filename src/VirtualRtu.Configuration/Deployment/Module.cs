using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VirtualRtu.Configuration.Deployment
{
    [Serializable]
    [JsonObject]
    public class Module
    {
        private string slaveJson;
        private List<Slave> slaves;

        public Module()
        {
            Slaves = new List<Slave>();
        }

        [JsonProperty("moduleId")] public string ModuleId { get; set; }

        [JsonProperty("loggingLevel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LoggingLevel { get; set; }

        [JsonProperty("instrumentationKey")] public string InstrumentationKey { get; set; }

        [JsonProperty("slaves")] public List<Slave> Slaves { get; set; }
    }
}