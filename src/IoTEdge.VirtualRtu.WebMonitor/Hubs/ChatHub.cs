using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.WebMonitor.Hubs
{
    public class ChatHub : Hub
    {
        public ChatHub()
        {
            Console.WriteLine("ChatHub started");
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
