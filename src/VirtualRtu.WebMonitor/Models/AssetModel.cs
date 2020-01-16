using VirtualRtu.WebMonitor.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualRtu.WebMonitor.Models
{
    public class AssetModel : PageModel
    {
        public AssetModel(MonitorConfig config)
        {
            this.config = config;
        }

        private MonitorConfig config;

        public string Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            GraphAssets assets = AssetConfiguration.Load(config.TableName, config.StorageConnectionString);
            List<VrtuAsset> list = new List<VrtuAsset>();

            foreach(var item in assets.VirtualRtus)
            {
                list.Add(new VrtuAsset(item));
            }

            Data = JsonConvert.SerializeObject(list);
            return Page();
        }
    }
}
