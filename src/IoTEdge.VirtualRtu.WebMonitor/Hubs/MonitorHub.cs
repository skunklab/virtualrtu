using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.WebMonitor.Hubs
{
    public class MonitorHub : Hub
    {
        public MonitorHub(MonitorConfig config, ILogStream logStream)
        {
            this.config = config;
            this.logStream = logStream;
        }

       
        private MonitorConfig config;
        private ILogStream logStream;


        public override Task OnConnectedAsync()
        {           
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
        public async Task Subscribe(string resource, bool monitor)
        {
            await logStream.SubscribeAsync(resource, monitor);
        }

        public async Task SubscribeAppInsights(string resource, bool monitor)
        {
            await logStream.SubscribeAppInsightsAsync(resource, monitor);
        }

        private string Decode(string resourceUriString)
        {
            string id = null;
            Uri uri = new Uri(resourceUriString);
            Uri uriNext = new Uri($"http://{uri.AbsolutePath.Remove(0, 1)}");

            if (uriNext.Segments.Length == 4)
            {
                id = $"{uriNext.Segments[1].Replace("/", "-")}{uriNext.Segments[2].Replace("/", "-")}{uriNext.Segments[3]}";
            }

            if (uriNext.Segments.Length == 2)
            {
                id = uriNext.Segments[1].Replace("/", "");
            }

            return id;
        }

    }
}
