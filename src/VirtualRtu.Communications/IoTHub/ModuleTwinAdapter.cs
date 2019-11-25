using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VirtualRtu.Communications.IoTHub
{
    public class ModuleTwinAdapter
    {
        public ModuleTwinAdapter(ILogger logger = null)
        {
            this.logger = logger;
        }

        public event EventHandler<ModuleTwinEventArgs> OnConfigurationReceived;
        private ModuleClient client;
        private ILogger logger;
        private string jsonString;
        private string luss;

        public async Task StartAsync()
        {
            if(client != null)
            {
                await client.CloseAsync();
            }
           
#if DEBUG
                client = ModuleClient.CreateFromConnectionString(System.Environment.GetEnvironmentVariable("MODULE_CONNECTIONSTRING"));
#else
                client = await ModuleClient.CreateFromEnvironmentAsync();    
#endif
            await client.OpenAsync();
            var moduleTwin = await client.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, client);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);
        }

        public async Task UpdateReportedProperties(string luss)
        {
            TwinCollection collection = new TwinCollection();            
            collection["luss"] = luss;
            collection["timestamp"] = DateTime.UtcNow.ToString();
            await client.UpdateReportedPropertiesAsync(collection);
        }


        private async Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            if(desiredProperties == null)
            {
                await Task.CompletedTask;
            }

            jsonString = null;
            luss = desiredProperties["luss"];
            string serviceUrl = desiredProperties["serviceUrl"];            

            if (!string.IsNullOrEmpty(luss) && !string.IsNullOrEmpty(serviceUrl))
            {
                jsonString = await GetFunctionResultAsync(luss, serviceUrl);
                if (jsonString == null)
                {
                    logger?.LogWarning($"Azure configuration function returned null.");
                }
                else
                {
                    OnConfigurationReceived?.Invoke(this, new ModuleTwinEventArgs(jsonString, luss));
                }
            }
            else
            {                
                logger?.LogInformation($"Desired properities are incomplete LUSS = {luss} - ServiceUrl = {serviceUrl}");
            }
        }

        private async Task<string> GetFunctionResultAsync(string luss, string serviceUrl)
        {
            try
            {
                string requestUrl = null;
#if DEBUG
                requestUrl = String.Format($"{serviceUrl}?luss={luss}");
#else
                requestUrl = String.Format($"{serviceUrl}&luss={luss}");
#endif

                HttpClient httpClient = new HttpClient();
                HttpResponseMessage message = await httpClient.GetAsync(requestUrl);
                if (message.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger?.LogWarning($"Azure configuration function returned status code {message.StatusCode}");
                    return null;
                }

                logger?.LogInformation($"Acquired new configuration from Azure configuration function.");
                return await message.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                logger?.LogError(new EventId(9001, "Azure Function"), ex, "Fault calling configuration service.");
                return null;
            }
        }
    }
}
