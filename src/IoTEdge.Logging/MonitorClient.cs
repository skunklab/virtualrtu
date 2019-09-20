using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using Piraeus.Clients.Mqtt;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.ModBus.Telemetry
{
    public class MonitorClient
    {        

        public MonitorClient(string monitoringPiSystem, string loggingPiSystem, PiraeusMqttClient mclient, string appInsightsKey = null)
        {
            this.monitorSystem = monitoringPiSystem;
            this.logSystem = loggingPiSystem;
            MqttClient = mclient;
            cache = MemoryCache.Default;

            if(appInsightsKey != null)
            {
                TelemetryConfiguration config = new TelemetryConfiguration(appInsightsKey);                
                client = new TelemetryClient(config);
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }
        }
        private TelemetryClient client;
        private PiraeusMqttClient mqttClient;
        private string monitorSystem;
        private string logSystem;
        private MemoryCache cache;

        public bool AppInsightsEnabled
        {
            get { return !TelemetryConfiguration.Active.DisableTelemetry; }
            set { TelemetryConfiguration.Active.DisableTelemetry = !value; }
        }

        public bool NativeEnabled { get; set; }

        public PiraeusMqttClient MqttClient
        {
            get { return mqttClient; }
            set
            {
                if(mqttClient != null && mqttClient.IsConnected)
                {
                    UnsubscribeMonitor().GetAwaiter();
                }

                mqttClient = value;
                SubscribeMonitor().GetAwaiter();
            }
        }

        public async Task SendInAsync(string name, byte unitId, ushort transactionId)
        {
            try
            {
                if(cache.Contains(transactionId.ToString()))
                {
                    cache.Remove(transactionId.ToString());
                }

                cache.Add(transactionId.ToString(), new Tuple<byte,long>(unitId, DateTime.Now.Ticks), DateTimeOffset.Now.AddSeconds(20.0));

                VrtuDiscreteTelemetry telem = new VrtuDiscreteTelemetry(name, unitId, transactionId, DirectionType.In, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));
                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(telem);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, logSystem, "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    client.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Monitor error - SendInAsync {ex.Message}");
            }
        }

        public async Task SendOutAsync(string name, byte unitId, ushort transactionId)
        {
            try
            {                
                VrtuDiscreteTelemetry telem = new VrtuDiscreteTelemetry(name, unitId, transactionId, DirectionType.Out, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));
                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(telem);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, logSystem, "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    client?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
                }

                if (cache.Contains(transactionId.ToString()))
                {
                    Tuple<byte, long> tuple = (Tuple<byte, long>)cache[transactionId.ToString()];
                    cache.Remove(transactionId.ToString());
                    TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - tuple.Item2);
                    VrtuDiscreteTelemetry vdts = new VrtuDiscreteTelemetry(name, unitId, transactionId, ts.TotalMilliseconds, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

                    if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                    {   
                        string jsonString = JsonConvert.SerializeObject(vdts);
                        byte[] data = Encoding.UTF8.GetBytes(jsonString);
                        await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, logSystem, "application/json", data);
                    }

                    if (AppInsightsEnabled)
                    {
                        client?.TrackEvent(name, vdts.GetEventProperties(), vdts.GetEventMetrics());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Monitor error - SendOutAsync {ex.Message}");
            }
        }

        //public async Task SendRoundtripAsync(string name, byte unitId, ushort transactionId, long previousTicks)
        //{
        //    try
        //    {
        //        long timeDelta = DateTime.UtcNow.Ticks - previousTicks;
        //        TimeSpan ts = TimeSpan.FromTicks(timeDelta);
        //        string timestamp = DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff");
        //        VrtuDiscreteTelemetry telem = new VrtuDiscreteTelemetry(name, unitId, transactionId, ts.TotalMilliseconds, timestamp);
        //        if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
        //        {
        //            string jsonString = JsonConvert.SerializeObject(telem);
        //            byte[] data = Encoding.UTF8.GetBytes(jsonString);
        //            await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, logSystem, "application/json", data);
        //        }

        //        if (AppInsightsEnabled)
        //        {
        //            client?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine($"Monitor error - SendRoundtripAsync {ex.Message}");
        //    }
        //}

        public void TraceEvent(SeverityLevel severity, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (!TelemetryConfiguration.Active.DisableTelemetry)
            {
                client?.TrackTrace(message, severity);
            }
        }

        private async Task SubscribeMonitor()
        {
            if(mqttClient != null && mqttClient.IsConnected)
            {
                await mqttClient.SubscribeAsync(monitorSystem, QualityOfServiceLevelType.AtMostOnce, MonitorAction);
            }
        }

        private async Task UnsubscribeMonitor()
        {
            if (mqttClient != null && mqttClient.IsConnected)
            {
                await mqttClient.UnsubscribeAsync(monitorSystem);
            }
        }

        private void MonitorAction(string topic, string contentType, byte[] message)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(message);
                MonitoringEvent mevent = JsonConvert.DeserializeObject<MonitoringEvent>(jsonString);

                switch (mevent.Type)
                {
                    case MonitoringEventType.None:
                        AppInsightsEnabled = false;
                        NativeEnabled = false;
                        break;
                    case MonitoringEventType.AppInsights:
                        AppInsightsEnabled = true;
                        NativeEnabled = false;
                        break;
                    case MonitoringEventType.Native:
                        AppInsightsEnabled = false;
                        NativeEnabled = true;
                        break;
                    case MonitoringEventType.All:
                        AppInsightsEnabled = true;
                        NativeEnabled = true;
                        break;
                    default:
                        Console.WriteLine("Invalid monitoring event type. Monitoring stopped.");
                        AppInsightsEnabled = false;
                        NativeEnabled = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                AppInsightsEnabled = false;
                NativeEnabled = false;
                Console.WriteLine($"Fault reading monitoring event type - {ex.Message}");
                Console.WriteLine("Monitoring stopped.");
            }
        }


    }
}
