using Newtonsoft.Json;

namespace AzureIoT.Deployment.Function
{
    public class LocalConfig
    {
        public LocalConfig()
        {
        }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("defaultTemplate")]
        public string DefaultTemplate { get; set; }

        [JsonProperty("defaultIoTHubConnectionString")]
        public string DefaultIoTHubConnectionString { get; set; }

        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

    }
}
