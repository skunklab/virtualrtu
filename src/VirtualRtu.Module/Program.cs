using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using VirtualRtu.Communications.Channels;
using VirtualRtu.Communications.Logging;
using VirtualRtu.Configuration;

namespace VirtualRtu.Module
{
    internal class Program
    {
        private static void Main(string[] args)
        {
<<<<<<< HEAD
=======
            /*
             *











             */
>>>>>>> vnext
            Console.WriteLine("eeee e  eeee e     eeeee");
            Console.WriteLine("8    8  8    8     8   8");
            Console.WriteLine("8eee 8e 8eee 8e    8e  8");
            Console.WriteLine("88   88 88   88    88  8 ");
            Console.WriteLine("88   88 88ee 88eee 88ee8");
            Console.WriteLine("");
            Console.WriteLine("eeeee eeeee eeeee eeee e   e  e eeeee e    e");
            Console.WriteLine("8   8 8   8   8   8    8   8  8 8   8 8    8");
            Console.WriteLine("8e    8eee8   8e  8eee 8e  8  8 8eee8 8eeee8");
            Console.WriteLine("88 \"8 88  8   88  88   88  8  8 88  8   88 ");
            Console.WriteLine("88ee8 88  8   88  88ee 88ee8ee8 88  8   88");
            Console.WriteLine("");

            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddModuleConfiguration(out ModuleConfig config);

                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(config.LoggingLevel);
                    });
                    services.AddLogging(builder => builder.AddLogging(config));
                    services.AddSingleton<Logger>(); //add the logger
                    services.AddSingleton<ModuleTcpChannel>();
                    services.AddHostedService<ModuleService>();
                })
                .ConfigureWebHost(options =>
                {
                    options.UseStartup<Startup>();
                    options.UseKestrel();
                    options.ConfigureKestrel(options =>
                    {
                        options.Limits.MaxConcurrentConnections = 10000;
                        options.Limits.MaxConcurrentUpgradedConnections = 10000;
                        options.Limits.MaxRequestBodySize = 100000;
                        options.Limits.MinRequestBodyDataRate =
                            new MinDataRate(100, TimeSpan.FromSeconds(10));
                        options.Limits.MinResponseDataRate =
                            new MinDataRate(100, TimeSpan.FromSeconds(10));
                        options.ListenAnyIP(8888);
                    });
                });
        }
    }
}