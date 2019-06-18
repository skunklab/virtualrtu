using System.Collections.Generic;
using IoTEdge.VirtualRtu.WebApp.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;



using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Identity.Web;

namespace IoTEdge.VirtualRtu.WebApp
{
    public class Startup
    {
        private WebAppConfig config;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var builder = new ConfigurationBuilder()
                .AddJsonFile("./secrets.json")
                .AddEnvironmentVariables("WEB_");

            IConfigurationRoot root = builder.Build();
            config = new WebAppConfig();
            ConfigurationBinder.Bind(root, config);

            services.AddSingleton<WebAppConfig>(config);


            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.AddAzureAdV2Authentication(Configuration);

            //services.AddMvc(o =>
            //{
            //    o.Filters.Add(new AuthorizeFilter("default"));
            //}).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);



            services.AddAuthorization(o =>
            {
                o.AddPolicy("default", policy =>
                {
                    // Require the basic "Access app-name" claim by default
                    policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "user_impersonation");
                });
            });

            services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.Authority = Configuration["Authentication:Authority"];
                o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    // Both App ID URI and client id are valid audiences in the access token
                    ValidAudiences = new List<string>
                    {                      
                        config.AppId,
                        config.ClientId
                    }
                };
            });
            // Add claims transformation to split the scope claim value
            services.AddSingleton<IClaimsTransformation, AzureAdScopeClaimTransformation>();


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.

                if (!config.Dockerized) //assumes ingress controller and SSL offloading if dockerized.
                {
                    app.UseHsts();
                }
            }

            if (!config.Dockerized) //assumes ingress controller and SSL offloading if dockerized.
            {
                app.UseHttpsRedirection();
            }

            //app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();
            //app.UseMvc();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            
        }
    }
}
