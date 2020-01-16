using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VirtualRtu.WebMonitor.Configuration
{
    [Serializable]
    [JsonObject]
    public class GraphAssets
    {
        public GraphAssets()
        {
            VirtualRtus = new List<VirtualRtuAsset>();
        }


        [JsonProperty("virtualRtus")]
        public List<VirtualRtuAsset> VirtualRtus { get; set; }

        /// <summary>
        /// returns index of virtual rtu
        /// </summary>
        /// <param name="virtualRtuId"></param>
        /// <returns></returns>
        public int this[string virtualRtuId]
        {
            get
            {
                return VirtualRtus.FindIndex((item) => item.Id == virtualRtuId.ToLowerInvariant());
            }
        }

        public void Add(string virtualRtuId, string deviceId)
        {
            if (this[virtualRtuId] == -1)
            {
                VirtualRtuAsset vasset = new VirtualRtuAsset() { Id = virtualRtuId.ToLowerInvariant() };
                vasset.Devices.Add(new DeviceAsset() { Id = deviceId.ToLowerInvariant() });
                VirtualRtus.Add(vasset);
            }
            else
            {
                var vasset = VirtualRtus[this[virtualRtuId]];
                if (vasset[deviceId] == -1)
                {
                    vasset.Devices.Add(new DeviceAsset() { Id = deviceId.ToLowerInvariant() });
                }
                else
                {
                    var dasset = vasset.Devices[vasset[deviceId]];                    
                }

            }
        }
    }
}
