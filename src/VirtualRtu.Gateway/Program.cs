using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using VirtualRtu.Communications.Logging;
using VirtualRtu.Configuration;

namespace VirtualRtu.Gateway
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ee   e eeeee eeeee e   e ");
            Console.WriteLine("88   8 8   8   8   8   8");
            Console.WriteLine("88  e8 8eee8e  8e  8e  8 ");
            Console.WriteLine(" 8  8  88   8  88  88  8");
            Console.WriteLine(" 8ee8  88   8  88  88ee8 ");
            Console.WriteLine("");

            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddModuleConfiguration(out VrtuConfig config);

                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(config.LoggingLevel);
                });
                services.AddLogging(builder => builder.AddLogging(config));
                services.AddSingleton<Logger>();    //add the logger
                services.AddHostedService<VirtualRtuService>();
            });
            
    }
}
