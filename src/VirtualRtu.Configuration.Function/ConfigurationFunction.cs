using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VirtualRtu.Configuration.Function
{
    public static class ConfigurationFunction
    {
        private static FunctionConfig config;

        [FunctionName("ConfigurationFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ExecutionContext context)
        {
            ILogger log = null;
            config = new FunctionConfig();

            if (File.Exists(string.Format($"{context.FunctionAppDirectory}/secrets.json")))
            {
                //secrets.json exists use it and environment variables
                var builder = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("secrets.json", true, true)
                    .AddEnvironmentVariables("FUNC_");

                IConfigurationRoot root = builder.Build();
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

                IConfigurationRoot root = builder.Build();
                root.Bind(config);

                config.StorageConnectionString = root.GetConnectionString("StorageConnectionString");
            }
            else
            {
                //no secrets or local.settings.json files...use only environment variables
                var builder = new ConfigurationBuilder()
                    .AddEnvironmentVariables("FUNC_");


                IConfigurationRoot root = builder.Build();
                root.Bind(config);
            }

            string luss = req.Query["luss"];

            try
            {
                if (string.IsNullOrEmpty(luss))
                {
                    throw new InvalidOperationException("No LUSS provided.");
                }

                ProvisionModel model = new ProvisionModel(luss, config, log);
                ModuleConfig moduleConfig = await model.ProvisionAsync();

                foreach (var slave in moduleConfig.Slaves)
                    slave.RemoveConstraints();

                return new OkObjectResult(moduleConfig);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}