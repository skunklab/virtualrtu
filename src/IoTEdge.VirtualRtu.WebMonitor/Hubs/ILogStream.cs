using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.WebMonitor.Hubs
{
    public interface ILogStream
    {
        Task SubscribeAsync(string resource, bool monitor);

        Task SubscribeAppInsightsAsync(string resource, bool monitor);

    }
}
