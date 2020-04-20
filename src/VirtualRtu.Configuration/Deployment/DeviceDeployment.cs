using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace VirtualRtu.Configuration.Deployment
{
    public class DeviceDeployment
    {
        private readonly string hubName;

        private readonly RegistryManager manager;
        private readonly string template;

        public DeviceDeployment(string iotHubConnectionString, string deploymentTemplate)
        {
            manager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            template = deploymentTemplate;
            IotHubConnectionStringBuilder builder = IotHubConnectionStringBuilder.Create(iotHubConnectionString);
            hubName = builder.IotHubName;
        }

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
            device.Capabilities = new DeviceCapabilities {IotEdge = true};
            device = await manager.AddDeviceAsync(device);
            var config = JsonConvert.DeserializeObject<ConfigurationContent>(template);
            await manager.ApplyConfigurationContentOnDeviceAsync(deviceId, config);

            return device;
        }

        public string GetDeviceConnectionString(Device device)
        {
            return string.Format(
                $"HostName={hubName}.azure-devices.net;DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}");
        }

        public async Task UpdateModuleAsync(Device device, string moduleId, List<KeyValuePair<string, string>> desired)
        {
            Microsoft.Azure.Devices.Module module = await manager.GetModuleAsync(device.Id, moduleId);
            if (module != null)
            {
                Twin twin = await manager.GetTwinAsync(device.Id, moduleId);
                foreach (var item in desired) twin.Properties.Desired[item.Key] = item.Value;

                await manager.UpdateTwinAsync(device.Id, moduleId, twin, twin.ETag);
            }
        }
    }
}