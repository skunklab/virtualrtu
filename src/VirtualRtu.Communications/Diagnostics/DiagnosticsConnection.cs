using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Piraeus.Clients.Mqtt;
using System;
using System.Text;
using System.Threading.Tasks;
using VirtualRtu.Communications.Caching;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.WebSockets;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Uris;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Communications.Diagnostics
{
    public class DiagnosticsConnection
    {
        
        public DiagnosticsConnection(VrtuConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {
            this.name = config.VirtualRtuId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            this.cache = new LocalCache("vrtudiag");
            this.inputPiSystem = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(config.Hostname, config.VirtualRtuId);
            this.outputPiSystem = UriGenerator.GetVirtualRtuTelemetryPiSystem(config.Hostname, config.VirtualRtuId);
            if (!string.IsNullOrEmpty(config.InstrumentationKey))
            {
                tconfig = new TelemetryConfiguration(config.InstrumentationKey);
                tconfig.DisableTelemetry = true;
                tclient = new TelemetryClient(tconfig);
            }
        }
        public DiagnosticsConnection(ModuleConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {            
            this.name = config.DeviceId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            this.cache = new LocalCache("diag");
            this.inputPiSystem = UriGenerator.GetDeviceDiagnosticsPiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId);
            this.outputPiSystem = UriGenerator.GetDeviceTelemetryPiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId);
            if(!string.IsNullOrEmpty(config.InstrumentationKey))
            {
                tconfig = new TelemetryConfiguration(config.InstrumentationKey);
                tconfig.DisableTelemetry = true;
                tclient = new TelemetryClient(tconfig);
            }
        }

        private PiraeusMqttClient mqttClient;
        private string name;
        private LocalCache cache;
        private ModuleConfig config;
        private ILogger logger;
        private string outputPiSystem;
        private string inputPiSystem;
        private TelemetryConfiguration tconfig;
        private TelemetryClient tclient;

        private bool AppInsightsEnabled { get; set; }

        private bool NativeEnabled { get; set; }

        public async Task SubscribeAsync()
        {
            await mqttClient.SubscribeAsync(inputPiSystem, SkunkLab.Protocols.Mqtt.QualityOfServiceLevelType.AtMostOnce, DiagnosticsAction);
        }

        public async Task PublishOutput(MbapHeader header, ushort transactionId)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

            if (NativeEnabled && mqttClient.IsConnected)
            {
                string jsonString = JsonConvert.SerializeObject(telem);
                byte[] msg = Encoding.UTF8.GetBytes(jsonString);
                await mqttClient.PublishAsync(SkunkLab.Protocols.Mqtt.QualityOfServiceLevelType.AtMostOnce, outputPiSystem, "application/json", msg);               
            }

            if (AppInsightsEnabled)
            {
                tclient?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
            }

            if (cache.Contains(transactionId.ToString()))
            {
                Tuple<byte, long> tuple = cache.Get<Tuple<byte, long>>(transactionId.ToString());
                cache.Remove(transactionId.ToString());
                TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - tuple.Item2);
                DiagnosticsEvent vdts = new DiagnosticsEvent(name, header.UnitId, transactionId, ts.TotalMilliseconds, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(vdts);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(SkunkLab.Protocols.Mqtt.QualityOfServiceLevelType.AtMostOnce, outputPiSystem, "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    tclient?.TrackEvent(name, vdts.GetEventProperties(), vdts.GetEventMetrics());
                }
            }
        }
        public async Task PublishInput(MbapHeader header, ushort transactionId)
        {
            if(!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId, DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));
            cache.Add(transactionId.ToString(), new Tuple<byte, long>(header.UnitId, DateTime.Now.Ticks), 20);

            if (NativeEnabled && mqttClient.IsConnected)
            {
                string jsonString = JsonConvert.SerializeObject(telem);
                byte[] msg = Encoding.UTF8.GetBytes(jsonString);
                await mqttClient.PublishAsync(SkunkLab.Protocols.Mqtt.QualityOfServiceLevelType.AtMostOnce, outputPiSystem, "application/json", msg);
            }

            if(AppInsightsEnabled)
            {
                tclient?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
            }
        }

        private void DiagnosticsAction(string piSystem, string contentType, byte[] message)
        {
            var msg = JsonConvert.DeserializeObject<DiagnosticsMessage>(Encoding.UTF8.GetString(message));
            
            //toggle diagnostics switches

            NativeEnabled = msg.Type != DiagnosticsEventType.None && 
                            (msg.Type == DiagnosticsEventType.All || 
                            (msg.Type == DiagnosticsEventType.Native || 
                            NativeEnabled && msg.Type == DiagnosticsEventType.AppInsights));
            AppInsightsEnabled = msg.Type != DiagnosticsEventType.None &&
                            (msg.Type == DiagnosticsEventType.All ||
                            (msg.Type == DiagnosticsEventType.AppInsights ||
                            AppInsightsEnabled && msg.Type == DiagnosticsEventType.Native));
        }

        

    }
}
