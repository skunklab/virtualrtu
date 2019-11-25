using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using Microsoft.AspNetCore.SignalR;

namespace IoTEdge.VirtualRtu.WebMonitor.Hubs
{
    public class LogStream : ILogStream
    {
        public LogStream(IHubContext<MonitorHub> context, MonitorConfig config)
        {
            this.context = context;
            this.config = config;
            cs = ClientSingleton.Create(config.Hostname, config.SymmetricKey);
            cs.OnReceive += Cs_OnReceive;             
        }

        

        private ClientSingleton cs;
        private MonitorConfig config;
        private IHubContext<MonitorHub> context;

        private async void Cs_OnReceive(object sender, MonitorEventArgs e)
        {  
            await context.Clients.All.SendAsync("ReceiveMessage", Decode(e.ResoureUriString), Encoding.UTF8.GetString(e.Message));
        }

        public async Task SubscribeAsync(string resource, bool monitor)
        {
            await cs.SubscribeAsync(resource, monitor);
        }

        public async Task SubscribeAppInsightsAsync(string resource, bool monitor)
        {
            await cs.SubscribeAppInsightsAsync(resource, monitor);
        }

        private string Decode(string resourceUriString)
        {
            string id = null;
            Uri uri = new Uri(resourceUriString);
            if(uri.Segments.Length == 3)
            {                
                id = uri.Segments[uri.Segments.Length - 2].Replace("/", "");
            }
            else
            {
                id = uri.Segments[uri.Segments.Length - 3].Replace("/","") + "-" + uri.Segments[uri.Segments.Length - 2].Replace("/","");
            }

            return id;
        }

    }
}
