using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IoTEdge.VirtualRtu.WebMonitor.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace IoTEdge.VirtualRtu.WebMonitor
{
    public class Startup
    {
        // The Client ID is used by the application to uniquely identify itself to Microsoft identity platform.
        string clientId = "sKkbTpUz6iWzmfJXS0RmJpAaxsHI4APmp4ACFUH9vsg=";

        //// RedirectUri is the URL where the user will be redirected to after they sign in.
        string redirectUri = "http://localhost:1111/";

        //// Tenant is the tenant ID (e.g. contoso.onmicrosoft.com, or 'common' for multi-tenant)
        string tenant = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        //// Authority is the URL for authority, composed by Microsoft identity platform endpoint and the tenant name (e.g. https://login.microsoftonline.com/contoso.onmicrosoft.com/v2.0)
        //string authority = String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://login.microsoftonline.com/microsoft.com/v2.0", tenant);

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration();
            services.AddSingleton<ILogStream, LogStream>();

            
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
            //        .AddAzureAD(options => Configuration.Bind("AzureAd", options));  
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options =>
                {
                    options.CallbackPath = "/signin-oidc";
                    options.ClientId = "1684ffe2-f7a6-4fb8-883e-3949c021a027";
                    options.Domain = "microsoft.onmicrosoft.com";
                    options.TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
                    options.SignedOutCallbackPath = "/signout-oidc";
                    options.Instance = "https://login.microsoftonline.com/";
                });

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                options.Authority = options.Authority + "/v2.0/";         // Microsoft identity platform
                //options.Authority = "https://login.microsoftonline.com/microsoft.com/v2.0";

                options.TokenValidationParameters.ValidateIssuer = false; // accept several tenants (here simplified)
            });

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);


            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddSignalR();
            services.AddMvc(options => options.EnableEndpointRouting = false);
        }


       
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseRouting();
            app.UseEndpoints(ac => ac.MapHub<MonitorHub>("/monitorHub"));

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            //app.UseSignalR(routes =>
            //{
            //    routes.MapHub<MonitorHub>("/monitorHub");
            //});

            app.UseMvc();
        }

     
    }
}
