using System;
using System.Text;
using Newtonsoft.Json;

namespace VirtualRtu.Configuration.Deployment
{
    [Serializable]
    [JsonObject]
    public class DeviceConfig
    {
        [JsonProperty("deviceId")] public string DeviceId { get; set; }

        [JsonProperty("virtualRtuId")] public string VirtualRtuId { get; set; }

        /// <summary>
        ///     Optional
        /// </summary>
        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        /// <summary>
        ///     Optional
        /// </summary>
        [JsonProperty("iotHubConnectionString")]
        public string IoTHubConnectionString { get; set; }

        /// <summary>
        ///     Optional
        /// </summary>
        [JsonProperty("base64Template")]
        public string Base64EncodedTemplate { get; set; }

        [JsonProperty("expiryMinutes")] public double Expiry { get; set; }

        [JsonProperty("module")] public Module Container { get; set; }

        public string GetTemplate()
        {
            if (!string.IsNullOrEmpty(Base64EncodedTemplate))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedTemplate));
            }

            return null;
        }
    }
}