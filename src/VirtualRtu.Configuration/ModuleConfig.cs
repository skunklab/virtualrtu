using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class ModuleConfig : VConfig
    {
        public ModuleConfig()
        {
        }

        //entity.Hostname, entity.DeviceId, entity.VirtualRtuId, entity.ModuleId, securityToken, entity.UnitIds,  entity.LoggingLevel, entity.InstrumentationKey);
        public ModuleConfig(string hostname, string virtualRtuId, string deviceId, string moduleId, List<Slave> slaves, string securityToken, LogLevel logLevel, string instrumentationKey)
        {
            this.Hostname = hostname;
            this.VirtualRtuId = virtualRtuId;
            this.DeviceId = deviceId;
            this.ModuleId = moduleId;
            this.SecurityToken = securityToken;
            this.LoggingLevel = logLevel;
            this.InstrumentationKey = instrumentationKey;
            this.Slaves = slaves;
        }

        public override event EventHandler<ConfigUpdateEventArgs> OnChanged;

        private string slaveJson;
        private List<Slave> slaves;
        

       
        /// <summary>
        /// The device identifier
        /// </summary>
        [JsonProperty("deviceId")]
        public virtual string DeviceId { get; set; }

        [JsonProperty("moduleId")]
        public virtual string ModuleId { get; set; } 

        /// <summary>
        /// JWT security token as string
        /// </summary>
        [JsonProperty("securityToken")]
        public virtual string SecurityToken { get; set; }

        [JsonProperty("slaves")]
        public List<Slave> Slaves
        {
            get
            {
                if (slaves != null)
                {
                    return slaves;
                }
                else if (!string.IsNullOrEmpty(slaveJson))
                {
                    return JsonConvert.DeserializeObject<List<Slave>>(slaveJson);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value != null && value.Count > 0)
                {
                    slaveJson = JsonConvert.SerializeObject(value);
                }
                else
                {
                    slaveJson = null;
                }
            }
        }
        
        [JsonProperty("slavesJsonString")]
        public string SlavesJsonString
        {
            get { return slaveJson; }
            set
            {
                slaveJson = value;
                if (!string.IsNullOrEmpty(slaveJson))
                {
                    slaves = JsonConvert.DeserializeObject<List<Slave>>(slaveJson);
                }
            }
        }

        public string GetSlavesString()
        {
            if (Slaves != null && Slaves.Count > 0)
            {
                return JsonConvert.SerializeObject(Slaves);

            }
            else
            {
                return null;
            }
        }

        public void UpdateConfig(string jsonString)
        {
            ModuleConfig config = JsonConvert.DeserializeObject<ModuleConfig>(jsonString);
            this.DeviceId = config.DeviceId;
            this.Hostname = config.Hostname;
            this.InstrumentationKey = config.InstrumentationKey;
            this.LoggingLevel = config.LoggingLevel;
            this.ModuleId = config.ModuleId;
            this.SecurityToken = config.SecurityToken;
            this.Slaves = config.Slaves;
            this.VirtualRtuId = config.VirtualRtuId;

            OnChanged?.Invoke(this, new ConfigUpdateEventArgs(true));
        }



    }
}
