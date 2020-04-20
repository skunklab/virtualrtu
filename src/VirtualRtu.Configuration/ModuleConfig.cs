using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class ModuleConfig : VConfig
    {
        private string slaveJson;
        private List<Slave> slaves;

        public ModuleConfig()
        {
        }

        //entity.Hostname, entity.DeviceId, entity.VirtualRtuId, entity.ModuleId, securityToken, entity.UnitIds,  entity.LoggingLevel, entity.InstrumentationKey);
        public ModuleConfig(string hostname, string virtualRtuId, string deviceId, string moduleId, List<Slave> slaves,
            string securityToken, LogLevel logLevel, string instrumentationKey)
        {
            Hostname = hostname;
            VirtualRtuId = virtualRtuId;
            DeviceId = deviceId;
            ModuleId = moduleId;
            SecurityToken = securityToken;
            LoggingLevel = logLevel;
            InstrumentationKey = instrumentationKey;
            Slaves = slaves;
        }


        /// <summary>
        ///     The device identifier
        /// </summary>
        [JsonProperty("deviceId")]
        public virtual string DeviceId { get; set; }

        [JsonProperty("moduleId")] public virtual string ModuleId { get; set; }

        /// <summary>
        ///     JWT security token as string
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

                if (!string.IsNullOrEmpty(slaveJson))
                {
                    return JsonConvert.DeserializeObject<List<Slave>>(slaveJson);
                }

                return null;
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
            get => slaveJson;
            set
            {
                slaveJson = value;
                if (!string.IsNullOrEmpty(slaveJson))
                {
                    slaves = JsonConvert.DeserializeObject<List<Slave>>(slaveJson);
                }
            }
        }

        public override event EventHandler<ConfigUpdateEventArgs> OnChanged;

        public string GetSlavesString()
        {
            if (Slaves != null && Slaves.Count > 0)
            {
                return JsonConvert.SerializeObject(Slaves);
            }

            return null;
        }

        public void UpdateConfig(string jsonString)
        {
            ModuleConfig config = JsonConvert.DeserializeObject<ModuleConfig>(jsonString);
            DeviceId = config.DeviceId;
            Hostname = config.Hostname;
            InstrumentationKey = config.InstrumentationKey;
            LoggingLevel = config.LoggingLevel;
            ModuleId = config.ModuleId;
            SecurityToken = config.SecurityToken;
            Slaves = config.Slaves;
            VirtualRtuId = config.VirtualRtuId;

            OnChanged?.Invoke(this, new ConfigUpdateEventArgs(true));
        }
    }
}