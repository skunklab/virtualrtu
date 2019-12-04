using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace VirtualRtu.Configuration.Vrtu
{
    [Serializable]
    [JsonObject]
    public class RangeFilter : IModbusFilter
    {
        private const ushort CT = 1;      //type 1
        private const ushort DI = 10000;  //type 2
        private const ushort IR = 30000;  //type 4
        private const ushort HR = 40000;  //type 3

        private ushort start = 0;
        private ushort end = 0;

        public RangeFilter()
        {
        }



        [JsonProperty("permission")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RangeFilterType Type { get; set; }

        [JsonProperty("start")]
        public ushort StartAddress { get; set; }

        [JsonProperty("end")]
        public ushort EndAddress { get; set; }

        public bool Apply(ushort address, ushort qty, byte scope)
        {
            if (start == 0)
            {
                SetRange(scope);
            }

            ushort max = (ushort)(address + (qty - 1));

            if (Type == RangeFilterType.Allow)
            {
                return max <= end && address >= start && address <= end && max >= start && max <= end;
            }
            else
            {
                return !(max <= address && address >= StartAddress && address <= EndAddress && max >= StartAddress && max <= EndAddress);
            }
        }


        private void SetRange(byte scope)
        {
            ushort factor = 0;
            if (scope == 2)
                factor = DI;
            else if (scope == 3)
                factor = HR;
            else if (scope == 4)
                factor = IR;
            else
                factor = 1;

            start = (ushort)(StartAddress - factor);
            end = (ushort)(EndAddress - factor);
        }


    }
}
