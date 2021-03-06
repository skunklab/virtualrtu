﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualRtu.WebMonitor.Configuration;

namespace VirtualRtu.WebMonitor
{
    public static class WebMonitorExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services,
            out MonitorConfig monitorConfig)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("./secrets.json")
                .AddEnvironmentVariables("WM_");

            IConfigurationRoot root = builder.Build();

            var config = new MonitorConfig();
            root.Bind(config);

            services.AddSingleton(config);
            monitorConfig = config;
            return services;
        }
    }
}