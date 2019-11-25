using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using VirtualRtu.Configuration;

namespace VirtualRtu.Gateway
{
    public static class GatewayExtensions
    {
        //public static IServiceCollection AddModuleConfiguration(this IServiceCollection services, out ModuleConfig config)
        //{
        //    IConfigurationBuilder builder = new ConfigurationBuilder();
        //    builder.AddJsonFile("./secrets.json")
        //        .AddEnvironmentVariables("MC_");
        //    IConfigurationRoot root = builder.Build();
        //    config = new ModuleConfig();
        //    ConfigurationBinder.Bind(root, config);
        //    services.AddSingleton<ModuleConfig>(config);

        //    return services;
        //}

        public static IServiceCollection AddModuleConfiguration(this IServiceCollection services, out VrtuConfig config)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile("./secrets.json")
                .AddEnvironmentVariables("VRTU_");
            IConfigurationRoot root = builder.Build();
            config = new VrtuConfig();
            ConfigurationBinder.Bind(root, config);
            services.AddSingleton<VrtuConfig>(config);

            return services;
        }

        public static ILoggingBuilder AddLogging(this ILoggingBuilder builder, VrtuConfig config)
        {
            LogLevel logLevel = config.LoggingLevel;
            builder.AddConsole();
            builder.SetMinimumLevel(logLevel);

            return builder;
        }
    }
}
