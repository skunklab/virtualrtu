using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IoTEdge.VRtu.Configuration
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

        [JsonProperty("map")]
        public Dictionary<ushort, RtuPiSystem> Map { get; set; }

        public void Add(ushort unitId, string rtuInputEvent, string rtuOutputEvent)
        {
            RtuPiSystem pisystem = new RtuPiSystem(unitId, rtuInputEvent, rtuOutputEvent);
            if(Map.ContainsKey(unitId))
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
            if(Map.ContainsKey(unitId))
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
            if(Map.ContainsKey(unitId))
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
