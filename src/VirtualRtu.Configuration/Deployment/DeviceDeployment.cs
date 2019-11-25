using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VirtualRtu.Configuration.Deployment
{
    public class DeviceDeployment
    {
        public DeviceDeployment(string iotHubConnectionString, string deploymentTemplate)
        {
            manager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            template = deploymentTemplate;
            IotHubConnectionStringBuilder builder = IotHubConnectionStringBuilder.Create(iotHubConnectionString);
            hubName = builder.IotHubName;
        }

        private RegistryManager manager;
        private string template;
        private string hubName;

        public async Task<Device> GetDevice(string deviceId)
        {
            return await manager.GetDeviceAsync(deviceId);
        }


        public async Task<Device> CreateDeviceDeploymentAsync(string deviceId)
        {
            Device device = await manager.GetDeviceAsync(deviceId);
            if (device != null)
            {
                await manager.RemoveDeviceAsync(deviceId);
            }

            device = new Device(deviceId);
            device.Capabilities = new DeviceCapabilities() { IotEdge = true };
            device = await manager.AddDeviceAsync(device);
            var config = JsonConvert.DeserializeObject<ConfigurationContent>(template);
            await manager.ApplyConfigurationContentOnDeviceAsync(deviceId, config);

            return device;
        }

        public string GetDeviceConnectionString(Device device)
        {
            return String.Format($"HostName={hubName}.azure-devices.net;DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}");
        }

        public async Task UpdateModuleAsync(Device device, string moduleId, List<KeyValuePair<string, string>> desired)
        {
            Microsoft.Azure.Devices.Module module = await manager.GetModuleAsync(device.Id, moduleId);
            if (module != null)
            {
                Twin twin = await manager.GetTwinAsync(device.Id, moduleId);
                foreach (var item in desired)
                {
                    twin.Properties.Desired[item.Key] = item.Value;
                }

                await manager.UpdateTwinAsync(device.Id, moduleId, twin, twin.ETag);
            }
        }
    }
}
