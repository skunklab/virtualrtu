using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace VirtualRtu.Configuration.Vrtu
{
    [Serializable]
    [JsonObject]
    public class Constraint
    {
        public Constraint()
        {
            Filters = new List<RangeFilter>();
        }

        [JsonProperty("constraint")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConstraintType Type { get; set; }

        [JsonProperty("scope")]
        public byte FunctionType { get; set; }

        [JsonProperty("filters")]
        public List<RangeFilter> Filters { get; set; }

        public bool Apply(ModbusTcpMessage message)
        {
            if (message is BasicModbusMessage)
            {
                return Apply(message as BasicModbusMessage);
            }
            else if (message is WriteSingleMessage)
            {
                return Apply(message as WriteSingleMessage);
            }
            else if(message is DiagnosticsMessage)
            {
                return Apply(message as DiagnosticsMessage);
            }
            else
            {
                throw new InvalidCastException("ModbusTcpMessage");
            }

        }
        
        private bool Apply(BasicModbusMessage message)
        {
            if (Type == ConstraintType.DenyAll)
                return false;
            if (Type == ConstraintType.AllowAll)
                return true;

            if (Filters == null || Filters.Count == 0)
                return true;

            foreach (var filter in Filters)
            {                
                if (filter.Apply(message.Address, message.Quantity, message.Function))
                    return true;
            }

            return false;
        }

        private bool Apply(WriteSingleMessage message)
        {
            if (Type == ConstraintType.DenyAll)
                return false;
            if (Type == ConstraintType.AllowAll)
                return true;

            if (Filters == null || Filters.Count == 0)
                return true;

            foreach (var filter in Filters)
            {
                if (filter.Apply(message.Address, 1, message.Function))
                    return true;
            }

            return false;
        }

        private bool Apply(DiagnosticsMessage mesage)
        {           
            if (Type == ConstraintType.DenyAll)
                return false;
            if (Type == ConstraintType.AllowAll)
                return true;

            if (Filters == null || Filters.Count == 0)
                return true;

            return false;
        }

    }
}
