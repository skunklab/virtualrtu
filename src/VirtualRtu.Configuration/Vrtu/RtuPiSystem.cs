using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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

        //public RtuPiSystem(int unitId, string rtuInputEvent, string rtuOutputEvent)
        //{
        //    UnitId = unitId;
        //    RtuInputEvent = rtuInputEvent;
        //    RtuOutputEvent = rtuOutputEvent;
        //}

        public RtuPiSystem(int unitId, string rtuInputEvent, string rtuOutputEvent, List<Constraint> constraints = null)
        {
            UnitId = unitId;
            RtuInputEvent = rtuInputEvent;
            RtuOutputEvent = rtuOutputEvent;            
            Constraints = constraints;
        }

        [JsonProperty("rtuInput")]
        public string RtuInputEvent { get; set; }

        [JsonProperty("rtuOutput")]
        public string RtuOutputEvent { get; set; }

        [JsonProperty("unitId")]
        public int UnitId { get; set; }

        [JsonProperty("constraints")]
        public List<Constraint> Constraints { get; set; }

        public bool Authorize(byte[] message)
        {
            if (Constraints == null || Constraints.Count == 0)
                return true;

            ModbusTcpMessage msg = ModbusTcpMessage.Create(message);
            
            foreach (var constraint in Constraints)
            {   if (constraint.FunctionType == msg.Function)
                {
                    if (!constraint.Apply(msg))
                        return false;
                }
            }

            return true;
        }
    }
}
