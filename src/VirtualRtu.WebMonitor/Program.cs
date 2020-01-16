using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtualRtu.WebMonitor.Configuration;

namespace VirtualRtu.WebMonitor
{
    public class Program
    {
      
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)                
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        string hostname = Dns.GetHostName();
                        Console.WriteLine($"Hostname = {hostname}");
                        IPAddress address = GetIPAddress(hostname);
                        if (hostname == "localhost")
                        {
                            options.Listen(address, 44386);
                        }
                        else
                        {
                            options.ListenAnyIP(8080);
                            Console.WriteLine("Listening on 8080");
                        }
                    });
                    webBuilder.UseStartup<Startup>();
                });


        private static IPAddress GetIPAddress(string hostname)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                if (hostInfo.AddressList[index].AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostInfo.AddressList[index];
                }
            }

            return null;
        }
    }
}
