using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using VirtualRtu.WebMonitor.Configuration;
using VirtualRtu.WebMonitor.Models;

namespace VirtualRtu.WebMonitor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MonitorConfig config;

        //private readonly UserManager<ApplicationUser> userManager;
        //private readonly IHttpContextAccessor context;
        public IndexModel(MonitorConfig config)
        {
            this.config = config;
            //this.userManager = userManager;
            //this.context = context;
        }

        public string Data { get; set; }
        public string Name { get; set; }

        public void OnGet()
        {
            Console.WriteLine("GET called in index page.");
            try
            {
                GraphAssets assets = AssetConfiguration.Load(config.TableName, config.StorageConnectionString);
                List<VrtuAsset> list = new List<VrtuAsset>();

                foreach (var item in assets.VirtualRtus) list.Add(new VrtuAsset(item));

                list.Sort();


                Data = JsonConvert.SerializeObject(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting assets - {ex.Message}");
            }

            ClaimsPrincipal user = HttpContext.User;
            Name = user.Identity.Name;
            Console.WriteLine($"User identity name = {Name}");
        }
    }
}