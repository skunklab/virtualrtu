using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.FieldGateway.Function
{
    [Serializable]
    [JsonObject]
    public class RtuMap
    {
        public RtuMap()
        {
            Map = new Dictionary<ushort, RtuPiSystem>();
        }

        public static async Task<RtuMap> LoadFromConnectionStringAsync(string container, string filename, string connectionString)
        {
            RtuMap rmap = null;
            CloudStorageAccount acct = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient client = acct.CreateCloudBlobClient();
            CloudBlobContainer containerRef = client.GetContainerReference(container);
            await containerRef.CreateIfNotExistsAsync();
            CloudBlockBlob blobRef = containerRef.GetBlockBlobReference(filename);
            if(await blobRef.ExistsAsync())
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    await blobRef.DownloadToStreamAsync(stream);

                    string jsonString = Encoding.UTF8.GetString(stream.ToArray());
                    rmap = JsonConvert.DeserializeObject<RtuMap>(jsonString);
                }

                return rmap;
            }
            else
            {
                return null;
            }

            
        }


        [JsonProperty("map")]
        public Dictionary<ushort, RtuPiSystem> Map { get; set; }

        public void Add(ushort unitId, string rtuInputEvent, string rtuOutputEvent)
        {
            RtuPiSystem pisystem = new RtuPiSystem(unitId, rtuInputEvent, rtuOutputEvent);
            if (Map.ContainsKey(unitId))
            {
                Map[unitId] = pisystem;
            }
            else
            {
                Map.Add(unitId, pisystem);
            }
        }

        public bool Remove(ushort unitId)
        {
            if (Map.ContainsKey(unitId))
            {
                Map.Remove(unitId);
                return true;
            }
            else
            {
                return false;
            }
        }

        public RtuPiSystem GetItem(ushort unitId)
        {
            if (Map.ContainsKey(unitId))
            {
                return Map[unitId];
            }
            else
            {
                return null;
            }
        }

        public bool HasItem(ushort unitId)
        {
            return Map.ContainsKey(unitId);
        }

        public async Task<bool> UpdateMapAsync(string containerName, string filename, string connectionString)
        {
            bool result = false;

            try
            {
                CloudStorageAccount acct = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient client = acct.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(filename);
                blob.Properties.ContentType = "application/json";
                string jsonString = JsonConvert.SerializeObject(this);
                byte[] payload = Encoding.UTF8.GetBytes(jsonString);

                await blob.UploadFromByteArrayAsync(payload, 0, payload.Length);

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;

        }
    }
}
