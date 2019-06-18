using Newtonsoft.Json;
using System;

namespace IoTEdge.FieldGateway.Function
{
    [Serializable]
    [JsonObject]
    public class ServiceConfig
    {
        public ServiceConfig()
        {
        }

        /// <summary>
        /// Storage account from RTU Map and LUSS Table
        /// </summary>
        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        /// <summary>
        /// The Piraeus hostname, e.g., growlingdog.eastus.cloudapp.azure.com
        /// </summary>
        [JsonProperty("piraeusHostname")]
        public string PiraeusHostname { get; set; }

        /// <summary>
        /// Name claim type for the security token sent to field gateway, e.g., http://skunklab.io/name
        /// </summary>
        [JsonProperty("nameClaimType")]
        public string NameClaimType { get; set; }

        /// <summary>
        /// Role claim type for security token sent to field gateway, e.g., http://skunklab.io/role
        /// </summary>
        [JsonProperty("roleClaimType")]
        public string RoleClaimType { get; set; }

        /// <summary>
        /// Symmetric key base64 encoded (256-bit/32-byte) used to authenticate field gateway with Piraeus
        /// </summary>
        [JsonProperty("symmetricKey")]
        public string SymmetricKey { get; set; }

        /// <summary>
        /// Lifetime of the issued token to the field gateway in minutes
        /// </summary>
        [JsonProperty("lifetimeMinutes")]
        public int LifetimeMinutes { get; set; }

        /// <summary>
        /// Issuer of security token sent to field gateway sent to Piraeus (verified by Piraeus), e.g., http://skunklab.io/
        /// </summary>
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        /// <summary>
        /// Audience of security token sent to field gateway sent to Piraeus (verified by Piraeus), e.g., http://skunklab.io/
        /// </summary>
        [JsonProperty("audience")]
        public string Audience { get; set; }

        /// <summary>
        /// The Piraeus API token (secret) to access the Piraeus Management API
        /// </summary>
        [JsonProperty("piraeusApiToken")]
        public string PiraeusApiToken { get; set; }

        /// <summary>
        /// Container name in storage account where RTU MAP is stored.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        /// The RTU MAP filename.
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// The storage table name where the LUSS is managed.
        /// </summary>
        [JsonProperty("tableName")]
        public string TableName { get; set; }
    }
}
