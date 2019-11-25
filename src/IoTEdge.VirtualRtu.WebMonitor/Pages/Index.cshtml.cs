using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTEdge.VirtualRtu.WebMonitor.Configuration;
using IoTEdge.VirtualRtu.WebMonitor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace IoTEdge.VirtualRtu.WebMonitor.Pages
{
    public class IndexModel : PageModel
    {
        public IndexModel(MonitorConfig config)
        {
            this.config = config;
        }

        public string Data { get; set; }

        private MonitorConfig config;
        public void OnGet()
        {
            GraphAssets assets = AssetConfiguration.Load(config.TableName, config.StorageConnectionString);
            List<VrtuAsset> list = new List<VrtuAsset>();

            foreach (var item in assets.VirtualRtus)
            {
                list.Add(new VrtuAsset(item));
            }

            

            Data = JsonConvert.SerializeObject(list);
        }
    }
}