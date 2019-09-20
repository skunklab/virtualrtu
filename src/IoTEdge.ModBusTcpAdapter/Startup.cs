using IoTEdge.ModBusTcpAdapter.Formatters;
using IoTEdge.VirtualRtu.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTEdge.ModBusTcpAdapter
{
    public class Startup
    {

        public IConfiguration Configuration { get; }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder();

            IConfigurationRoot root = builder.Build();

            EdgeGatewayConfiguration config = new EdgeGatewayConfiguration();
            ConfigurationBinder.Bind(root, config);



            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Rtu}/{action=Index}/{id?}");
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Field Gateway Service Running...");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration();

            services.AddMvc(o =>
            {
                o.InputFormatters.Insert(0, new BinaryInputFormatter());
                o.OutputFormatters.Insert(0, new BinaryOutputFormatter());
            });

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }
    }
}
