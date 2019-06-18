using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.WebApp.Models
{
    public class RtuModel 
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

        public string Hostname { get; set; } = "<dns>.<location>.cloudapp.azure.com";


    }
}
