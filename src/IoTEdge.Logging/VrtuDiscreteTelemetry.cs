using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IoTEdge.ModBus.Telemetry
{
    [Serializable]
    [JsonObject]
    public class VrtuDiscreteTelemetry
    {       
        public VrtuDiscreteTelemetry()
        {
        }

        public VrtuDiscreteTelemetry(string name, byte unitId, ushort transactionId, DirectionType direction, string timestamp)
        {
            Name = name;
            UnitId = unitId;
            TransactionId = transactionId;
            Direction = direction;
            Timestamp = timestamp;
        }

        public VrtuDiscreteTelemetry(string name, byte unitId, ushort transactionId, double latency, string timestamp)
        {
            Name = name;
            UnitId = unitId;
            TransactionId = transactionId;
            Direction = DirectionType.RoundTrip;
            Latency = latency;
            Timestamp = timestamp;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unitId")]
        public byte UnitId { get; set; }

        [JsonProperty("transactionId")]
        public ushort TransactionId { get; set; }

        [JsonProperty("direction")]
        public DirectionType Direction { get; set; }

        [JsonProperty("latency")]
        public double Latency { get; set; } = -1.0;

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        public IDictionary<string, string> GetEventProperties()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("UnitId", UnitId.ToString());
            properties.Add("TransactionId", TransactionId.ToString());
            properties.Add("Direction", Direction.ToString());
            properties.Add("VrtuTimestamp", DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

            return properties;
        }

        public IDictionary<string, double> GetEventMetrics()
        {
            if (Latency > -1.0)
            {
                IDictionary<string, double> metrics = new Dictionary<string, double>();
                metrics.Add("Latency", Latency);
                return metrics;
            }
            else
            {
                return null;
            }

        }
    }



    
}
