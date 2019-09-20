
using IoTEdge.ModBusTcpAdapter.Communications;
using IoTEdge.ModBusTcpAdapter.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter
{
    public static class AdapterExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            IConfigurationRoot root = builder.Build();
            AdapterConfig adapterConfig = null;
            ModuleTwinConfig mtc = GetModuleTwinConfigAsync().GetAwaiter().GetResult();
            if (mtc != null)
            {
                adapterConfig = mtc.ConvertToConfig();
                string jsonString = JsonConvert.SerializeObject(adapterConfig);
                File.WriteAllBytes(Constants.CONFIG_PATH, Encoding.UTF8.GetBytes(jsonString));
            }
            else
            {
                if(File.Exists(Constants.CONFIG_PATH))
                {
                    byte[] configData = File.ReadAllBytes(Constants.CONFIG_PATH);
                    string jsonString = Encoding.UTF8.GetString(configData);
                    adapterConfig = JsonConvert.DeserializeObject<AdapterConfig>(jsonString);
                }
                else
                {
                    //fault
                    throw new ConfigurationErrorsException("No configuration found for adapter.");
                }
            }

            ConnectionManager manager = new ConnectionManager(adapterConfig);

            ConfigurationBinder.Bind(root, adapterConfig);
            services.AddSingleton<IAdapterConfig>(adapterConfig);
            services.AddSingleton<IConnection>(manager);

            return services;
        }

        private static async Task<ModuleTwinConfig> GetModuleTwinConfigAsync()
        {
            try
            {
                return await ModuleTwinConfig.LoadAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error getting module twin - {ex.Message}");
                return null;
            }
        }
    }
}
