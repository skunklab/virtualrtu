using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace VirtualRtu.Configuration.Deployment
{
    [Serializable]
    [JsonObject]
    public class Module
    {
        public Module()
        {
            Slaves = new List<Slave>();
        }

        private string slaveJson;
        private List<Slave> slaves;

        [JsonProperty("moduleId")]
        public string ModuleId { get; set; }


        [JsonProperty("slaves")]
        public List<Slave> Slaves { get; set; }


        [JsonProperty("loggingLevel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LoggingLevel { get; set; }

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey { get; set; }



    }
}
