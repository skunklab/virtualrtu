using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using VirtualRtu.WebMonitor.Configuration;

namespace VirtualRtu.WebMonitor.Models
{
    public class AssetModel : PageModel
    {
        private readonly MonitorConfig config;

        public AssetModel(MonitorConfig config)
        {
            this.config = config;
        }

        public string Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            GraphAssets assets = AssetConfiguration.Load(config.TableName, config.StorageConnectionString);
            List<VrtuAsset> list = new List<VrtuAsset>();

            foreach (var item in assets.VirtualRtus) list.Add(new VrtuAsset(item));

            Data = JsonConvert.SerializeObject(list);
            return Page();
        }
    }
}