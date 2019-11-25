using Newtonsoft.Json;
using System;

namespace VirtualRtu.Configuration.Function
{
    [Serializable]
    [JsonObject]
    public class FunctionConfig
    {
        public FunctionConfig()
        {
        }


        [JsonProperty("symmetricKey")]
        public string SymmetricKey { get; set; }

        [JsonProperty("apiToken")]
        public string ApiToken { get; set; }

        [JsonProperty("lifetimeMinutes")]
        public int LifetimeMinutes { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("rtuMapContainer")]
        public string RtuMapContainer { get; set; }

        [JsonProperty("rtuMapFilename")]
        public string RtuMapFilename { get; set; }
    }
}
