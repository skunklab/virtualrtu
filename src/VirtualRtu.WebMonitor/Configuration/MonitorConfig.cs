using Newtonsoft.Json;
using System;

namespace VirtualRtu.WebMonitor.Configuration
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

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }


       

    }
}
