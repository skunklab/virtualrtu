using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace IoTEdge.VRtu.FieldGateway
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("------------Starting Field Gateway ----------");
            Console.WriteLine("---------------------------------------------");
            Thread.Sleep(30000);
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
            CreateWebHostBuilder(args).Build().Run();


            //IServiceCollection services = new ServiceCollection();
            //startup.ConfigureServices(services);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            //TODO
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel()
                .ConfigureKestrel((context, options) =>
                {
                    options.Limits.MaxConcurrentConnections = 100;
                    options.Limits.MaxConcurrentUpgradedConnections = 100;
                    options.Limits.MaxRequestBodySize = 1024*10;
                    options.Limits.MinRequestBodyDataRate =
                        new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    options.Limits.MinResponseDataRate =
                        new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    options.ListenAnyIP(8888);
                });
    }
}
