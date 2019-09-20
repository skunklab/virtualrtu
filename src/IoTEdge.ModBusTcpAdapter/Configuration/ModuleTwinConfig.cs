using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Configuration
{
    [Serializable]
    public class ModuleTwinConfig
    {
        public ModuleTwinConfig()
        {
        }

        public static async Task<ModuleTwinConfig> LoadAsync()
        {
            ModuleClient client = await ModuleClient.CreateFromEnvironmentAsync();
            await client.OpenAsync();
            Twin twin = await client.GetTwinAsync();
            TwinCollection collection = twin.Properties.Desired;
            ModuleTwinConfig config = new ModuleTwinConfig()
            {
                FieldGatewayContainerName = collection["fieldGatewayContainerName"],
                FieldGatewayPort = collection["fieldGatewayPort"],
                FieldgatewayPath = collection["fieldgatewayPath"],
                SlaveAddresses = collection["slaveAddresses"],
                SlavePorts = collection["slavePorts"],
                SlaveUnitIds = collection["slaveUnitIds"],
                SlaveUnitIdAlias = collection["slaveUnitIdAlias"]
            };

            return config;
        }

        public string FieldGatewayContainerName { get; set; }

        public int FieldGatewayPort { get; set; }

        public string FieldgatewayPath { get; set; }

        public string SlaveAddresses { get; set; }

        public string SlavePorts { get; set; }

        public string SlaveUnitIds { get; set; }
      
        public string SlaveUnitIdAlias { get; set; }

        public AdapterConfig ConvertToConfig()
        {
            string[] addressArray = SlaveAddresses.Split(";", StringSplitOptions.RemoveEmptyEntries);
            string[] portArray = SlavePorts.Split(";", StringSplitOptions.RemoveEmptyEntries);
            string[] unitIdArray = SlaveUnitIds.Split(";", StringSplitOptions.RemoveEmptyEntries);
            string[] aliasArray = SlaveUnitIdAlias.Split(";", StringSplitOptions.None);

            if(addressArray.Length == portArray.Length && addressArray.Length == unitIdArray.Length && (aliasArray == null || aliasArray.Length == 0 || addressArray.Length == aliasArray.Length))
            {
                List<SlaveConfig> list = new List<SlaveConfig>();
                int index = 0;
                while(index < addressArray.Length)
                {
                    byte? aliasId = !string.IsNullOrEmpty(aliasArray[index]) ? Convert.ToByte(aliasArray[index]) : new byte?();
                    list.Add(new SlaveConfig() { Address = addressArray[index], Port = Convert.ToInt32(portArray[index]), UnitId = Convert.ToByte(unitIdArray[index]), UnitIdAlias = aliasId });                   
                    index++;
                }

                return new AdapterConfig()
                {
                    FieldGatewayContainerName = this.FieldGatewayContainerName,
                    FieldgatewayPath = this.FieldgatewayPath,
                    FieldGatewayPort = this.FieldGatewayPort,
                    Slaves = list.ToArray()
                };
            }
            else
            {
                throw new ConfigurationErrorsException("Invalid configuration.");
            }
        }
    }
}
