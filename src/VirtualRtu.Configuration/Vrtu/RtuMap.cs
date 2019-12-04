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

namespace VirtualRtu.Configuration.Vrtu
{
    [Serializable]
    [JsonObject]
    public class RtuMap
    {
        public RtuMap()
        {
            Map = new Dictionary<byte, RtuPiSystem>();
        }

        public static async Task<RtuMap> LoadAsync(string uriString)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage message = await client.GetAsync(uriString);
            string jsonString = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RtuMap>(jsonString);
        }

        public static async Task<RtuMap> LoadAsync(string connectionString, string container, string filename)
        {
            try
            {
                BlobStorage storage = BlobStorage.CreateSingleton(connectionString);
                byte[] blobBytes = await storage.ReadBlockBlobAsync(container, filename);
                string jsonString = Encoding.UTF8.GetString(blobBytes);
                return JsonConvert.DeserializeObject<RtuMap>(jsonString);
            }
            catch { }

            return null;
        }

        //Name of the Virtual RTU
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("map")]
        public Dictionary<byte, RtuPiSystem> Map { get; set; }

        public void Add(byte unitId, string rtuInputEvent, string rtuOutputEvent, List<Constraint> contraints = null)
        {
            RtuPiSystem pisystem = new RtuPiSystem(unitId, rtuInputEvent, rtuOutputEvent, contraints);
            if (Map.ContainsKey(unitId))
            {
                Map[unitId] = pisystem;
            }
            else
            {
                Map.Add(unitId, pisystem);
            }
        }

        public bool Remove(byte unitId)
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

        public RtuPiSystem GetItem(byte unitId)
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

        public bool HasItem(byte unitId)
        {
            return Map.ContainsKey(unitId);
        }

        public async Task UpdateAsync(string connectionString, string container, string filename)
        {
            BlobStorage storage = BlobStorage.CreateSingleton(connectionString);
            string jsonString = JsonConvert.SerializeObject(this);
            await storage.WriteBlockBlobAsync(container, filename, Encoding.UTF8.GetBytes(jsonString), "application/json");
        }

    }
}
