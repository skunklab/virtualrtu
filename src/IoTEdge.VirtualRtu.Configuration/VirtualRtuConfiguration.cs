using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IoTEdge.VirtualRtu.Configuration
{
    public class VirtualRtuConfiguration
    {
        public VirtualRtuConfiguration()
        {
        }

        [JsonProperty("claimTypes")]
        public string ClaimTypes { get; set; }

        [JsonProperty("claimValues")]
        public string ClaimValues { get; set; }

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("symmetricKey")]
        public string SymmetricKey { get; set; }

        [JsonProperty("lifetimeMinutes")]
        public double? LifetimeMinutes { get; set; }

        [JsonProperty("piraeusHostname")]
        public string PiraeusHostname { get; set; }
              

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }


        //[JsonProperty("rtuMapSasUri")]
        //public string RtuMapSasUri { get; set; }

        public IEnumerable<Claim> GetClaimset()
        {
            string[] types = ClaimTypes.Split(";", StringSplitOptions.RemoveEmptyEntries);
            string[] values = ClaimValues.Split(";", StringSplitOptions.RemoveEmptyEntries);
            if(types.Length != values.Length)
            {
                throw new IndexOutOfRangeException("Claim types and values length mismatch.");
            }

            List<Claim> claims = new List<Claim>();

            for(int i=0;i<types.Length;i++)
            {
                claims.Add(new Claim(types[i], values[i]));    
            }

            return claims;
        }
    }

    
}
