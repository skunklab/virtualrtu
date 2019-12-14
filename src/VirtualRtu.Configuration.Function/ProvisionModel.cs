using Capl.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using VirtualRtu.Configuration.Deployment;
using VirtualRtu.Configuration.Tables;
using VirtualRtu.Configuration.Uris;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Configuration.Function
{
    public class ProvisionModel 
    {
        public ProvisionModel(string luss, FunctionConfig config, ILogger log)
        {
            this.luss = luss;
            this.config = config;
            this.log = log;
        }

        private FunctionConfig config;
        private string luss;
        private ILogger log;

        public async Task<ModuleConfig> ProvisionAsync()
        {
            ModuleConfig configuration = null;

            try
            {
                ContainerEntity entity = await ContainerEntity.LoadAsync(luss, config.TableName, config.StorageConnectionString);               
                if (entity.Access.HasValue)
                {
                    throw new SecurityException("Field gateway LUSS has previously been accessed.");
                }
                else
                {
                    entity.Access = DateTime.UtcNow;
                    await entity.UpdateAsync();
                }

                if (entity.Created > DateTime.UtcNow)
                {
                    throw new SecurityException($"Field gateway LUSS has expired with {entity.Created.ToString()} > {DateTime.Now.ToString()}");
                }

                //add resources to Piraeus
                UpdatePiraeus(entity, config.ApiToken);

                //update the rtu map
                await UpdateRtuMapAsync(entity, config.StorageConnectionString, config.RtuMapContainer, config.RtuMapFilename);

                //create the identity for the field gateway
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(String.Format($"http://{entity.Hostname.ToLowerInvariant()}/name"), entity.DeviceId.ToLowerInvariant()));
                claims.Add(new Claim(String.Format($"http://{entity.Hostname.ToLowerInvariant()}/role"), entity.ModuleId.ToLowerInvariant()));
                string issuer = String.Format($"http://{entity.Hostname.ToLowerInvariant()}/");
                string audience = issuer;

                string securityToken = GetSecurityToken(issuer, audience, claims);

                configuration = new ModuleConfig(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, entity.ModuleId, entity.Slaves, securityToken, entity.LoggingLevel, entity.InstrumentationKey);
            }
            catch (Exception ex)
            {
                log?.LogInformation($"Failed to provision field gateway with luss '{luss}'.");
                log?.LogError(ex.Message);
                throw ex;
            }

            return configuration;
        }

        private async Task UpdateRtuMapAsync(ContainerEntity entity, string connectionString, string container, string filename)
        {
            RtuMap map = await RtuMap.LoadAsync(connectionString, container, filename);
            map = map ?? new RtuMap();
            map.Name = map.Name ?? entity.VirtualRtuId;

            foreach(var slave in entity.Slaves)
            {
                if(map.HasItem(slave.UnitId))
                {
                    map.Remove(slave.UnitId);
                }                

                string requestUriString = UriGenerator.GetRtuPiSystem(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, slave.UnitId, true);
                string responseUriString = UriGenerator.GetRtuPiSystem(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, slave.UnitId, false);
                
                if (slave.Constraints != null && slave.Constraints.Count == 0)
                    slave.Constraints = null;

                map.Add(slave.UnitId, requestUriString, responseUriString, slave.Constraints);
            }

            await map.UpdateAsync(connectionString, container, filename);
        }

        private void UpdatePiraeus(ContainerEntity entity, string apiToken)
        {
            //get the access token to the API
            string accessToken = PiraeusApi.GetAccessToken(entity.Hostname, apiToken);

            //Module CAPL policies
            AuthorizationPolicy modulePublishPolicy = PiraeusApi.CreateDeviceCaplPolicy(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, true);
            AuthorizationPolicy moduleSubscribePolicy = PiraeusApi.CreateDeviceCaplPolicy(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, false);

            //VRTU CAPL policies
            AuthorizationPolicy vrtuPublishPolicy = PiraeusApi.CreateVirtualRtuCaplPolicy(entity.Hostname, entity.VirtualRtuId, true);
            AuthorizationPolicy vrtuSubscribePolicy = PiraeusApi.CreateVirtualRtuCaplPolicy(entity.Hostname, entity.VirtualRtuId, false);

            //add policy(s) to Piraeus
            PiraeusApi.AddCaplPolicy(modulePublishPolicy, entity.Hostname, accessToken);
            PiraeusApi.AddCaplPolicy(moduleSubscribePolicy, entity.Hostname, accessToken);
            PiraeusApi.AddCaplPolicy(vrtuPublishPolicy, entity.Hostname, accessToken);
            PiraeusApi.AddCaplPolicy(vrtuSubscribePolicy, entity.Hostname, accessToken);

            foreach(var slave in entity.Slaves)
            {
                EventMetadata rtuPubSystem = PiraeusApi.CreateRtuPiSystem("VRTU outbound pi-system", entity.Hostname, entity.VirtualRtuId, entity.DeviceId, slave.UnitId, vrtuPublishPolicy, moduleSubscribePolicy, true);
                EventMetadata rtuSubSystem = PiraeusApi.CreateRtuPiSystem("VRTU inbound pi-system", entity.Hostname, entity.VirtualRtuId, entity.DeviceId, slave.UnitId, moduleSubscribePolicy, vrtuPublishPolicy, false);

                PiraeusApi.AddEventMetadata(rtuPubSystem, entity.Hostname, accessToken);
                PiraeusApi.AddEventMetadata(rtuSubSystem, entity.Hostname, accessToken);
            }
           
            //create the diagnostics and telemetry CAPL policies
            AuthorizationPolicy diagnosticsPolicy = PiraeusApi.CreateDiagnosticsRequestPolicy(entity.Hostname, entity.VirtualRtuId);
            PiraeusApi.AddCaplPolicy(diagnosticsPolicy, entity.Hostname, accessToken);

            PiraeusApi.CreateVrtuDiagnosticsPiSystem("Diagnostics pi-system", entity.Hostname, entity.VirtualRtuId, diagnosticsPolicy, vrtuSubscribePolicy, accessToken);
            PiraeusApi.CreateVrtuTelemetryPiSystem("Telemetry pi-system", entity.Hostname, entity.VirtualRtuId, vrtuPublishPolicy, diagnosticsPolicy, accessToken);

            //create the pi-systems for diagnostics 
            EventMetadata diagnosticsPiSystem = PiraeusApi.CreateDeviceDiagnosticsPiSystem("Diagnostics pi-system", entity.Hostname, entity.VirtualRtuId, entity.DeviceId, diagnosticsPolicy, moduleSubscribePolicy);
            PiraeusApi.AddEventMetadata(diagnosticsPiSystem, entity.Hostname, accessToken);

            //create the pi-systems for logging
            EventMetadata telemetryPiSystem = PiraeusApi.CreateDeviceTelemetryPiSystem("Telemetry pi-system", entity.Hostname, entity.VirtualRtuId, entity.DeviceId, modulePublishPolicy, diagnosticsPolicy);
            PiraeusApi.AddEventMetadata(telemetryPiSystem, entity.Hostname, accessToken);
        }

        public virtual string GetSecurityToken(string issuer, string audience, List<Claim> claims)
        {
            JsonWebToken token = new JsonWebToken(config.SymmetricKey, claims, config.LifetimeMinutes, issuer, audience);
            return token.ToString();
        }
    }
}
