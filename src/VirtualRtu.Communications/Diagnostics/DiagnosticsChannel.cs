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
    public class DiagnosticsChannel
    {
        public DiagnosticsChannel(VrtuConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {
            name = config.VirtualRtuId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            cache = new LocalCache(Guid.NewGuid().ToString());

            inputPiSystem = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(config.Hostname, config.VirtualRtuId);
            outputPiSystem = UriGenerator.GetVirtualRtuTelemetryPiSystem(config.Hostname, config.VirtualRtuId);

            if (!string.IsNullOrEmpty(config.InstrumentationKey))
            {
                tconfig = new TelemetryConfiguration(config.InstrumentationKey);
                tconfig.DisableTelemetry = true;
                tclient = new TelemetryClient(tconfig);
            }
        }

        public DiagnosticsChannel(ModuleConfig config, PiraeusMqttClient mqttClient, ILogger logger = null)
        {
            name = config.DeviceId;
            this.mqttClient = mqttClient;
            this.logger = logger;
            cache = new LocalCache(Guid.NewGuid().ToString());

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

        public async Task StartAsync()
        {
            await mqttClient.SubscribeAsync(inputPiSystem, QualityOfServiceLevelType.AtMostOnce, DiagnosticsAction);
        }

        public async Task PublishInput(MbapHeader header)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, header.TransactionId,
                DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

            cache.Add(header.TransactionId.ToString(), DateTime.Now.Ticks, 20);

            if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
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

        public async Task PublishInput(MbapHeader header, ushort transactionId)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId,
                DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));
            cache.Add(transactionId.ToString(), new Tuple<byte, long>(header.UnitId, DateTime.Now.Ticks), 20);

            if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
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

        public async Task PublishOutput(MbapHeader header)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            if (cache.Contains(header.TransactionId.ToString()))
            {
                long ticks = cache.Get<long>(header.TransactionId.ToString());
                cache.Remove(header.TransactionId.ToString());
                TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - ticks);
                DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, header.TransactionId,
                    Math.Round(ts.TotalMilliseconds), DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));

                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(telem);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, outputPiSystem,
                        "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    tclient?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
                }
            }
        }

        public async Task PublishOutput(MbapHeader header, ushort transactionId)
        {
            if (!NativeEnabled && !AppInsightsEnabled)
            {
                return;
            }

            if (cache.Contains(transactionId.ToString()))
            {
                Tuple<byte, long> tuple = cache.Get<Tuple<byte, long>>(transactionId.ToString());
                cache.Remove(transactionId.ToString());
                TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - tuple.Item2);
                DiagnosticsEvent telem = new DiagnosticsEvent(name, header.UnitId, transactionId, header.TransactionId,
                    Math.Round(ts.TotalMilliseconds), DateTime.UtcNow.ToString("dd-MM-yyyyThh:mm:ss.ffff"));


                if (NativeEnabled && mqttClient != null && mqttClient.IsConnected)
                {
                    string jsonString = JsonConvert.SerializeObject(telem);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    await mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, outputPiSystem,
                        "application/json", data);
                }

                if (AppInsightsEnabled)
                {
                    tclient?.TrackEvent(name, telem.GetEventProperties(), telem.GetEventMetrics());
                }
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

        #region private fields

        private readonly PiraeusMqttClient mqttClient;
        private readonly string name;
        private readonly LocalCache cache;
        private ModuleConfig config;
        private ILogger logger;
        private readonly string outputPiSystem;
        private readonly string inputPiSystem;
        private readonly TelemetryConfiguration tconfig;
        private readonly TelemetryClient tclient;

        #endregion

        #region private properties

        private bool AppInsightsEnabled { get; set; }

        private bool NativeEnabled { get; set; }

        #endregion
    }
}