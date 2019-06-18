using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.Web.Models
{
    public class RtuModel : PageModel
    {
        public RtuModel()
        {

        }

        public void OnGet()
        {

        }

        public string VirtualRtuId { get; set; }

        public ushort UnitId { get; set; }

        public string DeviceId { get; set; }

        public string ModBusContainerName { get; set; } = "mbpa";

        public int ModBusPort { get; set; } = 8889;

        public string ModBusPath { get; set; } = "api/rtuInput";

        public int ExpirationMinutes { get; set; } = 60;


    }
}
