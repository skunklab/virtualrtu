using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AzureIoT.Deployment.Function.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VirtualRtu.Configuration.Deployment;
using VirtualRtu.Configuration.Tables;

namespace AzureIoT.Deployment.Function
{
    public static class DeploymentFunction
    {
        [FunctionName("DeploymentFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            ExecutionContext context)
        {
            ILogger log = null;
            LocalConfig config = new LocalConfig();

            try
            {
                IConfigurationRoot root = null;
                if (File.Exists(string.Format($"{context.FunctionAppDirectory}/secrets.json")))
                {
                    //secrets.json exists use it and environment variables
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("secrets.json", true, true)
                        .AddEnvironmentVariables("FUNC_");


                    root = builder.Build();
                    root.Bind(config);
                }
                else if (File.Exists(string.Format("{0}/{1}", context.FunctionAppDirectory, "local.settings.json")))
                {
                    //use for local testing...do not use in production
                    //remember to add the storage connection string
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", true, true)
                        .AddEnvironmentVariables("FUNC_");

                    root = builder.Build();
                    root.Bind(config);

                    config.StorageConnectionString = root.GetConnectionString("StorageConnectionString");
                }
                else
                {
                    //no secrets or local.settings.json files...use only environment variables
                    var builder = new ConfigurationBuilder()
                        .AddEnvironmentVariables("FUNC_");

                    root = builder.Build();
                    root.Bind(config);
                }
            }
            catch (Exception ex)
            {
                log?.LogError(ex.Message);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DeviceConfig dconfig = JsonConvert.DeserializeObject<DeviceConfig>(requestBody);
            dconfig = ConfigurationResolver.Configure(dconfig, config);

            try
            {
                string template = dconfig.GetTemplate();
                string funcType = req.Query["type"];
                DeviceDeployment deployment = new DeviceDeployment(dconfig.IoTHubConnectionString, template);

                if (string.IsNullOrEmpty(funcType))
                {
                    string connectionString = await DeployFull(config, dconfig, deployment);
                    return !string.IsNullOrEmpty(connectionString)
                        ? (ActionResult) new OkObjectResult(connectionString)
                        : new BadRequestObjectResult("Invalid connection string to return.");
                }

                if (funcType.ToLowerInvariant() == "update")
                {
                    string luss = await UpdateTableAsync(config, dconfig);
                    Device device = await deployment.GetDevice(dconfig.DeviceId);
                    await UpdateModuleAsync(device, dconfig.Container.ModuleId, luss, config.ServiceUrl, deployment);
                    return new OkResult();
                }

                return new BadRequestObjectResult("Invalid type parameter in query string.");
            }
            catch (Exception ex)
            {
                log?.LogError(ex.Message);
                return new BadRequestObjectResult("Failed to provision.");
            }
        }

        private static async Task<string> UpdateTableAsync(LocalConfig config, DeviceConfig dconfig)
        {
            var luss = LussGenerator.Create();
            ContainerEntity entity = new ContainerEntity(luss, config.Hostname, dconfig.Container.ModuleId,
                dconfig.VirtualRtuId, dconfig.DeviceId, dconfig.Container.Slaves, dconfig.Container.LoggingLevel,
                dconfig.Container.InstrumentationKey, TimeSpan.FromMinutes(dconfig.Expiry), config.TableName,
                dconfig.StorageConnectionString);
            await entity.UpdateAsync();
            return luss;
        }

        private static async Task UpdateModuleAsync(Device device, string moduleId, string luss, string serviceUrl,
            DeviceDeployment deployment)
        {
            List<KeyValuePair<string, string>> properties = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("luss", luss),
                new KeyValuePair<string, string>("serviceUrl", serviceUrl)
            };

            await deployment.UpdateModuleAsync(device, moduleId, properties);
        }

        private static async Task<string> DeployFull(LocalConfig config, DeviceConfig dconfig,
            DeviceDeployment deployment)
        {
            string luss = await UpdateTableAsync(config, dconfig);
            Device device = await deployment.CreateDeviceDeploymentAsync(dconfig.DeviceId);
            await UpdateModuleAsync(device, dconfig.Container.ModuleId, luss, config.ServiceUrl, deployment);
            return deployment.GetDeviceConnectionString(device);
        }
    }
}