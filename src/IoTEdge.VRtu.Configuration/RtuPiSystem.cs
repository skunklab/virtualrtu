using Newtonsoft.Json;
using System;

namespace IoTEdge.VRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class RtuPiSystem
    {
        public RtuPiSystem()
        {

        }

        public RtuPiSystem(string rtuInputEvent, string rtuOutputResource)
        {
            RtuInputEvent = rtuInputEvent;
            RtuOutputEvent = rtuOutputResource;
        }

        public RtuPiSystem(int unitId, string rtuInputEvent, string rtuOutputResource)
        {
            UnitId = unitId;
            RtuInputEvent = rtuInputEvent;
            RtuOutputEvent = rtuOutputResource;
        }

        [JsonProperty("rtuInput")]
        public string RtuInputEvent { get; set; }

        [JsonProperty("rtuOutput")]
        public string RtuOutputEvent { get; set; }

        [JsonProperty("unitId")]
        public int UnitId { get; set; }
    }

   
    
}
