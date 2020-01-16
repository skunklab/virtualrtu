using VirtualRtu.WebMonitor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualRtu.WebMonitor
{
    public static class WebMonitorExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, out MonitorConfig monitorConfig)
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("./secrets.json")
                    .AddEnvironmentVariables("WM_");

            IConfigurationRoot root = builder.Build();

            var config = new MonitorConfig();
            ConfigurationBinder.Bind(root, config);

            services.AddSingleton<MonitorConfig>(config);
            monitorConfig = config;
            return services;
        }
    }
}
