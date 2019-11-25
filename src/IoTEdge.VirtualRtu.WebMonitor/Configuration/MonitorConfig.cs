using Newtonsoft.Json;
using System;

namespace IoTEdge.VirtualRtu.WebMonitor.Configuration
{
    [Serializable]
    [JsonObject]
    public class MonitorConfig
    {
        public MonitorConfig()
        {

        }

        

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("symmetricKey")]
        public string SymmetricKey { get; set; }

       

    }
}
