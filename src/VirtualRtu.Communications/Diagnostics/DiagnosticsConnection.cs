using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Piraeus.Clients.Mqtt;
using SkunkLab.Protocols.Mqtt;
using VirtualRtu.Communications.Caching;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Configuration;
using VirtualRtu.Configuration.Uris;

namespace VirtualRtu.Communications.Diagnostics
{
    public class DiagnosticsConnection
    {
        private readonly LocalCache cache;
        private ModuleConfig config;
        private readonly string inputPiSystem;
        private ILogger logger;

        private readonly PiraeusMqttClient mqttClient;
        private readonly string name;
        private readonly string outputPiSystem;
        private readonly TelemetryClient tclient;
        private readonly TelemetryConfiguration tconfig;

        public DiagnosticsConnection(VrtuConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {
            name = config.VirtualRtuId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            cache = new LocalCache("vrtudiag");
            inputPiSystem = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(config.Hostname, config.VirtualRtuId);
            outputPiSystem = UriGenerator.GetVirtualRtuTelemetryPiSystem(config.Hostname, config.VirtualRtuId);
            if (!string.IsNullOrEmpty(config.InstrumentationKey))
            {
                tconfig = new TelemetryConfiguration(config.InstrumentationKey);
                tconfig.DisableTelemetry = true;
                tclient = new TelemetryClient(tconfig);
            }
        }

        public DiagnosticsConnection(ModuleConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {
            name = config.DeviceId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            cache = new LocalCache("diag");
            inputPiSystem =
                UriGenerator.GetDeviceDiagnosticsPiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId);
            outputPiSystem =
                UriGenerator.GetDeviceTelemetryPiSystem(config.Hostname, config.VirtualRtuId, config.DeviceId);
            if (!string.IsNullOrEmpty(config.InstrumentationKey))
            {
                tconfig = new TelemetryConfiguration(config.InstrumentationKey);
                tconfig.DisableTelemetry = true;
                tclient = new TelemetryClient(tconfig);
            }
        }

        private bool AppInsightsEnabled { get; set; }

        private bool NativeEnabled { get; set; }

        public async Task SubscribeAsync()
        {
            await mqttClient.SubscribeAsync(inputPiSystem, QualityOfServiceLevelType.AtMostOnce, DiagnosticsAction);
        }

        public async Task PublishOutput(MbapHeader header, ushort transactionId)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId,
                DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

            if (NativeEnabled && mqttClient.IsConnected)
            {
                string jsonString = JsonConvert.SerializeObject(telem);
                byte[] msg = Encoding.UTF8.GetBytes(jsonString);
                await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, outputPiSystem, "application/json",
                    msg);
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
                DiagnosticsEvent vdts = new DiagnosticsEvent(name, header.UnitId, transactionId, ts.TotalMilliseconds,
                    DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(vdts);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, outputPiSystem,
                        "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    tclient?.TrackEvent(name, vdts.GetEventProperties(), vdts.GetEventMetrics());
                }
            }
        }

        public async Task PublishInput(MbapHeader header, ushort transactionId)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId,
                DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));
            cache.Add(transactionId.ToString(), new Tuple<byte, long>(header.UnitId, DateTime.Now.Ticks), 20);

            if (NativeEnabled && mqttClient.IsConnected)
            {
                string jsonString = JsonConvert.SerializeObject(telem);
                byte[] msg = Encoding.UTF8.GetBytes(jsonString);
                await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, outputPiSystem, "application/json",
                    msg);
            }

            if (AppInsightsEnabled)
            {
                tclient?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
            }
        }

        private void DiagnosticsAction(string piSystem, string contentType, byte[] message)
        {
            var msg = JsonConvert.DeserializeObject<DiagnosticsMessage>(Encoding.UTF8.GetString(message));

            //toggle diagnostics switches

            NativeEnabled = msg.Type != DiagnosticsEventType.None &&
                            (msg.Type == DiagnosticsEventType.All || msg.Type == DiagnosticsEventType.Native ||
                             NativeEnabled && msg.Type == DiagnosticsEventType.AppInsights);
            AppInsightsEnabled = msg.Type != DiagnosticsEventType.None &&
                                 (msg.Type == DiagnosticsEventType.All ||
                                  msg.Type == DiagnosticsEventType.AppInsights ||
                                  AppInsightsEnabled && msg.Type == DiagnosticsEventType.Native);
        }
    }
}