using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IoTEdge.VirtualRtu.WebMonitor.Pages
{
    public class Index2Model : PageModel
    {
        public Index2Model(MonitorConfig config)
        {
            this.config = config;
        }

        private MonitorConfig config;

        public string VirtualRtu { get; set; }
        public string Device { get; set; }
        public string Gateway { get; set; }
        public string Modbus { get; set; }

        public void OnGet()
        {
            if(!this.Request.Query.ContainsKey("device"))
            {
                throw new Exception("Invalid request.");
            }

            string device = this.Request.Query["device"][0];


            //if (!device.Contains("#"))
            //{
            //    throw new Exception("Invalid virtual rtu and device.");
            //}

            string formatted = device.Replace("#", "");
            string[] parts = formatted.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length != 2)
            {
                throw new Exception("Invalid device specification.");
            }

            GraphAssets assets = AssetConfiguration.Load(config.TableName, config.StorageConnectionString);

            foreach(var vrtu in assets.VirtualRtus)
            {
                if(vrtu.Id.ToLowerInvariant() == parts[0].ToLowerInvariant())
                {
                    foreach(var item in vrtu.Devices)
                    {
                        if(item.Id.ToLowerInvariant() == parts[1].ToLowerInvariant())
                        {
                            Device = item.Id;
                            VirtualRtu = vrtu.Id;
                            return;
                        }
                    }
                }
            }



        }
    }
}