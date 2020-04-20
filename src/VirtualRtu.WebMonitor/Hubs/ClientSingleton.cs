using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Security.Tokens;
using VirtualRtu.Communications.Diagnostics;
using VirtualRtu.Configuration.Uris;

namespace VirtualRtu.WebMonitor.Hubs
{
    public class ClientSingleton
    {
        private static ClientSingleton instance;
        private readonly IChannel channel;
        private readonly PiraeusMqttClient client;
        private readonly CancellationTokenSource cts;

        private readonly string hostname;
        private readonly string securityToken;
        private readonly HashSet<string> subscriptions;
        private readonly string symmetricKey;
        private readonly string uriString;

        protected ClientSingleton(string hostname, string symmetricKey)
        {
            this.hostname = hostname;
            this.symmetricKey = symmetricKey;
            subscriptions = new HashSet<string>();
            securityToken = GetSecurityToken();
            uriString = $"wss://{hostname}/ws/api/connect";
            Uri uri = new Uri(uriString);
            cts = new CancellationTokenSource();
            channel = ChannelFactory.Create(uri, securityToken, "mqtt", new WebSocketConfig(), cts.Token);
            client = new PiraeusMqttClient(new MqttConfig(180), channel);
            string sessionId = Guid.NewGuid().ToString();
            ConnectAckCode code = client.ConnectAsync(sessionId, "JWT", securityToken, 180).GetAwaiter().GetResult();

            if (code != ConnectAckCode.ConnectionAccepted)
            {
                throw new Exception("Invalid MQTT connection code.");
            }
        }

        public static ClientSingleton Create(string hostname, string symmetricKey)
        {
            if (instance == null)
            {
                instance = new ClientSingleton(hostname, symmetricKey);
            }

            return instance;
        }

        public event System.EventHandler<MonitorEventArgs> OnReceive;

        public async Task SubscribeAsync(string resource, bool monitor)
        {
            string monitorUriString = null;
            string logUriString = null;

            string[] parts = resource.Split(new[] {"-"}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                //virtual rtu
                monitorUriString = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(hostname, parts[0]);
                logUriString = UriGenerator.GetVirtualRtuTelemetryPiSystem(hostname, parts[0]);
            }
            else if (parts.Length == 2)
            {
                //module
                monitorUriString = UriGenerator.GetDeviceDiagnosticsPiSystem(hostname, parts[0], parts[1]);
                logUriString = UriGenerator.GetDeviceTelemetryPiSystem(hostname, parts[0], parts[1]);
            }

            DiagnosticsMessage mevent = new DiagnosticsMessage
                {Type = monitor ? DiagnosticsEventType.Native : DiagnosticsEventType.None};

            string jsonString = JsonConvert.SerializeObject(mevent);
            await client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, monitorUriString, "application/json",
                Encoding.UTF8.GetBytes(jsonString));

            if (monitor)
            {
                if (!subscriptions.Contains(logUriString))
                {
                    subscriptions.Add(logUriString);
                    await client.SubscribeAsync(logUriString, QualityOfServiceLevelType.AtMostOnce, ReceiveLog);
                }
            }
            else
            {
                if (subscriptions.Contains(logUriString))
                {
                    subscriptions.Remove(logUriString);
                    await client.UnsubscribeAsync(logUriString);
                }
            }
        }

        public async Task SubscribeAppInsightsAsync(string resource, bool monitor)
        {
            string monitorUriString = null;

            string[] parts = resource.Split(new[] {"-"}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                //virtual rtu
                monitorUriString = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(hostname, parts[0]);
            }
            else if (parts.Length == 3)
            {
                //module
                monitorUriString = UriGenerator.GetDeviceDiagnosticsPiSystem(hostname, parts[0], parts[1]);
            }

            DiagnosticsMessage mevent = new DiagnosticsMessage
                {Type = monitor ? DiagnosticsEventType.AppInsights : DiagnosticsEventType.None};
            string jsonString = JsonConvert.SerializeObject(mevent);
            await client.PublishAsync(QualityOfServiceLevelType.AtMostOnce, monitorUriString, "application/json",
                Encoding.UTF8.GetBytes(jsonString));
        }

        private void ReceiveLog(string resourceUriString, string contentType, byte[] message)
        {
            //signal event

            OnReceive?.Invoke(this, new MonitorEventArgs(resourceUriString, contentType, message));
        }

        private string GetSecurityToken()
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim($"http://{hostname.ToLowerInvariant()}/role", "diagnostics")
            };
            string issuer = $"http://{hostname.ToLowerInvariant()}/";
            string audience = issuer;

            JsonWebToken jwt = new JsonWebToken(symmetricKey, claims, 65000, issuer, audience);
            return jwt.ToString();
        }
    }
}