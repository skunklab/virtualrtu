using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.Pooling;
using IoTEdge.VirtualRtu.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IoTEdge.VirtualRtu
{
    public class Startup
    {
        private VirtualRtuConfiguration config;
        private ClientManager clientManager;

        public IConfiguration Configuration { get; }
        public void Configure()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .AddJsonFile("./secrets.json")
                    .AddEnvironmentVariables("VRTU_");

                IConfigurationRoot root = builder.Build();

                config = new VirtualRtuConfiguration();
                ConfigurationBinder.Bind(root, config);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Startup Configure - {ex.Message}");
                throw ex;
            }

            string endpoint = string.Format($"wss://{config.PiraeusHostname}/ws/api/connect");
            IEnumerable<Claim> claims = config.GetClaimset();
            ClientManager.Configure(endpoint, claims, config);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                services.AddSingleton<VirtualRtuConfiguration>(config);
                //services.AddTransient<ListenerService>();
                services.AddSingleton<ListenerService>();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Startup Configure Services - {ex.Message}");
                throw ex;
            }
        }

        //private void CreateConnectionPool()
        //{
        //    try
        //    {
        //        //JsonWebToken token = new JsonWebToken(config.SymmetricKey, config.GetClaimset(), config.LifetimeMinutes, config.Issuer, config.Audience);

        //        //string securityToken = token.ToString();
        //        string securityToken = GetSecurityToken("vrtu", "manage");
        //        string url = string.Format($"wss://{config.PiraeusHostname}/ws/api/connect");
        //        pool = ConnectionPool.Create(url, securityToken, config.PoolSize);
        //        pool.Init();
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine($"Startup CreateConnectionPool - {ex.Message}");
        //        throw ex;
        //    }
        //}

        string GetSecurityToken(string name, string role)
        {
            //Normally a security token would be obtained externally
            //For the sample we are going to build a token that can
            //be authn'd and authz'd for this sample

            //string issuer = "http://skunklab.io/";
            //string audience = issuer;
            //string nameClaimType = "http://skunklab.io/name";
            //string roleClaimType = "http://skunklab.io/role";
            //string symmetricKey = "//////////////////////////////////////////8=";

            //List<Claim> claims = new List<Claim>()
            //{
            //    new Claim(nameClaimType, name),
            //    new Claim(roleClaimType, role)
            //};

            //return CreateJwt(audience, issuer, claims, symmetricKey, 60.0);
            List<Claim> list = new List<Claim>(config.GetClaimset());
            return CreateJwt(config.Audience, config.Issuer, list, config.SymmetricKey, config.LifetimeMinutes.Value);
        }

        string CreateJwt(string audience, string issuer, List<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            SkunkLab.Security.Tokens.JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }

    }
}
