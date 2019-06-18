
using IoTEdge.VRtu.FieldGateway.Communications;
using IoTEdge.VRtu.FieldGateway.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTEdge.VRtu.FieldGateway
{
    public static class FieldGatewayExtensions
    {
        public static IConfigurationBuilder AddGatewayConfiguration(this IConfigurationBuilder builder)
        {
            GatewayConfigurationSource source = new GatewayConfigurationSource();
            builder.Add(source);
            return builder;
        }
        //public static IServiceCollection AddConfiguration(this IServiceCollection services)
        //{
        //    IConfigurationBuilder builder = new ConfigurationBuilder();            
        //    IConfigurationRoot root = builder.Build();
            
        //    LocalConfig local = new LocalConfig();
        //    IotHubTwin twin = new IotHubTwin();
        //    IssuedConfig config = twin.GetModuleConfigAsync().GetAwaiter().GetResult();
        //    if (config == null)
        //    {
        //        //check for file
        //        if (local.HasDirectory && local.HasFile)
        //        {
        //            config = local.ReadConfig();
        //        }
        //        else
        //        {
        //            Console.WriteLine("Field Gateway cannot be configured.");
        //        }
        //    }
        //    else
        //    {
        //        local.WriteConfig(config);
        //    }

        //    ConfigurationBinder.Bind(root, config);
        //    services.AddSingleton<IssuedConfig>(config);

        //    return services;
        //}
    }
}
