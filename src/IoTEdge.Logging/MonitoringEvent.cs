using Newtonsoft.Json;
using System;

namespace IoTEdge.ModBus.Telemetry
{
    [Serializable]
    [JsonObject]
    public class MonitoringEvent
    {
        public MonitoringEvent()
        {
        }

        [JsonProperty("type")]
        public MonitoringEventType Type { get; set; }
    }
}
