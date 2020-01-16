using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.WebMonitor.Hubs
{
    public class MonitorHub : Hub
    {
        public MonitorHub(MonitorConfig config, ILogStream logStream)
        {
            this.config = config;
            this.logStream = logStream;
            moduleSubcriptions = new HashSet<string>();
            appSubscriptions = new HashSet<string>();
        }

       
        private MonitorConfig config;
        private ILogStream logStream;
        private HashSet<string> moduleSubcriptions;
        private HashSet<string> appSubscriptions;


        public override Task OnConnectedAsync()
        {           
            return base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            foreach(var item in moduleSubcriptions)
                await logStream.SubscribeAsync(item, false);

            foreach (var item in appSubscriptions)
                await logStream.SubscribeAppInsightsAsync(item, false);

            await base.OnDisconnectedAsync(exception);
        }
        public async Task Subscribe(string resource, bool monitor)
        {
            if(monitor && !moduleSubcriptions.Contains(resource))
                moduleSubcriptions.Add(resource);
            if (!monitor && moduleSubcriptions.Contains(resource))
                moduleSubcriptions.Remove(resource);

            await logStream.SubscribeAsync(resource, monitor);

        }

        public async Task SubscribeAppInsights(string resource, bool monitor)
        {
            if (monitor && !moduleSubcriptions.Contains(resource))
                appSubscriptions.Add(resource);
            if (!monitor && moduleSubcriptions.Contains(resource))
                appSubscriptions.Remove(resource);

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
