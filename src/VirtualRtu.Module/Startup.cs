using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VirtualRtu.Gateway.Formatters;

namespace VirtualRtu.Module
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("Rtu", "{controller=Manage}/{id}");
            });


        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.InputFormatters.Add(new BinaryInputFormatter());
                options.OutputFormatters.Add(new BinaryOutputFormatter());
            });

            services.AddRouting();
            services.AddMvcCore();

        }
    }
}
