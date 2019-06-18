using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SkunkLab.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class RtuMap
    {
        public RtuMap()
        {
            Map = new Dictionary<ushort, RtuPiSystem>();
        }

        public static async Task<RtuMap> LoadAsync(string uriString)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage message = await client.GetAsync(uriString);
            string jsonString = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RtuMap>(jsonString);
        }

        public static async Task<RtuMap> LoadAsync(string connectionString, string containerName, string filename)
        {            
            try
            {
                BlobStorage storage = BlobStorage.CreateSingleton(connectionString);
                byte[] blobBytes = await storage.ReadBlockBlobAsync(containerName, filename);
                string jsonString = Encoding.UTF8.GetString(blobBytes);
                return JsonConvert.DeserializeObject<RtuMap>(jsonString);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Fault loading RTU Map in VRTU - {ex.Message}");
                throw ex;
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



    }
}
