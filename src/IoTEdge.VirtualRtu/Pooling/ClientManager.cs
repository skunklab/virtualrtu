using IoTEdge.VirtualRtu.Configuration;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace IoTEdge.VirtualRtu.Pooling
{
    public class ClientManager
    {
        public static void Configure(string endpoint, IEnumerable<Claim> claims, VirtualRtuConfiguration config)
        {
            if(instance == null)
            {
                instance = new ClientManager(endpoint, claims, config);
            }
            
        }

        public static string GetSecurityToken()
        {
            return securityToken;
        }
        public static PiraeusMqttClient GetClient(CancellationToken token)
        {
            Uri uri = new Uri(endpoint);
            IChannel channel = ChannelFactory.Create(uri, securityToken, "mqtt", new WebSocketConfig(), token);
            return new PiraeusMqttClient(new MqttConfig(90), channel);
        }

        protected ClientManager(string endpointUrlString, IEnumerable<Claim> claims, VirtualRtuConfiguration vrtuConfig)
        {
            config = vrtuConfig;
            endpoint = endpointUrlString;
            securityToken = GetSecurityToken(claims);
        }


        private static VirtualRtuConfiguration config;
        private static string securityToken;
        private static string endpoint;
        private static ClientManager instance;

        string GetSecurityToken(IEnumerable<Claim> claims)
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
            //List<Claim> list = new List<Claim>(config.GetClaimset());
            return CreateJwt(config.Audience, config.Issuer, claims, config.SymmetricKey, config.LifetimeMinutes.Value);
        }

        string CreateJwt(string audience, string issuer, IEnumerable<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            SkunkLab.Security.Tokens.JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }

    }
}
