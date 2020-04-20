using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VirtualRtu.WebMonitor.Configuration;

namespace VirtualRtu.WebMonitor.Hubs
{
    public class LogStream : ILogStream
    {
        private readonly MonitorConfig config;
        private readonly IHubContext<MonitorHub> context;


        private readonly ClientSingleton cs;

        public LogStream(IHubContext<MonitorHub> context, MonitorConfig config)
        {
            this.context = context;
            this.config = config;
            cs = ClientSingleton.Create(config.Hostname, config.SymmetricKey);
            cs.OnReceive += Cs_OnReceive;
        }

        public async Task SubscribeAsync(string resource, bool monitor)
        {
            await cs.SubscribeAsync(resource, monitor);
        }

        public async Task SubscribeAppInsightsAsync(string resource, bool monitor)
        {
            await cs.SubscribeAppInsightsAsync(resource, monitor);
        }

        private async void Cs_OnReceive(object sender, MonitorEventArgs e)
        {
            await context.Clients.All.SendAsync("ReceiveMessage", Decode(e.ResoureUriString),
                Encoding.UTF8.GetString(e.Message));
        }

        private string Decode(string resourceUriString)
        {
            string id = null;
            Uri uri = new Uri(resourceUriString);
            if (uri.Segments.Length == 3)
            {
                id = uri.Segments[^2].Replace("/", "");
            }
            else
            {
                id = uri.Segments[^3].Replace("/", "") + "-" +
                     uri.Segments[^2].Replace("/", "");
            }

            return id;
        }
    }
}