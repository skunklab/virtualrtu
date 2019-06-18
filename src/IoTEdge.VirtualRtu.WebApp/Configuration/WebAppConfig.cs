using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IoTEdge.VirtualRtu.WebApp.Configuration
{
    [Serializable]
    [JsonObject]
    public class WebAppConfig
    {
        public WebAppConfig()
        {
        }

        [JsonProperty("dockerized")]
        public bool Dockerized { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("appId")]
        public string AppId { get; set; }
    }
}
