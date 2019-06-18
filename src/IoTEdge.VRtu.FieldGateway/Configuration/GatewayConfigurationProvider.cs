using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VRtu.FieldGateway.Communications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace IoTEdge.VRtu.FieldGateway.Configuration
{
    public class GatewayConfigurationProvider : ConfigurationProvider
    {
        public GatewayConfigurationProvider()
        {
            config = new NameValueCollection();
        }

        private NameValueCollection config;

        //public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        //{
        //    return null;
        //}

        //public IChangeToken GetReloadToken()
        //{
        //    return new EdgeChangeToken();
        //}

        public override void Load()
        {            
            EdgeGatewayConfiguration gwConfig = null;
            IotHubTwin twin = new IotHubTwin();
            gwConfig =  twin.GetModuleConfigAsync().GetAwaiter().GetResult();
            if(gwConfig != null)
            {
                string jsonString = JsonConvert.SerializeObject(config);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

                Console.WriteLine("Writing configuration to file.");
                File.WriteAllBytes(Constants.CONFIG_PATH, buffer);
            }
            else
            {
                byte[] byteArray = File.ReadAllBytes(Constants.CONFIG_PATH);
                gwConfig = JsonConvert.DeserializeObject<EdgeGatewayConfiguration>(Encoding.UTF8.GetString(byteArray));
            }

            if (gwConfig == null)
            {
                Console.WriteLine("No configuration found for gateway.");
                throw new Exception("Configuration not found for gateway.");
            }
            else
            {
                Data.Add("Hostname", gwConfig.Hostname);
                Data.Add("ModBusContainer", gwConfig.ModBusContainer);
                Data.Add("ModBusPath", gwConfig.ModBusPath);
                Data.Add("ModBusPort", gwConfig.ModBusPort.ToString());
                Data.Add("RtuInputPiSystem", gwConfig.RtuInputPiSystem);
                Data.Add("RtuOutputPiSsytem", gwConfig.RtuOutputPiSsytem);
                Data.Add("SecurityToken", gwConfig.SecurityToken);
                Data.Add("UnitId", gwConfig.UnitId.ToString());

                //config.Add("Hostname", gwConfig.Hostname);
                //config.Add("ModBusContainer", gwConfig.ModBusContainer);
                //config.Add("ModBusPath", gwConfig.ModBusPath);
                //config.Add("ModBusPort", gwConfig.ModBusPort.ToString());
                //config.Add("RtuInputPiSystem", gwConfig.RtuInputPiSystem);
                //config.Add("RtuOutputPiSsytem", gwConfig.RtuOutputPiSsytem);
                //config.Add("SecurityToken", gwConfig.SecurityToken);
                //config.Add("UnitId", gwConfig.UnitId.ToString());
            }
        }

        //public void Set(string key, string value)
        //{
        //    config.Set(key, value);
        //}

        //public bool TryGet(string key, out string value)
        //{
        //    string[] keys = config.AllKeys;

        //    if(keys.Contains(key))
        //    {
        //        value = config[key];
        //        return true;
        //    }
        //    else
        //    {
        //        value = null;
        //        return false;
        //    }
        //}
    }
}
