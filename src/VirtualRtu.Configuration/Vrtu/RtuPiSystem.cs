using Newtonsoft.Json;
using System;

namespace VirtualRtu.Configuration.Vrtu
{
    [Serializable]
    [JsonObject]
    public class RtuPiSystem
    {
        public RtuPiSystem()
        {

        }

        public RtuPiSystem(string rtuInputEvent, string rtuOutputEvent)
        {
            RtuInputEvent = rtuInputEvent;
            RtuOutputEvent = rtuOutputEvent;
        }

        public RtuPiSystem(int unitId, string rtuInputEvent, string rtuOutputEvent)
        {
            UnitId = unitId;
            RtuInputEvent = rtuInputEvent;
            RtuOutputEvent = rtuOutputEvent;
        }

        [JsonProperty("rtuInput")]
        public string RtuInputEvent { get; set; }

        [JsonProperty("rtuOutput")]
        public string RtuOutputEvent { get; set; }

        [JsonProperty("unitId")]
        public int UnitId { get; set; }
    }
}
